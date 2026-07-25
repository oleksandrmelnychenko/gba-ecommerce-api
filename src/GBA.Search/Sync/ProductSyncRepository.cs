using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GBA.Domain.EntityHelpers;
using GBA.Search.Models;
using GBA.Services.Services.Products;

namespace GBA.Search.Sync;

public interface IProductSyncRepository {
    Task<PricingDependencyRevisions> GetPricingDependencyRevisionsAsync();
    Task<int> CountLiveProductIdsAsync(IReadOnlyCollection<long> productIds);
    Task<RetailConfigurationSnapshot> GetRetailConfigurationSnapshotAsync();
    Task<ProductIdSyncPlan> GetProductIdSyncPlanAsync(
        DateTime since,
        string? acknowledgedConfigurationSignature);
    Task<ProductProjectionBatch> GetProductProjectionBatchAsync(
        long afterProductId,
        int take,
        string? expectedConfigurationSignature = null);
    Task<ProductIdSyncBatch> GetChangedProductIdBatchAsync(
        DateTime since,
        string? acknowledgedConfigurationSignature,
        long afterProductId,
        int take);
    Task<List<long>> GetDeletedProductIdBatchAsync(DateTime since, long afterProductId, int take);
    Task<ProductProjectionSnapshot> GetProductProjectionByIdsAsync(
        IReadOnlyCollection<long> ids,
        string? expectedConfigurationSignature = null);
    Task<bool> IsRetailConfigurationCurrentAsync(string expectedConfigurationSignature);
    Task<Dictionary<long, List<string>>> GetOriginalNumbersForProductsAsync(IEnumerable<long> productIds);
}

public sealed class ProductSyncRepository(Func<IDbConnection> connectionFactory) : IProductSyncRepository {
    private const int ProductIdsBatchSize = 2000;
    private const int MaxCatalogScopesPerProduct = 1024;
    private const string CatalogConfigurationSignature = "web-catalog-live-sql-v1";
    private static readonly IPricingDependencyRevisionProvider PricingRevisionProvider =
        new SqlPricingDependencyRevisionProvider();

    public Task<PricingDependencyRevisions> GetPricingDependencyRevisionsAsync() {
        using IDbConnection connection = connectionFactory();
        connection.Open();
        return Task.FromResult(PricingRevisionProvider.Get(connection));
    }

