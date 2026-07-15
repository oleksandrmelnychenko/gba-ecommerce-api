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
    Task<RetailConfigurationSnapshot> GetRetailConfigurationSnapshotAsync();
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
    private static readonly IPricingDependencyRevisionProvider PricingRevisionProvider =
        new SqlPricingDependencyRevisionProvider();

    private const string FenixRetailPricingCtes = RetailCatalogSelectionPolicy.SqlCtes;

    public Task<PricingDependencyRevisions> GetPricingDependencyRevisionsAsync() {
        using IDbConnection connection = connectionFactory();
        connection.Open();
        return Task.FromResult(PricingRevisionProvider.Get(connection));
    }

    private const string SourceNeutralCatalogAvailabilityCte = @",
CatalogAvailability AS (
    SELECT
        pa.ProductID,
        s.OrganizationID AS OrganizationId,
        s.ForVatProducts AS WithVat,
        CASE WHEN o.PriceSourceIsAmg = 1 THEN 'amg' ELSE 'fenix' END AS SourceSystem,
        SUM(CASE WHEN s.Locale = 'uk' THEN pa.Amount ELSE 0 END) AS AvailableQtyUk,
        SUM(CASE WHEN s.Locale = 'pl' THEN pa.Amount ELSE 0 END) AS AvailableQtyPl,
        SUM(pa.Amount) AS AvailableQty
    FROM ProductAvailability pa
    INNER JOIN Storage s ON s.ID = pa.StorageID
    INNER JOIN Organization o ON o.ID = s.OrganizationID
    WHERE pa.Deleted = 0
      AND s.Deleted = 0
      AND s.ForEcommerce = 1
      AND s.ForDefective = 0
      AND o.Deleted = 0
    GROUP BY
        pa.ProductID,
        s.OrganizationID,
        s.ForVatProducts,
        CASE WHEN o.PriceSourceIsAmg = 1 THEN 'amg' ELSE 'fenix' END
    HAVING SUM(pa.Amount) > 0
)";

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
    ISNULL(nonVatAvailability.AvailableQtyUk, 0) AS AvailableQtyUk,
    ISNULL(vatAvailability.AvailableQtyUk, 0) AS AvailableQtyUkVat,
    ISNULL(nonVatAvailability.AvailableQtyPl, 0) AS AvailableQtyPl,
    ISNULL(vatAvailability.AvailableQtyPl, 0) AS AvailableQtyPlVat,
    ISNULL(nonVatAvailability.AvailableQty, 0)
        + ISNULL(vatAvailability.AvailableQty, 0) AS AvailableQty,
    CAST(CASE WHEN ISNULL(nonVatAvailability.AvailableQty, 0) > 0 THEN 1 ELSE 0 END AS bit)
        AS HasNonVatCatalogAvailability,
    CAST(CASE WHEN ISNULL(vatAvailability.AvailableQty, 0) > 0 THEN 1 ELSE 0 END AS bit)
        AS HasVatCatalogAvailability,
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
    (
        SELECT
            availability.OrganizationId AS organizationId,
            availability.SourceSystem AS sourceSystem,
            CAST(availability.WithVat AS bit) AS withVat,
            availability.AvailableQtyUk AS availableQtyUk,
            availability.AvailableQtyPl AS availableQtyPl,
            availability.AvailableQty AS availableQty
        FROM CatalogAvailability availability
        WHERE availability.ProductID = p.ID
          AND ((availability.SourceSystem = 'fenix'
                AND (" + ProductSourceIdentitySql.CanonicalExpression("p", "fenix") + @") <> '')
            OR (availability.SourceSystem = 'amg'
                AND (" + ProductSourceIdentitySql.CanonicalExpression("p", "amg") + @") <> ''))
        ORDER BY availability.OrganizationId, availability.WithVat, availability.SourceSystem
        FOR JSON PATH
    ) AS CatalogScopesJson,
    ISNULL(ps.ID, 0) AS SlugId,
    ISNULL(ps.NetUID, '00000000-0000-0000-0000-000000000000') AS SlugNetUid,
    ISNULL(ps.Url, '') AS SlugUrl,
    ISNULL(ps.Locale, '') AS SlugLocale,
    rpc.CatalogOrganizationIdNonVat,
    rpc.CatalogOrganizationIdVat,
    rpc.CatalogAgreementSourceNonVat,
    rpc.CatalogAgreementSourceVat,
    rpc.CatalogAgreementNetUidNonVat,
    rpc.CatalogAgreementNetUidVat,
    rpc.CatalogPricingIdNonVat,
    rpc.CatalogPricingIdVat,
    rpc.CatalogCurrencyIdNonVat,
    rpc.CatalogCurrencyIdVat,
    ISNULL(dbo.GetCalculatedProductPriceForPricingSource(
        p.NetUID, rpc.NonVatPricingNetUid, rpc.NonVatAgreementNetUid), 0) AS RetailPrice,
    ISNULL(dbo.GetCalculatedProductPriceForPricingSource(
        p.NetUID, rpc.VatPricingNetUid, rpc.VatAgreementNetUid), 0) AS RetailPriceVat,
    rpc.CatalogCurrencyCodeNonVat AS RetailCurrencyCode,
    rpc.CatalogCurrencyCodeVat AS RetailCurrencyCodeVat,
    p.Updated
