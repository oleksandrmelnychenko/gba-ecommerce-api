#nullable enable

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using GBA.Domain.DbConnectionFactory.Contracts;

namespace GBA.Services.Infrastructure.SalesMutations;

/// <summary>SQL Server implementation of the permanent sales-creation ledger.</summary>
public sealed class SqlSalesCreationLedgerStore : ISalesCreationLedgerStore {
    private const string _ensureSchemaSql = """
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @LockResult int;
EXEC @LockResult = sys.sp_getapplock
    @Resource = N'gba-ecommerce:sales-creation-ledger-schema',
    @LockMode = N'Exclusive',
    @LockOwner = N'Transaction',
    @LockTimeout = 60000;

IF @LockResult < 0
    THROW 51045, N'Unable to acquire the ecommerce sales creation ledger schema lock.', 1;

IF OBJECT_ID(N'dbo.EcommerceSalesCreationLedger', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EcommerceSalesCreationLedger (
        OperationNetUid uniqueidentifier NOT NULL,
        OperationName nvarchar(128) NOT NULL,
        PrincipalNetUid uniqueidentifier NOT NULL,
        ClientNetUid uniqueidentifier NOT NULL,
        ModeFlag bit NOT NULL,
        RequestFingerprint binary(32) NOT NULL,
        ResponsePayload nvarchar(max) NULL,
        CreatedUtc datetime2(7) NOT NULL,
        CompletedUtc datetime2(7) NULL,
        CONSTRAINT PK_EcommerceSalesCreationLedger
            PRIMARY KEY CLUSTERED (OperationNetUid),
        CONSTRAINT CK_EcommerceSalesCreationLedger_ResponseJson
            CHECK (ResponsePayload IS NULL OR ISJSON(ResponsePayload) = 1),
        CONSTRAINT CK_EcommerceSalesCreationLedger_Completion
            CHECK ((ResponsePayload IS NULL AND CompletedUtc IS NULL)
                OR (ResponsePayload IS NOT NULL AND CompletedUtc IS NOT NULL))
    );
END;

IF COL_LENGTH(N'dbo.EcommerceSalesCreationLedger', N'OperationNetUid') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesCreationLedger', N'OperationName') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesCreationLedger', N'PrincipalNetUid') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesCreationLedger', N'ClientNetUid') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesCreationLedger', N'ModeFlag') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesCreationLedger', N'RequestFingerprint') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesCreationLedger', N'ResponsePayload') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesCreationLedger', N'CreatedUtc') IS NULL
   OR COL_LENGTH(N'dbo.EcommerceSalesCreationLedger', N'CompletedUtc') IS NULL
    THROW 51046, N'EcommerceSalesCreationLedger exists with an incompatible schema.', 1;

DECLARE @ExpectedColumns TABLE (
    ColumnName sysname NOT NULL PRIMARY KEY,
    TypeName sysname NOT NULL,
    MaxLength smallint NOT NULL,
    [Precision] tinyint NOT NULL,
    Scale tinyint NOT NULL,
    IsNullable bit NOT NULL
);

INSERT INTO @ExpectedColumns (
    ColumnName,
    TypeName,
    MaxLength,
    [Precision],
    Scale,
    IsNullable)
VALUES
    (N'OperationNetUid', N'uniqueidentifier', 16, 0, 0, 0),
    (N'OperationName', N'nvarchar', 256, 0, 0, 0),
    (N'PrincipalNetUid', N'uniqueidentifier', 16, 0, 0, 0),
    (N'ClientNetUid', N'uniqueidentifier', 16, 0, 0, 0),
    (N'ModeFlag', N'bit', 1, 1, 0, 0),
    (N'RequestFingerprint', N'binary', 32, 0, 0, 0),
    (N'ResponsePayload', N'nvarchar', -1, 0, 0, 1),
    (N'CreatedUtc', N'datetime2', 8, 27, 7, 0),
    (N'CompletedUtc', N'datetime2', 8, 27, 7, 1);

IF EXISTS (
    SELECT ColumnName, TypeName, MaxLength, [Precision], Scale, IsNullable
    FROM @ExpectedColumns
    EXCEPT
    SELECT column_definition.name,
           type_definition.name,
           column_definition.max_length,
           column_definition.precision,
           column_definition.scale,
           column_definition.is_nullable
    FROM sys.columns AS column_definition
    INNER JOIN sys.types AS type_definition
        ON type_definition.user_type_id = column_definition.user_type_id
    WHERE column_definition.object_id = OBJECT_ID(N'dbo.EcommerceSalesCreationLedger', N'U')
)
    THROW 51047, N'EcommerceSalesCreationLedger has incompatible column definitions.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints AS key_constraint
    INNER JOIN sys.indexes AS index_definition
        ON index_definition.object_id = key_constraint.parent_object_id
       AND index_definition.index_id = key_constraint.unique_index_id
    INNER JOIN sys.index_columns AS index_column
        ON index_column.object_id = index_definition.object_id
       AND index_column.index_id = index_definition.index_id
       AND index_column.key_ordinal = 1
    INNER JOIN sys.columns AS column_definition
        ON column_definition.object_id = index_column.object_id
       AND column_definition.column_id = index_column.column_id
    WHERE key_constraint.parent_object_id = OBJECT_ID(N'dbo.EcommerceSalesCreationLedger', N'U')
      AND key_constraint.[type] = N'PK'
      AND index_definition.[type] = 1
      AND index_definition.is_unique = 1
      AND column_definition.name = N'OperationNetUid'
      AND NOT EXISTS (
          SELECT 1
          FROM sys.index_columns AS extra_key
          WHERE extra_key.object_id = index_definition.object_id
            AND extra_key.index_id = index_definition.index_id
            AND extra_key.key_ordinal > 1)
)
    THROW 51048, N'EcommerceSalesCreationLedger requires a clustered primary key on OperationNetUid.', 1;

IF EXISTS (
    SELECT required_constraint.name
    FROM (VALUES
        (N'CK_EcommerceSalesCreationLedger_ResponseJson'),
        (N'CK_EcommerceSalesCreationLedger_Completion')
    ) AS required_constraint(name)
    WHERE NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints AS check_constraint
        WHERE check_constraint.parent_object_id = OBJECT_ID(N'dbo.EcommerceSalesCreationLedger', N'U')
          AND check_constraint.name = required_constraint.name
          AND check_constraint.is_disabled = 0
          AND check_constraint.is_not_trusted = 0)
)
    THROW 51049, N'EcommerceSalesCreationLedger requires enabled and trusted integrity constraints.', 1;

COMMIT TRANSACTION;
""";

