SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IS NULL OR DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 54740, N'Run ecommerce pricing Change Tracking setup in the application database.', 1;

DECLARE @MaintenanceRoleName sysname = N'GbaPricingChangeTrackingMaintenance';
DECLARE @RuntimeRoleName sysname = N'GbaPricingChangeTrackingRuntime';

IF DATABASE_PRINCIPAL_ID(@MaintenanceRoleName) IS NULL
   OR DATABASE_PRINCIPAL_ID(@RuntimeRoleName) IS NULL
BEGIN
    DECLARE @BootstrapLockResource nvarchar(255) =
        N'gba:ecommerce:pricing-change-tracking:role-bootstrap:'
        + CONVERT(nvarchar(128), DB_NAME());
    DECLARE @BootstrapLockResult int;
    DECLARE @BootstrapUnlockResult int;

    EXEC @BootstrapLockResult = sys.sp_getapplock
        @Resource = @BootstrapLockResource,
        @LockMode = N'Exclusive',
        @LockOwner = N'Session',
        @LockTimeout = 60000,
        @DbPrincipal = N'dbo';

    IF @BootstrapLockResult < 0
        THROW 54755, N'Could not acquire the owner-scoped pricing role bootstrap lock.', 1;

    BEGIN TRY
        DECLARE @CreateRoleSql nvarchar(max);
        IF DATABASE_PRINCIPAL_ID(@MaintenanceRoleName) IS NULL
        BEGIN
            SET @CreateRoleSql =
                N'CREATE ROLE ' + QUOTENAME(@MaintenanceRoleName) + N' AUTHORIZATION dbo;';
            EXEC sys.sp_executesql @CreateRoleSql;
        END;
        IF DATABASE_PRINCIPAL_ID(@RuntimeRoleName) IS NULL
        BEGIN
            SET @CreateRoleSql =
                N'CREATE ROLE ' + QUOTENAME(@RuntimeRoleName) + N' AUTHORIZATION dbo;';
            EXEC sys.sp_executesql @CreateRoleSql;
        END;

        EXEC @BootstrapUnlockResult = sys.sp_releaseapplock
            @Resource = @BootstrapLockResource,
            @LockOwner = N'Session',
            @DbPrincipal = N'dbo';
    END TRY
    BEGIN CATCH
        EXEC @BootstrapUnlockResult = sys.sp_releaseapplock
            @Resource = @BootstrapLockResource,
            @LockOwner = N'Session',
            @DbPrincipal = N'dbo';
        THROW;
    END CATCH;
END;

DECLARE @MaintenanceLockResource nvarchar(255) =
    N'gba:ecommerce:pricing-change-tracking:maintenance:'
    + CONVERT(nvarchar(128), DB_NAME());
DECLARE @MaintenanceLockResult int;
DECLARE @MaintenanceUnlockResult int;

EXEC @MaintenanceLockResult = sys.sp_getapplock
    @Resource = @MaintenanceLockResource,
    @LockMode = N'Exclusive',
    @LockOwner = N'Session',
    @LockTimeout = 60000,
    @DbPrincipal = @MaintenanceRoleName;

IF @MaintenanceLockResult < 0
    THROW 54754, N'Could not acquire the pricing Change Tracking maintenance lock.', 1;

BEGIN TRY
DECLARE @CurrentRecoveryForkId uniqueidentifier = (
    SELECT recovery.[recovery_fork_guid]
    FROM sys.database_recovery_status recovery
    WHERE recovery.[database_id] = DB_ID()
);

IF @CurrentRecoveryForkId IS NULL
    THROW 54747, N'Current database recovery-fork identity is unavailable.', 1;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.PricingChangeTrackingIncarnation', N'U') IS NULL
BEGIN
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
END;

IF COL_LENGTH(N'dbo.PricingChangeTrackingIncarnation', N'RepairGeneration') IS NULL
BEGIN
    ALTER TABLE dbo.PricingChangeTrackingIncarnation
        ADD [RepairGeneration] bigint NOT NULL
            CONSTRAINT [DF_PricingChangeTrackingIncarnation_RepairGeneration]
            DEFAULT (1) WITH VALUES;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints constraintDefinition
    WHERE constraintDefinition.[parent_object_id] =
          OBJECT_ID(N'dbo.PricingChangeTrackingIncarnation')
      AND constraintDefinition.[name] =
          N'CK_PricingChangeTrackingIncarnation_RepairGeneration')
BEGIN
    ALTER TABLE dbo.PricingChangeTrackingIncarnation
        ADD CONSTRAINT [CK_PricingChangeTrackingIncarnation_RepairGeneration]
        CHECK ([RepairGeneration] > 0);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.PricingChangeTrackingIncarnation)
BEGIN
    INSERT INTO dbo.PricingChangeTrackingIncarnation
        ([Id], [IncarnationId], [RecoveryForkId], [RepairGeneration],
         [RotatedAtUtc], [RotatedBy])
    VALUES
        (1, NEWID(), @CurrentRecoveryForkId, 1,
         SYSUTCDATETIME(), ORIGINAL_LOGIN());
END;

IF (SELECT COUNT(*) FROM dbo.PricingChangeTrackingIncarnation) <> 1
   OR NOT EXISTS (
       SELECT 1
       FROM dbo.PricingChangeTrackingIncarnation
       WHERE [Id] = 1
         AND [IncarnationId] <> '00000000-0000-0000-0000-000000000000'
         AND [RepairGeneration] > 0)
    THROW 54748, N'Pricing Change Tracking recovery incarnation is missing or malformed.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.PricingChangeTrackingIncarnation
    WHERE [Id] = 1
      AND [RecoveryForkId] = @CurrentRecoveryForkId)
    THROW 54749,
        N'Database recovery lineage changed. Run db/pricing-cache-change-tracking-rotate-incarnation.sql before enabling traffic.',
        1;

COMMIT TRANSACTION;

DECLARE @Required TABLE (
    [SchemaName] sysname NOT NULL,
    [TableName] sysname NOT NULL,
    PRIMARY KEY ([SchemaName], [TableName])
);

