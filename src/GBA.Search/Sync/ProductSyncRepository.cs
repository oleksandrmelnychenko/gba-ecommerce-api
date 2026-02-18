using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GBA.Search.Models;

namespace GBA.Search.Sync;

public sealed class ProductSyncRepository(Func<IDbConnection> connectionFactory) {
    public async Task<List<ProductSyncData>> GetAllProductsAsync() {
        using IDbConnection connection = connectionFactory();
        connection.Open();

        const string sql = @"
;WITH
-- Retail pricing config (hardcoded from Internet shop client agreements)
-- Non-VAT: PricingId=853, VAT: PricingId=848
RetailPricingConfig AS (
    SELECT 853 AS PricingIdNonVat, 848 AS PricingIdVat
),
-- Recursive CTE to find ROOT pricing ID (like GetBasePricingId UDF)
BasePricingHierarchy AS (
    SELECT pr.ID AS OriginalPricingId, pr.ID AS CurrentPricingId, pr.BasePricingID
    FROM Pricing pr WHERE pr.Deleted = 0 AND pr.ID IN (853, 848)
    UNION ALL
    SELECT bph.OriginalPricingId, pr.ID AS CurrentPricingId, pr.BasePricingID
    FROM Pricing pr
    INNER JOIN BasePricingHierarchy bph ON pr.ID = bph.BasePricingID
    WHERE pr.Deleted = 0
),
BasePricingIds AS (
    SELECT OriginalPricingId, CurrentPricingId AS BasePricingId
    FROM BasePricingHierarchy WHERE BasePricingID IS NULL
),
-- Get extra charge from ORIGINAL pricing (not base) - this is key!
OriginalPricingCharge AS (
    SELECT
        pr.ID AS PricingId,
        pr.CalculatedExtraCharge
    FROM Pricing pr
    WHERE pr.Deleted = 0 AND pr.ID IN (853, 848)
),
-- Product groups for extra charge lookup
ProductGroups AS (
    SELECT ppg.ProductID, ppg.ProductGroupID
    FROM ProductProductGroup ppg
    WHERE ppg.Deleted = 0
),
-- Get retail currency
RetailCurrency AS (
    SELECT TOP 1 c.Code
    FROM Currency c
    WHERE c.Deleted = 0 AND c.Code = 'UAH'
)
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
    ISNULL((SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0 AND s.Locale = 'uk' AND s.ForVatProducts = 0), 0) AS AvailableQtyUk,
    ISNULL((SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0 AND s.Locale = 'uk' AND s.ForVatProducts = 1), 0) AS AvailableQtyUkVat,
    ISNULL((SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0 AND s.Locale = 'pl' AND s.ForVatProducts = 0), 0) AS AvailableQtyPl,
    ISNULL((SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0 AND s.Locale = 'pl' AND s.ForVatProducts = 1), 0) AS AvailableQtyPlVat,
    ISNULL((SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0), 0) AS AvailableQty,
    p.IsForWeb,
    p.IsForSale,
    p.IsForZeroSale,
    ISNULL(ps.ID, 0) AS SlugId,
    ISNULL(ps.NetUID, '00000000-0000-0000-0000-000000000000') AS SlugNetUid,
    ISNULL(ps.Url, '') AS SlugUrl,
    ISNULL(ps.Locale, '') AS SlugLocale,
    -- Retail price: BasePrice (from BASE pricing) + BasePrice * ExtraCharge (from ORIGINAL pricing) / 100
    ISNULL(ROUND(
        pp.Price + (pp.Price * COALESCE(
            (SELECT TOP 1 ppgd.CalculatedExtraCharge FROM PricingProductGroupDiscount ppgd WHERE ppgd.PricingID = 853 AND ppgd.ProductGroupID = pg.ProductGroupID AND ppgd.Deleted = 0),
            opc.CalculatedExtraCharge,
            0
        ) / 100.0)
    , 2), 0) AS RetailPrice,
    ISNULL(ROUND(
        ppv.Price + (ppv.Price * COALESCE(
            (SELECT TOP 1 ppgd.CalculatedExtraCharge FROM PricingProductGroupDiscount ppgd WHERE ppgd.PricingID = 848 AND ppgd.ProductGroupID = pg.ProductGroupID AND ppgd.Deleted = 0),
            opcv.CalculatedExtraCharge,
            0
        ) / 100.0)
    , 2), 0) AS RetailPriceVat,
    ISNULL((SELECT Code FROM RetailCurrency), 'UAH') AS RetailCurrencyCode,
    p.Updated
FROM Product p
LEFT JOIN ProductSlug ps ON ps.ProductID = p.ID AND ps.Locale = 'uk' AND ps.Deleted = 0
LEFT JOIN ProductGroups pg ON pg.ProductID = p.ID
-- Join to get base pricing IDs
LEFT JOIN BasePricingIds bpi ON bpi.OriginalPricingId = 853
LEFT JOIN BasePricingIds bpiv ON bpiv.OriginalPricingId = 848
-- Get product price from BASE pricing
LEFT JOIN ProductPricing pp ON pp.ProductID = p.ID AND pp.PricingID = bpi.BasePricingId AND pp.Deleted = 0
LEFT JOIN ProductPricing ppv ON ppv.ProductID = p.ID AND ppv.PricingID = bpiv.BasePricingId AND ppv.Deleted = 0
-- Get extra charge from ORIGINAL pricing
LEFT JOIN OriginalPricingCharge opc ON opc.PricingId = 853
LEFT JOIN OriginalPricingCharge opcv ON opcv.PricingId = 848
WHERE p.Deleted = 0
ORDER BY p.ID";

        IEnumerable<ProductSyncData> products = await connection.QueryAsync<ProductSyncData>(sql, commandTimeout: 600);
        return products.AsList();
    }

