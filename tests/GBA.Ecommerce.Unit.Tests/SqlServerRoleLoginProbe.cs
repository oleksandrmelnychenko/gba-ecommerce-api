using Dapper;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Unit.Tests;

internal sealed class SqlServerRoleLoginProbe : IDisposable {
    private readonly string adminConnectionString;
    private bool disposed;

    public SqlServerRoleLoginProbe(
        string adminConnectionString,
        params string[] databaseRoles) {
        this.adminConnectionString = adminConnectionString;
        LoginName = "GbaPricingRoleProbe_" + Guid.NewGuid().ToString("N");
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
    N'CREATE USER ' + QUOTENAME(@LoginName) + N' FOR LOGIN ' + QUOTENAME(@LoginName);
EXEC sys.sp_executesql @Sql;",
            new { LoginName });

        foreach (string databaseRole in databaseRoles) {
            application.Execute(@"
IF DATABASE_PRINCIPAL_ID(@DatabaseRole) IS NULL
    THROW 54761, N'Requested database role does not exist.', 1;
DECLARE @Sql nvarchar(max) =
    N'ALTER ROLE ' + QUOTENAME(@DatabaseRole) + N' ADD MEMBER '
    + QUOTENAME(@LoginName) + N';';
EXEC sys.sp_executesql @Sql;",
                new { DatabaseRole = databaseRole, LoginName });
        }
    }

    public string LoginName { get; }
    public string Password { get; }
    public string DatabaseName { get; }

    public SqlConnection OpenConnection() {
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