INSERT INTO @Required ([SchemaName], [TableName])
VALUES
    (N'dbo', N'Agreement'),
    (N'dbo', N'ClientAgreement'),
    (N'dbo', N'Currency'),
    (N'dbo', N'OrderItem'),
    (N'dbo', N'Organization'),
    (N'dbo', N'Pricing'),
    (N'dbo', N'PricingProductGroupDiscount'),
    (N'dbo', N'PricingSourceCutoverState'),
    (N'dbo', N'PricingSourceDefinition'),
    (N'dbo', N'PricingSourceSyncState'),
    (N'dbo', N'Product'),
    (N'dbo', N'ProductGroupDiscount'),
    (N'dbo', N'ProductPricing'),
    (N'dbo', N'ProductPricingSourceSnapshot'),
    (N'dbo', N'ProductProductGroup');

IF EXISTS (
    SELECT 1
    FROM @Required AS required
    LEFT JOIN sys.schemas AS requiredSchema
        ON requiredSchema.[name] = required.[SchemaName]
    LEFT JOIN sys.tables AS requiredTable
        ON requiredTable.[schema_id] = requiredSchema.[schema_id]
       AND requiredTable.[name] = required.[TableName]
    WHERE requiredTable.[object_id] IS NULL)
BEGIN
    SELECT N'MissingTable' AS [DifferenceType],
           required.[SchemaName],
           required.[TableName]
    FROM @Required AS required
    LEFT JOIN sys.schemas AS requiredSchema
        ON requiredSchema.[name] = required.[SchemaName]
    LEFT JOIN sys.tables AS requiredTable
        ON requiredTable.[schema_id] = requiredSchema.[schema_id]
       AND requiredTable.[name] = required.[TableName]
    WHERE requiredTable.[object_id] IS NULL
    ORDER BY required.[SchemaName], required.[TableName];

    THROW 54741, N'Pricing Change Tracking dependencies are missing; apply approved migrations first.', 1;
END;

DECLARE @RequiredPriceFunction TABLE (
    [SchemaName] sysname NOT NULL,
    [FunctionName] sysname NOT NULL,
    PRIMARY KEY ([SchemaName], [FunctionName])
);

INSERT INTO @RequiredPriceFunction ([SchemaName], [FunctionName])
VALUES
    (N'dbo', N'GetCalculatedProductPriceForPricingSource'),
    (N'dbo', N'GetCalculatedProductPriceWithSharesAndVat');

DECLARE @PriceFunctionRoot TABLE (
    [ObjectId] int NOT NULL PRIMARY KEY
);

INSERT INTO @PriceFunctionRoot ([ObjectId])
SELECT priceFunction.[object_id]
FROM @RequiredPriceFunction required
INNER JOIN sys.schemas priceFunctionSchema
    ON priceFunctionSchema.[name] = required.[SchemaName]
INNER JOIN sys.objects priceFunction
    ON priceFunction.[schema_id] = priceFunctionSchema.[schema_id]
   AND priceFunction.[name] = required.[FunctionName]
   AND priceFunction.[type] IN (N'FN', N'IF', N'TF');

IF (SELECT COUNT(*) FROM @PriceFunctionRoot) <> (SELECT COUNT(*) FROM @RequiredPriceFunction)
BEGIN
    SELECT N'MissingPriceFunction' AS [DifferenceType],
           required.[SchemaName],
           required.[FunctionName]
    FROM @RequiredPriceFunction required
    LEFT JOIN sys.schemas functionSchema
        ON functionSchema.[name] = required.[SchemaName]
    LEFT JOIN sys.objects priceFunction
        ON priceFunction.[schema_id] = functionSchema.[schema_id]
       AND priceFunction.[name] = required.[FunctionName]
       AND priceFunction.[type] IN (N'FN', N'IF', N'TF')
    WHERE priceFunction.[object_id] IS NULL
    ORDER BY required.[SchemaName], required.[FunctionName];

    THROW 54742, N'Pricing Change Tracking price functions are missing.', 1;
END;

DECLARE @ActualPriceInput TABLE (
    [SchemaName] sysname NOT NULL,
    [TableName] sysname NOT NULL,
    PRIMARY KEY ([SchemaName], [TableName])
);

DECLARE @PriceDependencyObject TABLE (
    [ObjectId] int NOT NULL PRIMARY KEY
);

;WITH PriceDependency ([ObjectId], [DependencyPath]) AS (
    SELECT root.[ObjectId],
           CONVERT(nvarchar(max), N'/' + CONVERT(nvarchar(20), root.[ObjectId]) + N'/')
    FROM @PriceFunctionRoot root

    UNION ALL

    SELECT dependency.[referenced_id],
           closure.[DependencyPath] + CONVERT(nvarchar(20), dependency.[referenced_id]) + N'/'
    FROM PriceDependency closure
    INNER JOIN sys.sql_expression_dependencies dependency
        ON dependency.[referencing_id] = closure.[ObjectId]
    WHERE dependency.[referenced_id] IS NOT NULL
      AND closure.[DependencyPath] NOT LIKE
          N'%/' + CONVERT(nvarchar(20), dependency.[referenced_id]) + N'/%'
)
INSERT INTO @PriceDependencyObject ([ObjectId])
SELECT DISTINCT dependency.[ObjectId]
FROM PriceDependency dependency
OPTION (MAXRECURSION 100);

DECLARE @UnresolvedPriceDependencyCount int = (
    SELECT COUNT(*)
    FROM @PriceDependencyObject closure
    INNER JOIN sys.sql_expression_dependencies dependency
        ON dependency.[referencing_id] = closure.[ObjectId]
    LEFT JOIN sys.objects referencedObject
        ON referencedObject.[object_id] = dependency.[referenced_id]
    WHERE dependency.[referenced_id] IS NULL
       OR (dependency.[referenced_class_desc] = N'OBJECT_OR_COLUMN'
           AND referencedObject.[object_id] IS NULL)
);
DECLARE @CrossDatabasePriceDependencyCount int = (
    SELECT COUNT(*)
    FROM @PriceDependencyObject closure
    INNER JOIN sys.sql_expression_dependencies dependency
        ON dependency.[referencing_id] = closure.[ObjectId]
    LEFT JOIN sys.synonyms referencedSynonym
        ON referencedSynonym.[object_id] = dependency.[referenced_id]
    LEFT JOIN sys.schemas referencedSchema
        ON referencedSchema.[name] =
           COALESCE(dependency.[referenced_schema_name], N'dbo')
    LEFT JOIN sys.synonyms namedSynonym
        ON namedSynonym.[schema_id] = referencedSchema.[schema_id]
       AND namedSynonym.[name] = dependency.[referenced_entity_name]
    WHERE dependency.[referenced_server_name] IS NOT NULL
       OR dependency.[referenced_database_name] IS NOT NULL
       OR PARSENAME(COALESCE(
              referencedSynonym.[base_object_name],
              namedSynonym.[base_object_name]), 4) IS NOT NULL
       OR PARSENAME(COALESCE(
              referencedSynonym.[base_object_name],
              namedSynonym.[base_object_name]), 3) IS NOT NULL
);
DECLARE @SynonymBackedPriceDependencyCount int = (
    SELECT COUNT(*)
    FROM @PriceDependencyObject closure
    INNER JOIN sys.sql_expression_dependencies dependency
        ON dependency.[referencing_id] = closure.[ObjectId]
    LEFT JOIN sys.synonyms referencedSynonym
        ON referencedSynonym.[object_id] = dependency.[referenced_id]
    LEFT JOIN sys.schemas referencedSchema
        ON referencedSchema.[name] =
           COALESCE(dependency.[referenced_schema_name], N'dbo')
    LEFT JOIN sys.synonyms namedSynonym
        ON namedSynonym.[schema_id] = referencedSchema.[schema_id]
       AND namedSynonym.[name] = dependency.[referenced_entity_name]
    WHERE referencedSynonym.[object_id] IS NOT NULL
       OR namedSynonym.[object_id] IS NOT NULL
);

