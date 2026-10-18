using System;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using Microsoft.Data.SqlClient;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

[CollectionDefinition(Name)]
public sealed class CheckoutSqlCollection : ICollectionFixture<CheckoutSqlFixture> {
    public const string Name = "checkout-sql";
}

public sealed class CheckoutSqlFixture : IDisposable {
    public const string ConnectionStringVariable = "ECOM_TEST_CHECKOUT_SQL";

    private const string LedgerScriptPath = "db/ecommerce-sales-creation-ledger.sql";

    private readonly string? _serverPath;

    public CheckoutSqlFixture() {
        ConnectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(ConnectionString)) return;

        ConnectionString = new SqlConnectionStringBuilder(ConnectionString) {
            Pooling = true,
            Enlist = true
        }.ConnectionString;

        using SqlConnection connection = new(ConnectionString);
        connection.Open();

        string databaseName = connection.ExecuteScalar<string?>("SELECT DB_NAME();") ?? string.Empty;
        if (databaseName.IndexOf("Test", StringComparison.OrdinalIgnoreCase) < 0 ||
            databaseName.Equals("ConcordDb_V5", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Refusing to run checkout SQL tests against '{databaseName}'. Point {ConnectionStringVariable} at a disposable test database.");

        foreach (string batch in SplitBatches(File.ReadAllText(Path.Combine(FindRepositoryRoot(), LedgerScriptPath))))
            connection.Execute(batch);

        connection.Execute("""
            IF OBJECT_ID(N'dbo.CheckoutAtomicityProbe', N'U') IS NULL
                CREATE TABLE dbo.CheckoutAtomicityProbe (
                    ID bigint IDENTITY(1, 1) NOT NULL PRIMARY KEY,
                    OperationNetUid uniqueidentifier NOT NULL,
                    Note nvarchar(100) NOT NULL);
            """);

        HasConcordSchema = connection.ExecuteScalar<int>(
            "SELECT CASE WHEN OBJECT_ID(N'dbo.OrderItem', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Sale', N'U') IS NOT NULL THEN 1 ELSE 0 END;") == 1;

        _serverPath = Path.Combine(Path.GetTempPath(), $"gba-checkout-sql-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_serverPath);
        NoltFolderManager.InitializeFolderManager(_serverPath);
        WriteCrmConfiguration();

        IsConfigured = true;
    }

    public string? ConnectionString { get; }

    public bool IsConfigured { get; }

    public bool HasConcordSchema { get; }

    public IDbConnectionFactory ConnectionFactory => new SqlTestConnectionFactory(ConnectionString!);

    public SqlConnection Open() {
        SqlConnection connection = new(ConnectionString);
        connection.Open();
        return connection;
    }

    public void WriteCrmConfiguration() {
        File.WriteAllText(
            NoltFolderManager.GetEcommerceCrmConfigJsonFilePath(),
            "{\"CrmServerUrl\":\"http://crm.checkout.test\",\"CrmServerUrlRelease\":\"http://crm.checkout.test\"}");
    }

    public void RemoveCrmConfiguration() {
        File.Delete(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath());
    }

    public void Dispose() {
        if (_serverPath != null && Directory.Exists(_serverPath))
            Directory.Delete(_serverPath, recursive: true);
    }

    private static string[] SplitBatches(string script) {
        return Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToArray();
    }

    private static string FindRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null) {
            if (File.Exists(Path.Combine(directory.FullName, LedgerScriptPath))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The gba-ecommerce-api repository root was not found.");
    }

    private sealed class SqlTestConnectionFactory(string connectionString) : IDbConnectionFactory {
        public IDbConnection NewSqlConnection() => new SqlConnection(connectionString);

        public IDbConnection NewDataAnalyticSqlConnection() => throw new NotSupportedException();

        public IDbConnection NewIdentitySqlConnection() => throw new NotSupportedException();

        public IDbConnection NewFenixOneCSqlConnection() => throw new NotSupportedException();

        public IDbConnection NewAmgOneCSqlConnection() => throw new NotSupportedException();
    }
}

public sealed class RecordingCrmHandler : HttpMessageHandler {
    public ConcurrentQueue<string> Paths { get; } = new();

    public IHttpClientFactory Factory() {
        Mock<IHttpClientFactory> factory = new();
        factory
            .Setup(candidate => candidate.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(this, disposeHandler: false));
        return factory.Object;
    }

    public int Count(string pathFragment) {
        return Paths.Count(path => path.Contains(pathFragment, StringComparison.Ordinal));
    }

    public async Task<bool> WaitForAsync(string pathFragment, int expected, TimeSpan timeout) {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline) {
            if (Count(pathFragment) >= expected) return true;
            await Task.Delay(25);
        }

        return Count(pathFragment) >= expected;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        Paths.Enqueue(request.RequestUri!.PathAndQuery);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{}")
        });
    }
}