    private static readonly string ProductProjectionSql = @"
	SELECT
    p.ID AS Id,
    p.NetUID AS NetUid,
    p.VendorCode,
    ISNULL(p.SearchVendorCode, '') AS SearchVendorCode,
    ISNULL(p.Name, '') AS Name,
    ISNULL(p.NameUA, '') AS NameUA,
    ISNULL(p.Description, '') AS Description,
    ISNULL(p.DescriptionUA, '') AS DescriptionUA,
    ISNULL(p.MainOriginalNumber, '') AS MainOriginalNumber,
    ISNULL(p.Size, '') AS Size,
    LTRIM(RTRIM(CONCAT(ISNULL(p.SynonymsUA, ''), ' ', ISNULL(p.SearchSynonymsUA, '')))) AS Synonyms,
    ISNULL(p.SearchName, '') AS SearchName,
    ISNULL(p.SearchNameUA, '') AS SearchNameUA,
    ISNULL(p.SearchDescription, '') AS SearchDescription,
    ISNULL(p.SearchDescriptionUA, '') AS SearchDescriptionUA,
    ISNULL(p.SearchSize, '') AS SearchSize,
    ISNULL(p.PackingStandard, '') AS PackingStandard,
    ISNULL(p.OrderStandard, '') AS OrderStandard,
    ISNULL(p.UCGFEA, '') AS Ucgfea,
    ISNULL(p.Volume, '') AS Volume,
    ISNULL(p.[Top], '') AS [Top],
    ISNULL(p.Weight, 0) AS Weight,
    p.HasAnalogue,
    p.HasComponent,
    p.HasImage,
    ISNULL(p.Image, '') AS Image,
    p.MeasureUnitID AS MeasureUnitId,
    CAST(0 AS float) AS AvailableQtyUk,
    CAST(0 AS float) AS AvailableQtyUkVat,
    CAST(0 AS float) AS AvailableQtyPl,
    CAST(0 AS float) AS AvailableQtyPlVat,
    CAST(0 AS float) AS AvailableQty,
    CAST(0 AS bit) AS HasNonVatCatalogAvailability,
    CAST(0 AS bit) AS HasVatCatalogAvailability,
    CAST(CASE WHEN (" + ProductSourceIdentitySql.CanonicalExpression("p", "fenix") + @")
        <> '' THEN 1 ELSE 0 END AS bit)
        AS HasNonVatCatalogSource,
    CAST(CASE WHEN (" + ProductSourceIdentitySql.CanonicalExpression("p", "fenix") + @")
        <> '' THEN 1 ELSE 0 END AS bit)
        AS HasVatCatalogSource,
    p.IsForWeb,
    p.IsForSale,
    p.IsForZeroSale,
    " + ProductSourceIdentitySql.CanonicalExpression("p", "fenix") + @" AS ProductSourceFenix,
    " + ProductSourceIdentitySql.CanonicalExpression("p", "amg") + @" AS ProductSourceAmg,
    CAST(CASE WHEN (" + ProductSourceIdentitySql.CanonicalExpression("p", "fenix") + @") <> ''
        AND " + ProductSourceIdentitySql.CanonicalProductForSourcePredicate("p", "fenix") + @"
        THEN 1 ELSE 0 END AS bit) AS IsCanonicalFenix,
    CAST(CASE WHEN (" + ProductSourceIdentitySql.CanonicalExpression("p", "amg") + @") <> ''
        AND " + ProductSourceIdentitySql.CanonicalProductForSourcePredicate("p", "amg") + @"
        THEN 1 ELSE 0 END AS bit) AS IsCanonicalAmg,
    N'[]' AS CatalogScopesJson,
    ISNULL(ps.ID, 0) AS SlugId,
    ISNULL(ps.NetUID, '00000000-0000-0000-0000-000000000000') AS SlugNetUid,
    ISNULL(ps.Url, '') AS SlugUrl,
    ISNULL(ps.Locale, '') AS SlugLocale,
    CAST(0 AS bigint) AS CatalogOrganizationIdNonVat,
    CAST(0 AS bigint) AS CatalogOrganizationIdVat,
    N'' AS CatalogAgreementSourceNonVat,
    N'' AS CatalogAgreementSourceVat,
    CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier)
        AS CatalogAgreementNetUidNonVat,
    CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier)
        AS CatalogAgreementNetUidVat,
    CAST(0 AS bigint) AS CatalogPricingIdNonVat,
    CAST(0 AS bigint) AS CatalogPricingIdVat,
    CAST(0 AS bigint) AS CatalogCurrencyIdNonVat,
    CAST(0 AS bigint) AS CatalogCurrencyIdVat,
    CAST(0 AS decimal(19, 4)) AS RetailPrice,
    CAST(0 AS decimal(19, 4)) AS RetailPriceVat,
    N'UAH' AS RetailCurrencyCode,
    N'UAH' AS RetailCurrencyCodeVat,
    p.Updated
FROM Product p";

    private static readonly string ProductProjectionTailSql = @"
OUTER APPLY (
    SELECT TOP (1) slug.ID, slug.NetUID, slug.Url, slug.Locale
    FROM ProductSlug slug
    WHERE slug.ProductID = p.ID
      AND slug.Locale = 'uk'
      AND slug.Deleted = 0
    ORDER BY slug.ID
) ps
WHERE p.Deleted = 0
  AND p.IsForWeb = 1
  AND " + ProductSourceIdentitySql.AnyCanonicalSourcePredicate("p") + @"
ORDER BY p.ID";

    private const string DirectChangedProductIdsSql = @"
SELECT p.ID
FROM Product p
WHERE p.Updated > @Since OR p.Created > @Since

UNION

SELECT pon.ProductID
FROM ProductOriginalNumber pon
INNER JOIN Product p ON p.ID = pon.ProductID AND p.Deleted = 0
WHERE pon.Updated > @Since OR pon.Created > @Since

UNION

SELECT pon.ProductID
FROM OriginalNumber originalNumber
INNER JOIN ProductOriginalNumber pon ON pon.OriginalNumberID = originalNumber.ID
INNER JOIN Product p ON p.ID = pon.ProductID AND p.Deleted = 0
WHERE originalNumber.Updated > @Since OR originalNumber.Created > @Since

UNION

SELECT slug.ProductID
FROM ProductSlug slug
INNER JOIN Product p ON p.ID = slug.ProductID AND p.Deleted = 0
WHERE slug.Updated > @Since OR slug.Created > @Since";

    private const string RetailConfigurationSignatureSql = @"
