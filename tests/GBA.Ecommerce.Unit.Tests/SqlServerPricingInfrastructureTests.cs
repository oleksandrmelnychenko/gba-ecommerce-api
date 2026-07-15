using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using GBA.Domain.Repositories.Products;
using GBA.Services.Services.Products;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Unit.Tests;

[Collection("EcommerceSqlIntegration")]
public sealed class SqlServerPricingInfrastructureTests {
    private const string ConnectionStringEnvironmentVariable =
        SqlIntegrationTestEnvironment.ConnectionStringEnvironmentVariable;
    private const string DisposableDatabaseNamePrefix =
        SqlIntegrationTestEnvironment.DisposableDatabaseNamePrefix;

    [Fact]
    public void ChangeTrackingManifest_MatchesCurrentEcommercePriceInputs() {
        string[] expected = [
            "Agreement",
            "ClientAgreement",
            "Currency",
            "OrderItem",
            "Organization",
            "Pricing",
            "PricingProductGroupDiscount",
            "PricingSourceCutoverState",
            "PricingSourceDefinition",
            "PricingSourceSyncState",
            "Product",
            "ProductGroupDiscount",
            "ProductPricing",
            "ProductPricingSourceSnapshot",
            "ProductProductGroup"
        ];

        Assert.Equal(expected, RequiredDependencyTables());
        Assert.Equal(SqlPricingDependencyRevisionProvider.ExpectedTrackedTableCount, expected.Length);
        Assert.Contains("OrderItem", expected);
        Assert.DoesNotContain("Sale", expected);
        Assert.DoesNotContain("ExchangeRate", expected);
        Assert.DoesNotContain("GovExchangeRate", expected);
    }

