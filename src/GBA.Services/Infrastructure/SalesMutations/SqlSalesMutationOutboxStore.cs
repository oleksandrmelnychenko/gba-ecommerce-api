using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using GBA.Domain.DbConnectionFactory.Contracts;

namespace GBA.Services.Infrastructure.SalesMutations;

public sealed class SqlSalesMutationOutboxStore : ISalesMutationOutboxStore {
    private const string _ensureSchemaSql = """
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @LockResult int;
EXEC @LockResult = sys.sp_getapplock
    @Resource = N'gba-ecommerce:sales-mutation-outbox-schema',
    @LockMode = N'Exclusive',
    @LockOwner = N'Transaction',
    @LockTimeout = 60000;

IF @LockResult < 0
    THROW 51040, N'Unable to acquire the ecommerce sales mutation outbox schema lock.', 1;

IF OBJECT_ID(N'dbo.EcommerceSalesMutationOutbox', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EcommerceSalesMutationOutbox (
        OperationNetUid uniqueidentifier NOT NULL,
        OperationName nvarchar(128) NOT NULL,
        RequestUrl nvarchar(2048) NOT NULL,
        Payload nvarchar(max) NOT NULL,
        PayloadSha256 binary(32) NOT NULL,
        Status tinyint NOT NULL,
        AttemptCount int NOT NULL,
        AuthenticationFailureCount int NOT NULL
            CONSTRAINT DF_EcommerceSalesMutationOutbox_AuthenticationFailureCount DEFAULT (0),
        NextAttemptUtc datetime2(7) NOT NULL,
        LeaseToken uniqueidentifier NULL,
        LeaseExpiresUtc datetime2(7) NULL,
        LastAttemptUtc datetime2(7) NULL,
        LastError nvarchar(2000) NULL,
        CreatedUtc datetime2(7) NOT NULL,
        UpdatedUtc datetime2(7) NOT NULL,
        CompletedUtc datetime2(7) NULL,
        CONSTRAINT PK_EcommerceSalesMutationOutbox
            PRIMARY KEY CLUSTERED (OperationNetUid),
        CONSTRAINT CK_EcommerceSalesMutationOutbox_Status
            CHECK (Status IN (0, 1, 2, 3)),
        CONSTRAINT CK_EcommerceSalesMutationOutbox_AttemptCount
            CHECK (AttemptCount >= 0),
        CONSTRAINT CK_EcommerceSalesMutationOutbox_AuthenticationFailureCount
            CHECK (AuthenticationFailureCount >= 0),
        CONSTRAINT CK_EcommerceSalesMutationOutbox_PayloadJson
            CHECK (ISJSON(Payload) = 1)
    );
END;

IF COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'AuthenticationFailureCount') IS NULL
BEGIN
    EXEC sys.sp_executesql N'
        ALTER TABLE dbo.EcommerceSalesMutationOutbox
            ADD AuthenticationFailureCount int NOT NULL
                CONSTRAINT DF_EcommerceSalesMutationOutbox_AuthenticationFailureCount
                DEFAULT (0) WITH VALUES;';
END;

IF COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'OperationNetUid') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'OperationName') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'RequestUrl') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'Payload') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'PayloadSha256') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'Status') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'AttemptCount') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'AuthenticationFailureCount') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'NextAttemptUtc') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'LeaseToken') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'LeaseExpiresUtc') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'LastAttemptUtc') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'LastError') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'CreatedUtc') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'UpdatedUtc') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesMutationOutbox', N'CompletedUtc') IS NULL
    THROW 51041, N'EcommerceSalesMutationOutbox exists with an incompatible schema.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.EcommerceSalesMutationOutbox')
      AND name = N'CK_EcommerceSalesMutationOutbox_AuthenticationFailureCount')
BEGIN
    EXEC sys.sp_executesql N'
        ALTER TABLE dbo.EcommerceSalesMutationOutbox WITH CHECK
            ADD CONSTRAINT CK_EcommerceSalesMutationOutbox_AuthenticationFailureCount
            CHECK (AuthenticationFailureCount >= 0);';
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.EcommerceSalesMutationOutbox')
      AND name = N'IX_EcommerceSalesMutationOutbox_Dispatch')
BEGIN
    CREATE NONCLUSTERED INDEX IX_EcommerceSalesMutationOutbox_Dispatch
        ON dbo.EcommerceSalesMutationOutbox
            (Status, NextAttemptUtc, LeaseExpiresUtc, CreatedUtc)
        INCLUDE (AttemptCount, OperationName);
END;

COMMIT TRANSACTION;
""";