SELECT '" + CatalogConfigurationSignature + @"' AS Signature, CAST(1 AS bit) AS IsValid";

    public async Task<List<ProductSyncData>> GetAllProductsAsync() {
        ProductSyncSnapshot snapshot = await GetProductSnapshotAsync();
        return snapshot.Products;
    }

    public async Task<RetailConfigurationSnapshot> GetRetailConfigurationSnapshotAsync() {
        using IDbConnection connection = connectionFactory();
        connection.Open();
        return await GetRetailConfigurationAsync(connection);
    }

    public async Task<ProductProjectionBatch> GetProductProjectionBatchAsync(
        long afterProductId,
        int take,
        string? expectedConfigurationSignature = null) {
        int boundedTake = Math.Clamp(take, 1, ProductIdsBatchSize);
        using IDbConnection connection = connectionFactory();
        connection.Open();

        RetailConfigurationSnapshot configuration = await GetRetailConfigurationAsync(connection);
        if (!HasValidRetailConfiguration(configuration)
            || (!string.IsNullOrWhiteSpace(expectedConfigurationSignature)
                && !string.Equals(
                    expectedConfigurationSignature,
                    configuration.Signature,
                    StringComparison.Ordinal))) {
            return ProductProjectionBatch.Invalid(configuration.Signature, afterProductId);
        }

        const string candidateIdsSql = @"
SELECT TOP (@Take) p.ID
FROM Product p
WHERE p.Deleted = 0
  AND p.IsForWeb = 1
  AND p.ID > @AfterProductId
ORDER BY p.ID";
        List<long> candidateIds = (await connection.QueryAsync<long>(
                candidateIdsSql,
                new { Take = boundedTake, AfterProductId = afterProductId },
                commandTimeout: 120))
            .AsList();
        if (candidateIds.Count == 0) {
            return new ProductProjectionBatch(
                [],
                configuration.Signature,
                HasValidRetailConfiguration: true,
                LastScannedProductId: afterProductId,
                ScannedCount: 0,
                HasMore: false);
        }

        const string requestedProductIdsCte = @"
RequestedProductIds AS (
    SELECT p.ID FROM Product p WHERE p.Deleted = 0 AND p.ID IN @Ids
)";
        string projectionSql = BuildProductProjectionSql(
            requestedProductIdsCte,
            "INNER JOIN RequestedProductIds requested ON requested.ID = p.ID");
        List<ProductSyncData> products = await QueryUniqueProductsAsync(
            connection,
            projectionSql,
            new { Ids = candidateIds },
            commandTimeout: 180);

        RetailConfigurationSnapshot completedConfiguration = await GetRetailConfigurationAsync(connection);
        bool configurationStayedValid = HasValidRetailConfiguration(completedConfiguration)
                                        && string.Equals(
                                            configuration.Signature,
                                            completedConfiguration.Signature,
                                            StringComparison.Ordinal);
        return configurationStayedValid
            ? new ProductProjectionBatch(
                products,
                configuration.Signature,
                HasValidRetailConfiguration: true,
                LastScannedProductId: candidateIds[^1],
                ScannedCount: candidateIds.Count,
                HasMore: candidateIds.Count == boundedTake)
            : ProductProjectionBatch.Invalid(completedConfiguration.Signature, afterProductId);
    }

    public async Task<ProductSyncSnapshot> GetProductSnapshotAsync() {
        using IDbConnection connection = connectionFactory();
        connection.Open();

        RetailConfigurationSnapshot configuration = await GetRetailConfigurationAsync(connection);
        if (!HasValidRetailConfiguration(configuration)) {
            return new ProductSyncSnapshot(
                [],
                configuration.Signature,
                HasValidRetailConfiguration: false);
        }

        List<ProductSyncData> products = await QueryUniqueProductsAsync(
            connection,
            BuildProductProjectionSql(),
            commandTimeout: 600);

        RetailConfigurationSnapshot completedConfiguration = await GetRetailConfigurationAsync(connection);
        if (!HasValidRetailConfiguration(completedConfiguration)
            || !string.Equals(
                configuration.Signature,
                completedConfiguration.Signature,
                StringComparison.Ordinal)) {
            return new ProductSyncSnapshot(
                [],
                completedConfiguration.Signature,
                HasValidRetailConfiguration: false);
        }

        return new ProductSyncSnapshot(
            products,
            configuration.Signature,
            HasValidRetailConfiguration: true);
    }

    public async Task<List<ProductSyncData>> GetChangedProductsAsync(DateTime since) {
        using IDbConnection connection = connectionFactory();
        connection.Open();

        RetailConfigurationSnapshot configuration = await GetRetailConfigurationAsync(connection);
        if (!HasValidRetailConfiguration(configuration)) return [];

        List<long> ids = (await connection.QueryAsync<long>(
                BuildChangedProductIdsSql(),
                new { Since = since },
                commandTimeout: 120))
            .AsList();
        if (ids.Count == 0) return [];

        const string changedProductIdsCte = @"
ChangedProductIds AS (
    SELECT p.ID FROM Product p WHERE p.ID IN @Ids
)";
        string projectionSql = BuildProductProjectionSql(
            changedProductIdsCte,
            "INNER JOIN ChangedProductIds changed ON changed.ID = p.ID",
            requireCatalogEligibility: false);
        List<ProductSyncData> products = [];
        foreach (long[] batch in ids.Chunk(ProductIdsBatchSize)) {
            products.AddRange(await QueryUniqueProductsAsync(
                connection,
                projectionSql,
                new { Ids = batch },
                commandTimeout: 120));
        }

        return products.DistinctBy(product => product.Id).ToList();
    }

    public async Task<List<long>> GetChangedProductIdsAsync(DateTime since) {
        ProductIdSyncPlan plan = await GetProductIdSyncPlanAsync(since, null);
        return plan.ProductIds;
    }

    public async Task<ProductIdSyncPlan> GetProductIdSyncPlanAsync(
        DateTime since,
        string? acknowledgedConfigurationSignature) {
        using IDbConnection connection = connectionFactory();
        connection.Open();

        RetailConfigurationSnapshot configuration = await GetRetailConfigurationAsync(connection);
        bool forceFullRefresh = !HasValidRetailConfiguration(configuration)
                                || !string.Equals(
                                    acknowledgedConfigurationSignature,
                                    configuration.Signature,
                                    StringComparison.Ordinal);

        if (!HasValidRetailConfiguration(configuration)) {
            return new ProductIdSyncPlan(
                [],
                configuration.Signature,
                RequiresFullReconciliation: true,
                HasValidRetailConfiguration: false);
        }

        if (forceFullRefresh) {
            return new ProductIdSyncPlan(
                [],
                configuration.Signature,
                RequiresFullReconciliation: true,
                HasValidRetailConfiguration: true);
        }

        IEnumerable<long> ids = await connection.QueryAsync<long>(
            BuildChangedProductIdsSql(),
            new { Since = since },
            commandTimeout: 120);

        return new ProductIdSyncPlan(
            ids.AsList(),
            configuration.Signature,
            RequiresFullReconciliation: forceFullRefresh,
            HasValidRetailConfiguration: true);
    }

    public async Task<ProductIdSyncBatch> GetChangedProductIdBatchAsync(
        DateTime since,
        string? acknowledgedConfigurationSignature,
        long afterProductId,
        int take) {
        int boundedTake = Math.Clamp(take, 1, ProductIdsBatchSize);
        using IDbConnection connection = connectionFactory();
        connection.Open();

        RetailConfigurationSnapshot configuration = await GetRetailConfigurationAsync(connection);
        bool valid = HasValidRetailConfiguration(configuration);
        bool requiresFull = !valid
                            || !string.Equals(
                                acknowledgedConfigurationSignature,
                                configuration.Signature,
                                StringComparison.Ordinal);
        if (!valid || requiresFull) {
            return new ProductIdSyncBatch(
                [],
                configuration.Signature,
                requiresFull,
                valid,
                afterProductId,
                HasMore: false);
        }

        string sql = BuildChangedProductIdBatchSql();
        List<long> ids = (await connection.QueryAsync<long>(
                sql,
                new {
                    Since = since,
                    Take = boundedTake,
                    AfterProductId = afterProductId
                },
                commandTimeout: 120))
            .AsList();

        RetailConfigurationSnapshot completedConfiguration = await GetRetailConfigurationAsync(connection);
        bool configurationStayedValid = HasValidRetailConfiguration(completedConfiguration)
                                        && string.Equals(
                                            configuration.Signature,
                                            completedConfiguration.Signature,
                                            StringComparison.Ordinal);
        return new ProductIdSyncBatch(
            configurationStayedValid ? ids : [],
            completedConfiguration.Signature,
            RequiresFullReconciliation: !configurationStayedValid,
            HasValidRetailConfiguration: configurationStayedValid,
            LastScannedProductId: ids.Count == 0 ? afterProductId : ids[^1],
            HasMore: configurationStayedValid && ids.Count == boundedTake);
    }

    public async Task<List<ProductSyncData>> GetProductsByIdsAsync(IReadOnlyCollection<long> ids) {
        ProductProjectionSnapshot snapshot = await GetProductProjectionByIdsAsync(ids);
        return snapshot.Products;
    }

    public async Task<ProductProjectionSnapshot> GetProductProjectionByIdsAsync(
        IReadOnlyCollection<long> ids,
        string? expectedConfigurationSignature = null) {
        if (ids.Count == 0) {
            return new ProductProjectionSnapshot(
                [],
                expectedConfigurationSignature ?? string.Empty,
                HasValidRetailConfiguration: true);
        }

        const string changedProductIdsCte = @"
ChangedProductIds AS (
    SELECT p.ID FROM Product p WHERE p.Deleted = 0 AND p.ID IN @Ids
)";

        string sql = BuildProductProjectionSql(
            changedProductIdsCte,
            "INNER JOIN ChangedProductIds c ON c.ID = p.ID",
            requireCatalogEligibility: false);

        List<long> uniqueIds = ids.Distinct().ToList();
        List<ProductSyncData> products = new List<ProductSyncData>(uniqueIds.Count);

        using IDbConnection connection = connectionFactory();
        connection.Open();

        RetailConfigurationSnapshot configuration = await GetRetailConfigurationAsync(connection);
        if (!HasValidRetailConfiguration(configuration)
            || (!string.IsNullOrWhiteSpace(expectedConfigurationSignature)
                && !string.Equals(
                    expectedConfigurationSignature,
                    configuration.Signature,
                    StringComparison.Ordinal))) {
            return new ProductProjectionSnapshot(
                [],
                configuration.Signature,
                HasValidRetailConfiguration: false);
        }

        for (int i = 0; i < uniqueIds.Count; i += ProductIdsBatchSize) {
            List<long> batch = uniqueIds.Skip(i).Take(ProductIdsBatchSize).ToList();
            products.AddRange(await QueryUniqueProductsAsync(
                connection,
                sql,
                new { Ids = batch },
                commandTimeout: 120));
        }

        RetailConfigurationSnapshot completedConfiguration = await GetRetailConfigurationAsync(connection);
        bool configurationStayedValid = HasValidRetailConfiguration(completedConfiguration)
                                        && string.Equals(
                                            configuration.Signature,
                                            completedConfiguration.Signature,
                                            StringComparison.Ordinal);

        return new ProductProjectionSnapshot(
            configurationStayedValid
                ? products.DistinctBy(product => product.Id).ToList()
                : [],
            completedConfiguration.Signature,
            configurationStayedValid);
    }

    public async Task<bool> IsRetailConfigurationCurrentAsync(string expectedConfigurationSignature) {
        if (string.IsNullOrWhiteSpace(expectedConfigurationSignature)) return false;

        using IDbConnection connection = connectionFactory();
        connection.Open();
        RetailConfigurationSnapshot configuration = await GetRetailConfigurationAsync(connection);
        return HasValidRetailConfiguration(configuration)
               && string.Equals(
                   expectedConfigurationSignature,
                   configuration.Signature,
                   StringComparison.Ordinal);
    }

    private static string BuildProductProjectionSql(
        string? firstCte = null,
        string? productJoin = null,
        bool requireCatalogEligibility = true) {
        _ = requireCatalogEligibility;
        string projection = ProductProjectionSql
                            + "\n"
                            + (productJoin ?? string.Empty)
                            + ProductProjectionTailSql;
        return firstCte == null
            ? projection
            : ";WITH\n" + firstCte + projection;
    }

    private static string BuildChangedProductIdsSql() {
        return BuildChangedProductIdsPreparationSql() + @"
SELECT ID
FROM #ChangedProductIds
ORDER BY ID;";
    }

    private static string BuildChangedProductIdBatchSql() {
        return BuildChangedProductIdsPreparationSql() + @"
SELECT TOP (@Take) ID
FROM #ChangedProductIds
WHERE ID > @AfterProductId
ORDER BY ID;";
    }

    private static string BuildChangedProductIdsPreparationSql() {
        return @"
CREATE TABLE #DirectChangedProductIds (
    ID bigint NOT NULL PRIMARY KEY
);