    [Fact]
    public void OperationalChangeTrackingScript_MatchesRuntimeManifestAndRejectsExtras() {
        string script = ReadOperationalScript();

        Assert.Equal(RequiredDependencyTables(), ScriptRequiredDependencyTables(script));
        Assert.Equal(
            RequiredDependencyTables(),
            RuntimeProcedureRequiredDependencyTables(script));
        Assert.Equal(RequiredDependencyTables(), RepairFenceDependencyTables(script));
        Assert.Equal(
            "v4",
            GetPrivateConstant(
                typeof(SqlPricingDependencyRevisionProvider),
                "ChangeTokenVersion"));
        Assert.Contains("PriceDependency", script, StringComparison.Ordinal);
        Assert.Contains("GetCalculatedProductPriceWithSharesAndVat", script, StringComparison.Ordinal);
        Assert.Contains("GetCalculatedProductPriceForPricingSource", script, StringComparison.Ordinal);
        Assert.Contains("ExtraTrackedTable", script, StringComparison.Ordinal);
        Assert.Contains("ActualTrackedTableCount", script, StringComparison.Ordinal);
        Assert.Contains("THROW 54744", script, StringComparison.Ordinal);
        Assert.Contains("TRACK_COLUMNS_UPDATED = OFF", script, StringComparison.Ordinal);
        Assert.Contains("PricingChangeTrackingIncarnation", script, StringComparison.Ordinal);
        Assert.Contains("recovery_fork_guid", script, StringComparison.Ordinal);
        Assert.Contains("sys.sp_getapplock", script, StringComparison.Ordinal);
        Assert.Contains("@LockOwner = N'Session'", script, StringComparison.Ordinal);
        Assert.Contains("@DbPrincipal = @MaintenanceRoleName", script, StringComparison.Ordinal);
        Assert.DoesNotContain("@DbPrincipal = N'public'", script, StringComparison.Ordinal);
        Assert.Contains("GbaPricingChangeTrackingMaintenance", script, StringComparison.Ordinal);
        Assert.Contains("GbaPricingChangeTrackingRuntime", script, StringComparison.Ordinal);
        Assert.Contains("GetEcommercePricingChangeTrackingState", script, StringComparison.Ordinal);
        Assert.Contains("WITH EXECUTE AS OWNER", script, StringComparison.Ordinal);
        Assert.Contains("WITH EXECUTE AS ''dbo''", script, StringComparison.Ordinal);
        Assert.Contains(
            "repairFenceDefinition.[execute_as_principal_id]",
            script,
            StringComparison.Ordinal);
        Assert.Contains("GRANT EXECUTE ON OBJECT", script, StringComparison.Ordinal);
        Assert.Contains(
            "TO [GbaPricingChangeTrackingMaintenance]",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "RotateEcommercePricingChangeTrackingIncarnation",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "REVOKE SELECT, INSERT, UPDATE, DELETE",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "TO [public]",
            script,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "TO [db_datawriter]",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "DENY SELECT, INSERT, UPDATE, DELETE, ALTER, CONTROL",
            script,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "GRANT SELECT, UPDATE ON OBJECT::dbo.PricingChangeTrackingIncarnation",
            script,
            StringComparison.Ordinal);
        Assert.Contains("BEGIN TRANSACTION", script, StringComparison.Ordinal);
        Assert.Contains(
            "pricing-cache-change-tracking-rotate-incarnation.sql",
            script,
            StringComparison.Ordinal);
        Assert.DoesNotContain("DISABLE CHANGE_TRACKING", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sys.sql_expression_dependencies", script, StringComparison.Ordinal);
        Assert.Contains("sys.synonyms", script, StringComparison.Ordinal);
        Assert.Contains("referenced_database_name", script, StringComparison.Ordinal);
        Assert.Contains("PARSENAME", script, StringComparison.Ordinal);
        Assert.Contains("THROW 54758", script, StringComparison.Ordinal);
        Assert.Contains("GBA_CT_REPAIR_FENCE_V1", script, StringComparison.Ordinal);
        Assert.Contains("AFTER CREATE_TABLE, ALTER_TABLE, DROP_TABLE", script, StringComparison.Ordinal);
        Assert.Contains("[RepairGeneration]", script, StringComparison.Ordinal);
        Assert.Contains("[begin_version]", script, StringComparison.Ordinal);

        string runtimeProcedure = RuntimeStateProcedureSql(script);
        Assert.Contains("HASHBYTES", runtimeProcedure, StringComparison.Ordinal);
        Assert.Contains("[DefinitionHash] AS [definitionHash]", runtimeProcedure, StringComparison.Ordinal);
        Assert.Contains("[PricingModuleHashManifest]", runtimeProcedure, StringComparison.Ordinal);
        Assert.Contains("[PricingTrackedTableManifest]", runtimeProcedure, StringComparison.Ordinal);
        Assert.Contains("[objectId]", runtimeProcedure, StringComparison.Ordinal);
        Assert.Contains("[beginVersion]", runtimeProcedure, StringComparison.Ordinal);
        Assert.Contains("[UnresolvedPriceDependencyCount]", runtimeProcedure, StringComparison.Ordinal);
        Assert.Contains("[CrossDatabasePriceDependencyCount]", runtimeProcedure, StringComparison.Ordinal);
        Assert.Contains("[SynonymBackedPriceDependencyCount]", runtimeProcedure, StringComparison.Ordinal);
        Assert.DoesNotMatch(
            new Regex(@"\[Definition\]\s+AS\s+\[definition\]", RegexOptions.IgnoreCase),
            runtimeProcedure);
        Assert.DoesNotContain("[PricingModuleManifest]", runtimeProcedure, StringComparison.Ordinal);

        string rotationScript = ReadRotationScript();
        Assert.Contains(
            "EXEC dbo.RotateEcommercePricingChangeTrackingIncarnation",
            rotationScript,
            StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE dbo.PricingChangeTrackingIncarnation", rotationScript, StringComparison.Ordinal);
        Assert.DoesNotContain("NEWID()", rotationScript, StringComparison.Ordinal);
        Assert.Contains("NEWID()", script, StringComparison.Ordinal);
        Assert.Contains("sys.sp_getapplock", script, StringComparison.Ordinal);
    }

    [Fact]
    public void RecoveryRunbook_HasExecutableSetupRotationRebuildAndReadinessGates() {
        string runbook = ReadRunbook();

        Assert.Contains("-i db/pricing-cache-change-tracking.sql", runbook, StringComparison.Ordinal);
        Assert.Contains(
            "-i db/pricing-cache-change-tracking-rotate-incarnation.sql",
            runbook,
            StringComparison.Ordinal);
        Assert.Contains("Authorization: Bearer $GBA_ADMIN_BEARER_TOKEN", runbook, StringComparison.Ordinal);
        Assert.Contains("/api/v1/uk/elasticsearch/sync/full", runbook, StringComparison.Ordinal);
        Assert.Contains(".Body.Success == true", runbook, StringComparison.Ordinal);
        Assert.Contains("set -euo pipefail", runbook, StringComparison.Ordinal);
        Assert.Contains("if ! test \"$FULL_SYNC_CODE\" = '200'", runbook, StringComparison.Ordinal);
        Assert.Contains("Full Elasticsearch rebuild returned Success=false", runbook, StringComparison.Ordinal);
        Assert.Contains("/api/v1/uk/elasticsearch/health", runbook, StringComparison.Ordinal);
        Assert.Contains(".Body.PricingRevisionsCurrent == true", runbook, StringComparison.Ordinal);
        Assert.Contains(".Body.ConfigurationConsistent == true", runbook, StringComparison.Ordinal);
        Assert.Contains(".Body.syncStateReadable == true", runbook, StringComparison.Ordinal);
        Assert.Contains(".Body.stale == false", runbook, StringComparison.Ordinal);
        Assert.Contains(".Body.incrementalCatchUpRequired == false", runbook, StringComparison.Ordinal);
        Assert.Contains(
            "/api/v1/uk/products/search?value=filter&limit=1&offset=0",
            runbook,
            StringComparison.Ordinal);
        Assert.Contains(
            "/api/v1/uk/elasticsearch/search?query=filter&limit=1&offset=0",
            runbook,
            StringComparison.Ordinal);
        Assert.Contains("test \"$SEARCH_CODE\" = '503'", runbook, StringComparison.Ordinal);
        Assert.Contains("test \"$SEARCH_CODE\" = '200'", runbook, StringComparison.Ordinal);
        Assert.Contains("SearchSync__Enabled=false", runbook, StringComparison.Ordinal);
        Assert.Contains("/api/v1/uk/elasticsearch/sync/incremental", runbook, StringComparison.Ordinal);
        Assert.Contains("all write-capable", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GBA_SQL_MAINTENANCE_USER", runbook, StringComparison.Ordinal);
        Assert.Contains(
            "SQLCMDPASSWORD=\"$GBA_SQL_MAINTENANCE_PASSWORD\" sqlcmd",
            runbook,
            StringComparison.Ordinal);
        Assert.Contains("db_datawriter", runbook, StringComparison.Ordinal);
        Assert.Contains("@RequiredDeny", runbook, StringComparison.Ordinal);
        Assert.Contains("RepairGeneration", runbook, StringComparison.Ordinal);
        Assert.Contains("PricingTrackedTableManifest", runbook, StringComparison.Ordinal);
        Assert.Contains("UnresolvedPriceDependencyCount", runbook, StringComparison.Ordinal);
        Assert.Contains("CrossDatabasePriceDependencyCount", runbook, StringComparison.Ordinal);
        Assert.Contains("SynonymBackedPriceDependencyCount", runbook, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("FILTER' OR 1=1;--")]
    [InlineData("FILTER'); DROP TABLE dbo.Product;--")]
    public void VendorCodePredicate_TreatsQuoteAndSqlPayloadAsParameterData(string payload) {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        connection.Execute(
            "INSERT INTO dbo.Product ([ID], [NetUID], [VendorCode], [Deleted], [Updated], [Payload]) "
            + "VALUES (1, NEWID(), N'SAFE-CODE', 0, SYSUTCDATETIME(), 0)");
        string predicate = GetPrivateConstant(
            typeof(GetMultipleProductsRepository),
            "VendorCodePredicateSql");

        int matches = connection.QuerySingle<int>(
            "SELECT COUNT(*) FROM dbo.Product AS [Product] WHERE " + predicate,
            new { VendorCodes = new[] { payload } });
        int productTableCount = connection.QuerySingle<int>(
            "SELECT COUNT(*) FROM sys.tables WHERE [object_id] = OBJECT_ID(N'dbo.Product')");

        Assert.Equal(0, matches);
        Assert.Equal(1, productTableCount);
        Assert.Equal("[Product].VendorCode IN @VendorCodes ", predicate);
    }

    [Fact]
    public void ChangeTrackingToken_SeesSameTimestampUpdateHardDeleteAndOtherReplica() {
        using SqlConnection? firstConnection = OpenDisposableConnectionOrNull();
        if (firstConnection == null) return;
        using SqlConnection secondConnection = new(
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)!);
        secondConnection.Open();
        ResetPricingTestSchema(firstConnection);

        SqlPricingDependencyRevisionProvider firstProvider = new();
        SqlPricingDependencyRevisionProvider secondProvider = new();
        DateTime unchangedTimestamp = new(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc);

        firstConnection.Execute(
            "INSERT INTO dbo.Product ([ID], [NetUID], [SourceFenixID], [SourceFenixCode], "
            + "[Deleted], [Updated], [Payload]) "
            + "VALUES (1, NEWID(), 0x01, 11, 0, @Updated, 1)",
            new { Updated = unchangedTimestamp });
        PricingDependencyRevisions afterInsert = firstProvider.Get(firstConnection);

        firstConnection.Execute(
            "UPDATE dbo.Product SET [Payload] = 2, [Updated] = @Updated WHERE [ID] = 1",
            new { Updated = unchangedTimestamp });
        PricingDependencyRevisions afterSameTimestampUpdate = firstProvider.Get(firstConnection);

        firstConnection.Execute("DELETE FROM dbo.Product WHERE [ID] = 1");
        PricingDependencyRevisions afterHardDelete = firstProvider.Get(firstConnection);
        PricingDependencyRevisions observedByOtherReplica = secondProvider.Get(secondConnection);

        Assert.True(afterInsert.IsValid);
        Assert.True(
            ReadVersion(afterSameTimestampUpdate) > ReadVersion(afterInsert),
            "A payload update with an unchanged Updated timestamp must advance the token.");
        Assert.True(
            ReadVersion(afterHardDelete) > ReadVersion(afterSameTimestampUpdate),
            "A hard delete must advance the token.");
        Assert.Equal(afterHardDelete, observedByOtherReplica);
    }

    [Fact]
    public void ChangeTrackingToken_DoesNotAdvanceForUntrackedSalesWrite() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        SqlPricingDependencyRevisionProvider provider = new();
        PricingDependencyRevisions before = provider.Get(connection);

        connection.Execute(@"
CREATE TABLE dbo.PriceCacheUnrelatedSale (
    [ID] bigint NOT NULL PRIMARY KEY,
    [Payload] int NULL
);
INSERT INTO dbo.PriceCacheUnrelatedSale ([ID], [Payload]) VALUES (1, 1);");

        PricingDependencyRevisions after = provider.Get(connection);

        Assert.True(before.IsValid);
        Assert.Equal(before, after);
    }

    [Fact]
    public void PricingModuleBodyChange_WithUnchangedDependencies_ChangesRevision() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        SqlPricingDependencyRevisionProvider provider = new();
        PricingDependencyRevisions before = provider.Get(connection);

        connection.Execute(@"
ALTER FUNCTION dbo.GetCalculatedProductPriceForPricingSource (
    @ProductNetUid uniqueidentifier,
    @PricingNetUid uniqueidentifier,
    @AgreementNetUid uniqueidentifier)
RETURNS decimal(18, 4)
AS
BEGIN
    DECLARE @DependencyProbe int = dbo.PriceCacheDependencyProbe();
    RETURN CONVERT(decimal(18, 4), 56.78 + (0 * @DependencyProbe));
END");

        PricingDependencyRevisions after = provider.Get(connection);

        Assert.True(before.IsValid);
        Assert.True(after.IsValid);
        Assert.Equal(ReadVersion(before), ReadVersion(after));
        Assert.False(before.MatchesExactly(after));
    }

    [Fact]
    public void DisableAndReenableTrackedTable_ChangesIdentityFenceAndRevision() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        SqlPricingDependencyRevisionProvider provider = new();
        PricingDependencyRevisions before = provider.Get(connection);
        long repairGenerationBefore = ReadRepairGeneration(connection);

        connection.Execute("ALTER TABLE dbo.Product DISABLE CHANGE_TRACKING");
        PricingChangeTrackingStatus disabled = provider.GetStatus(connection);
        connection.Execute(@"
ALTER TABLE dbo.Product
    ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);");

        PricingChangeTrackingStatus after = provider.GetStatus(connection);

        Assert.True(before.IsValid);
        Assert.False(disabled.IsAvailable);
        Assert.Equal(1, disabled.MissingTrackedTableCount);
        Assert.True(after.IsAvailable);
        Assert.True(ReadRepairGeneration(connection) > repairGenerationBefore);
        Assert.False(before.MatchesExactly(after.Revisions));
    }

    [Fact]
    public void TruncateTrackedTable_ChangesBeginVersionFenceAndRevision() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        connection.Execute(@"
INSERT INTO dbo.Product
    ([ID], [NetUID], [Deleted], [Updated])
VALUES
    (9000, NEWID(), 0, SYSUTCDATETIME());");
        SqlPricingDependencyRevisionProvider provider = new();
        PricingDependencyRevisions before = provider.Get(connection);
        long beginVersionBefore = ReadBeginVersion(connection, "Product");

        connection.Execute("TRUNCATE TABLE dbo.Product");

        PricingDependencyRevisions after = provider.Get(connection);

        Assert.True(before.IsValid);
        Assert.True(after.IsValid);
        Assert.NotEqual(beginVersionBefore, ReadBeginVersion(connection, "Product"));
        Assert.False(before.MatchesExactly(after));
    }

    [Fact]
    public void MissingRepairFence_FailsClosedAndSetupAdvancesRepairGeneration() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        SqlPricingDependencyRevisionProvider provider = new();
        PricingDependencyRevisions before = provider.Get(connection);
        long generationBefore = ReadRepairGeneration(connection);

        connection.Execute(
            "DISABLE TRIGGER GbaPricingChangeTrackingRepairFence ON DATABASE");
        PricingChangeTrackingStatus disabled = provider.GetStatus(connection);
        connection.Execute(ReadOperationalScript(), commandTimeout: 120);
        PricingChangeTrackingStatus repaired = provider.GetStatus(connection);

        Assert.False(disabled.IsAvailable);
        Assert.False(disabled.RepairFenceValid);
        Assert.True(repaired.IsAvailable);
        Assert.True(repaired.RepairFenceValid);
        Assert.True(ReadRepairGeneration(connection) > generationBefore);
        Assert.False(before.MatchesExactly(repaired.Revisions));
    }

    [Fact]
    public void RecoveryIncarnationRotation_InvalidatesOldRevisionWithoutCtAdvance() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        SqlPricingDependencyRevisionProvider provider = new();
        PricingDependencyRevisions before = provider.Get(connection);
        Guid beforeIncarnation = ReadRecoveryIncarnation(connection);
        long repairGenerationBefore = ReadRepairGeneration(connection);

        connection.Execute(ReadRotationScript());

        PricingDependencyRevisions after = provider.Get(connection);
        Guid afterIncarnation = ReadRecoveryIncarnation(connection);

        Assert.True(before.IsValid);
        Assert.True(after.IsValid);
        Assert.Equal(ReadVersion(before), ReadVersion(after));
        Assert.NotEqual(beforeIncarnation, afterIncarnation);
        Assert.True(ReadRepairGeneration(connection) > repairGenerationBefore);
        Assert.False(before.MatchesExactly(after));
    }