IF @UnresolvedPriceDependencyCount <> 0
   OR @CrossDatabasePriceDependencyCount <> 0
   OR @SynonymBackedPriceDependencyCount <> 0
BEGIN
    SELECT @UnresolvedPriceDependencyCount AS [UnresolvedPriceDependencyCount],
           @CrossDatabasePriceDependencyCount AS [CrossDatabasePriceDependencyCount],
           @SynonymBackedPriceDependencyCount AS [SynonymBackedPriceDependencyCount];

    THROW 54758,
        N'Pricing dependencies contain unresolved, cross-database, or synonym-backed references.',
        1;
END;

INSERT INTO @ActualPriceInput ([SchemaName], [TableName])
SELECT DISTINCT inputSchema.[name], inputTable.[name]
FROM @PriceDependencyObject dependency
INNER JOIN sys.tables inputTable
    ON inputTable.[object_id] = dependency.[ObjectId]
INNER JOIN sys.schemas inputSchema
    ON inputSchema.[schema_id] = inputTable.[schema_id]

UNION

SELECT N'dbo', N'Currency'
;

DECLARE @ActualPricingModuleCount int;
DECLARE @UnreadablePricingModuleCount int;

SELECT @ActualPricingModuleCount = COUNT(*),
       @UnreadablePricingModuleCount = COALESCE(
           SUM(CASE WHEN moduleDefinition.[definition] IS NULL THEN 1 ELSE 0 END),
           0)
FROM @PriceDependencyObject dependency
INNER JOIN sys.sql_modules moduleDefinition
    ON moduleDefinition.[object_id] = dependency.[ObjectId];

IF @ActualPricingModuleCount < (SELECT COUNT(*) FROM @RequiredPriceFunction)
   OR @UnreadablePricingModuleCount <> 0
    THROW 54750, N'Pricing module definitions are missing or unreadable; revision hashing cannot be enabled.', 1;

DECLARE @UnlistedPriceInputCount int = (
    SELECT COUNT(*)
    FROM @ActualPriceInput input
    LEFT JOIN @Required required
        ON required.[SchemaName] = input.[SchemaName]
       AND required.[TableName] = input.[TableName]
    WHERE required.[TableName] IS NULL
);
DECLARE @NonInputManifestEntryCount int = (
    SELECT COUNT(*)
    FROM @Required required
    LEFT JOIN @ActualPriceInput input
        ON input.[SchemaName] = required.[SchemaName]
       AND input.[TableName] = required.[TableName]
    WHERE input.[TableName] IS NULL
);

IF @UnlistedPriceInputCount <> 0 OR @NonInputManifestEntryCount <> 0
BEGIN
    SELECT N'UnlistedPriceInput' AS [DifferenceType],
           input.[SchemaName],
           input.[TableName]
    FROM @ActualPriceInput input
    LEFT JOIN @Required required
        ON required.[SchemaName] = input.[SchemaName]
       AND required.[TableName] = input.[TableName]
    WHERE required.[TableName] IS NULL

    UNION ALL

    SELECT N'NonInputManifestEntry',
           required.[SchemaName],
           required.[TableName]
    FROM @Required required
    LEFT JOIN @ActualPriceInput input
        ON input.[SchemaName] = required.[SchemaName]
       AND input.[TableName] = required.[TableName]
    WHERE input.[TableName] IS NULL
    ORDER BY [DifferenceType], [SchemaName], [TableName];

    SELECT (SELECT COUNT(*) FROM @Required) AS [ExpectedManifestInputCount],
           (SELECT COUNT(*) FROM @ActualPriceInput) AS [ActualManifestInputCount],
           @UnlistedPriceInputCount AS [UnlistedPriceInputCount],
           @NonInputManifestEntryCount AS [NonInputManifestEntryCount];

    THROW 54743, N'Pricing Change Tracking manifest does not match transitive price-function inputs.', 1;
END;

DECLARE @ExpectedTrackedTableCount int = (SELECT COUNT(*) FROM @Required);
DECLARE @ActualTrackedTableCount int = (SELECT COUNT(*) FROM sys.change_tracking_tables);
DECLARE @MissingTrackedTableCount int = (
    SELECT COUNT(*)
    FROM @Required required
    INNER JOIN sys.schemas requiredSchema
        ON requiredSchema.[name] = required.[SchemaName]
    INNER JOIN sys.tables requiredTable
        ON requiredTable.[schema_id] = requiredSchema.[schema_id]
       AND requiredTable.[name] = required.[TableName]
    LEFT JOIN sys.change_tracking_tables tracked
        ON tracked.[object_id] = requiredTable.[object_id]
    WHERE tracked.[object_id] IS NULL
);
DECLARE @ExtraTrackedTableCount int = (
    SELECT COUNT(*)
    FROM sys.change_tracking_tables tracked
    INNER JOIN sys.tables trackedTable
        ON trackedTable.[object_id] = tracked.[object_id]
    INNER JOIN sys.schemas trackedSchema
        ON trackedSchema.[schema_id] = trackedTable.[schema_id]
    LEFT JOIN @Required required
        ON required.[SchemaName] = trackedSchema.[name]
       AND required.[TableName] = trackedTable.[name]
    WHERE required.[TableName] IS NULL
);