    private const string _enqueueSql = """
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;

DECLARE @ExistingOperationName nvarchar(128);
DECLARE @ExistingRequestUrl nvarchar(2048);
DECLARE @ExistingPayloadSha256 binary(32);
DECLARE @ExistingPayload nvarchar(max);

SELECT
    @ExistingOperationName = OperationName,
    @ExistingRequestUrl = RequestUrl,
    @ExistingPayloadSha256 = PayloadSha256,
    @ExistingPayload = Payload
FROM dbo.EcommerceSalesMutationOutbox WITH (UPDLOCK, HOLDLOCK)
WHERE OperationNetUid = @OperationNetUid;

IF @ExistingOperationName IS NULL
BEGIN
    INSERT INTO dbo.EcommerceSalesMutationOutbox (
        OperationNetUid,
        OperationName,
        RequestUrl,
        Payload,
        PayloadSha256,
        Status,
        AttemptCount,
        AuthenticationFailureCount,
        NextAttemptUtc,
        CreatedUtc,
        UpdatedUtc)
    VALUES (
        @OperationNetUid,
        @OperationName,
        @RequestUrl,
        @Payload,
        @PayloadSha256,
        0,
        0,
        0,
        @NextAttemptUtc,
        @CreatedUtc,
        @CreatedUtc);
END
ELSE IF @ExistingOperationName <> @OperationName
     OR @ExistingRequestUrl <> @RequestUrl
     OR @ExistingPayloadSha256 <> @PayloadSha256
     OR @ExistingPayload <> @Payload
BEGIN
    THROW 51042, N'An ecommerce sales mutation operation key cannot be reused for another request.', 1;
END;

COMMIT TRANSACTION;
""";

    private const string _claimNextSql = """
;WITH Candidate AS (
    SELECT TOP (1) *
    FROM dbo.EcommerceSalesMutationOutbox WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE (Status = 0 AND NextAttemptUtc <= @UtcNow)
       OR (Status = 1 AND LeaseExpiresUtc <= @UtcNow)
    ORDER BY NextAttemptUtc, CreatedUtc, OperationNetUid
)
UPDATE Candidate
SET Status = 1,
    AttemptCount = AttemptCount + 1,
    LeaseToken = @LeaseToken,
    LeaseExpiresUtc = @LeaseExpiresUtc,
    LastAttemptUtc = @UtcNow,
    UpdatedUtc = @UtcNow
OUTPUT
    inserted.OperationNetUid,
    inserted.OperationName,
    inserted.RequestUrl,
    inserted.Payload,
    inserted.LeaseToken,
    inserted.AttemptCount,
    inserted.AuthenticationFailureCount;
""";

    private readonly IDbConnectionFactory _connectionFactory;