    [Fact]
    public async Task RecoveryIncarnationRotation_ConcurrentCallsSerializeAndKeepSingletonValid() {
        using SqlConnection? firstConnection = OpenDisposableConnectionOrNull();
        if (firstConnection == null) return;
        ResetPricingTestSchema(firstConnection);
        using SqlConnection secondConnection = new(
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)!);
        await secondConnection.OpenAsync();
        SqlPricingDependencyRevisionProvider provider = new();
        PricingDependencyRevisions before = provider.Get(firstConnection);
        string rotationScript = ReadRotationScript();

        await Task.WhenAll(
            firstConnection.ExecuteAsync(rotationScript, commandTimeout: 120),
            secondConnection.ExecuteAsync(rotationScript, commandTimeout: 120));

        PricingChangeTrackingStatus after = provider.GetStatus(firstConnection);
        int singletonRows = firstConnection.QuerySingle<int>(
            "SELECT COUNT(*) FROM dbo.PricingChangeTrackingIncarnation WHERE [Id] = 1");
        Assert.True(after.IsAvailable);
        Assert.True(after.RecoveryLineageMatches);
        Assert.Equal(1, singletonRows);
        Assert.False(before.MatchesExactly(after.Revisions));
    }

    [Fact]
    public void BackupMutateRestore_ChangesRecoveryForkAndRequiresExplicitRotation() {
        string? connectionString = SqlIntegrationTestEnvironment.GetConnectionString();
        if (connectionString == null) return;
        using (SqlConnection connection = new(connectionString)) {
            connection.Open();
            ResetPricingTestSchema(connection);
        }

        SqlServerRecoveryForkProbeResult result = SqlServerRecoveryForkProbe.Run(
            connectionString,
            ReadRotationScript());

        Assert.NotEqual(result.BackupRecoveryForkId, result.RestoredRecoveryForkId);
        Assert.NotEqual(result.BackupIncarnationId, result.MutatedIncarnationId);
        Assert.NotEqual(result.BackupIncarnationId, result.RotatedRestoredIncarnationId);
        Assert.True(result.MutatedChangeTrackingVersion > result.BackupChangeTrackingVersion);
        Assert.Equal(result.BackupChangeTrackingVersion, result.RestoredChangeTrackingVersion);
        Assert.True(result.MutationWasRolledBack);
        Assert.False(result.BeforeRotation.IsAvailable);
        Assert.True(result.BeforeRotation.RecoveryIncarnationPresent);
        Assert.False(result.BeforeRotation.RecoveryLineageMatches);
        Assert.True(result.AfterRotation.IsAvailable);
        Assert.True(result.AfterRotation.RecoveryLineageMatches);
    }

    [Fact]
    public void ProviderAndSetup_RejectExtraTrackedTableWithoutAutoDisablingIt() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        connection.Execute(@"
CREATE TABLE dbo.PriceCacheUnexpectedTracked (
    [ID] bigint NOT NULL PRIMARY KEY,
    [Payload] int NULL
);
ALTER TABLE dbo.PriceCacheUnexpectedTracked
    ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);");

        try {
            SqlPricingDependencyRevisionProvider provider = new();
            PricingChangeTrackingStatus status = provider.GetStatus(connection);

            Assert.False(status.IsAvailable);
            Assert.Equal(15, status.ExpectedTrackedTableCount);
            Assert.Equal(16, status.ActualTrackedTableCount);
            Assert.Equal(0, status.MissingTrackedTableCount);
            Assert.Equal(1, status.ExtraTrackedTableCount);

            SqlException exception = Assert.Throws<SqlException>(() =>
                connection.Execute(ReadOperationalScript(), commandTimeout: 120));
            Assert.Equal(54744, exception.Number);
            Assert.Equal(
                1,
                connection.QuerySingle<int>(@"
SELECT COUNT(*)
FROM sys.change_tracking_tables
WHERE [object_id] = OBJECT_ID(N'dbo.PriceCacheUnexpectedTracked')"));
        } finally {
            connection.Execute("DROP TABLE IF EXISTS dbo.PriceCacheUnexpectedTracked");
        }
    }

    [Fact]
    public void Provider_RejectsMissingTrackedTableAndReportsCounts() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        connection.Execute("ALTER TABLE dbo.Product DISABLE CHANGE_TRACKING");

        PricingChangeTrackingStatus status =
            new SqlPricingDependencyRevisionProvider().GetStatus(connection);

        Assert.False(status.IsAvailable);
        Assert.Equal(15, status.ExpectedTrackedTableCount);
        Assert.Equal(14, status.ActualTrackedTableCount);
        Assert.Equal(1, status.MissingTrackedTableCount);
        Assert.Equal(0, status.ExtraTrackedTableCount);
    }

    [Fact]
    public void Provider_FailsClosedWhenPriceFunctionInputsDriftBeyondManifest() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        connection.Execute(@"
CREATE TABLE dbo.ExchangeRate (
    [ID] bigint NOT NULL PRIMARY KEY,
    [Payload] int NULL
);
CREATE TABLE dbo.GovExchangeRate (
    [ID] bigint NOT NULL PRIMARY KEY,
    [Payload] int NULL
);");
        connection.Execute(CreateDependencyProbeSql(alter: true, includeExchangeRates: true));

        PricingChangeTrackingStatus status =
            new SqlPricingDependencyRevisionProvider().GetStatus(connection);

        Assert.False(status.IsAvailable);
        Assert.Equal(15, status.ActualTrackedTableCount);
        Assert.Equal(2, status.UnlistedPriceInputCount);
        Assert.Equal(0, status.NonInputManifestEntryCount);
    }

    [Fact]
    public void ProviderAndSetup_RejectSynonymBackedPriceDependency() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        connection.Execute("CREATE SYNONYM dbo.PriceCacheProductSynonym FOR dbo.Product;");
        connection.Execute(
            CreateDependencyProbeSql(alter: true, includeExchangeRates: false)
                .Replace(
                    "FROM dbo.[Product]",
                    "FROM dbo.PriceCacheProductSynonym",
                    StringComparison.Ordinal));

        PricingChangeTrackingStatus status =
            new SqlPricingDependencyRevisionProvider().GetStatus(connection);
        SqlException exception = Assert.Throws<SqlException>(() =>
            connection.Execute(ReadOperationalScript(), commandTimeout: 120));

        Assert.False(status.IsAvailable);
        Assert.True(status.SynonymBackedPriceDependencyCount > 0);
        Assert.Equal(54758, exception.Number);
    }

    [Fact]
    public void ProviderAndSetup_RejectCrossDatabasePriceDependency() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        connection.Execute(@"
DROP TABLE IF EXISTS tempdb.dbo.PriceCacheCrossDatabaseInput;
CREATE TABLE tempdb.dbo.PriceCacheCrossDatabaseInput (
    [ID] bigint NOT NULL PRIMARY KEY,
    [Payload] int NULL
);");
        connection.Execute(@"
CREATE VIEW dbo.PriceCacheCrossDatabaseView
AS
SELECT [Payload]
FROM tempdb.dbo.PriceCacheCrossDatabaseInput;");
        connection.Execute(
            CreateDependencyProbeSql(alter: true, includeExchangeRates: false)
                .Replace(
                    "FROM dbo.[Product]",
                    "FROM dbo.PriceCacheCrossDatabaseView",
                    StringComparison.Ordinal));

        PricingChangeTrackingStatus status =
            new SqlPricingDependencyRevisionProvider().GetStatus(connection);
        SqlException exception = Assert.Throws<SqlException>(() =>
            connection.Execute(ReadOperationalScript(), commandTimeout: 120));

        Assert.False(status.IsAvailable);
        Assert.True(status.CrossDatabasePriceDependencyCount > 0);
        Assert.Equal(54758, exception.Number);
    }

    [Fact]
    public void ProviderAndSetup_RejectUnresolvedPriceDependency() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        connection.Execute(@"
CREATE TABLE dbo.PriceCacheUnresolvedInput (
    [ID] bigint NOT NULL PRIMARY KEY,
    [Payload] int NULL
);");
        connection.Execute(@"
CREATE VIEW dbo.PriceCacheUnresolvedView
AS
SELECT [Payload]
FROM dbo.PriceCacheUnresolvedInput;");
        connection.Execute(
            CreateDependencyProbeSql(alter: true, includeExchangeRates: false)
                .Replace(
                    "FROM dbo.[Product]",
                    "FROM dbo.PriceCacheUnresolvedView",
                    StringComparison.Ordinal));
        connection.Execute("DROP TABLE dbo.PriceCacheUnresolvedInput;");

        PricingChangeTrackingStatus status =
            new SqlPricingDependencyRevisionProvider().GetStatus(connection);
        SqlException exception = Assert.Throws<SqlException>(() =>
            connection.Execute(ReadOperationalScript(), commandTimeout: 120));

        Assert.False(status.IsAvailable);
        Assert.True(status.UnresolvedPriceDependencyCount > 0);
        Assert.Equal(54758, exception.Number);
    }

    [Fact]
    public void OperationalChangeTrackingScript_IsIdempotentOnDisposableDatabase() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection, enableRequiredTableTracking: false);
        string script = ReadOperationalScript();
        Guid initialIncarnation = ReadRecoveryIncarnation(connection);

        connection.Execute(script, commandTimeout: 120);
        PricingDependencyRevisions afterFirstSetup =
            new SqlPricingDependencyRevisionProvider().Get(connection);
        long repairGenerationAfterFirstSetup = ReadRepairGeneration(connection);
        connection.Execute(script, commandTimeout: 120);
        PricingChangeTrackingStatus status =
            new SqlPricingDependencyRevisionProvider().GetStatus(connection);

        Assert.True(status.IsAvailable);
        Assert.Equal(15, status.ExpectedTrackedTableCount);
        Assert.Equal(15, status.ActualTrackedTableCount);
        Assert.Equal(0, status.MissingTrackedTableCount);
        Assert.Equal(0, status.ExtraTrackedTableCount);
        Assert.Equal(2, status.ActualPriceFunctionCount);
        Assert.True(status.ActualPricingModuleCount >= 3);
        Assert.Equal(0, status.UnreadablePricingModuleCount);
        Assert.True(status.RecoveryIncarnationPresent);
        Assert.True(status.RecoveryLineageMatches);
        Assert.True(status.RepairFenceValid);
        Assert.Equal(
            connection.QuerySingle<int>(
                "SELECT DATABASE_PRINCIPAL_ID(N'dbo');"),
            connection.QuerySingle<int>(@"
SELECT moduleDefinition.[execute_as_principal_id]
FROM sys.triggers repairFence
INNER JOIN sys.sql_modules moduleDefinition
    ON moduleDefinition.[object_id] = repairFence.[object_id]
WHERE repairFence.[parent_class_desc] = N'DATABASE'
  AND repairFence.[name] = N'GbaPricingChangeTrackingRepairFence';"));
        Assert.Equal(0, status.UnreadableTrackedTableIdentityCount);
        Assert.Equal(0, status.UnresolvedPriceDependencyCount);
        Assert.Equal(0, status.CrossDatabasePriceDependencyCount);
        Assert.Equal(0, status.SynonymBackedPriceDependencyCount);
        Assert.Equal(initialIncarnation, ReadRecoveryIncarnation(connection));
        Assert.Equal(afterFirstSetup, status.Revisions);
        Assert.Equal(repairGenerationAfterFirstSetup, ReadRepairGeneration(connection));

        string[] maintenancePermissions = connection.Query<string>(@"
SELECT CONCAT(
    OBJECT_SCHEMA_NAME(permission.[major_id]), N'.',
    OBJECT_NAME(permission.[major_id]), N':',
    permission.[permission_name])
FROM sys.database_permissions permission
WHERE permission.[grantee_principal_id] =
      DATABASE_PRINCIPAL_ID(N'GbaPricingChangeTrackingMaintenance')
  AND permission.[class] = 1
  AND permission.[major_id] IN (
      OBJECT_ID(N'dbo.GetEcommercePricingChangeTrackingState'),
      OBJECT_ID(N'dbo.RotateEcommercePricingChangeTrackingIncarnation'),
      OBJECT_ID(N'dbo.PricingChangeTrackingIncarnation'))
  AND permission.[state] IN (N'G', N'W')
ORDER BY 1;").ToArray();
        Assert.Equal(new[] {
            "dbo.GetEcommercePricingChangeTrackingState:EXECUTE",
            "dbo.RotateEcommercePricingChangeTrackingIncarnation:EXECUTE"
        }, maintenancePermissions);
    }

    [Fact]
    public async Task OperationalChangeTrackingScript_ConcurrentSetupSerializesAndRemainsExact() {
        using SqlConnection? firstConnection = OpenDisposableConnectionOrNull();
        if (firstConnection == null) return;
        ResetPricingTestSchema(firstConnection, enableRequiredTableTracking: false);
        using SqlConnection secondConnection = new(
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)!);
        await secondConnection.OpenAsync();
        string script = ReadOperationalScript();

        await Task.WhenAll(
            firstConnection.ExecuteAsync(script, commandTimeout: 120),
            secondConnection.ExecuteAsync(script, commandTimeout: 120));

        PricingChangeTrackingStatus status =
            new SqlPricingDependencyRevisionProvider().GetStatus(firstConnection);
        Assert.True(status.IsAvailable);
        Assert.Equal(15, status.ExpectedTrackedTableCount);
        Assert.Equal(15, status.ActualTrackedTableCount);
        Assert.Equal(0, status.MissingTrackedTableCount);
        Assert.Equal(0, status.ExtraTrackedTableCount);
    }

    [Fact]
    public void RuntimeRole_RealLoginReadsOwnerExecutedStateButCannotMaintainOrBlockSetup() {
        string? connectionString = SqlIntegrationTestEnvironment.GetConnectionString();
        if (connectionString == null) return;
        using SqlConnection admin = new(connectionString);
        admin.Open();
        ResetPricingTestSchema(admin);
        Guid incarnationBefore = ReadRecoveryIncarnation(admin);
        using SqlServerRuntimeLoginProbe login = new(connectionString);
        using SqlConnection runtime = login.OpenRuntimeConnection();

        Assert.Equal(login.LoginName, runtime.QuerySingle<string>("SELECT ORIGINAL_LOGIN();"));
        Assert.Equal(
            1,
            runtime.QuerySingle<int>(
                "SELECT IS_ROLEMEMBER(N'GbaPricingChangeTrackingRuntime');"));
        IDictionary<string, object> runtimeState =
            (IDictionary<string, object>)runtime.QuerySingle(
                "EXEC dbo.GetEcommercePricingChangeTrackingState;");
        Assert.DoesNotContain("PricingModuleManifest", runtimeState.Keys);
        string hashManifest = Assert.IsType<string>(
            runtimeState["PricingModuleHashManifest"]);
        using (JsonDocument manifest = JsonDocument.Parse(hashManifest)) {
            JsonElement[] modules = manifest.RootElement.EnumerateArray().ToArray();
            Assert.NotEmpty(modules);
            Assert.All(modules, module => {
                Assert.False(module.TryGetProperty("definition", out _));
                string hash = module.GetProperty("definitionHash").GetString()!;
                Assert.Matches("^[0-9A-F]{64}$", hash);
            });
        }
        string trackedTableManifest = Assert.IsType<string>(
            runtimeState["PricingTrackedTableManifest"]);
        using (JsonDocument manifest = JsonDocument.Parse(trackedTableManifest)) {
            JsonElement[] tables = manifest.RootElement.EnumerateArray().ToArray();
            Assert.Equal(15, tables.Length);
            Assert.All(tables, table => {
                Assert.True(table.GetProperty("objectId").GetInt32() > 0);
                Assert.True(table.GetProperty("beginVersion").GetInt64() >= 0);
            });
        }
        PricingChangeTrackingStatus status =
            new SqlPricingDependencyRevisionProvider().GetStatus(runtime);
        Assert.True(status.IsAvailable);
        Assert.True(status.RecoveryLineageMatches);
        Assert.True(status.RepairFenceValid);
        Assert.Equal(0, status.UnreadableTrackedTableIdentityCount);
        Assert.Equal(0, status.UnresolvedPriceDependencyCount);
        Assert.StartsWith(
            "product-pricing:v4:",
            status.Revisions.ProductPricing,
            StringComparison.Ordinal);

        Assert.Throws<SqlException>(() => runtime.QuerySingle<Guid>(@"
SELECT [IncarnationId]
FROM dbo.PricingChangeTrackingIncarnation
WHERE [Id] = 1;"));
        Assert.Throws<SqlException>(() => runtime.Execute(@"
UPDATE dbo.PricingChangeTrackingIncarnation
SET [IncarnationId] = NEWID()
WHERE [Id] = 1;"));
        Assert.Throws<SqlException>(() => runtime.Execute(
            "ALTER TABLE dbo.Product DISABLE CHANGE_TRACKING;"));
        Assert.Throws<SqlException>(() => runtime.Execute(
            ReadRotationScript(),
            commandTimeout: 120));
        Assert.Throws<SqlException>(() => runtime.Execute(
            ReadOperationalScript(),
            commandTimeout: 120));
        Assert.Equal(incarnationBefore, ReadRecoveryIncarnation(admin));

        string lockResource =
            "gba:ecommerce:pricing-change-tracking:maintenance:" + admin.Database;
        Assert.Throws<SqlException>(() => runtime.QuerySingle<int>(@"
DECLARE @Result int;
EXEC @Result = sys.sp_getapplock
    @Resource = @Resource,
    @LockMode = N'Exclusive',
    @LockOwner = N'Session',
    @LockTimeout = 0,
    @DbPrincipal = N'GbaPricingChangeTrackingMaintenance';
SELECT @Result;", new { Resource = lockResource }));

        int publicLockResult = runtime.QuerySingle<int>(@"
DECLARE @Result int;
EXEC @Result = sys.sp_getapplock
    @Resource = @Resource,
    @LockMode = N'Exclusive',
    @LockOwner = N'Session',
    @LockTimeout = 0,
    @DbPrincipal = N'public';
SELECT @Result;", new { Resource = lockResource });
        Assert.True(publicLockResult >= 0);
        try {
            admin.Execute(ReadOperationalScript(), commandTimeout: 120);
        } finally {
            runtime.Execute(@"
EXEC sys.sp_releaseapplock
    @Resource = @Resource,
    @LockOwner = N'Session',
    @DbPrincipal = N'public';", new { Resource = lockResource });
        }
    }

    [Fact]
    public void BroadWriterLogins_CannotMutateSingletonButMaintenanceCanRotate() {
        string? connectionString = SqlIntegrationTestEnvironment.GetConnectionString();
        if (connectionString == null) return;
        using SqlConnection admin = new(connectionString);
        admin.Open();
        ResetPricingTestSchema(admin);
        Guid incarnationBefore = ReadRecoveryIncarnation(admin);
        long generationBefore = ReadRepairGeneration(admin);

        using (SqlServerRoleLoginProbe writerLogin = new(
                   connectionString,
                   "db_datareader",
                   "db_datawriter"))
        using (SqlConnection writer = writerLogin.OpenConnection()) {
            writer.Execute(@"
INSERT INTO dbo.Product
    ([ID], [NetUID], [Deleted], [Updated])
VALUES
    (9001, NEWID(), 0, SYSUTCDATETIME());");
            AssertSingletonMutationDenied(writer);
            AssertPermissionDenied(() => writer.Execute(
                "EXEC dbo.RotateEcommercePricingChangeTrackingIncarnation;"));
        }

        Assert.Equal(incarnationBefore, ReadRecoveryIncarnation(admin));
        Assert.Equal(generationBefore, ReadRepairGeneration(admin));

        using (SqlServerRoleLoginProbe maintenanceLogin = new(
                   connectionString,
                   "db_datareader",
                   "db_datawriter",
                   "GbaPricingChangeTrackingMaintenance"))
        using (SqlConnection maintenance = maintenanceLogin.OpenConnection()) {
            maintenance.Execute(@"
INSERT INTO dbo.Product
    ([ID], [NetUID], [Deleted], [Updated])
VALUES
    (9002, NEWID(), 0, SYSUTCDATETIME());");
            AssertSingletonMutationDenied(maintenance);
            maintenance.Execute(ReadRotationScript(), commandTimeout: 120);

            PricingChangeTrackingStatus status =
                new SqlPricingDependencyRevisionProvider().GetStatus(maintenance);
            Assert.True(status.IsAvailable);
        }

        Assert.NotEqual(incarnationBefore, ReadRecoveryIncarnation(admin));
        Assert.True(ReadRepairGeneration(admin) > generationBefore);
    }

    [Fact]
    public void RuntimePriceSql_UsesSourceWorldWithoutComparingAgreementAndProductIdentity() {
        using SqlConnection? connection = OpenDisposableConnectionOrNull();
        if (connection == null) return;
        ResetPricingTestSchema(connection);
        Guid agreementNetUid = Guid.NewGuid();
        Guid matchingProductNetUid = Guid.NewGuid();
        Guid otherProductNetUid = Guid.NewGuid();
        DateTime updated = new(2026, 7, 15, 8, 30, 0, DateTimeKind.Utc);

        connection.Execute(
            "INSERT INTO dbo.Currency ([ID], [Code], [Deleted], [Updated], [Payload]) "
            + "VALUES (1, 'UAH', 0, @Updated, 0); "
            + "INSERT INTO dbo.Agreement ([ID], [CurrencyID], [Updated], [Payload]) "
            + "VALUES (1, 1, @Updated, 0); "
            + "INSERT INTO dbo.ClientAgreement ([ID], [NetUID], [AgreementID], [Deleted], [Updated], [Payload]) "
            + "VALUES (1, @AgreementNetUid, 1, 0, @Updated, 0); "
            + "INSERT INTO dbo.Product ([ID], [NetUID], [SourceFenixID], [SourceFenixCode], "
            + "[Deleted], [Updated], [Payload]) VALUES "
            + "(1, @MatchingProductNetUid, 0x01, 11, 0, @Updated, 0), "
            + "(2, @OtherProductNetUid, 0x02, 22, 0, @Updated, 0), "
            + "(3, NEWID(), 0x01, 11, 0, @Updated, 0);",
            new {
                AgreementNetUid = agreementNetUid,
                MatchingProductNetUid = matchingProductNetUid,
                OtherProductNetUid = otherProductNetUid,
                Updated = updated
            });

        OptimizedProductRepository repository = new(connection);
        Dictionary<long, ProductPriceInfo> prices = repository.GetPricesOnly(
            [1, 2, 3],
            agreementNetUid,
            organizationId: null,
            withVat: false,
            catalogSource: "fenix");

        Assert.Equal(new long[] { 1, 2 }, prices.Keys.OrderBy(id => id));
        Assert.All(prices.Values, result => {
            Assert.Equal(12.34m, result.Price);
            Assert.Equal("UAH", result.CurrencyCode);
        });
    }

    private static SqlConnection? OpenDisposableConnectionOrNull() {
        string? connectionString = SqlIntegrationTestEnvironment.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return null;

        SqlConnection connection = new(connectionString);
        connection.Open();
        SqlIntegrationTestEnvironment.EnsureDisposableDatabase(connection);
        return connection;
    }

    private static long ReadVersion(PricingDependencyRevisions revisions) {
        int separator = revisions.ProductPricing.LastIndexOf(':');
        Assert.True(separator > 0);
        return long.Parse(revisions.ProductPricing[(separator + 1)..]);
    }

    private static void ResetPricingTestSchema(
        SqlConnection connection,
        bool enableRequiredTableTracking = true) {
        if (!connection.Database.StartsWith(
                DisposableDatabaseNamePrefix,
                StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} must target a disposable database "
                + $"whose name starts with '{DisposableDatabaseNamePrefix}'.");
        }

        string databaseName = connection.Database.Replace("]", "]]", StringComparison.Ordinal);
        long? currentVersion = connection.QuerySingle<long?>(
            "SELECT CHANGE_TRACKING_CURRENT_VERSION()");
        if (!currentVersion.HasValue) {
            connection.Execute(
                $"ALTER DATABASE [{databaseName}] SET CHANGE_TRACKING = ON "
                + "(CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON)");
        }

        connection.Execute(@"
DROP TRIGGER IF EXISTS GbaPricingChangeTrackingRepairFence ON DATABASE;
DROP FUNCTION IF EXISTS dbo.GetCalculatedProductPriceWithSharesAndVat;
DROP FUNCTION IF EXISTS dbo.GetCalculatedProductPriceForPricingSource;
DROP FUNCTION IF EXISTS dbo.PriceCacheDependencyProbe;
DROP PROCEDURE IF EXISTS dbo.GetEcommercePricingChangeTrackingState;
DROP PROCEDURE IF EXISTS dbo.RotateEcommercePricingChangeTrackingIncarnation;
DROP VIEW IF EXISTS dbo.PriceCacheCrossDatabaseView;
DROP VIEW IF EXISTS dbo.PriceCacheUnresolvedView;
IF OBJECT_ID(N'dbo.PriceCacheProductSynonym', N'SN') IS NOT NULL
    DROP SYNONYM dbo.PriceCacheProductSynonym;
DROP TABLE IF EXISTS dbo.PriceCacheUnexpectedTracked;
DROP TABLE IF EXISTS dbo.PriceCacheUnrelatedSale;
DROP TABLE IF EXISTS dbo.PriceCacheUnresolvedInput;
DROP TABLE IF EXISTS dbo.ExchangeRate;
DROP TABLE IF EXISTS dbo.GovExchangeRate;
DROP TABLE IF EXISTS dbo.PricingChangeTrackingIncarnation;");
        connection.Execute("DROP TABLE IF EXISTS tempdb.dbo.PriceCacheCrossDatabaseInput;");
        foreach (string table in RequiredDependencyTables().Reverse()) {
            connection.Execute($"DROP TABLE IF EXISTS dbo.[{table}]");
        }

        foreach (string table in RequiredDependencyTables()) {
            string createSql = table switch {
                "Product" => @"
CREATE TABLE dbo.Product (
    [ID] bigint NOT NULL PRIMARY KEY,
    [NetUID] uniqueidentifier NOT NULL,
    [VendorCode] nvarchar(256) NULL,
    [SourceFenixID] varbinary(64) NULL,
    [SourceFenixCode] bigint NULL,
    [SourceAmgID] varbinary(64) NULL,
    [SourceAmgCode] bigint NULL,
    [Deleted] bit NOT NULL,
    [Updated] datetime2 NOT NULL,
    [Payload] int NULL
)",
                "Currency" => @"
CREATE TABLE dbo.Currency (
    [ID] bigint NOT NULL PRIMARY KEY,
    [Code] nvarchar(16) NULL,
    [Deleted] bit NOT NULL,
    [Updated] datetime2 NOT NULL,
    [Payload] int NULL
)",
                "Agreement" => @"
CREATE TABLE dbo.Agreement (
    [ID] bigint NOT NULL PRIMARY KEY,
    [CurrencyID] bigint NULL,
    [Updated] datetime2 NOT NULL,
    [Payload] int NULL
)",
                "ClientAgreement" => @"
CREATE TABLE dbo.ClientAgreement (
    [ID] bigint NOT NULL PRIMARY KEY,
    [NetUID] uniqueidentifier NOT NULL,
    [AgreementID] bigint NULL,
    [Deleted] bit NOT NULL,
    [Updated] datetime2 NOT NULL,
    [Payload] int NULL
)",
                _ => $@"
CREATE TABLE dbo.[{table}] (
    [ID] bigint NOT NULL PRIMARY KEY,
    [Updated] datetime2 NOT NULL,
    [Payload] int NULL
)"
            };
            connection.Execute(createSql);
            if (enableRequiredTableTracking) {
                connection.Execute(
                    $"ALTER TABLE dbo.[{table}] ENABLE CHANGE_TRACKING "
                    + "WITH (TRACK_COLUMNS_UPDATED = OFF)");
            }
        }

        connection.Execute(CreateDependencyProbeSql(alter: false, includeExchangeRates: false));
        connection.Execute(@"
CREATE FUNCTION dbo.GetCalculatedProductPriceWithSharesAndVat (
    @ProductNetUid uniqueidentifier,
    @ClientAgreementNetUid uniqueidentifier,
    @Culture nvarchar(8),
    @WithVat bit,
    @OrderItemId bigint)
RETURNS decimal(18, 4)
AS
BEGIN
    DECLARE @DependencyProbe int = dbo.PriceCacheDependencyProbe();
    IF @OrderItemId IS NOT NULL
        SELECT @DependencyProbe = @DependencyProbe + ISNULL(MAX([Payload]), 0)
        FROM dbo.OrderItem;
    RETURN CONVERT(decimal(18, 4), 12.34 + (0 * @DependencyProbe));
END");
        connection.Execute(@"
CREATE FUNCTION dbo.GetCalculatedProductPriceForPricingSource (
    @ProductNetUid uniqueidentifier,
    @PricingNetUid uniqueidentifier,
    @AgreementNetUid uniqueidentifier)
RETURNS decimal(18, 4)
AS
BEGIN
    DECLARE @DependencyProbe int = dbo.PriceCacheDependencyProbe();
    RETURN CONVERT(decimal(18, 4), 12.34 + (0 * @DependencyProbe));
END");
        connection.Execute(@"
CREATE TABLE dbo.PricingChangeTrackingIncarnation (
    [Id] tinyint NOT NULL,
    [IncarnationId] uniqueidentifier NOT NULL,
    [RecoveryForkId] uniqueidentifier NOT NULL,
    [RepairGeneration] bigint NOT NULL,
    [RotatedAtUtc] datetime2(7) NOT NULL,
    [RotatedBy] sysname NOT NULL,
    CONSTRAINT [PK_PricingChangeTrackingIncarnation]
        PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_PricingChangeTrackingIncarnation_Singleton]
        CHECK ([Id] = 1),
    CONSTRAINT [CK_PricingChangeTrackingIncarnation_RepairGeneration]
        CHECK ([RepairGeneration] > 0)
);
INSERT INTO dbo.PricingChangeTrackingIncarnation
    ([Id], [IncarnationId], [RecoveryForkId], [RepairGeneration],
     [RotatedAtUtc], [RotatedBy])
SELECT 1, NEWID(), recovery.[recovery_fork_guid], 1,
       SYSUTCDATETIME(), ORIGINAL_LOGIN()
FROM sys.database_recovery_status recovery
WHERE recovery.[database_id] = DB_ID();");

        if (enableRequiredTableTracking) {
            connection.Execute(ReadOperationalScript(), commandTimeout: 120);
        }
    }

    private static string CreateDependencyProbeSql(bool alter, bool includeExchangeRates) {
        string createOrAlter = alter ? "ALTER" : "CREATE";
        string exchangeRateInputs = includeExchangeRates
            ? @"
    SELECT @DependencyProbe = @DependencyProbe + ISNULL(MAX([Payload]), 0) FROM dbo.ExchangeRate;
    SELECT @DependencyProbe = @DependencyProbe + ISNULL(MAX([Payload]), 0) FROM dbo.GovExchangeRate;"
            : string.Empty;
        string requiredInputs = string.Join(
            Environment.NewLine,
            RequiredDependencyTables().Select(table =>
                $"    SELECT @DependencyProbe = @DependencyProbe + ISNULL(MAX([Payload]), 0) FROM dbo.[{table}];"));

        return $@"{createOrAlter} FUNCTION dbo.PriceCacheDependencyProbe()
RETURNS int
AS
BEGIN
    DECLARE @DependencyProbe int = 0;
{requiredInputs}{exchangeRateInputs}
    RETURN @DependencyProbe;
END";
    }

    private static string ReadOperationalScript() {
        string scriptPath = Path.Combine(
            AppContext.BaseDirectory,
            "pricing-cache-change-tracking.sql");
        return File.ReadAllText(scriptPath);
    }

    private static string ReadRotationScript() {
        string scriptPath = Path.Combine(
            AppContext.BaseDirectory,
            "pricing-cache-change-tracking-rotate-incarnation.sql");
        return File.ReadAllText(scriptPath);
    }

    private static string ReadRunbook() {
        string runbookPath = Path.Combine(
            AppContext.BaseDirectory,
            "pricing-cache-change-tracking-runbook.md");
        return File.ReadAllText(runbookPath);
    }

    private static Guid ReadRecoveryIncarnation(IDbConnection connection) {
        return connection.QuerySingle<Guid>(@"
SELECT [IncarnationId]
FROM dbo.PricingChangeTrackingIncarnation
WHERE [Id] = 1");
    }

    private static long ReadRepairGeneration(IDbConnection connection) {
        return connection.QuerySingle<long>(@"
SELECT [RepairGeneration]
FROM dbo.PricingChangeTrackingIncarnation
WHERE [Id] = 1");
    }

    private static long ReadBeginVersion(IDbConnection connection, string tableName) {
        return connection.QuerySingle<long>(@"
SELECT tracked.[begin_version]
FROM sys.change_tracking_tables tracked
WHERE tracked.[object_id] = OBJECT_ID(N'dbo.' + @TableName)",
            new { TableName = tableName });
    }

    private static void AssertSingletonMutationDenied(IDbConnection connection) {
        AssertPermissionDenied(() => connection.Execute(@"
INSERT INTO dbo.PricingChangeTrackingIncarnation
    ([Id], [IncarnationId], [RecoveryForkId], [RepairGeneration],
     [RotatedAtUtc], [RotatedBy])
VALUES
    (2, NEWID(), NEWID(), 1, SYSUTCDATETIME(), ORIGINAL_LOGIN());"));
        AssertPermissionDenied(() => connection.Execute(@"
UPDATE dbo.PricingChangeTrackingIncarnation
SET [IncarnationId] = NEWID()
WHERE [Id] = 1;"));
        AssertPermissionDenied(() => connection.Execute(@"
DELETE FROM dbo.PricingChangeTrackingIncarnation
WHERE [Id] = 1;"));
    }

    private static void AssertPermissionDenied(Action action) {
        SqlException exception = Assert.Throws<SqlException>(action);
        Assert.Equal(229, exception.Number);
    }

    private static IReadOnlyList<string> ScriptRequiredDependencyTables(string script) {
        Match requiredBlock = Regex.Match(
            script,
            @"INSERT INTO @Required \(\[SchemaName\], \[TableName\]\)\s*VALUES(?<values>.*?);",
            RegexOptions.Singleline);
        Assert.True(requiredBlock.Success, "The setup script pricing manifest was not found.");
        return ParseDependencyValues(requiredBlock.Groups["values"].Value);
    }

    private static IReadOnlyList<string> RuntimeProcedureRequiredDependencyTables(
        string script) {
        Match requiredBlock = Regex.Match(
            script,
            @"CREATE OR ALTER PROCEDURE dbo\.GetEcommercePricingChangeTrackingState.*?FROM \(VALUES(?<values>.*?)\) dependency",
            RegexOptions.Singleline);
        Assert.True(requiredBlock.Success, "The runtime state procedure pricing manifest was not found.");
        return Regex.Matches(
                requiredBlock.Groups["values"].Value,
                @"\(N''dbo'', N''(?<table>[^']+)''\)")
            .Select(match => match.Groups["table"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<string> RepairFenceDependencyTables(string script) {
        Match requiredBlock = Regex.Match(
            script,
            @"GBA_CT_REPAIR_FENCE_V1.*?AND @TableName IN \((?<values>.*?)\)",
            RegexOptions.Singleline);
        Assert.True(requiredBlock.Success, "The repair-fence pricing manifest was not found.");
        return Regex.Matches(
                requiredBlock.Groups["values"].Value,
                @"N''(?<table>[^']+)''")
            .Select(match => match.Groups["table"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string RuntimeStateProcedureSql(string script) {
        Match procedure = Regex.Match(
            script,
            @"CREATE OR ALTER PROCEDURE dbo\.GetEcommercePricingChangeTrackingState.*?END;'",
            RegexOptions.Singleline);
        Assert.True(procedure.Success, "The runtime state procedure was not found.");
        return procedure.Value;
    }

    private static IReadOnlyList<string> RequiredDependencyTables() {
        string valuesSql = GetPrivateConstant(
            typeof(SqlPricingDependencyRevisionProvider),
            "RequiredDependencyValuesSql");
        return ParseDependencyValues(valuesSql);
    }

    private static IReadOnlyList<string> ParseDependencyValues(string valuesSql) {
        return Regex.Matches(valuesSql, @"\(N'dbo', N'(?<table>[^']+)'\)")
            .Select(match => match.Groups["table"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string GetPrivateConstant(Type type, string fieldName) {
        FieldInfo field = type.GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{type.Name}.{fieldName} was not found.");
        return (string)(field.GetRawConstantValue()
            ?? throw new InvalidOperationException($"{type.Name}.{fieldName} was empty."));
    }
}