IF @ExtraTrackedTableCount <> 0
BEGIN
    SELECT N'ExtraTrackedTable' AS [DifferenceType],
           trackedSchema.[name] AS [SchemaName],
           trackedTable.[name] AS [TableName]
    FROM sys.change_tracking_tables tracked
    INNER JOIN sys.tables trackedTable
        ON trackedTable.[object_id] = tracked.[object_id]
    INNER JOIN sys.schemas trackedSchema
        ON trackedSchema.[schema_id] = trackedTable.[schema_id]
    LEFT JOIN @Required required
        ON required.[SchemaName] = trackedSchema.[name]
       AND required.[TableName] = trackedTable.[name]
    WHERE required.[TableName] IS NULL
    ORDER BY trackedSchema.[name], trackedTable.[name];

    SELECT @ExpectedTrackedTableCount AS [ExpectedTrackedTableCount],
           @ActualTrackedTableCount AS [ActualTrackedTableCount],
           @MissingTrackedTableCount AS [MissingTrackedTableCount],
           @ExtraTrackedTableCount AS [ExtraTrackedTableCount];

    THROW 54744, N'Unexpected Change Tracking tables exist; review and disable them explicitly before setup.', 1;
END;

DECLARE @DatabaseName sysname = DB_NAME();
DECLARE @ConfigureDatabase nvarchar(max);

IF NOT EXISTS (
    SELECT 1
    FROM sys.change_tracking_databases
    WHERE [database_id] = DB_ID())
    SET @ConfigureDatabase =
        N'ALTER DATABASE ' + QUOTENAME(@DatabaseName) +
        N' SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);';
ELSE
    SET @ConfigureDatabase =
        N'ALTER DATABASE ' + QUOTENAME(@DatabaseName) +
        N' SET CHANGE_TRACKING (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);';

EXEC sys.sp_executesql @ConfigureDatabase;

DECLARE @RepairFenceMarker nvarchar(64) = N'GBA_CT_REPAIR_FENCE_V1';
DECLARE @RepairFenceNeedsRepair bit = CASE WHEN EXISTS (
    SELECT 1
    FROM sys.triggers repairFence
    INNER JOIN sys.sql_modules repairFenceDefinition
        ON repairFenceDefinition.[object_id] = repairFence.[object_id]
    WHERE repairFence.[parent_class_desc] = N'DATABASE'
      AND repairFence.[name] = N'GbaPricingChangeTrackingRepairFence'
      AND repairFence.[is_disabled] = 0
      AND repairFenceDefinition.[execute_as_principal_id] =
          DATABASE_PRINCIPAL_ID(N'dbo')
      AND repairFenceDefinition.[definition] LIKE N'%' + @RepairFenceMarker + N'%'
      AND (SELECT COUNT(*)
           FROM sys.trigger_events repairFenceEvent
           WHERE repairFenceEvent.[object_id] = repairFence.[object_id]
             AND repairFenceEvent.[type_desc] IN (
                 N'CREATE_TABLE', N'ALTER_TABLE', N'DROP_TABLE')) = 3
      AND NOT EXISTS (
          SELECT 1
          FROM sys.trigger_events repairFenceEvent
          WHERE repairFenceEvent.[object_id] = repairFence.[object_id]
            AND repairFenceEvent.[type_desc] NOT IN (
                N'CREATE_TABLE', N'ALTER_TABLE', N'DROP_TABLE')))
    THEN 0 ELSE 1 END;

BEGIN TRANSACTION;

IF @RepairFenceNeedsRepair = 1
BEGIN
    EXEC sys.sp_executesql N'
CREATE OR ALTER TRIGGER GbaPricingChangeTrackingRepairFence
ON DATABASE
WITH EXECUTE AS ''dbo''
AFTER CREATE_TABLE, ALTER_TABLE, DROP_TABLE
AS
BEGIN
    SET NOCOUNT ON;

    -- GBA_CT_REPAIR_FENCE_V1
    DECLARE @EventData xml = EVENTDATA();
    DECLARE @SchemaName sysname = @EventData.value(
        ''(/EVENT_INSTANCE/SchemaName)[1]'', ''nvarchar(128)'');
    DECLARE @TableName sysname = @EventData.value(
        ''(/EVENT_INSTANCE/ObjectName)[1]'', ''nvarchar(128)'');

    IF @SchemaName = N''dbo''
       AND @TableName IN (
           N''Agreement'',
           N''ClientAgreement'',
           N''Currency'',
           N''OrderItem'',
           N''Organization'',
           N''Pricing'',
           N''PricingProductGroupDiscount'',
           N''PricingSourceCutoverState'',
           N''PricingSourceDefinition'',
           N''PricingSourceSyncState'',
           N''Product'',
           N''ProductGroupDiscount'',
           N''ProductPricing'',
           N''ProductPricingSourceSnapshot'',
           N''ProductProductGroup'')
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM dbo.PricingChangeTrackingIncarnation WITH (UPDLOCK, HOLDLOCK)
            WHERE [Id] = 1
              AND [RepairGeneration] > 0
              AND [RepairGeneration] < 9223372036854775807)
            THROW 54759,
                N''Pricing Change Tracking repair generation cannot advance.'', 1;

        UPDATE dbo.PricingChangeTrackingIncarnation
        SET [RepairGeneration] = [RepairGeneration] + 1,
            [RotatedAtUtc] = SYSUTCDATETIME(),
            [RotatedBy] = ORIGINAL_LOGIN()
        WHERE [Id] = 1;
    END;
END;';

    ENABLE TRIGGER GbaPricingChangeTrackingRepairFence ON DATABASE;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.PricingChangeTrackingIncarnation WITH (UPDLOCK, HOLDLOCK)
        WHERE [Id] = 1
          AND [RepairGeneration] > 0
          AND [RepairGeneration] < 9223372036854775807)
        THROW 54759,
            N'Pricing Change Tracking repair generation cannot advance.', 1;

    UPDATE dbo.PricingChangeTrackingIncarnation
    SET [RepairGeneration] = [RepairGeneration] + 1,
        [RotatedAtUtc] = SYSUTCDATETIME(),
        [RotatedBy] = ORIGINAL_LOGIN()
    WHERE [Id] = 1;
END;

DECLARE @EnableTables nvarchar(max);