INSERT INTO #DirectChangedProductIds (ID)
" + DirectChangedProductIdsSql + @";

CREATE TABLE #ChangedProductIds (
    ID bigint NOT NULL PRIMARY KEY
);

INSERT INTO #ChangedProductIds (ID)
SELECT ID FROM #DirectChangedProductIds;

IF EXISTS (SELECT 1 FROM #DirectChangedProductIds)
BEGIN
" + BuildSourceSiblingInsertSql("Fenix") + @"
" + BuildSourceSiblingInsertSql("Amg") + @"
END;
";
    }

    private static string BuildSourceSiblingInsertSql(string sourceName) {
        string sourceId = $"Source{sourceName}ID";
        string sourceCode = $"Source{sourceName}Code";
        return $@"
    INSERT INTO #ChangedProductIds (ID)
    SELECT DISTINCT candidate.ID
    FROM #DirectChangedProductIds direct
    INNER JOIN Product changed ON changed.ID = direct.ID
    INNER JOIN Product candidate
        ON candidate.Deleted = 0
       AND candidate.{sourceCode} = changed.{sourceCode}
    WHERE changed.{sourceCode} IS NOT NULL
      AND (
            (ISNULL(DATALENGTH(changed.{sourceId}), 0) > 0
             AND candidate.{sourceId} = changed.{sourceId})
         OR (ISNULL(DATALENGTH(changed.{sourceId}), 0) = 0
             AND ISNULL(DATALENGTH(candidate.{sourceId}), 0) = 0)
      )
      AND NOT EXISTS (
          SELECT 1 FROM #ChangedProductIds existing WHERE existing.ID = candidate.ID
      );

    IF EXISTS (
        SELECT 1
        FROM #DirectChangedProductIds direct
        INNER JOIN Product changed ON changed.ID = direct.ID
        WHERE changed.{sourceCode} IS NULL
          AND ISNULL(DATALENGTH(changed.{sourceId}), 0) > 0
    )
    BEGIN
        INSERT INTO #ChangedProductIds (ID)
        SELECT DISTINCT candidate.ID
        FROM #DirectChangedProductIds direct
        INNER JOIN Product changed ON changed.ID = direct.ID
        INNER JOIN Product candidate
            ON candidate.Deleted = 0
           AND candidate.{sourceCode} IS NULL
           AND candidate.{sourceId} = changed.{sourceId}
        WHERE changed.{sourceCode} IS NULL
          AND ISNULL(DATALENGTH(changed.{sourceId}), 0) > 0
          AND NOT EXISTS (
              SELECT 1 FROM #ChangedProductIds existing WHERE existing.ID = candidate.ID
          );
    END;";
    }

    private static async Task<List<ProductSyncData>> QueryUniqueProductsAsync(
        IDbConnection connection,
        string sql,
        object? parameters = null,
        int commandTimeout = 120) {
        IEnumerable<ProductSyncData> products = await connection.QueryAsync<ProductSyncData>(
            sql,
            parameters,
            commandTimeout: commandTimeout);

        List<ProductSyncData> uniqueProducts = SelectCanonicalProducts(products);
        foreach (ProductSyncData product in uniqueProducts) {
            product.CatalogScopes = ParseCatalogScopes(product.CatalogScopesJson, product.Id);
        }

        return uniqueProducts;
    }

    private static List<ProductSyncData> SelectCanonicalProducts(
        IEnumerable<ProductSyncData> products) {
        HashSet<string> seenFenixSources = new(StringComparer.Ordinal);
        HashSet<string> seenAmgSources = new(StringComparer.Ordinal);
        List<ProductSyncData> canonical = [];

        foreach (ProductSyncData product in products
                     .DistinctBy(item => item.Id)
                     .OrderBy(item => item.Id)) {
            if (product.IsCanonicalFenix
                && !string.IsNullOrWhiteSpace(product.ProductSourceFenix)
                && !seenFenixSources.Add(product.ProductSourceFenix))
                product.IsCanonicalFenix = false;
            if (product.IsCanonicalAmg
                && !string.IsNullOrWhiteSpace(product.ProductSourceAmg)
                && !seenAmgSources.Add(product.ProductSourceAmg))
                product.IsCanonicalAmg = false;
            if (!product.IsCanonicalFenix && !product.IsCanonicalAmg) continue;

            canonical.Add(product);
        }

        return canonical;
    }

    private static List<ProductCatalogScopeData> ParseCatalogScopes(string? json, long productId) {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try {
            List<ProductCatalogScopeData> scopes = JsonSerializer.Deserialize<List<ProductCatalogScopeData>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            if (scopes.Count > MaxCatalogScopesPerProduct) {
                throw new InvalidOperationException(
                    $"Product {productId} exceeds the catalog-scope safety limit of {MaxCatalogScopesPerProduct}.");
            }

            if (scopes.Any(scope => !scope.IsValid)) {
                throw new InvalidOperationException(
                    $"Product {productId} contains an invalid catalog availability scope.");
            }

            return scopes;
        } catch (JsonException ex) {
            throw new InvalidOperationException(
                $"Product {productId} contains malformed catalog availability JSON.",
                ex);
        }
    }

    private static async Task<RetailConfigurationSnapshot> GetRetailConfigurationAsync(IDbConnection connection) {
        return await connection.QuerySingleAsync<RetailConfigurationSnapshot>(RetailConfigurationSignatureSql);
    }

    private static bool HasValidRetailConfiguration(RetailConfigurationSnapshot configuration) {
        return configuration.IsValid && !string.IsNullOrWhiteSpace(configuration.Signature);
    }

    /// <summary>
    /// How many of the given indexed product ids still exist in the catalog. A 1C re-mint
    /// replaces every product id, so a near-zero result means the whole generation is orphaned.
    /// </summary>
    public async Task<int> CountLiveProductIdsAsync(IReadOnlyCollection<long> productIds) {
        if (productIds.Count == 0) return 0;

        using IDbConnection connection = connectionFactory();
        connection.Open();

        const string sql = @"
SELECT COUNT(*) FROM Product p WHERE p.Deleted = 0 AND p.ID IN @Ids";
        return await connection.ExecuteScalarAsync<int>(
            sql,
            new { Ids = productIds.Distinct().ToArray() },
            commandTimeout: 30);
    }

    public async Task<List<long>> GetDeletedProductIdsAsync(DateTime since) {
        using IDbConnection connection = connectionFactory();
        connection.Open();

        const string sql = @"
SELECT p.ID
FROM Product p
WHERE p.Deleted = 1 AND p.Updated > @Since";

        IEnumerable<long> ids = await connection.QueryAsync<long>(sql, new { Since = since });
        return ids.AsList();
    }

    public async Task<List<long>> GetDeletedProductIdBatchAsync(
        DateTime since,
        long afterProductId,
        int take) {
        int boundedTake = Math.Clamp(take, 1, ProductIdsBatchSize);
        using IDbConnection connection = connectionFactory();
        connection.Open();

        const string sql = @"
SELECT TOP (@Take) p.ID
FROM Product p
WHERE p.Deleted = 1
  AND p.Updated > @Since
  AND p.ID > @AfterProductId
ORDER BY p.ID";
        return (await connection.QueryAsync<long>(
                sql,
                new { Since = since, AfterProductId = afterProductId, Take = boundedTake },
                commandTimeout: 120))
            .AsList();
    }

    public async Task<Dictionary<long, List<string>>> GetOriginalNumbersForProductsAsync(IEnumerable<long> productIds) {
        List<long> productIdsList = productIds as List<long> ?? productIds.ToList();
        if (productIdsList.Count == 0) return new Dictionary<long, List<string>>();

        using IDbConnection connection = connectionFactory();
        connection.Open();

        const string sql = @"
SELECT
    pon.ProductID,
    on_.Number
FROM ProductOriginalNumber pon
INNER JOIN OriginalNumber on_ ON on_.ID = pon.OriginalNumberID
WHERE pon.Deleted = 0
  AND pon.ProductID IN @ProductIds";

        Dictionary<long, List<string>> result = new Dictionary<long, List<string>>();

        for (int i = 0; i < productIdsList.Count; i += ProductIdsBatchSize) {
            List<long> batch = productIdsList.Skip(i).Take(ProductIdsBatchSize).ToList();

            IEnumerable<(long ProductId, string Number)> rows = await connection.QueryAsync<(long ProductId, string Number)>(
                sql, new { ProductIds = batch });

            foreach ((long productId, string number) in rows) {
                if (!result.TryGetValue(productId, out List<string>? list)) {
                    list = [];
                    result[productId] = list;
                }
                if (!string.IsNullOrWhiteSpace(number)) {
                    list.Add(number);
                }
            }
        }

        return result;
    }
}

