using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Dapper;
using GBA.Search.Sync;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Api.Tests;

[Collection(CheckoutSqlCollection.Name)]
public sealed class ProductSyncChangeProbeSqlTests : IDisposable {
    private const int ProductCount = 1500;

    private const string LegacyDirectChangedProductIdsSql = @"
SELECT p.ID FROM Product p WHERE p.Deleted = 0 AND p.Updated > @Since
UNION
SELECT pon.ProductID FROM ProductOriginalNumber pon INNER JOIN Product p ON p.ID = pon.ProductID AND p.Deleted = 0 WHERE pon.Updated > @Since OR pon.Created > @Since
UNION
SELECT pon.ProductID FROM OriginalNumber on_ INNER JOIN ProductOriginalNumber pon ON pon.OriginalNumberID = on_.ID AND pon.Deleted = 0 INNER JOIN Product p ON p.ID = pon.ProductID AND p.Deleted = 0 WHERE on_.Updated > @Since
UNION
SELECT pa.ProductID FROM ProductAvailability pa INNER JOIN Product p ON p.ID = pa.ProductID AND p.Deleted = 0 WHERE pa.Updated > @Since
UNION
SELECT pp.ProductID FROM ProductPricing pp INNER JOIN Product p ON p.ID = pp.ProductID AND p.Deleted = 0 WHERE pp.Updated > @Since OR pp.Created > @Since
UNION
SELECT ppg.ProductID FROM ProductProductGroup ppg INNER JOIN Product p ON p.ID = ppg.ProductID AND p.Deleted = 0 WHERE ppg.Updated > @Since OR ppg.Created > @Since
UNION
SELECT ps.ProductID FROM ProductSlug ps INNER JOIN Product p ON p.ID = ps.ProductID AND p.Deleted = 0 WHERE ps.Updated > @Since OR ps.Created > @Since";

    private const string LegacyHasGlobalRetailDependencyChangesSql = @"
SELECT CAST(CASE
    WHEN EXISTS (
        SELECT 1
        FROM PricingProductGroupDiscount
        WHERE Updated > @Since OR Created > @Since
    ) OR EXISTS (
        SELECT 1
        FROM ProductGroupDiscount
        WHERE Updated > @Since OR Created > @Since
    )
    THEN 1 ELSE 0
END AS bit)";

    private static readonly XNamespace Showplan = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";

    private readonly CheckoutSqlFixture fixture;
    private readonly string? databaseName;
    private readonly string? connectionString;
    private readonly List<DateTime> stamps = [];

    public ProductSyncChangeProbeSqlTests(CheckoutSqlFixture fixture) {
        this.fixture = fixture;
        if (!fixture.IsConfigured) return;

        string name = "GbaSearchChangeProbeTest_" + Guid.NewGuid().ToString("N");
        using SqlConnection server = fixture.Open();
        server.Execute($"CREATE DATABASE [{name}]");
        databaseName = name;
        connectionString = new SqlConnectionStringBuilder(fixture.ConnectionString) {
            InitialCatalog = name, Pooling = false
        }.ConnectionString;
        try {
            using SqlConnection db = Open();
            db.Execute(SchemaSql, commandTimeout: 300);
            foreach (string batch in SeedBatches(new Random(20261020))) db.Execute(batch, commandTimeout: 300);
            db.Execute(IndexSql, commandTimeout: 300);
        } catch {
            Dispose();
            throw;
        }
    }

    public void Dispose() {
        if (databaseName == null) return;
        SqlConnection.ClearAllPools();
        using SqlConnection server = fixture.Open();
        server.Execute($"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]");
    }

    [ProductSyncSqlFact]
    public void The_sargable_probe_returns_exactly_the_legacy_ids_at_every_timestamp_boundary() {
        using SqlConnection db = Open();
        int compared = 0;
        HashSet<int> distinctSizes = [];
        foreach (DateTime? since in BoundarySinces().Select(value => (DateTime?)value).Append(null)) {
            long[] legacy = db.Query<long>(LegacyDirectChangedProductIdsSql, new { Since = since }).ToArray();
            long[] current = db.Query<long>(ProductSyncRepository.DirectChangedProductIdsSql, new { Since = since }).ToArray();

            Assert.Equal(legacy.Length, legacy.Distinct().Count());
            Assert.Equal(current.Length, current.Distinct().Count());
            Assert.Equal(legacy.OrderBy(id => id), current.OrderBy(id => id));
            Assert.Equal(
                db.QuerySingle<bool>(LegacyHasGlobalRetailDependencyChangesSql, new { Since = since }),
                db.QuerySingle<bool>(ProductSyncRepository.HasGlobalRetailDependencyChangesSql, new { Since = since }));
            distinctSizes.Add(legacy.Length);
            compared++;
        }

        Assert.True(compared > 200, $"Only {compared} boundaries were compared.");
        Assert.True(distinctSizes.Count > 20, $"Only {distinctSizes.Count} distinct result sizes were exercised.");
        Assert.Contains(0, distinctSizes);
    }