SELECT @EnableTables = STRING_AGG(
    CONVERT(nvarchar(max),
        N'ALTER TABLE ' + QUOTENAME(required.[SchemaName]) + N'.' +
        QUOTENAME(required.[TableName]) +
        N' ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);'),
    CHAR(10))
FROM @Required AS required
INNER JOIN sys.schemas AS requiredSchema
    ON requiredSchema.[name] = required.[SchemaName]
INNER JOIN sys.tables AS requiredTable
    ON requiredTable.[schema_id] = requiredSchema.[schema_id]
   AND requiredTable.[name] = required.[TableName]
LEFT JOIN sys.change_tracking_tables AS tracked
    ON tracked.[object_id] = requiredTable.[object_id]
WHERE tracked.[object_id] IS NULL;

IF @EnableTables IS NOT NULL
    EXEC sys.sp_executesql @EnableTables;

IF CHANGE_TRACKING_CURRENT_VERSION() IS NULL
    THROW 54745, N'Database Change Tracking is not active after setup.', 1;

SET @ActualTrackedTableCount = (SELECT COUNT(*) FROM sys.change_tracking_tables);
SET @MissingTrackedTableCount = (
    SELECT COUNT(*)
    FROM @Required required
    INNER JOIN sys.schemas requiredSchema
        ON requiredSchema.[name] = required.[SchemaName]
    INNER JOIN sys.tables requiredTable
        ON requiredTable.[schema_id] = requiredSchema.[schema_id]
       AND requiredTable.[name] = required.[TableName]
    LEFT JOIN sys.change_tracking_tables tracked
        ON tracked.[object_id] = requiredTable.[object_id]
    WHERE tracked.[object_id] IS NULL
);
SET @ExtraTrackedTableCount = (
    SELECT COUNT(*)
    FROM sys.change_tracking_tables tracked
    INNER JOIN sys.tables trackedTable
        ON trackedTable.[object_id] = tracked.[object_id]
    INNER JOIN sys.schemas trackedSchema
        ON trackedSchema.[schema_id] = trackedTable.[schema_id]
    LEFT JOIN @Required required
        ON required.[SchemaName] = trackedSchema.[name]
       AND required.[TableName] = trackedTable.[name]
    WHERE required.[TableName] IS NULL
);

IF @MissingTrackedTableCount <> 0 OR @ExtraTrackedTableCount <> 0
BEGIN
    SELECT @ExpectedTrackedTableCount AS [ExpectedTrackedTableCount],
           @ActualTrackedTableCount AS [ActualTrackedTableCount],
           @MissingTrackedTableCount AS [MissingTrackedTableCount],
           @ExtraTrackedTableCount AS [ExtraTrackedTableCount];

    THROW 54746, N'Pricing Change Tracking setup did not produce the exact allowlist.', 1;
END;

DECLARE @UnreadableTrackedTableIdentityCount int = (
    SELECT COUNT(*)
    FROM @Required required
    INNER JOIN sys.schemas requiredSchema
        ON requiredSchema.[name] = required.[SchemaName]
    INNER JOIN sys.tables requiredTable
        ON requiredTable.[schema_id] = requiredSchema.[schema_id]
       AND requiredTable.[name] = required.[TableName]
    LEFT JOIN sys.change_tracking_tables tracked
        ON tracked.[object_id] = requiredTable.[object_id]
    WHERE tracked.[object_id] IS NULL
       OR tracked.[begin_version] IS NULL
);

IF @UnreadableTrackedTableIdentityCount <> 0
    THROW 54759,
        N'Pricing Change Tracking table identity or begin version is unavailable.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.triggers repairFence
    INNER JOIN sys.sql_modules repairFenceDefinition
        ON repairFenceDefinition.[object_id] = repairFence.[object_id]
    WHERE repairFence.[parent_class_desc] = N'DATABASE'
      AND repairFence.[name] = N'GbaPricingChangeTrackingRepairFence'
      AND repairFence.[is_disabled] = 0
      AND repairFenceDefinition.[execute_as_principal_id] =
          DATABASE_PRINCIPAL_ID(N'dbo')
      AND repairFenceDefinition.[definition] LIKE N'%' + @RepairFenceMarker + N'%'
      AND (SELECT COUNT(*)
           FROM sys.trigger_events repairFenceEvent
           WHERE repairFenceEvent.[object_id] = repairFence.[object_id]
             AND repairFenceEvent.[type_desc] IN (
                 N'CREATE_TABLE', N'ALTER_TABLE', N'DROP_TABLE')) = 3
      AND NOT EXISTS (
          SELECT 1
          FROM sys.trigger_events repairFenceEvent
          WHERE repairFenceEvent.[object_id] = repairFence.[object_id]
            AND repairFenceEvent.[type_desc] NOT IN (
                N'CREATE_TABLE', N'ALTER_TABLE', N'DROP_TABLE')))
    THROW 54759, N'Pricing Change Tracking repair fence is unavailable.', 1;

COMMIT TRANSACTION;

EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.GetEcommercePricingChangeTrackingState
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentVersion bigint = CHANGE_TRACKING_CURRENT_VERSION();

    WITH RequiredDependency ([SchemaName], [TableName]) AS (
        SELECT [SchemaName], [TableName]
        FROM (VALUES
            (N''dbo'', N''Agreement''),
            (N''dbo'', N''ClientAgreement''),
            (N''dbo'', N''Currency''),
            (N''dbo'', N''OrderItem''),
            (N''dbo'', N''Organization''),
            (N''dbo'', N''Pricing''),
            (N''dbo'', N''PricingProductGroupDiscount''),
            (N''dbo'', N''PricingSourceCutoverState''),
            (N''dbo'', N''PricingSourceDefinition''),
            (N''dbo'', N''PricingSourceSyncState''),
            (N''dbo'', N''Product''),
            (N''dbo'', N''ProductGroupDiscount''),
            (N''dbo'', N''ProductPricing''),
            (N''dbo'', N''ProductPricingSourceSnapshot''),
            (N''dbo'', N''ProductProductGroup'')
        ) dependency ([SchemaName], [TableName])
    ), PriceFunctionRoot AS (
        SELECT priceFunction.[object_id]
        FROM sys.objects priceFunction
        INNER JOIN sys.schemas priceFunctionSchema
            ON priceFunctionSchema.[schema_id] = priceFunction.[schema_id]
        WHERE priceFunctionSchema.[name] = N''dbo''
          AND priceFunction.[name] IN (
              N''GetCalculatedProductPriceWithSharesAndVat'',
              N''GetCalculatedProductPriceForPricingSource'')
          AND priceFunction.[type] IN (N''FN'', N''IF'', N''TF'')
    ), PriceDependency ([ObjectId], [DependencyPath]) AS (
        SELECT root.[object_id],
               CONVERT(nvarchar(max), N''/'' + CONVERT(nvarchar(20), root.[object_id]) + N''/'')
        FROM PriceFunctionRoot root

        UNION ALL

        SELECT dependency.[referenced_id],
               closure.[DependencyPath] + CONVERT(nvarchar(20), dependency.[referenced_id]) + N''/''
        FROM PriceDependency closure
        INNER JOIN sys.sql_expression_dependencies dependency
            ON dependency.[referencing_id] = closure.[ObjectId]
        WHERE dependency.[referenced_id] IS NOT NULL
          AND closure.[DependencyPath] NOT LIKE
              N''%/'' + CONVERT(nvarchar(20), dependency.[referenced_id]) + N''/%''
    ), PriceDependencyEdge AS (
        SELECT dependency.[referenced_id] AS [ReferencedId],
               dependency.[referenced_class_desc] AS [ReferencedClassDesc],
               dependency.[referenced_server_name] AS [ReferencedServerName],
               dependency.[referenced_database_name] AS [ReferencedDatabaseName],
               referencedObject.[object_id] AS [ExistingReferencedObjectId],
               referencedSynonym.[object_id] AS [ReferencedSynonymId],
               namedSynonym.[object_id] AS [NamedSynonymId],
               COALESCE(
                   referencedSynonym.[base_object_name],
                   namedSynonym.[base_object_name]) AS [SynonymBaseObjectName]
        FROM PriceDependency closure
        INNER JOIN sys.sql_expression_dependencies dependency
            ON dependency.[referencing_id] = closure.[ObjectId]
        LEFT JOIN sys.objects referencedObject
            ON referencedObject.[object_id] = dependency.[referenced_id]
        LEFT JOIN sys.synonyms referencedSynonym
            ON referencedSynonym.[object_id] = dependency.[referenced_id]
        LEFT JOIN sys.schemas referencedSchema
            ON referencedSchema.[name] =
               COALESCE(dependency.[referenced_schema_name], N''dbo'')
        LEFT JOIN sys.synonyms namedSynonym
            ON namedSynonym.[schema_id] = referencedSchema.[schema_id]
           AND namedSynonym.[name] = dependency.[referenced_entity_name]
    ), ActualPriceInput ([SchemaName], [TableName]) AS (
        SELECT DISTINCT inputSchema.[name], inputTable.[name]
        FROM PriceDependency dependency
        INNER JOIN sys.tables inputTable
            ON inputTable.[object_id] = dependency.[ObjectId]
        INNER JOIN sys.schemas inputSchema
            ON inputSchema.[schema_id] = inputTable.[schema_id]

        UNION

        SELECT N''dbo'', N''Currency''
    ), PriceModuleObject ([ObjectId]) AS (
        SELECT DISTINCT dependency.[ObjectId]
        FROM PriceDependency dependency
    ), PriceModule AS (
        SELECT moduleSchema.[name] AS [SchemaName],
               moduleObject.[name] AS [ModuleName],
               moduleObject.[type] AS [ModuleType],
               moduleDefinition.[uses_ansi_nulls] AS [UsesAnsiNulls],
               moduleDefinition.[uses_quoted_identifier] AS [UsesQuotedIdentifier],
               moduleDefinition.[is_schema_bound] AS [IsSchemaBound],
               CONVERT(varchar(64), HASHBYTES(
                   N''SHA2_256'',
                   CONVERT(varbinary(max), moduleDefinition.[definition])), 2)
                   AS [DefinitionHash]
        FROM PriceModuleObject dependency
        INNER JOIN sys.objects moduleObject
            ON moduleObject.[object_id] = dependency.[ObjectId]
        INNER JOIN sys.schemas moduleSchema
            ON moduleSchema.[schema_id] = moduleObject.[schema_id]
        INNER JOIN sys.sql_modules moduleDefinition
            ON moduleDefinition.[object_id] = dependency.[ObjectId]
    ), ActualTrackedDependency (
        [SchemaName], [TableName], [ObjectId], [BeginVersion]) AS (
        SELECT trackedSchema.[name],
               trackedTable.[name],
               tracked.[object_id],
               tracked.[begin_version]
        FROM sys.change_tracking_tables tracked
        INNER JOIN sys.tables trackedTable
            ON trackedTable.[object_id] = tracked.[object_id]
        INNER JOIN sys.schemas trackedSchema
            ON trackedSchema.[schema_id] = trackedTable.[schema_id]
    )
    SELECT
        recoveryStatus.[recovery_fork_guid] AS [CurrentRecoveryForkId],
        incarnation.[RecoveryForkId] AS [RecordedRecoveryForkId],
        incarnation.[IncarnationId] AS [RecoveryIncarnationId],
        incarnation.[RepairGeneration],
        CONVERT(bit, CASE WHEN incarnation.[Id] = 1 THEN 1 ELSE 0 END)
            AS [RecoveryIncarnationPresent],
        CONVERT(bit, CASE
            WHEN incarnation.[RecoveryForkId] = recoveryStatus.[recovery_fork_guid] THEN 1
            ELSE 0
        END) AS [RecoveryLineageMatches],
        CONVERT(bit, CASE WHEN EXISTS (
            SELECT 1
            FROM sys.triggers repairFence
            INNER JOIN sys.sql_modules repairFenceDefinition
                ON repairFenceDefinition.[object_id] = repairFence.[object_id]
            WHERE repairFence.[parent_class_desc] = N''DATABASE''
              AND repairFence.[name] = N''GbaPricingChangeTrackingRepairFence''
              AND repairFence.[is_disabled] = 0
              AND repairFenceDefinition.[execute_as_principal_id] =
                  DATABASE_PRINCIPAL_ID(N''dbo'')
              AND repairFenceDefinition.[definition] LIKE
                  N''%GBA_CT_REPAIR_FENCE_V1%''
              AND (SELECT COUNT(*)
                   FROM sys.trigger_events repairFenceEvent
                   WHERE repairFenceEvent.[object_id] = repairFence.[object_id]
                     AND repairFenceEvent.[type_desc] IN (
                         N''CREATE_TABLE'', N''ALTER_TABLE'', N''DROP_TABLE'')) = 3
              AND NOT EXISTS (
                  SELECT 1
                  FROM sys.trigger_events repairFenceEvent
                  WHERE repairFenceEvent.[object_id] = repairFence.[object_id]
                    AND repairFenceEvent.[type_desc] NOT IN (
                        N''CREATE_TABLE'', N''ALTER_TABLE'', N''DROP_TABLE'')))
            THEN 1 ELSE 0 END) AS [RepairFenceValid],
        @CurrentVersion AS [CurrentVersion],
        (SELECT COUNT(*) FROM RequiredDependency) AS [ExpectedTrackedTableCount],
        (SELECT COUNT(*) FROM ActualTrackedDependency) AS [ActualTrackedTableCount],
        (SELECT COUNT(*)
         FROM RequiredDependency required
         LEFT JOIN ActualTrackedDependency tracked
           ON tracked.[SchemaName] = required.[SchemaName]
          AND tracked.[TableName] = required.[TableName]
         WHERE tracked.[TableName] IS NULL) AS [MissingTrackedTableCount],
        (SELECT COUNT(*)
         FROM ActualTrackedDependency tracked
         LEFT JOIN RequiredDependency required
           ON required.[SchemaName] = tracked.[SchemaName]
          AND required.[TableName] = tracked.[TableName]
         WHERE required.[TableName] IS NULL) AS [ExtraTrackedTableCount],
        (SELECT COUNT(*)
         FROM RequiredDependency required
         LEFT JOIN ActualTrackedDependency tracked
           ON tracked.[SchemaName] = required.[SchemaName]
          AND tracked.[TableName] = required.[TableName]
         WHERE tracked.[ObjectId] IS NULL
            OR tracked.[BeginVersion] IS NULL) AS [UnreadableTrackedTableIdentityCount],
        (SELECT tracked.[SchemaName] AS [schemaName],
                tracked.[TableName] AS [tableName],
                tracked.[ObjectId] AS [objectId],
                tracked.[BeginVersion] AS [beginVersion]
         FROM ActualTrackedDependency tracked
         INNER JOIN RequiredDependency required
           ON required.[SchemaName] = tracked.[SchemaName]
          AND required.[TableName] = tracked.[TableName]
         ORDER BY tracked.[SchemaName] COLLATE Latin1_General_100_BIN2,
                  tracked.[TableName] COLLATE Latin1_General_100_BIN2
         FOR JSON PATH, INCLUDE_NULL_VALUES) AS [PricingTrackedTableManifest],
        2 AS [ExpectedPriceFunctionCount],
        (SELECT COUNT(*) FROM PriceFunctionRoot) AS [ActualPriceFunctionCount],
        (SELECT COUNT(*) FROM PriceModule) AS [ActualPricingModuleCount],
        (SELECT COUNT(*) FROM PriceModule WHERE [DefinitionHash] IS NULL)
            AS [UnreadablePricingModuleCount],
        (SELECT [SchemaName] AS [schemaName],
                [ModuleName] AS [moduleName],
                [ModuleType] AS [moduleType],
                [UsesAnsiNulls] AS [usesAnsiNulls],
                [UsesQuotedIdentifier] AS [usesQuotedIdentifier],
                [IsSchemaBound] AS [isSchemaBound],
                [DefinitionHash] AS [definitionHash]
         FROM PriceModule
         ORDER BY [SchemaName] COLLATE Latin1_General_100_BIN2,
                  [ModuleName] COLLATE Latin1_General_100_BIN2,
                  [ModuleType] COLLATE Latin1_General_100_BIN2
         FOR JSON PATH, INCLUDE_NULL_VALUES) AS [PricingModuleHashManifest],
        (SELECT COUNT(*)
         FROM PriceDependencyEdge dependency
         WHERE dependency.[ReferencedId] IS NULL
            OR (dependency.[ReferencedClassDesc] = N''OBJECT_OR_COLUMN''
                AND dependency.[ExistingReferencedObjectId] IS NULL))
            AS [UnresolvedPriceDependencyCount],
        (SELECT COUNT(*)
         FROM PriceDependencyEdge dependency
         WHERE dependency.[ReferencedServerName] IS NOT NULL
            OR dependency.[ReferencedDatabaseName] IS NOT NULL
            OR PARSENAME(dependency.[SynonymBaseObjectName], 4) IS NOT NULL
            OR PARSENAME(dependency.[SynonymBaseObjectName], 3) IS NOT NULL)
            AS [CrossDatabasePriceDependencyCount],
        (SELECT COUNT(*)
         FROM PriceDependencyEdge dependency
         WHERE dependency.[ReferencedSynonymId] IS NOT NULL
            OR dependency.[NamedSynonymId] IS NOT NULL)
            AS [SynonymBackedPriceDependencyCount],
        (SELECT COUNT(*)
         FROM ActualPriceInput input
         LEFT JOIN RequiredDependency required
           ON required.[SchemaName] = input.[SchemaName]
          AND required.[TableName] = input.[TableName]
         WHERE required.[TableName] IS NULL) AS [UnlistedPriceInputCount],
        (SELECT COUNT(*)
         FROM RequiredDependency required
         LEFT JOIN ActualPriceInput input
           ON input.[SchemaName] = required.[SchemaName]
          AND input.[TableName] = required.[TableName]
         WHERE input.[TableName] IS NULL) AS [NonInputManifestEntryCount]
    FROM sys.database_recovery_status recoveryStatus
    LEFT JOIN dbo.PricingChangeTrackingIncarnation incarnation
        ON incarnation.[Id] = 1
    WHERE recoveryStatus.[database_id] = DB_ID()
    OPTION (MAXRECURSION 100);
