using System.Data;
using Dapper;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.EntityHelpers.ProductAvailabilityModels;
using GBA.Domain.Repositories.Products;
using GBA.Services.Infrastructure.Checkout;
using Microsoft.Data.SqlClient;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class CheckoutStockTempFactAttribute : FactAttribute {
    public const string ConnectionVariable = "GBA_DASHBOARD_SQL_FIXTURE_CONNECTION";
    public CheckoutStockTempFactAttribute() {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
            Skip = "Requires a SELECT-only SQL fixture connection; writes are confined to owned global temporary tables.";
    }
}

/// <summary>Exercises the real checkout executor and production lock/decrement/ledger SQL
/// against isolated tempdb rows. No application table, customer or sale is changed.</summary>
public sealed class CheckoutStockTempSqlTests {
    [CheckoutStockTempFact]
    public async Task Last_unit_race_commits_one_receipt_and_order_and_rejects_the_loser_without_partial_writes() {
        var settings = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(CheckoutStockTempFactAttribute.ConnectionVariable)!) {
            Enlist = true, Pooling = false
        };
        await using SqlConnection owner = new(settings.ConnectionString);
        await owner.OpenAsync();
        Assert.Equal(0, await owner.ExecuteScalarAsync<int>("SELECT ISNULL(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','INSERT'),0);"));
        string suffix = Guid.NewGuid().ToString("N");
        string stock = "##CheckoutStock_" + suffix, ledger = "##CheckoutLedger_" + suffix, orders = "##CheckoutOrders_" + suffix;
        await owner.ExecuteAsync($"""
            CREATE TABLE {stock}(ID bigint NOT NULL PRIMARY KEY,ProductID bigint NOT NULL,StorageID bigint NOT NULL,Amount float NOT NULL,Deleted bit NOT NULL,Updated datetime2 NULL);
            CREATE INDEX IX_Product ON {stock}(ProductID);
            INSERT {stock}(ID,ProductID,StorageID,Amount,Deleted) VALUES(1,501,7,1,0);
            CREATE TABLE {ledger}(OperationNetUid uniqueidentifier NOT NULL PRIMARY KEY,OperationName nvarchar(64) NOT NULL,PrincipalNetUid uniqueidentifier NOT NULL,
                ClientNetUid uniqueidentifier NOT NULL,ModeFlag bit NOT NULL,RequestFingerprint binary(32) NOT NULL,ResponsePayload nvarchar(max) NULL,CreatedUtc datetime2 NOT NULL,CompletedUtc datetime2 NULL);
            CREATE TABLE {orders}(ID bigint IDENTITY NOT NULL PRIMARY KEY,NetUID uniqueidentifier NOT NULL,OperationNetUid uniqueidentifier NOT NULL,Qty float NOT NULL);
            """);
        try {
            Mock<IDbConnectionFactory> factory = new();
            factory.Setup(f => f.NewSqlConnection()).Returns(() => new SqlConnection(settings.ConnectionString));
            TempLedger receipts = new(ledger);
            CheckoutExecutor executor = new(factory.Object, receipts);
            CheckoutRequest winnerRequest = CheckoutExecutorTests.NewRequest(), loserRequest = CheckoutExecutorTests.NewRequest();
            var winnerLocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using ManualResetEventSlim releaseWinner = new();
            var loserEnteringLock = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            bool loserEffect = false;
            GBA.Domain.Entities.Sales.Sale Create(IDbConnection connection, CheckoutRequest request, bool first) {
                if (!first) loserEnteringLock.TrySetResult();
                connection.Query(CheckoutLocks.LockProductStockSql.Replace("[dbo].[ProductAvailability]", stock), new { ProductId = 501L }).AsList();
                if (first) {
                    winnerLocked.TrySetResult();
                    if (!releaseWinner.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException("Test winner was not released.");
                }
                long id = connection.ExecuteScalar<long>($"INSERT {orders}(NetUID,OperationNetUid,Qty) OUTPUT inserted.ID VALUES(@NetUid,@OperationNetUid,1);",
                    new { NetUid = Guid.NewGuid(), request.OperationNetUid });
                int changed = connection.Execute(ProductAvailabilityRepository.DecreaseAmountSql.Replace("[dbo].[ProductAvailability]", stock),
                    new { Id = 1L, Qty = 1d, Tolerance = 0.0000001d });
                if (changed != 1) {
                    GBA.Common.Helpers.TransactionalEffects.AfterCommit(() => loserEffect = true);
                    throw new InsufficientProductAvailabilityException(1,501,7,1,0);
                }
                return new() { Id = id, NetUid = connection.ExecuteScalar<Guid>($"SELECT NetUID FROM {orders} WHERE ID=@Id;",new { Id=id }) };
            }
            Task<CheckoutResult> winner = Task.Run(() => executor.Execute(winnerRequest, c => Create(c,winnerRequest,true)));
            await winnerLocked.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Task<CheckoutResult> loser = Task.Run(() => executor.Execute(loserRequest, c => Create(c,loserRequest,false)));
            try {
                await loserEnteringLock.Task.WaitAsync(TimeSpan.FromSeconds(15));
                await Task.Delay(100);
                Assert.False(loser.IsCompleted);
            } finally { releaseWinner.Set(); }
            CheckoutResult accepted = await winner.WaitAsync(TimeSpan.FromSeconds(15));
            CheckoutConflictException rejected = await Assert.ThrowsAsync<CheckoutConflictException>(async () => await loser.WaitAsync(TimeSpan.FromSeconds(15)));
            Assert.Equal(CheckoutFailureDetails.StockChanged,rejected.Code);
            Assert.Equal(new CheckoutFailureDetails(CheckoutFailureDetails.StockChanged,false,false),CheckoutFailureDetails.From(rejected));
            Assert.False(loserEffect);
            Assert.Equal(0d,await owner.ExecuteScalarAsync<double>($"SELECT Amount FROM {stock} WHERE ID=1;"));
            Assert.Equal(1,await owner.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {orders};"));
            Assert.Equal(1,await owner.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {ledger};"));
            Assert.Equal(0,await owner.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {ledger} WHERE OperationNetUid=@OperationNetUid;",new { loserRequest.OperationNetUid }));
            CheckoutResult replay = executor.Execute(winnerRequest,_=>throw new InvalidOperationException("A committed winner must not run again."));
            Assert.True(replay.IsReplay);Assert.Equal(accepted.Sale.NetUid,replay.Sale.NetUid);
        } finally {
            await owner.ExecuteAsync($"DROP TABLE IF EXISTS {orders}; DROP TABLE IF EXISTS {ledger}; DROP TABLE IF EXISTS {stock};");
        }
    }

    private sealed class TempLedger(string table) : ICheckoutLedger {
        string Sql(string sql)=>sql.Replace("[dbo].[EcommerceSalesCreationLedger]",table);
        public CheckoutLedgerEntry Find(IDbConnection c,Guid id)=>c.QuerySingleOrDefault<CheckoutLedgerEntry>(Sql(SqlCheckoutLedger.FindSql),new{OperationNetUid=id})!;
        public CheckoutClaim TryClaim(IDbConnection c,CheckoutRequest r) {
            int result=c.ExecuteScalar<int>(Sql(SqlCheckoutLedger.BuildClaimSql(5000)),new{r.OperationNetUid,r.OperationName,r.PrincipalNetUid,r.ClientNetUid,r.ModeFlag,r.RequestFingerprint});
            return result switch {1=>CheckoutClaim.Claimed,0=>CheckoutClaim.AlreadyRecorded,-1=>CheckoutClaim.Busy,_=>throw new InvalidOperationException()};
        }
        public void Complete(IDbConnection c,CheckoutRequest r,string payload)=>Assert.Equal(1,c.Execute(Sql(SqlCheckoutLedger.CompleteSql),new{r.OperationNetUid,r.OperationName,r.PrincipalNetUid,ResponsePayload=payload}));
    }
}
