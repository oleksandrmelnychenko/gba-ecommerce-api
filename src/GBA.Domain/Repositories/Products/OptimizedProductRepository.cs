using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Repositories.Products;

/// <summary>
/// Fetches product search projections while delegating price selection to the
/// source-aware database pricing functions.
/// </summary>
public sealed class OptimizedProductRepository {
    private static readonly string ProductBatchSql = @"
;WITH AgreementData AS (
    SELECT TOP (1)
        a.OrganizationID,
        a.CurrencyID,
        a.WithVATAccounting,
        c.Code AS CurrencyCode
    FROM ClientAgreement ca
    INNER JOIN Agreement a ON a.ID = ca.AgreementID
    LEFT JOIN Currency c ON c.ID = a.CurrencyID AND c.Deleted = 0
    WHERE ca.NetUID = @ClientAgreementNetId
      AND ca.Deleted = 0
      AND a.Deleted = 0
    ORDER BY ca.ID
),
ProductData AS (
    SELECT
        p.ID,
        p.NetUID,
        p.VendorCode,
        p.HasAnalogue,
        p.HasComponent,
        p.HasImage,
        p.[Image],
        p.IsForSale,
        p.IsForWeb,
        p.IsForZeroSale,
        p.MainOriginalNumber,
        p.MeasureUnitID,
        p.OrderStandard,
        p.PackingStandard,
        p.Size,
        p.[Top],
        p.UCGFEA,
        p.Volume,
        p.[Weight],
        CASE WHEN @Culture = 'uk' THEN COALESCE(p.NameUA, p.Name) ELSE COALESCE(p.NamePl, p.Name) END AS Name,
        CASE WHEN @Culture = 'uk' THEN COALESCE(p.DescriptionUA, p.Description) ELSE COALESCE(p.DescriptionPl, p.Description) END AS Description,
        ids.RowNum AS SearchRowNumber
    FROM Product p
    INNER JOIN #ProductIds ids ON ids.Id = p.ID
    WHERE p.Deleted = 0
      AND " + ProductSourceIdentitySql.CanonicalSourceWorldPredicate("p") + @"
),
Availability AS (
    SELECT
        pa.ProductID,
        SUM(CASE WHEN s.Locale = 'uk' AND s.ForVatProducts = 1 THEN pa.Amount ELSE 0 END) AS AvailableQtyUkVAT,
        SUM(CASE WHEN s.Locale = 'uk' AND s.ForVatProducts = 0 THEN pa.Amount ELSE 0 END) AS AvailableQtyUkNonVAT,
        SUM(CASE WHEN s.Locale = 'pl' AND s.ForVatProducts = 1 THEN pa.Amount ELSE 0 END) AS AvailableQtyPlVAT,
        SUM(CASE WHEN s.Locale = 'pl' AND s.ForVatProducts = 0 THEN pa.Amount ELSE 0 END) AS AvailableQtyPlNonVAT
    FROM ProductAvailability pa
    INNER JOIN Storage s ON s.ID = pa.StorageID
        AND s.Deleted = 0
        AND s.ForDefective = 0
        AND s.OrganizationID = COALESCE(
            @OrganizationId,
            (SELECT OrganizationID FROM AgreementData))
    WHERE pa.Deleted = 0
      AND pa.ProductID IN (SELECT Id FROM #ProductIds)
    GROUP BY pa.ProductID
)
SELECT
    pd.ID,
    pd.NetUID AS NetUid,
    pd.VendorCode,
    pd.Name,
    pd.Description,
    pd.Size,
    pd.PackingStandard,
    pd.OrderStandard,
    pd.UCGFEA,
    pd.Volume,
    pd.[Top],
    ISNULL(av.AvailableQtyUkVAT, 0) + ISNULL(av.AvailableQtyUkNonVAT, 0) AS AvailableQtyUk,
    0 AS AvailableQtyRoad,
    ISNULL(av.AvailableQtyUkVAT, 0) AS AvailableQtyUkVAT,
    ISNULL(av.AvailableQtyPlVAT, 0) + ISNULL(av.AvailableQtyPlNonVAT, 0) AS AvailableQtyPl,
    ISNULL(av.AvailableQtyPlVAT, 0) AS AvailableQtyPlVAT,
    pd.[Weight],
    pd.HasAnalogue,
    pd.HasComponent,
    pd.HasImage,
    pd.IsForWeb,
    pd.IsForSale,
    pd.IsForZeroSale,
    pd.MainOriginalNumber,
    pd.[Image],
    pd.MeasureUnitID AS MeasureUnitId,
    ISNULL(price.CalculatedPrice, 0) AS CurrentPrice,
    ISNULL(price.CalculatedPrice, 0) AS CurrentLocalPrice,
    ISNULL(price.CalculatedPrice, 0) AS CurrentWithVatPrice,
    ISNULL(price.CalculatedPrice, 0) AS CurrentLocalWithVatPrice,
    COALESCE(
        (SELECT TOP (1) Code FROM Currency WHERE ID = @CurrencyId AND Deleted = 0),
        agreement.CurrencyCode,
        'UAH') AS CurrencyCode,
    pd.SearchRowNumber,
    slug.ID AS SlugId,
    slug.NetUID AS SlugNetUid,
    slug.Url,
    slug.Locale,
    slug.ProductID AS SlugProductId,
    slug.Created AS SlugCreated,
    slug.Updated AS SlugUpdated,
    slug.Deleted AS SlugDeleted
FROM ProductData pd
CROSS JOIN AgreementData agreement
OUTER APPLY (VALUES (
    dbo.GetCalculatedProductPriceWithSharesAndVat(
        pd.NetUID,
        @ClientAgreementNetId,
        @Culture,
        COALESCE(@WithVat, agreement.WithVATAccounting),
        NULL)
)) price(CalculatedPrice)
LEFT JOIN Availability av ON av.ProductID = pd.ID
OUTER APPLY (
    SELECT TOP (1)
        ps.ID,
        ps.NetUID,
        ps.Url,
        ps.Locale,
        ps.ProductID,
        ps.Created,
        ps.Updated,
        ps.Deleted
    FROM ProductSlug ps
    WHERE ps.ProductID = pd.ID
      AND ps.Locale = @Culture
      AND ps.Deleted = 0
    ORDER BY ps.ID
) slug
ORDER BY pd.SearchRowNumber;

DROP TABLE #ProductIds;";

    private readonly IDbConnection _connection;

    public OptimizedProductRepository(IDbConnection connection) {
        _connection = connection;
    }

    /// <summary>
    /// Gets only calculated prices for products (lightweight query for V3 search).
    /// </summary>
    public Dictionary<long, ProductPriceInfo> GetPricesOnly(
        List<long> productIds,
        Guid clientAgreementNetId,
        long? organizationId,
        bool withVat,
        string catalogSource,
        string culture = "uk") {
        if (productIds == null || productIds.Count == 0)
            return new Dictionary<long, ProductPriceInfo>();

        StringBuilder tempTableSql = BuildProductIdsTable(productIds, withRowNumbers: false);

        string sql = @"
SELECT
    p.ID AS Id,
    dbo.GetCalculatedProductPriceWithSharesAndVat(
        p.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS Price,
    (SELECT TOP (1) c.Code
     FROM Currency c
     INNER JOIN Agreement a ON a.CurrencyID = c.ID
     INNER JOIN ClientAgreement ca ON ca.AgreementID = a.ID
     WHERE ca.NetUID = @ClientAgreementNetId AND c.Deleted = 0) AS CurrencyCode
FROM Product p
INNER JOIN #ProductIds ids ON ids.Id = p.ID
WHERE p.Deleted = 0
  AND " + ProductSourceIdentitySql.CanonicalSourceWorldPredicate("p") + @";

DROP TABLE #ProductIds;";

        List<dynamic> results = _connection.Query<dynamic>(
            tempTableSql + sql,
            new {
                ClientAgreementNetId = clientAgreementNetId,
                Culture = culture,
                WithVat = withVat,
                CatalogSource = catalogSource
            }).ToList();

        return results
            .GroupBy(result => (long)result.Id)
            .ToDictionary(
                group => group.Key,
                group => {
                    dynamic result = group.First();
                    return new ProductPriceInfo {
                        Price = (decimal)(result.Price ?? 0m),
                        CurrencyCode = result.CurrencyCode ?? "UAH"
                    };
                });
    }

    /// <summary>
    /// Gets prices for retail users through their real retail agreement.
    /// </summary>
    public Dictionary<long, ProductPriceInfo> GetPricesOnlyForRetail(
        List<long> productIds,
        Guid retailAgreementNetId,
        long currencyId,
        bool withVat,
        string catalogSource,
        string culture = "uk") {
        if (productIds == null || productIds.Count == 0)
            return new Dictionary<long, ProductPriceInfo>();

        StringBuilder tempTableSql = BuildProductIdsTable(productIds, withRowNumbers: false);

        string sql = @"
SELECT
    p.ID AS Id,
    dbo.GetCalculatedProductPriceWithSharesAndVat(
        p.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS Price,
    (SELECT TOP (1) Code FROM Currency WHERE ID = @CurrencyId AND Deleted = 0) AS CurrencyCode
FROM Product p
INNER JOIN #ProductIds ids ON ids.Id = p.ID
WHERE p.Deleted = 0
  AND " + ProductSourceIdentitySql.CanonicalSourceWorldPredicate("p") + @";

DROP TABLE #ProductIds;";

        List<dynamic> results = _connection.Query<dynamic>(
            tempTableSql + sql,
            new {
                ClientAgreementNetId = retailAgreementNetId,
                CurrencyId = currencyId,
                Culture = culture,
                WithVat = withVat,
                CatalogSource = catalogSource
            }).ToList();

        return results
            .GroupBy(result => (long)result.Id)
            .ToDictionary(
                group => group.Key,
                group => {
                    dynamic result = group.First();
                    return new ProductPriceInfo {
                        Price = (decimal)(result.Price ?? 0m),
                        CurrencyCode = result.CurrencyCode ?? "UAH"
                    };
                });
    }

    public List<FromSearchProduct> GetProductsByIdsWithPrices(
        List<long> productIds,
        Guid clientAgreementNetId,
        string culture,
        bool withVat,
        long? organizationId,
        string catalogSource) {
        return GetProductsByIds(
            productIds,
            clientAgreementNetId,
            culture,
            organizationId,
            currencyId: null,
            withVat,
            catalogSource);
    }

    /// <summary>
    /// Gets products for anonymous retail users through their resolved retail agreement.
    /// </summary>
    public List<FromSearchProduct> GetProductsByIdsForRetail(
        List<long> productIds,
        Guid clientAgreementNetId,
        string culture,
        long organizationId,
        long currencyId,
        long pricingId,
        string catalogSource) {
        return GetProductsByIds(
            productIds,
            clientAgreementNetId,
            culture,
            organizationId,
            currencyId,
            withVat: null,
            catalogSource);
    }

    private List<FromSearchProduct> GetProductsByIds(
        List<long> productIds,
        Guid clientAgreementNetId,
        string culture,
        long? organizationId,
        long? currencyId,
        bool? withVat,
        string catalogSource) {
        if (productIds == null || productIds.Count == 0)
            return new List<FromSearchProduct>();

        string sql = BuildProductIdsTable(productIds, withRowNumbers: true) + ProductBatchSql;
        List<dynamic> results = _connection.Query<dynamic>(sql, new {
            ClientAgreementNetId = clientAgreementNetId,
            Culture = culture,
            OrganizationId = organizationId,
            CurrencyId = currencyId,
            WithVat = withVat,
            CatalogSource = catalogSource
        }).ToList();

        Dictionary<long, FromSearchProduct> products = new();

        foreach (dynamic result in results) {
            long productId = (long)result.ID;
            if (products.ContainsKey(productId)) continue;

            FromSearchProduct product = new() {
                Id = productId,
                NetUid = (Guid)result.NetUid,
                VendorCode = result.VendorCode,
                Name = result.Name,
                Description = result.Description,
                Size = result.Size,
                PackingStandard = result.PackingStandard,
                OrderStandard = result.OrderStandard,
                UCGFEA = result.UCGFEA,
                Volume = result.Volume,
                Top = result.Top,
                AvailableQtyUk = (double)result.AvailableQtyUk,
                AvailableQtyRoad = (double)result.AvailableQtyRoad,
                AvailableQtyUkVAT = (double)result.AvailableQtyUkVAT,
                AvailableQtyPl = (double)result.AvailableQtyPl,
                AvailableQtyPlVAT = (double)result.AvailableQtyPlVAT,
                Weight = (double)result.Weight,
                HasAnalogue = (bool)result.HasAnalogue,
                HasComponent = (bool)result.HasComponent,
                HasImage = (bool)result.HasImage,
                IsForWeb = (bool)result.IsForWeb,
                IsForSale = (bool)result.IsForSale,
                IsForZeroSale = (bool)result.IsForZeroSale,
                MainOriginalNumber = result.MainOriginalNumber,
                Image = result.Image,
                MeasureUnitId = (long)result.MeasureUnitId,
                CurrentPrice = (decimal)result.CurrentPrice,
                CurrentLocalPrice = (decimal)result.CurrentLocalPrice,
                CurrentWithVatPrice = (decimal)result.CurrentWithVatPrice,
                CurrentLocalWithVatPrice = (decimal)result.CurrentLocalWithVatPrice,
                CurrencyCode = result.CurrencyCode,
                SearchRowNumber = Convert.ToInt64(result.SearchRowNumber)
            };

            if (result.SlugId != null && (long)result.SlugId > 0) {
                product.ProductSlug = new ProductSlug {
                    Id = (long)result.SlugId,
                    NetUid = (Guid)result.SlugNetUid,
                    Url = result.Url,
                    Locale = result.Locale,
                    ProductId = (long)result.SlugProductId,
                    Created = (DateTime)result.SlugCreated,
                    Updated = (DateTime)result.SlugUpdated,
                    Deleted = (bool)result.SlugDeleted
                };
            }

            products.Add(productId, product);
        }

        return products.Values.OrderBy(product => product.SearchRowNumber).ToList();
    }

    private static StringBuilder BuildProductIdsTable(IEnumerable<long> productIds, bool withRowNumbers) {
        List<long> uniqueIds = productIds.Distinct().ToList();
        StringBuilder sql = new();
        sql.AppendLine(withRowNumbers
            ? "CREATE TABLE #ProductIds (Id BIGINT PRIMARY KEY, RowNum INT);"
            : "CREATE TABLE #ProductIds (Id BIGINT PRIMARY KEY);");

        const int batchSize = 1000;
        for (int index = 0; index < uniqueIds.Count; index += batchSize) {
            List<long> batch = uniqueIds.Skip(index).Take(batchSize).ToList();
            string values = withRowNumbers
                ? string.Join(",", batch.Select((id, offset) => $"({id},{index + offset + 1})"))
                : string.Join(",", batch.Select(id => $"({id})"));

            sql.AppendLine(withRowNumbers
                ? $"INSERT INTO #ProductIds (Id, RowNum) VALUES {values};"
                : $"INSERT INTO #ProductIds (Id) VALUES {values};");
        }

        return sql;
    }
}

public sealed class ProductPriceInfo {
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "UAH";
}
