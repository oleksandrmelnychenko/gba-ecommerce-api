using Dapper;
using GBA.Services.Services.Products;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Unit.Tests;

internal sealed record SqlServerRecoveryForkProbeResult(
    Guid BackupRecoveryForkId,
    Guid RestoredRecoveryForkId,
    Guid BackupIncarnationId,
    Guid MutatedIncarnationId,
    Guid RotatedRestoredIncarnationId,
    long BackupChangeTrackingVersion,
    long MutatedChangeTrackingVersion,
    long RestoredChangeTrackingVersion,
    bool MutationWasRolledBack,
    PricingChangeTrackingStatus BeforeRotation,
    PricingChangeTrackingStatus AfterRotation);

internal static class SqlServerRecoveryForkProbe {
    public static SqlServerRecoveryForkProbeResult Run(
        string connectionString,
        string rotationScript) {
        string databaseName;
        string backupPath;
        Guid backupFork;
        Guid backupIncarnation;
        long backupVersion;
        Guid mutatedIncarnation;
        long mutatedVersion;

        using (SqlConnection connection = new(connectionString)) {
            connection.Open();
            SqlIntegrationTestEnvironment.EnsureDisposableDatabase(connection);
            databaseName = connection.Database;
            string backupDirectory = connection.QuerySingle<string>(
                "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'))");
            backupPath = backupDirectory.TrimEnd('/', '\\')
                         + "/gba-pricing-recovery-" + Guid.NewGuid().ToString("N") + ".bak";

            connection.Execute(@"
INSERT INTO dbo.Product ([ID], [NetUID], [Deleted], [Updated], [Payload])
VALUES (900001, NEWID(), 0, SYSUTCDATETIME(), 1);");
            backupFork = ReadRecoveryFork(connection);
            backupIncarnation = ReadIncarnation(connection);
            backupVersion = ReadChangeTrackingVersion(connection);
            connection.Execute(
                $"BACKUP DATABASE [{EscapeIdentifier(databaseName)}] TO DISK = @BackupPath "
                + "WITH INIT, COPY_ONLY, CHECKSUM;",
                new { BackupPath = backupPath },
                commandTimeout: 180);

            connection.Execute(@"
UPDATE dbo.Product SET [Payload] = 2 WHERE [ID] = 900001;
INSERT INTO dbo.Product ([ID], [NetUID], [Deleted], [Updated], [Payload])
VALUES (900002, NEWID(), 0, SYSUTCDATETIME(), 2);");
            connection.Execute(rotationScript, commandTimeout: 120);
            mutatedIncarnation = ReadIncarnation(connection);
            mutatedVersion = ReadChangeTrackingVersion(connection);
        }

        SqlConnection.ClearAllPools();
        string masterConnectionString = SqlIntegrationTestEnvironment.WithDatabase(
            connectionString,
            "master");
        try {
            using (SqlConnection master = new(masterConnectionString)) {
                master.Open();
                master.Execute(
                    $"ALTER DATABASE [{EscapeIdentifier(databaseName)}] "
                    + "SET SINGLE_USER WITH ROLLBACK IMMEDIATE;",
                    commandTimeout: 120);
                try {
                    master.Execute(
                        $"RESTORE DATABASE [{EscapeIdentifier(databaseName)}] "
                        + "FROM DISK = @BackupPath WITH REPLACE, RECOVERY;",
                        new { BackupPath = backupPath },
                        commandTimeout: 180);
                } finally {
                    master.Execute(
                        $"ALTER DATABASE [{EscapeIdentifier(databaseName)}] SET MULTI_USER;",
                        commandTimeout: 120);
                }
            }

            SqlConnection.ClearAllPools();
            using SqlConnection restored = new(connectionString);
            restored.Open();
            Guid restoredFork = ReadRecoveryFork(restored);
            long restoredVersion = ReadChangeTrackingVersion(restored);
            bool mutationRolledBack = restored.QuerySingle<int>(@"
SELECT CASE WHEN
    (SELECT [Payload] FROM dbo.Product WHERE [ID] = 900001) = 1
    AND NOT EXISTS (SELECT 1 FROM dbo.Product WHERE [ID] = 900002)
THEN 1 ELSE 0 END;") == 1;
            SqlPricingDependencyRevisionProvider provider = new();
            PricingChangeTrackingStatus beforeRotation = provider.GetStatus(restored);
            restored.Execute(rotationScript, commandTimeout: 120);
            PricingChangeTrackingStatus afterRotation = provider.GetStatus(restored);
            Guid rotatedRestoredIncarnation = ReadIncarnation(restored);

            return new SqlServerRecoveryForkProbeResult(
                backupFork,
                restoredFork,
                backupIncarnation,
                mutatedIncarnation,
                rotatedRestoredIncarnation,
                backupVersion,
                mutatedVersion,
                restoredVersion,
                mutationRolledBack,
                beforeRotation,
                afterRotation);
        } finally {
            try {
                using SqlConnection master = new(masterConnectionString);
                master.Open();
                master.Execute(
                    "EXEC master.dbo.xp_delete_file 0, @BackupPath, N'BAK';",
                    new { BackupPath = backupPath });
            } catch (SqlException) {
                // Probe correctness does not depend on best-effort backup-file cleanup.
            }
        }
    }

    private static Guid ReadRecoveryFork(SqlConnection connection) =>
        connection.QuerySingle<Guid>(@"
SELECT [recovery_fork_guid]
FROM sys.database_recovery_status
WHERE [database_id] = DB_ID();");

    private static Guid ReadIncarnation(SqlConnection connection) =>
        connection.QuerySingle<Guid>(@"
SELECT [IncarnationId]
FROM dbo.PricingChangeTrackingIncarnation
WHERE [Id] = 1;");

    private static long ReadChangeTrackingVersion(SqlConnection connection) =>
        connection.QuerySingle<long>("SELECT CHANGE_TRACKING_CURRENT_VERSION();");

    private static string EscapeIdentifier(string value) =>
        value.Replace("]", "]]", StringComparison.Ordinal);
}