END;';

EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.RotateEcommercePricingChangeTrackingIncarnation
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MaintenanceRoleName sysname =
        N''GbaPricingChangeTrackingMaintenance'';
    DECLARE @MaintenanceLockResource nvarchar(255) =
        N''gba:ecommerce:pricing-change-tracking:maintenance:''
        + CONVERT(nvarchar(128), DB_NAME());
    DECLARE @MaintenanceLockResult int;
    DECLARE @MaintenanceUnlockResult int;

    EXEC @MaintenanceLockResult = sys.sp_getapplock
        @Resource = @MaintenanceLockResource,
        @LockMode = N''Exclusive'',
        @LockOwner = N''Session'',
        @LockTimeout = 60000,
        @DbPrincipal = @MaintenanceRoleName;

    IF @MaintenanceLockResult < 0
        THROW 54754,
            N''Could not acquire the pricing Change Tracking maintenance lock.'', 1;

    BEGIN TRY
        DECLARE @CurrentRecoveryForkId uniqueidentifier = (
            SELECT recovery.[recovery_fork_guid]
            FROM sys.database_recovery_status recovery
            WHERE recovery.[database_id] = DB_ID()
        );

        IF @CurrentRecoveryForkId IS NULL
            THROW 54752,
                N''Current database recovery-fork identity is unavailable.'', 1;

        BEGIN TRANSACTION;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.PricingChangeTrackingIncarnation WITH (UPDLOCK, HOLDLOCK)
            WHERE [Id] = 1
              AND [RepairGeneration] > 0
              AND [RepairGeneration] < 9223372036854775807)
            THROW 54753,
                N''Pricing Change Tracking recovery-incarnation singleton is missing or exhausted.'', 1;

        UPDATE dbo.PricingChangeTrackingIncarnation
        SET [IncarnationId] = NEWID(),
            [RecoveryForkId] = @CurrentRecoveryForkId,
            [RepairGeneration] = [RepairGeneration] + 1,
            [RotatedAtUtc] = SYSUTCDATETIME(),
            [RotatedBy] = ORIGINAL_LOGIN()
        WHERE [Id] = 1;

        IF @@ROWCOUNT <> 1
            THROW 54753,
                N''Pricing Change Tracking recovery-incarnation singleton is missing.'', 1;

        COMMIT TRANSACTION;

        SELECT [IncarnationId] AS [RecoveryIncarnationId],
               [RecoveryForkId],
               [RepairGeneration],
               [RotatedAtUtc],
               [RotatedBy]
        FROM dbo.PricingChangeTrackingIncarnation
        WHERE [Id] = 1;

        EXEC @MaintenanceUnlockResult = sys.sp_releaseapplock
            @Resource = @MaintenanceLockResource,
            @LockOwner = N''Session'',
            @DbPrincipal = @MaintenanceRoleName;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        EXEC @MaintenanceUnlockResult = sys.sp_releaseapplock
            @Resource = @MaintenanceLockResource,
            @LockOwner = N''Session'',
            @DbPrincipal = @MaintenanceRoleName;
        THROW;
    END CATCH;