    [ProductSyncSqlFact]
    public async Task The_repository_change_set_matches_the_legacy_change_set() {
        ProductSyncRepository repository = new(() => new SqlConnection(connectionString));
        using SqlConnection db = Open();
        int withDependencies = 0;
        foreach (DateTime since in BoundarySinces().Where((_, index) => index % 3 == 0)) {
            HashSet<long> expected = [.. db.Query<long>(LegacyDirectChangedProductIdsSql, new { Since = since })];
            if (db.QuerySingle<bool>(LegacyHasGlobalRetailDependencyChangesSql, new { Since = since })) {
                expected.UnionWith(db.Query<long>(ProductSyncRepository.GlobalRetailDependencyProductIdsSql, new { Since = since }));
                withDependencies++;
            }

            List<long> actual = await repository.GetChangedProductIdsAsync(since);

            Assert.Equal(actual.Count, actual.Distinct().Count());
            Assert.Equal(expected.OrderBy(id => id), actual.OrderBy(id => id));
        }

        Assert.True(withDependencies > 0);
    }

    [ProductSyncSqlFact]
    public void Deleted_rows_null_timestamps_and_orphans_keep_their_legacy_meaning() {
        using SqlConnection db = Open();
        db.Execute("""
            INSERT dbo.Product(ID, Deleted, Updated, Created) VALUES
                (900001, 1, '2030-01-01T00:00:00.0000001', '2030-01-01'),
                (900002, 0, NULL, NULL),
                (900003, 0, '2001-01-01', '2001-01-01');
            INSERT dbo.ProductPricing(ID, ProductID, PricingID, Deleted, Updated, Created) VALUES
                (900001, 900001, 1, 0, '2030-01-01T00:00:00.0000001', NULL),
                (900002, 900002, 1, 1, NULL, '2030-01-01T00:00:00.0000001'),
                (900003, 999999, 1, 0, '2030-01-01T00:00:00.0000001', NULL),
                (900004, NULL, 1, 0, '2030-01-01T00:00:00.0000001', NULL);
            INSERT dbo.ProductSlug(ID, ProductID, Deleted, Updated, Created) VALUES
                (900001, 900003, 1, '2030-01-01T00:00:00.0000001', '2030-01-01T00:00:00.0000001'),
                (900002, 900003, 0, '2030-01-01T00:00:00.0000001', NULL);
            """);

        DateTime since = new(2030, 1, 1);
        long[] current = db.Query<long>(ProductSyncRepository.DirectChangedProductIdsSql, new { Since = since }).OrderBy(id => id).ToArray();

        Assert.Equal(new long[] { 900002, 900003 }, current);
        Assert.Equal(db.Query<long>(LegacyDirectChangedProductIdsSql, new { Since = since }).OrderBy(id => id), current);
        Assert.Empty(db.Query<long>(ProductSyncRepository.DirectChangedProductIdsSql, new { Since = new DateTime(2030, 1, 1, 0, 0, 0, 3) }));
    }