public sealed record ProductSyncSnapshot(
    List<ProductSyncData> Products,
    string RetailConfigurationSignature,
    bool HasValidRetailConfiguration);

public sealed record ProductIdSyncPlan(
    List<long> ProductIds,
    string RetailConfigurationSignature,
    bool RequiresFullReconciliation,
    bool HasValidRetailConfiguration);

public sealed record ProductProjectionSnapshot(
    List<ProductSyncData> Products,
    string RetailConfigurationSignature,
    bool HasValidRetailConfiguration);

public sealed record ProductProjectionBatch(
    List<ProductSyncData> Products,
    string RetailConfigurationSignature,
    bool HasValidRetailConfiguration,
    long LastScannedProductId,
    int ScannedCount,
    bool HasMore) {
    public static ProductProjectionBatch Invalid(string signature, long afterProductId) => new(
        [],
        signature,
        HasValidRetailConfiguration: false,
        LastScannedProductId: afterProductId,
        ScannedCount: 0,
        HasMore: false);
}

public sealed record ProductIdSyncBatch(
    List<long> ProductIds,
    string RetailConfigurationSignature,
    bool RequiresFullReconciliation,
    bool HasValidRetailConfiguration,
    long LastScannedProductId,
    bool HasMore);

public sealed class RetailConfigurationSnapshot {
    public string Signature { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}

public sealed class ProductSyncData {
    public long Id { get; set; }
    public Guid NetUid { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string SearchVendorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameUA { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionUA { get; set; } = string.Empty;
    public string MainOriginalNumber { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Synonyms { get; set; } = string.Empty;
    public string SearchName { get; set; } = string.Empty;
    public string SearchNameUA { get; set; } = string.Empty;
    public string SearchDescription { get; set; } = string.Empty;
    public string SearchDescriptionUA { get; set; } = string.Empty;
    public string SearchSize { get; set; } = string.Empty;

    // Product details
    public string PackingStandard { get; set; } = string.Empty;
    public string OrderStandard { get; set; } = string.Empty;
    public string Ucgfea { get; set; } = string.Empty;
    public string Volume { get; set; } = string.Empty;
    public string Top { get; set; } = string.Empty;
    public double Weight { get; set; }
    public bool HasAnalogue { get; set; }
    public bool HasComponent { get; set; }
    public bool HasImage { get; set; }
    public string Image { get; set; } = string.Empty;
    public long MeasureUnitId { get; set; }

    // Availability
    public double AvailableQtyUk { get; set; }
    public double AvailableQtyUkVat { get; set; }
    public double AvailableQtyPl { get; set; }
    public double AvailableQtyPlVat { get; set; }
    public double AvailableQty { get; set; }

    // Flags
    public bool IsForWeb { get; set; }
    public bool IsForSale { get; set; }
    public bool IsForZeroSale { get; set; }

    // Slug
    public long SlugId { get; set; }
    public Guid SlugNetUid { get; set; }
    public string SlugUrl { get; set; } = string.Empty;
    public string SlugLocale { get; set; } = string.Empty;

    // Retail pricing
    public decimal RetailPrice { get; set; }
    public decimal RetailPriceVat { get; set; }
    public string RetailCurrencyCode { get; set; } = "UAH";

    // Indexed catalog identity
    public long CatalogOrganizationIdNonVat { get; set; }
    public long CatalogOrganizationIdVat { get; set; }
    public string CatalogAgreementSourceNonVat { get; set; } = string.Empty;
    public string CatalogAgreementSourceVat { get; set; } = string.Empty;
    public string ProductSourceFenix { get; set; } = string.Empty;
    public string ProductSourceAmg { get; set; } = string.Empty;
    public bool IsCanonicalFenix { get; set; }
    public bool IsCanonicalAmg { get; set; }
    public string CatalogScopesJson { get; set; } = "[]";
    public List<ProductCatalogScopeData> CatalogScopes { get; set; } = [];
    public Guid CatalogAgreementNetUidNonVat { get; set; }
    public Guid CatalogAgreementNetUidVat { get; set; }
    public long CatalogPricingIdNonVat { get; set; }
    public long CatalogPricingIdVat { get; set; }
    public long CatalogCurrencyIdNonVat { get; set; }
    public long CatalogCurrencyIdVat { get; set; }
    public bool HasNonVatCatalogAvailability { get; set; }
    public bool HasVatCatalogAvailability { get; set; }
    public bool HasNonVatCatalogSource { get; set; }
    public bool HasVatCatalogSource { get; set; }
    public string RetailCurrencyCodeVat { get; set; } = string.Empty;

    public DateTime Updated { get; set; }
}

public sealed class ProductCatalogScopeData {
    public long OrganizationId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public bool WithVat { get; set; }
    public double AvailableQtyUk { get; set; }
    public double AvailableQtyPl { get; set; }
    public double AvailableQty { get; set; }

    public bool IsValid => OrganizationId > 0
                           && (string.Equals(SourceSystem, "fenix", StringComparison.Ordinal)
                               || string.Equals(SourceSystem, "amg", StringComparison.Ordinal))
                           && AvailableQty > 0
                           && double.IsFinite(AvailableQty)
                           && double.IsFinite(AvailableQtyUk)
                           && double.IsFinite(AvailableQtyPl);
}
