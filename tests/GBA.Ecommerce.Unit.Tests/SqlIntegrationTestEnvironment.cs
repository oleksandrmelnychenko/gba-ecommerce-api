using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Unit.Tests;

internal static class SqlIntegrationTestEnvironment {
    public const string ConnectionStringEnvironmentVariable =
        "GBA_ECOMMERCE_SQL_INTEGRATION_CONNECTION_STRING";
    public const string RequiredEnvironmentVariable =
        "GBA_ECOMMERCE_SQL_INTEGRATION_REQUIRED";
    public const string DisposableDatabaseNamePrefix = "GbaEcommerceRevisionTests";

    public static string? GetConnectionString() {
        string? connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);
        bool required = string.Equals(
            Environment.GetEnvironmentVariable(RequiredEnvironmentVariable),
            "1",
            StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(connectionString) && required) {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} is required when "
                + $"{RequiredEnvironmentVariable}=1.");
        }

        return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString;
    }

    public static void EnsureDisposableDatabase(SqlConnection connection) {
        if (!connection.Database.StartsWith(
                DisposableDatabaseNamePrefix,
                StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} must target a disposable database "
                + $"whose name starts with '{DisposableDatabaseNamePrefix}'.");
        }
    }

    public static string WithDatabase(string connectionString, string database) {
        SqlConnectionStringBuilder builder = new(connectionString) {
            InitialCatalog = database
        };
        return builder.ConnectionString;
    }
}