    public async Task<List<ProductSyncData>> GetChangedProductsAsync(DateTime since) {
        using IDbConnection connection = connectionFactory();
        connection.Open();

        const string sql = @"
;WITH ChangedProductIds AS (
    SELECT p.ID FROM Product p WHERE p.Deleted = 0 AND p.Updated > @Since
    UNION
    SELECT pon.ProductID AS ID FROM ProductOriginalNumber pon INNER JOIN Product p ON p.ID = pon.ProductID AND p.Deleted = 0 WHERE pon.Updated > @Since OR pon.Created > @Since
    UNION
    SELECT pon.ProductID AS ID FROM OriginalNumber on_ INNER JOIN ProductOriginalNumber pon ON pon.OriginalNumberID = on_.ID AND pon.Deleted = 0 INNER JOIN Product p ON p.ID = pon.ProductID AND p.Deleted = 0 WHERE on_.Updated > @Since
    UNION
    SELECT pa.ProductID AS ID FROM ProductAvailability pa INNER JOIN Product p ON p.ID = pa.ProductID AND p.Deleted = 0 WHERE pa.Updated > @Since
),
BasePricingHierarchy AS (
    SELECT pr.ID AS OriginalPricingId, pr.ID AS CurrentPricingId, pr.BasePricingID FROM Pricing pr WHERE pr.Deleted = 0 AND pr.ID IN (853, 848)
    UNION ALL
    SELECT bph.OriginalPricingId, pr.ID, pr.BasePricingID FROM Pricing pr INNER JOIN BasePricingHierarchy bph ON pr.ID = bph.BasePricingID WHERE pr.Deleted = 0
),
BasePricingIds AS (SELECT OriginalPricingId, CurrentPricingId AS BasePricingId FROM BasePricingHierarchy WHERE BasePricingID IS NULL),
OriginalPricingCharge AS (SELECT pr.ID AS PricingId, pr.CalculatedExtraCharge FROM Pricing pr WHERE pr.Deleted = 0 AND pr.ID IN (853, 848)),
ProductGroups AS (SELECT ppg.ProductID, ppg.ProductGroupID FROM ProductProductGroup ppg WHERE ppg.Deleted = 0),
RetailCurrency AS (SELECT TOP 1 c.Code FROM Currency c WHERE c.Deleted = 0 AND c.Code = 'UAH')
SELECT
    p.ID AS Id, p.NetUID AS NetUid, p.VendorCode,
    ISNULL(p.SearchVendorCode, '') AS SearchVendorCode,
    ISNULL(p.Name, '') AS Name, ISNULL(p.NameUA, '') AS NameUA,
    ISNULL(p.Description, '') AS Description, ISNULL(p.DescriptionUA, '') AS DescriptionUA,
    ISNULL(p.MainOriginalNumber, '') AS MainOriginalNumber, ISNULL(p.Size, '') AS Size,
    LTRIM(RTRIM(CONCAT(ISNULL(p.SynonymsUA, ''), ' ', ISNULL(p.SearchSynonymsUA, '')))) AS Synonyms,
    ISNULL(p.SearchName, '') AS SearchName, ISNULL(p.SearchNameUA, '') AS SearchNameUA,
    ISNULL(p.SearchDescription, '') AS SearchDescription, ISNULL(p.SearchDescriptionUA, '') AS SearchDescriptionUA,
    ISNULL(p.SearchSize, '') AS SearchSize,
    ISNULL(p.PackingStandard, '') AS PackingStandard, ISNULL(p.OrderStandard, '') AS OrderStandard,
    ISNULL(p.UCGFEA, '') AS Ucgfea, ISNULL(p.Volume, '') AS Volume,
    ISNULL(p.[Top], '') AS [Top], ISNULL(p.Weight, 0) AS Weight,
    p.HasAnalogue, p.HasComponent, p.HasImage, ISNULL(p.Image, '') AS Image, p.MeasureUnitID AS MeasureUnitId,
    ISNULL((SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0 AND s.Locale = 'uk' AND s.ForVatProducts = 0), 0) AS AvailableQtyUk,
    ISNULL((SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0 AND s.Locale = 'uk' AND s.ForVatProducts = 1), 0) AS AvailableQtyUkVat,
    ISNULL((SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0 AND s.Locale = 'pl' AND s.ForVatProducts = 0), 0) AS AvailableQtyPl,
    ISNULL((SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0 AND s.Locale = 'pl' AND s.ForVatProducts = 1), 0) AS AvailableQtyPlVat,
    ISNULL((SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0), 0) AS AvailableQty,
    p.IsForWeb, p.IsForSale, p.IsForZeroSale,
    ISNULL(ps.ID, 0) AS SlugId, ISNULL(ps.NetUID, '00000000-0000-0000-0000-000000000000') AS SlugNetUid,
    ISNULL(ps.Url, '') AS SlugUrl, ISNULL(ps.Locale, '') AS SlugLocale,
    ISNULL(ROUND(pp.Price + (pp.Price * COALESCE((SELECT TOP 1 ppgd.CalculatedExtraCharge FROM PricingProductGroupDiscount ppgd WHERE ppgd.PricingID = 853 AND ppgd.ProductGroupID = pg.ProductGroupID AND ppgd.Deleted = 0), opc.CalculatedExtraCharge, 0) / 100.0), 2), 0) AS RetailPrice,
    ISNULL(ROUND(ppv.Price + (ppv.Price * COALESCE((SELECT TOP 1 ppgd.CalculatedExtraCharge FROM PricingProductGroupDiscount ppgd WHERE ppgd.PricingID = 848 AND ppgd.ProductGroupID = pg.ProductGroupID AND ppgd.Deleted = 0), opcv.CalculatedExtraCharge, 0) / 100.0), 2), 0) AS RetailPriceVat,
    ISNULL((SELECT Code FROM RetailCurrency), 'UAH') AS RetailCurrencyCode, p.Updated
FROM Product p
INNER JOIN ChangedProductIds c ON c.ID = p.ID
LEFT JOIN ProductSlug ps ON ps.ProductID = p.ID AND ps.Locale = 'uk' AND ps.Deleted = 0
LEFT JOIN ProductGroups pg ON pg.ProductID = p.ID
LEFT JOIN BasePricingIds bpi ON bpi.OriginalPricingId = 853
LEFT JOIN BasePricingIds bpiv ON bpiv.OriginalPricingId = 848
LEFT JOIN ProductPricing pp ON pp.ProductID = p.ID AND pp.PricingID = bpi.BasePricingId AND pp.Deleted = 0
LEFT JOIN ProductPricing ppv ON ppv.ProductID = p.ID AND ppv.PricingID = bpiv.BasePricingId AND ppv.Deleted = 0
LEFT JOIN OriginalPricingCharge opc ON opc.PricingId = 853
LEFT JOIN OriginalPricingCharge opcv ON opcv.PricingId = 848
WHERE p.Deleted = 0
ORDER BY p.ID";

        IEnumerable<ProductSyncData> products = await connection.QueryAsync<ProductSyncData>(sql, new { Since = since }, commandTimeout: 300);
        return products.AsList();
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

        const int batchSize = 2000;
        for (int i = 0; i < productIdsList.Count; i += batchSize) {
            List<long> batch = productIdsList.Skip(i).Take(batchSize).ToList();

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

    public DateTime Updated { get; set; }
}
