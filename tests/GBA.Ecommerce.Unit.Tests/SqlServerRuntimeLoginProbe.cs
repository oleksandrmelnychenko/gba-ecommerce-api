using Dapper;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Unit.Tests;

internal sealed class SqlServerRuntimeLoginProbe : IDisposable {
    private readonly string adminConnectionString;
    private bool disposed;

    public SqlServerRuntimeLoginProbe(string adminConnectionString) {
        this.adminConnectionString = adminConnectionString;
        LoginName = "GbaPricingRuntimeProbe_" + Guid.NewGuid().ToString("N");
        Password = "Gba!" + Guid.NewGuid().ToString("N") + "aA1";

        using SqlConnection application = new(adminConnectionString);
        application.Open();
        SqlIntegrationTestEnvironment.EnsureDisposableDatabase(application);
        DatabaseName = application.Database;

        using SqlConnection master = new(
            SqlIntegrationTestEnvironment.WithDatabase(adminConnectionString, "master"));
        master.Open();
        master.Execute(@"
DECLARE @Sql nvarchar(max) =
    N'CREATE LOGIN ' + QUOTENAME(@LoginName)
    + N' WITH PASSWORD = ' + QUOTENAME(@Password, '''')
    + N', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;';
EXEC sys.sp_executesql @Sql;",
            new { LoginName, Password });
        application.Execute(@"
DECLARE @Sql nvarchar(max) =
    N'CREATE USER ' + QUOTENAME(@LoginName) + N' FOR LOGIN ' + QUOTENAME(@LoginName)
    + N'; ALTER ROLE [GbaPricingChangeTrackingRuntime] ADD MEMBER '
    + QUOTENAME(@LoginName) + N';';
EXEC sys.sp_executesql @Sql;",
            new { LoginName });
    }

    public string LoginName { get; }
    public string Password { get; }
    public string DatabaseName { get; }

    public SqlConnection OpenRuntimeConnection() {
        SqlConnectionStringBuilder builder = new(adminConnectionString) {
            IntegratedSecurity = false,
            UserID = LoginName,
            Password = Password,
            InitialCatalog = DatabaseName
        };
        SqlConnection connection = new(builder.ConnectionString);
        connection.Open();
        return connection;
    }

    public void Dispose() {
        if (disposed) return;
        disposed = true;
        SqlConnection.ClearAllPools();

        using SqlConnection application = new(adminConnectionString);
        application.Open();
        application.Execute(@"
IF DATABASE_PRINCIPAL_ID(@LoginName) IS NOT NULL
BEGIN
    DECLARE @Sql nvarchar(max) = N'DROP USER ' + QUOTENAME(@LoginName) + N';';
    EXEC sys.sp_executesql @Sql;
END;",
            new { LoginName });

        using SqlConnection master = new(
            SqlIntegrationTestEnvironment.WithDatabase(adminConnectionString, "master"));
        master.Open();
        master.Execute(@"
IF SUSER_ID(@LoginName) IS NOT NULL
BEGIN
    DECLARE @Sql nvarchar(max) = N'DROP LOGIN ' + QUOTENAME(@LoginName) + N';';
    EXEC sys.sp_executesql @Sql;
END;",
            new { LoginName });
    }
}