FROM Product p";

    private static readonly string ProductProjectionTailSql = @"
CROSS JOIN RetailPricingConfig rpc
LEFT JOIN FenixProductAvailability nonVatAvailability
    ON nonVatAvailability.ProductID = p.ID
   AND nonVatAvailability.CatalogWithVat = 0
   AND nonVatAvailability.CatalogOrganizationId = rpc.CatalogOrganizationIdNonVat
LEFT JOIN FenixProductAvailability vatAvailability
    ON vatAvailability.ProductID = p.ID
   AND vatAvailability.CatalogWithVat = 1
   AND vatAvailability.CatalogOrganizationId = rpc.CatalogOrganizationIdVat
OUTER APPLY (
    SELECT TOP (1) slug.ID, slug.NetUID, slug.Url, slug.Locale
    FROM ProductSlug slug
    WHERE slug.ProductID = p.ID
      AND slug.Locale = 'uk'
      AND slug.Deleted = 0
    ORDER BY slug.ID
) ps
WHERE p.Deleted = 0
  AND " + ProductSourceIdentitySql.AnyCanonicalSourcePredicate("p") + @"
/*CATALOG_ELIGIBILITY*/
ORDER BY p.ID";

    private static readonly string ProductCatalogEligibilitySql = @"
  AND EXISTS (
      SELECT 1
      FROM CatalogAvailability availability
      WHERE availability.ProductID = p.ID
        AND ((availability.SourceSystem = 'fenix'
              AND (" + ProductSourceIdentitySql.CanonicalExpression("p", "fenix") + @") <> '')
          OR (availability.SourceSystem = 'amg'
              AND (" + ProductSourceIdentitySql.CanonicalExpression("p", "amg") + @") <> ''))
  )";

    private static readonly string ChangedProductIdsCte = @"