    [ProductSyncSqlFact]
    public void Every_timestamp_branch_seeks_its_index_and_products_are_reached_by_key() {
        using SqlConnection db = Open();
        string plan = EstimatedPlan(db, $"DECLARE @Since datetime = '2026-10-19T12:00:00';\n{ProductSyncRepository.DirectChangedProductIdsSql}");
        (string Operator, string? Table, string? Index)[] paths = AccessPaths(plan);

        foreach (string index in new[] {
                     "IX_Product_Updated", "IX_ProductOriginalNumber_Updated", "IX_ProductOriginalNumber_Created",
                     "IX_OriginalNumber_Updated", "IX_ProductAvailability_Updated", "IX_ProductPricing_Updated",
                     "IX_ProductPricing_Created", "IX_ProductProductGroup_Updated", "IX_ProductProductGroup_Created",
                     "IX_ProductSlug_Updated", "IX_ProductSlug_Created"
                 }) {
            Assert.Contains(paths, path => path.Index == $"[{index}]" && path.Operator == "Index Seek");
        }

        Assert.Contains(paths, path => path.Table == "[Product]" && path.Index == "[PK_Product]" && path.Operator == "Clustered Index Seek");
        Assert.DoesNotContain(paths, path => path.Table == "[Product]" && path.Operator.Contains("Scan", StringComparison.Ordinal));
        Assert.DoesNotContain(XDocument.Parse(plan).Descendants(Showplan + "RelOp"),
            relOp => (string?)relOp.Attribute("PhysicalOp") == "Parallelism");
        Assert.Contains("NonParallelPlanReason=\"MaxDOPSetToOne\"", plan, StringComparison.Ordinal);

        string gate = EstimatedPlan(db, $"DECLARE @Since datetime = '2026-10-19T12:00:00';\n{ProductSyncRepository.HasGlobalRetailDependencyChangesSql}");
        Assert.All(AccessPaths(gate), path => Assert.Equal("Index Seek", path.Operator));
    }

    private SqlConnection Open() {
        SqlConnection connection = new(connectionString);
        connection.Open();
        return connection;
    }

    private IEnumerable<DateTime> BoundarySinces() {
        SortedSet<DateTime> sinces = [new DateTime(1753, 1, 1), new DateTime(1970, 1, 1), new DateTime(9999, 12, 31, 23, 59, 59, 997)];
        foreach (DateTime stamp in stamps) {
            if (stamp.Year < 1754) continue;
            DateTime millisecond = new(stamp.Ticks - stamp.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Unspecified);
            foreach (int offset in new[] { -3, -1, 0, 1, 3 }) sinces.Add(millisecond.AddMilliseconds(offset));
        }

        return sinces;
    }

    private IEnumerable<string> SeedBatches(Random random) {
        DateTime anchor = new(2026, 10, 19, 12, 0, 0);
        long[] tickOffsets = [0, 33333, 66667, 33332, 33334, 66666, 66668, 9999999, 1];
        for (int i = 0; i < 40; i++) {
            DateTime second = anchor.AddSeconds(-random.Next(0, 3 * 86400));
            foreach (long ticks in tickOffsets) {
                if (random.NextDouble() < 0.5) stamps.Add(second.AddTicks(ticks));
            }
        }

        for (int i = 0; i < 30; i++) stamps.Add(anchor.AddSeconds(-random.Next(0, 3 * 86400)).AddTicks(random.Next(0, 10_000_000)));
        stamps.AddRange([new DateTime(1970, 1, 1), new DateTime(1753, 1, 1), new DateTime(2025, 6, 1)]);

        string Stamp() => random.NextDouble() < 0.05
            ? "NULL"
            : "'" + stamps[random.Next(stamps.Count)].ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "'";
        string Bit(double probability) => random.NextDouble() < probability ? "1" : "0";
        string Maybe(string value, double probability = 0.03) => random.NextDouble() < probability ? "NULL" : value;
        string ProductId() {
            double roll = random.NextDouble();
            if (roll < 0.02) return "NULL";
            if (roll < 0.06) return (ProductCount + random.Next(1, 200)).ToString(CultureInfo.InvariantCulture);
            return random.Next(1, ProductCount + 1).ToString(CultureInfo.InvariantCulture);
        }

        List<string> batches = [];
        void Insert(string table, string columns, int count, Func<int, string> row) {
            for (int start = 1; start <= count; start += 500) {
                StringBuilder sql = new($"INSERT dbo.{table}({columns}) VALUES ");
                for (int id = start; id < Math.Min(start + 500, count + 1); id++) {
                    if (id > start) sql.Append(',');
                    sql.Append('(').Append(row(id)).Append(')');
                }

                batches.Add(sql.ToString());
            }
        }

        Insert("Product", "ID, Deleted, Updated, Created", ProductCount, id => $"{id}, {Bit(0.15)}, {Stamp()}, {Stamp()}");
        Insert("OriginalNumber", "ID, Deleted, Updated, Created", 200, id => $"{id}, {Bit(0.1)}, {Stamp()}, {Stamp()}");
        Insert("ProductOriginalNumber", "ID, ProductID, OriginalNumberID, Deleted, Updated, Created", 400,
            id => $"{id}, {ProductId()}, {Maybe(random.Next(1, 210).ToString(CultureInfo.InvariantCulture))}, {Bit(0.2)}, {Stamp()}, {Stamp()}");
        Insert("ProductAvailability", "ID, ProductID, StorageID, Deleted, Updated, Created", 200,
            id => $"{id}, {ProductId()}, {random.Next(1, 4)}, {Bit(0.2)}, {Stamp()}, {Stamp()}");
        Insert("ProductPricing", "ID, ProductID, PricingID, Deleted, Updated, Created", 300,
            id => $"{id}, {ProductId()}, {random.Next(1, 5)}, {Bit(0.2)}, {Stamp()}, {Stamp()}");
        Insert("ProductProductGroup", "ID, ProductID, ProductGroupID, Deleted, Updated, Created", 700,
            id => $"{id}, {ProductId()}, {Maybe(random.Next(1, 150).ToString(CultureInfo.InvariantCulture))}, {Bit(0.2)}, {Stamp()}, {Stamp()}");
        Insert("ProductSlug", "ID, ProductID, Deleted, Updated, Created", 250,
            id => $"{id}, {ProductId()}, {Bit(0.2)}, {Stamp()}, {Stamp()}");
        Insert("PricingProductGroupDiscount", "ID, PricingID, ProductGroupID, Deleted, Updated, Created", 20,
            id => $"{id}, {random.Next(1, 5)}, {Maybe(random.Next(1, 160).ToString(CultureInfo.InvariantCulture), 0.05)}, {Bit(0.2)}, {Stamp()}, {Stamp()}");
        Insert("Client", "ID, IsForRetail, Deleted", 30, id => $"{id}, {Bit(0.5)}, {Bit(0.2)}");
        Insert("ClientAgreement", "ID, ClientID, Deleted", 60,
            id => $"{id}, {Maybe(random.Next(1, 34).ToString(CultureInfo.InvariantCulture), 0.05)}, {Bit(0.2)}");
        Insert("ProductGroupDiscount", "ID, ClientAgreementID, ProductGroupID, Deleted, Updated, Created", 60,
            id => $"{id}, {Maybe(random.Next(1, 64).ToString(CultureInfo.InvariantCulture), 0.05)}, {Maybe(random.Next(1, 160).ToString(CultureInfo.InvariantCulture), 0.05)}, {Bit(0.2)}, {Stamp()}, {Stamp()}");
        return batches;
    }