END;';

GRANT EXECUTE ON OBJECT::dbo.GetEcommercePricingChangeTrackingState
    TO [GbaPricingChangeTrackingRuntime];
GRANT EXECUTE ON OBJECT::dbo.GetEcommercePricingChangeTrackingState
    TO [GbaPricingChangeTrackingMaintenance];
GRANT EXECUTE ON OBJECT::dbo.RotateEcommercePricingChangeTrackingIncarnation
    TO [GbaPricingChangeTrackingMaintenance];
REVOKE SELECT, INSERT, UPDATE, DELETE
    ON OBJECT::dbo.PricingChangeTrackingIncarnation
    FROM [GbaPricingChangeTrackingMaintenance];
DENY INSERT, UPDATE, DELETE
    ON OBJECT::dbo.PricingChangeTrackingIncarnation
    TO [public];
DENY INSERT, UPDATE, DELETE
    ON OBJECT::dbo.PricingChangeTrackingIncarnation
    TO [GbaPricingChangeTrackingMaintenance];
DENY SELECT, INSERT, UPDATE, DELETE, ALTER, CONTROL
    ON OBJECT::dbo.PricingChangeTrackingIncarnation
    TO [GbaPricingChangeTrackingRuntime];
DENY EXECUTE ON OBJECT::dbo.RotateEcommercePricingChangeTrackingIncarnation
    TO [GbaPricingChangeTrackingRuntime];

SELECT DB_NAME() AS [DatabaseName],
       CHANGE_TRACKING_CURRENT_VERSION() AS [CurrentVersion],
       incarnation.[IncarnationId] AS [RecoveryIncarnationId],
       incarnation.[RecoveryForkId] AS [RecordedRecoveryForkId],
       incarnation.[RepairGeneration],
       @CurrentRecoveryForkId AS [CurrentRecoveryForkId],
       trackingDatabase.[is_auto_cleanup_on] AS [AutoCleanup],
       trackingDatabase.[retention_period] AS [RetentionPeriod],
       trackingDatabase.[retention_period_units_desc] AS [RetentionUnits],
       @ExpectedTrackedTableCount AS [ExpectedTrackedTableCount],
       @ActualTrackedTableCount AS [ActualTrackedTableCount],
       @MissingTrackedTableCount AS [MissingTrackedTableCount],
       @ExtraTrackedTableCount AS [ExtraTrackedTableCount],
       @UnreadableTrackedTableIdentityCount AS [UnreadableTrackedTableIdentityCount],
       (SELECT COUNT(*) FROM @RequiredPriceFunction) AS [ExpectedPriceFunctionCount],
       (SELECT COUNT(*) FROM @PriceFunctionRoot) AS [ActualPriceFunctionCount],
       @ActualPricingModuleCount AS [ActualPricingModuleCount],
       @UnreadablePricingModuleCount AS [UnreadablePricingModuleCount],
       @UnresolvedPriceDependencyCount AS [UnresolvedPriceDependencyCount],
       @CrossDatabasePriceDependencyCount AS [CrossDatabasePriceDependencyCount],
       @SynonymBackedPriceDependencyCount AS [SynonymBackedPriceDependencyCount],
       @UnlistedPriceInputCount AS [UnlistedPriceInputCount],
       @NonInputManifestEntryCount AS [NonInputManifestEntryCount],
       @MaintenanceRoleName AS [MaintenanceRoleName],
       @RuntimeRoleName AS [RuntimeRoleName]
FROM sys.change_tracking_databases AS trackingDatabase
INNER JOIN dbo.PricingChangeTrackingIncarnation incarnation
    ON incarnation.[Id] = 1
WHERE trackingDatabase.[database_id] = DB_ID();

EXEC @MaintenanceUnlockResult = sys.sp_releaseapplock
    @Resource = @MaintenanceLockResource,
    @LockOwner = N'Session',
    @DbPrincipal = @MaintenanceRoleName;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    EXEC @MaintenanceUnlockResult = sys.sp_releaseapplock
        @Resource = @MaintenanceLockResource,
        @LockOwner = N'Session',
        @DbPrincipal = @MaintenanceRoleName;
    THROW;
END CATCH;
