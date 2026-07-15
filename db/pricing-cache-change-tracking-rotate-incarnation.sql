-- Run this command after every database restore, point-in-time recovery, or
-- data-loss failover, before application traffic is enabled. It explicitly
-- acknowledges the current recovery fork and invalidates every prior pricing
-- cache and Elasticsearch pricing revision.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IS NULL OR DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 54740, N'Run pricing recovery-incarnation rotation in the application database.', 1;

DECLARE @MaintenanceRoleName sysname = N'GbaPricingChangeTrackingMaintenance';

IF DATABASE_PRINCIPAL_ID(@MaintenanceRoleName) IS NULL
    THROW 54756, N'Run db/pricing-cache-change-tracking.sql before rotating the recovery incarnation.', 1;

IF OBJECT_ID(
        N'dbo.RotateEcommercePricingChangeTrackingIncarnation',
        N'P') IS NULL
    THROW 54751,
        N'Run db/pricing-cache-change-tracking.sql before rotating the recovery incarnation.',
        1;

EXEC dbo.RotateEcommercePricingChangeTrackingIncarnation;