    private static string EstimatedPlan(SqlConnection connection, string statement) {
        connection.Execute("SET SHOWPLAN_XML ON;");
        try {
            return string.Concat(connection.Query<string>(statement));
        } finally {
            connection.Execute("SET SHOWPLAN_XML OFF;");
        }
    }

    private static (string Operator, string? Table, string? Index)[] AccessPaths(string plan) =>
        XDocument.Parse(plan).Descendants(Showplan + "RelOp")
            .Select(relOp => (
                Operator: (string)relOp.Attribute("PhysicalOp")!,
                Object: relOp.Elements().Where(child => child.Name.LocalName is "IndexScan" or "TableScan")
                    .Elements(Showplan + "Object").FirstOrDefault()))
            .Where(path => path.Object is not null)
            .Select(path => (path.Operator, (string?)path.Object!.Attribute("Table"), (string?)path.Object.Attribute("Index")))
            .ToArray();

    private const string SchemaSql = """
        CREATE TABLE dbo.Product(ID bigint NOT NULL CONSTRAINT PK_Product PRIMARY KEY, Deleted bit NOT NULL, Updated datetime2 NULL, Created datetime2 NULL);
        CREATE TABLE dbo.OriginalNumber(ID bigint NOT NULL CONSTRAINT PK_OriginalNumber PRIMARY KEY, Deleted bit NOT NULL, Updated datetime2 NULL, Created datetime2 NULL);
        CREATE TABLE dbo.ProductOriginalNumber(ID bigint NOT NULL CONSTRAINT PK_ProductOriginalNumber PRIMARY KEY, ProductID bigint NULL, OriginalNumberID bigint NULL, Deleted bit NOT NULL, Updated datetime2 NULL, Created datetime2 NULL);
        CREATE TABLE dbo.ProductAvailability(ID bigint NOT NULL CONSTRAINT PK_ProductAvailability PRIMARY KEY, ProductID bigint NULL, StorageID bigint NOT NULL, Deleted bit NOT NULL, Updated datetime2 NULL, Created datetime2 NULL);
        CREATE TABLE dbo.ProductPricing(ID bigint NOT NULL CONSTRAINT PK_ProductPricing PRIMARY KEY, ProductID bigint NULL, PricingID bigint NOT NULL, Deleted bit NOT NULL, Updated datetime2 NULL, Created datetime2 NULL);
        CREATE TABLE dbo.ProductProductGroup(ID bigint NOT NULL CONSTRAINT PK_ProductProductGroup PRIMARY KEY, ProductID bigint NULL, ProductGroupID bigint NULL, Deleted bit NOT NULL, Updated datetime2 NULL, Created datetime2 NULL);
        CREATE TABLE dbo.ProductSlug(ID bigint NOT NULL CONSTRAINT PK_ProductSlug PRIMARY KEY, ProductID bigint NULL, Deleted bit NOT NULL, Updated datetime2 NULL, Created datetime2 NULL);
        CREATE TABLE dbo.PricingProductGroupDiscount(ID bigint NOT NULL CONSTRAINT PK_PricingProductGroupDiscount PRIMARY KEY, PricingID bigint NOT NULL, ProductGroupID bigint NULL, Deleted bit NOT NULL, Updated datetime2 NULL, Created datetime2 NULL);
        CREATE TABLE dbo.Client(ID bigint NOT NULL CONSTRAINT PK_Client PRIMARY KEY, IsForRetail bit NOT NULL, Deleted bit NOT NULL);
        CREATE TABLE dbo.ClientAgreement(ID bigint NOT NULL CONSTRAINT PK_ClientAgreement PRIMARY KEY, ClientID bigint NULL, Deleted bit NOT NULL);
        CREATE TABLE dbo.ProductGroupDiscount(ID bigint NOT NULL CONSTRAINT PK_ProductGroupDiscount PRIMARY KEY, ClientAgreementID bigint NULL, ProductGroupID bigint NULL, Deleted bit NOT NULL, Updated datetime2 NULL, Created datetime2 NULL);
        """;