GlobalRetailDependencyChanges AS (
    SELECT 1 AS HasChanges
    WHERE @ForceFullRefresh = 1
       OR EXISTS (
            SELECT 1
            FROM Pricing pricing
            WHERE pricing.Updated > @Since OR pricing.Created > @Since
       )
       OR EXISTS (
            SELECT 1
            FROM Client retailClient
            INNER JOIN ClientAgreement ca ON ca.ClientID = retailClient.ID
            INNER JOIN Agreement a ON a.ID = ca.AgreementID
            LEFT JOIN Organization o ON o.ID = a.OrganizationID
            WHERE retailClient.IsForRetail = 1
              AND (retailClient.Updated > @Since OR retailClient.Created > @Since
                OR ca.Updated > @Since OR ca.Created > @Since
                OR a.Updated > @Since OR a.Created > @Since
                OR o.Updated > @Since OR o.Created > @Since)
       )
       OR EXISTS (
            SELECT 1
            FROM Storage storage
            LEFT JOIN Organization o ON o.ID = storage.OrganizationID
            WHERE (storage.ForEcommerce = 1 OR o.PriceSourceIsAmg = 0)
              AND (storage.Updated > @Since OR storage.Created > @Since
                OR o.Updated > @Since OR o.Created > @Since)
       )
       OR EXISTS (
            SELECT 1
            FROM Currency currency
            WHERE (currency.Updated > @Since OR currency.Created > @Since)
              AND EXISTS (
                  SELECT 1
                  FROM Agreement agreement
                  INNER JOIN ClientAgreement clientAgreement
                      ON clientAgreement.AgreementID = agreement.ID
                  INNER JOIN Client retailClient
                      ON retailClient.ID = clientAgreement.ClientID
                  WHERE retailClient.IsForRetail = 1
                    AND agreement.CurrencyID = currency.ID
              )
       )
       OR EXISTS (
            SELECT 1
            FROM ExchangeRate exchangeRate
            WHERE exchangeRate.Updated > @Since OR exchangeRate.Created > @Since
       )
       OR EXISTS (
            SELECT 1
            FROM GovExchangeRate govExchangeRate
            WHERE govExchangeRate.Updated > @Since OR govExchangeRate.Created > @Since
       )
),
DirectChangedProductIds AS (
    SELECT p.ID
    FROM Product p
    WHERE p.Deleted = 0 AND (p.Updated > @Since OR p.Created > @Since)

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

    SELECT availability.ProductID
    FROM ProductAvailability availability
    INNER JOIN Product p ON p.ID = availability.ProductID AND p.Deleted = 0
    WHERE availability.Updated > @Since OR availability.Created > @Since

    UNION

    SELECT slug.ProductID
    FROM ProductSlug slug
    INNER JOIN Product p ON p.ID = slug.ProductID AND p.Deleted = 0
    WHERE slug.Updated > @Since OR slug.Created > @Since

    UNION

    SELECT productPricing.ProductID
    FROM ProductPricing productPricing
    INNER JOIN Product p ON p.ID = productPricing.ProductID AND p.Deleted = 0
    WHERE productPricing.Updated > @Since OR productPricing.Created > @Since

    UNION

    SELECT productGroup.ProductID
    FROM ProductProductGroup productGroup
    INNER JOIN Product p ON p.ID = productGroup.ProductID AND p.Deleted = 0
    WHERE productGroup.Updated > @Since OR productGroup.Created > @Since

    UNION

    SELECT productGroup.ProductID
    FROM PricingProductGroupDiscount pricingDiscount
    INNER JOIN ProductProductGroup productGroup
        ON productGroup.ProductGroupID = pricingDiscount.ProductGroupID
       AND productGroup.Deleted = 0
    INNER JOIN Product p ON p.ID = productGroup.ProductID AND p.Deleted = 0
    WHERE pricingDiscount.Updated > @Since OR pricingDiscount.Created > @Since

    UNION

    SELECT productGroup.ProductID
    FROM ProductGroupDiscount agreementDiscount
    INNER JOIN ProductProductGroup productGroup
        ON productGroup.ProductGroupID = agreementDiscount.ProductGroupID
       AND productGroup.Deleted = 0
    INNER JOIN Product p ON p.ID = productGroup.ProductID AND p.Deleted = 0
    WHERE agreementDiscount.Updated > @Since OR agreementDiscount.Created > @Since

    UNION

    SELECT productGroup.ProductID
    FROM ProductGroup changedGroup
    INNER JOIN ProductProductGroup productGroup
        ON productGroup.ProductGroupID = changedGroup.ID
       AND productGroup.Deleted = 0
    INNER JOIN Product p ON p.ID = productGroup.ProductID AND p.Deleted = 0
    WHERE changedGroup.Updated > @Since OR changedGroup.Created > @Since

    UNION

    SELECT p.ID
    FROM Product p
    CROSS JOIN GlobalRetailDependencyChanges dependencyChange
) ,
SourceChangedProducts AS (
    SELECT product.*
    FROM Product product
    WHERE product.ID IN (SELECT ID FROM DirectChangedProductIds)
       OR product.Updated > @Since
       OR product.Created > @Since
),
ChangedProductIds AS (
    SELECT ID
    FROM DirectChangedProductIds

    UNION

    SELECT candidate.ID
    FROM Product candidate
    INNER JOIN SourceChangedProducts changed
        ON " + ProductSourceIdentitySql.SameSourceEntityPredicate("changed", "candidate", "fenix") + @"
        OR " + ProductSourceIdentitySql.SameSourceEntityPredicate("changed", "candidate", "amg") + @"
    WHERE candidate.Deleted = 0
)";

    private const string RetailConfigurationSignatureSql = @"