    public SqlSalesMutationOutboxStore(IDbConnectionFactory connectionFactory) {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            _ensureSchemaSql,
            cancellationToken: cancellationToken));
    }

    public async Task<SalesMutationOutboxMessage> GetAsync(
        Guid operationNetUid,
        CancellationToken cancellationToken) {
        if (operationNetUid == Guid.Empty)
            throw new ArgumentException("Operation key is required.", nameof(operationNetUid));

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return await connection.QuerySingleOrDefaultAsync<SalesMutationOutboxMessage>(
            new CommandDefinition(
                """
SELECT
    OperationNetUid,
    OperationName,
    RequestUrl,
    Payload,
    PayloadSha256,
    Status,
    AttemptCount,
    AuthenticationFailureCount,
    NextAttemptUtc,
    CreatedUtc
FROM dbo.EcommerceSalesMutationOutbox
WHERE OperationNetUid = @OperationNetUid;
""",
                new { OperationNetUid = operationNetUid },
                cancellationToken: cancellationToken));
    }

    public async Task EnqueueAsync(
        SalesMutationOutboxMessage message,
        CancellationToken cancellationToken) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        await EnqueueAsync(connection, message, cancellationToken);
    }

    public async Task EnqueueAsync(
        IDbConnection connection,
        SalesMutationOutboxMessage message,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(connection);
        ValidateForEnqueue(message);
        await connection.ExecuteAsync(new CommandDefinition(
            _enqueueSql,
            new {
                message.OperationNetUid,
                message.OperationName,
                message.RequestUrl,
                message.Payload,
                message.PayloadSha256,
                message.NextAttemptUtc,
                message.CreatedUtc
            },
            cancellationToken: cancellationToken));
    }

    public async Task<SalesMutationOutboxLease> ClaimNextAsync(
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken) {
        if (leaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));

        Guid leaseToken = Guid.NewGuid();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return await connection.QuerySingleOrDefaultAsync<SalesMutationOutboxLease>(
            new CommandDefinition(
                _claimNextSql,
                new {
                    UtcNow = RequireUtc(utcNow),
                    LeaseToken = leaseToken,
                    LeaseExpiresUtc = RequireUtc(utcNow).Add(leaseDuration)
                },
                cancellationToken: cancellationToken));
    }

    public Task<bool> CompleteAsync(
        Guid operationNetUid,
        Guid leaseToken,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        ExecuteFencedUpdateAsync(
            """
UPDATE dbo.EcommerceSalesMutationOutbox
SET Status = 2,
    LeaseToken = NULL,
    LeaseExpiresUtc = NULL,
    LastError = NULL,
    CompletedUtc = @UtcNow,
    UpdatedUtc = @UtcNow
WHERE OperationNetUid = @OperationNetUid
  AND Status = 1
  AND LeaseToken = @LeaseToken;
""",
            operationNetUid,
            leaseToken,
            RequireUtc(utcNow),
            null,
            null,
            SalesMutationDeliveryFailureKind.None,
            cancellationToken);

    public Task<bool> RetryAsync(
        Guid operationNetUid,
        Guid leaseToken,
        DateTime utcNow,
        DateTime nextAttemptUtc,
        string lastError,
        SalesMutationDeliveryFailureKind failureKind,
        CancellationToken cancellationToken) =>
        ExecuteFencedUpdateAsync(
            """
UPDATE dbo.EcommerceSalesMutationOutbox
SET Status = 0,
    LeaseToken = NULL,
    LeaseExpiresUtc = NULL,
    LastError = @LastError,
    AuthenticationFailureCount = AuthenticationFailureCount +
        CASE WHEN @FailureKind = 1 THEN 1 ELSE 0 END,
    NextAttemptUtc = @NextAttemptUtc,
    UpdatedUtc = @UtcNow
WHERE OperationNetUid = @OperationNetUid
  AND Status = 1
  AND LeaseToken = @LeaseToken;
""",
            operationNetUid,
            leaseToken,
            RequireUtc(utcNow),
            RequireUtc(nextAttemptUtc),
            NormalizeError(lastError),
            failureKind,
            cancellationToken);

    public Task<bool> DeadLetterAsync(
        Guid operationNetUid,
        Guid leaseToken,
        DateTime utcNow,
        string lastError,
        SalesMutationDeliveryFailureKind failureKind,
        CancellationToken cancellationToken) =>
        ExecuteFencedUpdateAsync(
            """
UPDATE dbo.EcommerceSalesMutationOutbox
SET Status = 3,
    LeaseToken = NULL,
    LeaseExpiresUtc = NULL,
    LastError = @LastError,
    AuthenticationFailureCount = AuthenticationFailureCount +
        CASE WHEN @FailureKind = 1 THEN 1 ELSE 0 END,
    UpdatedUtc = @UtcNow
WHERE OperationNetUid = @OperationNetUid
  AND Status = 1
  AND LeaseToken = @LeaseToken;
""",
            operationNetUid,
            leaseToken,
            RequireUtc(utcNow),
            null,
            NormalizeError(lastError),
            failureKind,
            cancellationToken);

    public async Task<int> DeleteCompletedBeforeAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            """
DELETE TOP (1000)
FROM dbo.EcommerceSalesMutationOutbox
WHERE Status = 2
  AND CompletedUtc < @CutoffUtc;
""",
            new { CutoffUtc = RequireUtc(cutoffUtc) },
            cancellationToken: cancellationToken));
    }

    public async Task<SalesMutationOutboxStats> GetStatsAsync(CancellationToken cancellationToken) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return await connection.QuerySingleAsync<SalesMutationOutboxStats>(new CommandDefinition(
            """
SELECT
    COALESCE(SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END), 0) AS PendingCount,
    COALESCE(SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END), 0) AS LeasedCount,
    COALESCE(SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END), 0) AS DeadLetterCount,
    COALESCE(SUM(CASE WHEN Status IN (0, 1) AND AuthenticationFailureCount > 0 THEN 1 ELSE 0 END), 0)
        AS AuthenticationFailureCount,
    MIN(CASE WHEN Status IN (0, 1) THEN CreatedUtc END) AS OldestPendingUtc
FROM dbo.EcommerceSalesMutationOutbox;
""",
            cancellationToken: cancellationToken));
    }

    private async Task<bool> ExecuteFencedUpdateAsync(
        string sql,
        Guid operationNetUid,
        Guid leaseToken,
        DateTime utcNow,
        DateTime? nextAttemptUtc,
        string lastError,
        SalesMutationDeliveryFailureKind failureKind,
        CancellationToken cancellationToken) {
        if (operationNetUid == Guid.Empty) throw new ArgumentException("Operation key is required.", nameof(operationNetUid));
        if (leaseToken == Guid.Empty) throw new ArgumentException("Lease token is required.", nameof(leaseToken));
        if (failureKind is not (SalesMutationDeliveryFailureKind.None or
            SalesMutationDeliveryFailureKind.Authentication))
            throw new ArgumentOutOfRangeException(nameof(failureKind));

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new {
                OperationNetUid = operationNetUid,
                LeaseToken = leaseToken,
                UtcNow = utcNow,
                NextAttemptUtc = nextAttemptUtc,
                LastError = lastError,
                FailureKind = (byte) failureKind
            },
            cancellationToken: cancellationToken));
        return affected == 1;
    }

    private static void ValidateForEnqueue(SalesMutationOutboxMessage message) {
        ArgumentNullException.ThrowIfNull(message);
        if (message.OperationNetUid == Guid.Empty) throw new ArgumentException("Operation key is required.", nameof(message));
        ArgumentException.ThrowIfNullOrWhiteSpace(message.OperationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.RequestUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Payload);
        if (message.AuthenticationFailureCount != 0)
            throw new ArgumentException("A new outbox message cannot contain authentication failures.", nameof(message));
        if (message.PayloadSha256 is not { Length: 32 })
            throw new ArgumentException("A SHA-256 payload fingerprint is required.", nameof(message));
        RequireUtc(message.NextAttemptUtc);
        RequireUtc(message.CreatedUtc);
    }

    private static DateTime RequireUtc(DateTime value) {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("An explicit UTC timestamp is required.", nameof(value));
        return value;
    }

    private static string NormalizeError(string error) {
        if (string.IsNullOrWhiteSpace(error)) return "Unknown delivery failure.";
        string normalized = error.Trim();
        return normalized.Length <= 2000 ? normalized : normalized[..2000];
    }
}