    private const string IndexSql = """
        CREATE INDEX IX_Product_Updated ON dbo.Product(Updated);
        CREATE INDEX IX_OriginalNumber_Updated ON dbo.OriginalNumber(Updated);
        CREATE INDEX IX_ProductOriginalNumber_Updated ON dbo.ProductOriginalNumber(Updated) INCLUDE (ProductID);
        CREATE INDEX IX_ProductOriginalNumber_Created ON dbo.ProductOriginalNumber(Created) INCLUDE (ProductID);
        CREATE INDEX IX_ProductOriginalNumber_Deleted_OriginalNumberID ON dbo.ProductOriginalNumber(Deleted, OriginalNumberID);
        CREATE INDEX IX_ProductAvailability_Updated ON dbo.ProductAvailability(Updated) INCLUDE (ProductID);
        CREATE INDEX IX_ProductPricing_Updated ON dbo.ProductPricing(Updated) INCLUDE (ProductID);
        CREATE INDEX IX_ProductPricing_Created ON dbo.ProductPricing(Created) INCLUDE (ProductID);
        CREATE INDEX IX_ProductProductGroup_Updated ON dbo.ProductProductGroup(Updated) INCLUDE (ProductID);
        CREATE INDEX IX_ProductProductGroup_Created ON dbo.ProductProductGroup(Created) INCLUDE (ProductID);
        CREATE INDEX IX_ProductSlug_Updated ON dbo.ProductSlug(Updated) INCLUDE (ProductID);
        CREATE INDEX IX_ProductSlug_Created ON dbo.ProductSlug(Created) INCLUDE (ProductID);
        CREATE INDEX IX_ProductGroupDiscount_Updated ON dbo.ProductGroupDiscount(Updated) INCLUDE (ClientAgreementID, ProductGroupID);
        CREATE INDEX IX_ProductGroupDiscount_Created ON dbo.ProductGroupDiscount(Created) INCLUDE (ClientAgreementID, ProductGroupID);
        CREATE INDEX IX_PricingProductGroupDiscount_Updated ON dbo.PricingProductGroupDiscount(Updated) INCLUDE (ProductGroupID);
        CREATE INDEX IX_PricingProductGroupDiscount_Created ON dbo.PricingProductGroupDiscount(Created) INCLUDE (ProductGroupID);
        UPDATE STATISTICS dbo.Product WITH FULLSCAN;
        """;
}

public sealed class ProductSyncSqlFactAttribute : FactAttribute {
    public ProductSyncSqlFactAttribute() {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(CheckoutSqlFixture.ConnectionStringVariable)))
            Skip = "A disposable SQL database is required for the search change probe.";
    }
}
