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

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.EcommerceSalesCreationLedger', N'U')
      AND [type] = N'PK'
      AND name = N'PK_EcommerceSalesCreationLedger')
    THROW 51048, N'EcommerceSalesCreationLedger requires its primary key on OperationNetUid.', 1;

COMMIT TRANSACTION;