    private const string _registerSql = """
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;

DECLARE @WasInserted bit = 0;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.EcommerceSalesCreationLedger WITH (UPDLOCK, HOLDLOCK)
    WHERE OperationNetUid = @OperationNetUid)
BEGIN
    INSERT INTO dbo.EcommerceSalesCreationLedger (
        OperationNetUid,
        OperationName,
        PrincipalNetUid,
        ClientNetUid,
        ModeFlag,
        RequestFingerprint,
        CreatedUtc)
    VALUES (
        @OperationNetUid,
        @OperationName,
        @PrincipalNetUid,
        @ClientNetUid,
        @ModeFlag,
        @RequestFingerprint,
        @CreatedUtc);
    SET @WasInserted = 1;
END;

SELECT
    @WasInserted AS WasInserted,
    OperationNetUid,
    OperationName,
    PrincipalNetUid,
    ClientNetUid,
    ModeFlag,
    RequestFingerprint,
    ResponsePayload,
    CreatedUtc,
    CompletedUtc
FROM dbo.EcommerceSalesCreationLedger WITH (HOLDLOCK)
WHERE OperationNetUid = @OperationNetUid;

COMMIT TRANSACTION;
""";

    private readonly IDbConnectionFactory _connectionFactory;

    public SqlSalesCreationLedgerStore(IDbConnectionFactory connectionFactory) {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            _ensureSchemaSql,
            cancellationToken: cancellationToken));
    }

    public async Task<SalesCreationLedgerEntry?> GetAsync(
        Guid operationNetUid,
        CancellationToken cancellationToken) {
        if (operationNetUid == Guid.Empty)
            throw new ArgumentException("Idempotency key is required.", nameof(operationNetUid));

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return await connection.QuerySingleOrDefaultAsync<SalesCreationLedgerEntry>(
            new CommandDefinition(
                """
SELECT
    OperationNetUid,
    OperationName,
    PrincipalNetUid,
    ClientNetUid,
    ModeFlag,
    RequestFingerprint,
    ResponsePayload,
    CreatedUtc,
    CompletedUtc
FROM dbo.EcommerceSalesCreationLedger
WHERE OperationNetUid = @OperationNetUid;
""",
                new { OperationNetUid = operationNetUid },
                cancellationToken: cancellationToken));
    }

    public async Task<SalesCreationLedgerRegistration> RegisterAsync(
        IDbConnection connection,
        SalesCreationRequest request,
        DateTime createdUtc,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(connection);
        ValidateRequest(request);

        RegistrationRow row = await connection.QuerySingleAsync<RegistrationRow>(
            new CommandDefinition(
                _registerSql,
                new {
                    request.OperationNetUid,
                    request.OperationName,
                    request.PrincipalNetUid,
                    request.ClientNetUid,
                    request.ModeFlag,
                    request.RequestFingerprint,
                    CreatedUtc = RequireUtc(createdUtc)
                },
                cancellationToken: cancellationToken));
        SalesCreationLedgerEntry entry = row.ToEntry();
        SalesCreationRequestKey.EnsureMatches(request, entry);

        if (!row.WasInserted && string.IsNullOrWhiteSpace(entry.ResponsePayload))
            throw new InvalidOperationException(
                "The durable sales creation ledger contains an incomplete committed receipt.");

        return new SalesCreationLedgerRegistration(row.WasInserted, entry);
    }

    public async Task CompleteAsync(
        IDbConnection connection,
        SalesCreationRequest request,
        string responsePayload,
        DateTime completedUtc,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(connection);
        ValidateRequest(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(responsePayload);

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            """
UPDATE dbo.EcommerceSalesCreationLedger
SET ResponsePayload = @ResponsePayload,
    CompletedUtc = @CompletedUtc
WHERE OperationNetUid = @OperationNetUid
  AND OperationName = @OperationName
  AND PrincipalNetUid = @PrincipalNetUid
  AND ClientNetUid = @ClientNetUid
  AND ModeFlag = @ModeFlag
  AND RequestFingerprint = @RequestFingerprint
  AND ResponsePayload IS NULL
  AND CompletedUtc IS NULL;
""",
            new {
                request.OperationNetUid,
                request.OperationName,
                request.PrincipalNetUid,
                request.ClientNetUid,
                request.ModeFlag,
                request.RequestFingerprint,
                ResponsePayload = responsePayload,
                CompletedUtc = RequireUtc(completedUtc)
            },
            cancellationToken: cancellationToken));
        if (affected != 1)
            throw new InvalidOperationException(
                "Unable to complete the durable sales creation ledger receipt.");
    }

    private static void ValidateRequest(SalesCreationRequest request) {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OperationNetUid == Guid.Empty)
            throw new ArgumentException("Idempotency key is required.", nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationName);
        if (request.OperationName.Length > 128)
            throw new ArgumentException("Operation name is too long.", nameof(request));
        if (request.PrincipalNetUid == Guid.Empty || request.ClientNetUid == Guid.Empty)
            throw new ArgumentException("Principal and client keys are required.", nameof(request));
        if (request.RequestFingerprint is not { Length: 32 })
            throw new ArgumentException("A SHA-256 request fingerprint is required.", nameof(request));
    }

    private static DateTime RequireUtc(DateTime value) {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("An explicit UTC timestamp is required.", nameof(value));
        return value;
    }

    private sealed class RegistrationRow {
        public bool WasInserted { get; init; }
        public Guid OperationNetUid { get; init; }
        public string OperationName { get; init; } = string.Empty;
        public Guid PrincipalNetUid { get; init; }
        public Guid ClientNetUid { get; init; }
        public bool ModeFlag { get; init; }
        public byte[] RequestFingerprint { get; init; } = [];
        public string? ResponsePayload { get; init; }
        public DateTime CreatedUtc { get; init; }
        public DateTime? CompletedUtc { get; init; }

        public SalesCreationLedgerEntry ToEntry() => new() {
            OperationNetUid = OperationNetUid,
            OperationName = OperationName,
            PrincipalNetUid = PrincipalNetUid,
            ClientNetUid = ClientNetUid,
            ModeFlag = ModeFlag,
            RequestFingerprint = RequestFingerprint,
            ResponsePayload = ResponsePayload,
            CreatedUtc = CreatedUtc,
            CompletedUtc = CompletedUtc
        };
    }
}