;WITH
" + FenixRetailPricingCtes + @"
SELECT CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(
    'storages=',
    (
        SELECT CONCAT(
            'S:', storage.ID,
            ':', ISNULL(CONVERT(varchar(20), storage.OrganizationID), ''),
            ':', CONVERT(int, storage.Deleted),
            ':', CONVERT(int, storage.ForEcommerce),
            ':', CONVERT(int, storage.ForDefective),
            ':', CONVERT(int, storage.ForVatProducts),
            ':', ISNULL(storage.Locale, ''),
            ':', storage.RetailPriority,
            ':', ISNULL(CONVERT(int, organization.Deleted), -1),
            ':', ISNULL(CONVERT(int, organization.PriceSourceIsAmg), -1),
            ';') AS [text()]
        FROM Storage storage
        LEFT JOIN Organization organization ON organization.ID = storage.OrganizationID
        ORDER BY storage.ID
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'),
    '|agreements=',
    (
        SELECT CONCAT(
            'A:', retailClient.ID,
            ':', CONVERT(int, retailClient.Deleted),
            ':', CONVERT(int, retailClient.IsActive),
            ':', clientAgreement.ID,
            ':', CONVERT(varchar(36), clientAgreement.NetUID),
            ':', CONVERT(int, clientAgreement.Deleted),
            ':', agreement.ID,
            ':', CONVERT(int, agreement.Deleted),
            ':', CONVERT(int, agreement.IsActive),
            ':', CONVERT(int, agreement.IsDefault),
            ':', CONVERT(int, agreement.IsSelected),
            ':', CONVERT(int, agreement.WithVATAccounting),
            ':', ISNULL(CONVERT(varchar(20), agreement.OrganizationID), ''),
            ':', ISNULL(CONVERT(varchar(20), agreement.PricingID), ''),
            ':', ISNULL(CONVERT(varchar(20), agreement.CurrencyID), ''),
            ':', ISNULL(CONVERT(varchar(128), agreement.SourceFenixID, 2), ''),
            ':', ISNULL(CONVERT(varchar(20), agreement.SourceFenixCode), ''),
            ':', ISNULL(CONVERT(varchar(128), agreement.SourceAmgID, 2), ''),
            ':', ISNULL(CONVERT(varchar(20), agreement.SourceAmgCode), ''),
            ':', ISNULL(CONVERT(varchar(36), pricing.NetUID), ''),
            ':', ISNULL(CONVERT(int, pricing.Deleted), -1),
            ':', ISNULL(CONVERT(varchar(20), pricing.BasePricingID), ''),
            ':', ISNULL(CONVERT(varchar(50), pricing.CalculatedExtraCharge), ''),
            ':', CONVERT(varchar(33), agreement.Updated, 126),
            ':', CONVERT(varchar(33), clientAgreement.Updated, 126),
            ';') AS [text()]
        FROM Client retailClient
        INNER JOIN ClientAgreement clientAgreement ON clientAgreement.ClientID = retailClient.ID
        INNER JOIN Agreement agreement ON agreement.ID = clientAgreement.AgreementID
        LEFT JOIN Pricing pricing ON pricing.ID = agreement.PricingID
        WHERE retailClient.IsForRetail = 1
        ORDER BY retailClient.ID, clientAgreement.ID, agreement.ID
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'),
    '|currency=',
    (
        SELECT CONCAT(
            'C:', currency.ID,
            ':', ISNULL(currency.Code, ''),
            ':', CONVERT(int, currency.Deleted),
            ';') AS [text()]
        FROM Currency currency
        WHERE EXISTS (
            SELECT 1
            FROM Agreement agreement
            INNER JOIN ClientAgreement clientAgreement ON clientAgreement.AgreementID = agreement.ID
            INNER JOIN Client retailClient ON retailClient.ID = clientAgreement.ClientID
            WHERE retailClient.IsForRetail = 1
              AND agreement.CurrencyID = currency.ID
        )
        ORDER BY currency.ID
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)')
)), 2) AS Signature,
CAST(CASE
    WHEN (SELECT COUNT(*) FROM FenixRetailStorage) = 2
     AND EXISTS (SELECT 1 FROM RetailPricingConfig)
    THEN 1
    ELSE 0
END AS bit) AS IsValid";

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
        string sql = BuildProductProjectionSql(
            ChangedProductIdsCte,
            "INNER JOIN ChangedProductIds c ON c.ID = p.ID",
            requireCatalogEligibility: false);

        using IDbConnection connection = connectionFactory();
        connection.Open();

        RetailConfigurationSnapshot configuration = await GetRetailConfigurationAsync(connection);
        if (!HasValidRetailConfiguration(configuration)) return [];

        const bool forceFullRefresh = true;

        return await QueryUniqueProductsAsync(
            connection,
            sql,
            new { Since = since, ForceFullRefresh = forceFullRefresh },
            commandTimeout: 300);
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
            new { Since = since, ForceFullRefresh = forceFullRefresh },
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

        string sql = ";WITH\n" + ChangedProductIdsCte + @"
SELECT TOP (@Take) ID
FROM ChangedProductIds
WHERE ID > @AfterProductId
ORDER BY ID";
        List<long> ids = (await connection.QueryAsync<long>(
                sql,
                new {
                    Since = since,
                    ForceFullRefresh = false,
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
        string ctes = firstCte == null
            ? FenixRetailPricingCtes + SourceNeutralCatalogAvailabilityCte
            : firstCte + ",\n" + FenixRetailPricingCtes + SourceNeutralCatalogAvailabilityCte;

        string projectionTail = ProductProjectionTailSql.Replace(
            "/*CATALOG_ELIGIBILITY*/",
            requireCatalogEligibility ? ProductCatalogEligibilitySql : string.Empty,
            StringComparison.Ordinal);
        return ";WITH\n" + ctes + ProductProjectionSql + "\n" + productJoin + projectionTail;
    }

    private static string BuildChangedProductIdsSql() {
        return ";WITH\n" + ChangedProductIdsCte + "\nSELECT ID FROM ChangedProductIds";
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
