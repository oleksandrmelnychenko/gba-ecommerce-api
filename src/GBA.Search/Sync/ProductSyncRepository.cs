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
;WITH RetailConfiguration AS (
    SELECT TOP (1)
        ca.NetUID AS AgreementNetUid,
        a.PricingID AS PricingId,
        a.WithVATAccounting AS WithVat,
        c.Code AS CurrencyCode
    FROM Storage s
    INNER JOIN Agreement a
        ON a.OrganizationID = s.OrganizationID
        AND a.WithVATAccounting = s.ForVatProducts
        AND a.Deleted = 0
    INNER JOIN ClientAgreement ca
        ON ca.AgreementID = a.ID
        AND ca.Deleted = 0
    INNER JOIN Client client
        ON client.ID = ca.ClientID
        AND client.IsForRetail = 1
        AND client.Deleted = 0
    INNER JOIN Currency c
        ON c.ID = a.CurrencyID
        AND c.Deleted = 0
    WHERE s.Deleted = 0
      AND s.ForEcommerce = 1
    ORDER BY s.RetailPriority, ca.ID
),
BasePricingHierarchy AS (
    SELECT rc.PricingId AS OriginalPricingId, pr.ID AS CurrentPricingId, pr.BasePricingID
    FROM RetailConfiguration rc
    INNER JOIN Pricing pr ON pr.ID = rc.PricingId AND pr.Deleted = 0
    UNION ALL
    SELECT bph.OriginalPricingId, pr.ID, pr.BasePricingID
    FROM Pricing pr
    INNER JOIN BasePricingHierarchy bph ON pr.ID = bph.BasePricingID
    WHERE pr.Deleted = 0
),
BasePricingIds AS (
    SELECT OriginalPricingId, CurrentPricingId AS BasePricingId
    FROM BasePricingHierarchy
    WHERE BasePricingID IS NULL
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
    ISNULL(ROUND(
        pp.Price + (pp.Price * COALESCE(
            charge.CalculatedExtraCharge, pricing.CalculatedExtraCharge, 0) / 100.0)
    , 2), 0) AS RetailPrice,
    ISNULL(ROUND(
        pp.Price + (pp.Price * COALESCE(
            charge.CalculatedExtraCharge, pricing.CalculatedExtraCharge, 0) / 100.0)
    , 2), 0) AS RetailPriceVat,
    ISNULL(rc.CurrencyCode, 'EUR') AS RetailCurrencyCode,
    p.Updated
FROM Product p
OUTER APPLY (
    SELECT TOP (1) ps.ID, ps.NetUID, ps.Url, ps.Locale
    FROM ProductSlug ps
    WHERE ps.ProductID = p.ID
      AND ps.Locale = 'uk'
      AND ps.Deleted = 0
    ORDER BY ps.Updated DESC, ps.ID DESC
) ps
LEFT JOIN RetailConfiguration rc ON 1 = 1
OUTER APPLY (
    SELECT TOP (1) ppg.ProductGroupID
    FROM ProductProductGroup ppg
    WHERE ppg.ProductID = p.ID
      AND ppg.Deleted = 0
) pg
OUTER APPLY (
    SELECT TOP (1) ppgd.CalculatedExtraCharge
    FROM PricingProductGroupDiscount ppgd
    WHERE ppgd.PricingID = rc.PricingId
      AND ppgd.ProductGroupID = pg.ProductGroupID
      AND ppgd.Deleted = 0
) charge
LEFT JOIN BasePricingIds bpi ON bpi.OriginalPricingId = rc.PricingId
OUTER APPLY (
    SELECT TOP (1) pp.Price
    FROM ProductPricing pp
    WHERE pp.ProductID = p.ID
      AND pp.PricingID = bpi.BasePricingId
      AND pp.Deleted = 0
    ORDER BY pp.Updated DESC, pp.ID DESC
) pp
LEFT JOIN Pricing pricing ON pricing.ID = rc.PricingId AND pricing.Deleted = 0
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
    UNION
    SELECT pp.ProductID AS ID FROM ProductPricing pp INNER JOIN Product p ON p.ID = pp.ProductID AND p.Deleted = 0 WHERE pp.Updated > @Since OR pp.Created > @Since
    UNION
    SELECT ppg.ProductID AS ID FROM ProductProductGroup ppg INNER JOIN Product p ON p.ID = ppg.ProductID AND p.Deleted = 0 WHERE ppg.Updated > @Since OR ppg.Created > @Since
    UNION
    SELECT ps.ProductID AS ID FROM ProductSlug ps INNER JOIN Product p ON p.ID = ps.ProductID AND p.Deleted = 0 WHERE ps.Updated > @Since OR ps.Created > @Since
    UNION
    SELECT ppg.ProductID AS ID
    FROM PricingProductGroupDiscount ppgd
    INNER JOIN ProductProductGroup ppg ON ppg.ProductGroupID = ppgd.ProductGroupID AND ppg.Deleted = 0
    INNER JOIN Product p ON p.ID = ppg.ProductID AND p.Deleted = 0
    WHERE ppgd.Updated > @Since OR ppgd.Created > @Since
    UNION
    SELECT ppg.ProductID AS ID
    FROM ProductGroupDiscount pgd
    INNER JOIN ClientAgreement ca ON ca.ID = pgd.ClientAgreementID AND ca.Deleted = 0
    INNER JOIN Client client ON client.ID = ca.ClientID AND client.IsForRetail = 1 AND client.Deleted = 0
    INNER JOIN ProductProductGroup ppg ON ppg.ProductGroupID = pgd.ProductGroupID AND ppg.Deleted = 0
    INNER JOIN Product p ON p.ID = ppg.ProductID AND p.Deleted = 0
    WHERE pgd.Updated > @Since OR pgd.Created > @Since
),
RetailConfiguration AS (
    SELECT TOP (1)
        ca.NetUID AS AgreementNetUid,
        a.PricingID AS PricingId,
        a.WithVATAccounting AS WithVat,
        c.Code AS CurrencyCode
    FROM Storage s
    INNER JOIN Agreement a
        ON a.OrganizationID = s.OrganizationID
        AND a.WithVATAccounting = s.ForVatProducts
        AND a.Deleted = 0
    INNER JOIN ClientAgreement ca
        ON ca.AgreementID = a.ID
        AND ca.Deleted = 0
    INNER JOIN Client client
        ON client.ID = ca.ClientID
        AND client.IsForRetail = 1
        AND client.Deleted = 0
    INNER JOIN Currency c
        ON c.ID = a.CurrencyID
        AND c.Deleted = 0
    WHERE s.Deleted = 0
      AND s.ForEcommerce = 1
    ORDER BY s.RetailPriority, ca.ID
),
BasePricingHierarchy AS (
    SELECT rc.PricingId AS OriginalPricingId, pr.ID AS CurrentPricingId, pr.BasePricingID
    FROM RetailConfiguration rc
    INNER JOIN Pricing pr ON pr.ID = rc.PricingId AND pr.Deleted = 0
    UNION ALL
    SELECT bph.OriginalPricingId, pr.ID, pr.BasePricingID
    FROM Pricing pr
    INNER JOIN BasePricingHierarchy bph ON pr.ID = bph.BasePricingID
    WHERE pr.Deleted = 0
),
BasePricingIds AS (
    SELECT OriginalPricingId, CurrentPricingId AS BasePricingId
    FROM BasePricingHierarchy
    WHERE BasePricingID IS NULL
)
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
    ISNULL(ROUND(
        pp.Price + (pp.Price * COALESCE(
            charge.CalculatedExtraCharge, pricing.CalculatedExtraCharge, 0) / 100.0)
    , 2), 0) AS RetailPrice,
    ISNULL(ROUND(
        pp.Price + (pp.Price * COALESCE(
            charge.CalculatedExtraCharge, pricing.CalculatedExtraCharge, 0) / 100.0)
    , 2), 0) AS RetailPriceVat,
    ISNULL(rc.CurrencyCode, 'EUR') AS RetailCurrencyCode, p.Updated
FROM Product p
INNER JOIN ChangedProductIds c ON c.ID = p.ID
OUTER APPLY (
    SELECT TOP (1) ps.ID, ps.NetUID, ps.Url, ps.Locale
    FROM ProductSlug ps
    WHERE ps.ProductID = p.ID
      AND ps.Locale = 'uk'
      AND ps.Deleted = 0
    ORDER BY ps.Updated DESC, ps.ID DESC
) ps
LEFT JOIN RetailConfiguration rc ON 1 = 1
OUTER APPLY (
    SELECT TOP (1) ppg.ProductGroupID
    FROM ProductProductGroup ppg
    WHERE ppg.ProductID = p.ID
      AND ppg.Deleted = 0
) pg
OUTER APPLY (
    SELECT TOP (1) ppgd.CalculatedExtraCharge
    FROM PricingProductGroupDiscount ppgd
    WHERE ppgd.PricingID = rc.PricingId
      AND ppgd.ProductGroupID = pg.ProductGroupID
      AND ppgd.Deleted = 0
) charge
LEFT JOIN BasePricingIds bpi ON bpi.OriginalPricingId = rc.PricingId
OUTER APPLY (
    SELECT TOP (1) pp.Price
    FROM ProductPricing pp
    WHERE pp.ProductID = p.ID
      AND pp.PricingID = bpi.BasePricingId
      AND pp.Deleted = 0
    ORDER BY pp.Updated DESC, pp.ID DESC
) pp
LEFT JOIN Pricing pricing ON pricing.ID = rc.PricingId AND pricing.Deleted = 0
WHERE p.Deleted = 0
ORDER BY p.ID";

        IEnumerable<ProductSyncData> products = await connection.QueryAsync<ProductSyncData>(sql, new { Since = since }, commandTimeout: 300);
        return products.AsList();
    }

    public async Task<List<long>> GetChangedProductIdsAsync(DateTime since) {
        using IDbConnection connection = connectionFactory();
        connection.Open();

        const string sql = @"
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
SELECT ps.ProductID FROM ProductSlug ps INNER JOIN Product p ON p.ID = ps.ProductID AND p.Deleted = 0 WHERE ps.Updated > @Since OR ps.Created > @Since
UNION
SELECT ppg.ProductID
FROM PricingProductGroupDiscount ppgd
INNER JOIN ProductProductGroup ppg ON ppg.ProductGroupID = ppgd.ProductGroupID AND ppg.Deleted = 0
INNER JOIN Product p ON p.ID = ppg.ProductID AND p.Deleted = 0
WHERE ppgd.Updated > @Since OR ppgd.Created > @Since
UNION
SELECT ppg.ProductID
FROM ProductGroupDiscount pgd
INNER JOIN ClientAgreement ca ON ca.ID = pgd.ClientAgreementID AND ca.Deleted = 0
INNER JOIN Client client ON client.ID = ca.ClientID AND client.IsForRetail = 1 AND client.Deleted = 0
INNER JOIN ProductProductGroup ppg ON ppg.ProductGroupID = pgd.ProductGroupID AND ppg.Deleted = 0
INNER JOIN Product p ON p.ID = ppg.ProductID AND p.Deleted = 0
WHERE pgd.Updated > @Since OR pgd.Created > @Since";

        IEnumerable<long> ids = await connection.QueryAsync<long>(sql, new { Since = since }, commandTimeout: 120);
        return ids.AsList();
    }

    public async Task<List<ProductSyncData>> GetProductsByIdsAsync(IReadOnlyCollection<long> ids) {
        if (ids.Count == 0) return new List<ProductSyncData>();

        using IDbConnection connection = connectionFactory();
        connection.Open();

        const string sql = @"
;WITH ChangedProductIds AS (
    SELECT p.ID FROM Product p WHERE p.Deleted = 0 AND p.ID IN @Ids
),
RetailConfiguration AS (
    SELECT TOP (1)
        ca.NetUID AS AgreementNetUid,
        a.PricingID AS PricingId,
        a.WithVATAccounting AS WithVat,
        c.Code AS CurrencyCode
    FROM Storage s
    INNER JOIN Agreement a
        ON a.OrganizationID = s.OrganizationID
        AND a.WithVATAccounting = s.ForVatProducts
        AND a.Deleted = 0
    INNER JOIN ClientAgreement ca
        ON ca.AgreementID = a.ID
        AND ca.Deleted = 0
    INNER JOIN Client client
        ON client.ID = ca.ClientID
        AND client.IsForRetail = 1
        AND client.Deleted = 0
    INNER JOIN Currency c
        ON c.ID = a.CurrencyID
        AND c.Deleted = 0
    WHERE s.Deleted = 0
      AND s.ForEcommerce = 1
    ORDER BY s.RetailPriority, ca.ID
),
BasePricingHierarchy AS (
    SELECT rc.PricingId AS OriginalPricingId, pr.ID AS CurrentPricingId, pr.BasePricingID
    FROM RetailConfiguration rc
    INNER JOIN Pricing pr ON pr.ID = rc.PricingId AND pr.Deleted = 0
    UNION ALL
    SELECT bph.OriginalPricingId, pr.ID, pr.BasePricingID
    FROM Pricing pr
    INNER JOIN BasePricingHierarchy bph ON pr.ID = bph.BasePricingID
    WHERE pr.Deleted = 0
),
BasePricingIds AS (
    SELECT OriginalPricingId, CurrentPricingId AS BasePricingId
    FROM BasePricingHierarchy
    WHERE BasePricingID IS NULL
)
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
    ISNULL(ROUND(
        pp.Price + (pp.Price * COALESCE(
            charge.CalculatedExtraCharge, pricing.CalculatedExtraCharge, 0) / 100.0)
    , 2), 0) AS RetailPrice,
    ISNULL(ROUND(
        pp.Price + (pp.Price * COALESCE(
            charge.CalculatedExtraCharge, pricing.CalculatedExtraCharge, 0) / 100.0)
    , 2), 0) AS RetailPriceVat,
    ISNULL(rc.CurrencyCode, 'EUR') AS RetailCurrencyCode, p.Updated
FROM Product p
INNER JOIN ChangedProductIds c ON c.ID = p.ID
OUTER APPLY (
    SELECT TOP (1) ps.ID, ps.NetUID, ps.Url, ps.Locale
    FROM ProductSlug ps
    WHERE ps.ProductID = p.ID
      AND ps.Locale = 'uk'
      AND ps.Deleted = 0
    ORDER BY ps.Updated DESC, ps.ID DESC
) ps
LEFT JOIN RetailConfiguration rc ON 1 = 1
OUTER APPLY (
    SELECT TOP (1) ppg.ProductGroupID
    FROM ProductProductGroup ppg
    WHERE ppg.ProductID = p.ID
      AND ppg.Deleted = 0
) pg
OUTER APPLY (
    SELECT TOP (1) ppgd.CalculatedExtraCharge
    FROM PricingProductGroupDiscount ppgd
    WHERE ppgd.PricingID = rc.PricingId
      AND ppgd.ProductGroupID = pg.ProductGroupID
      AND ppgd.Deleted = 0
) charge
LEFT JOIN BasePricingIds bpi ON bpi.OriginalPricingId = rc.PricingId
OUTER APPLY (
    SELECT TOP (1) pp.Price
    FROM ProductPricing pp
    WHERE pp.ProductID = p.ID
      AND pp.PricingID = bpi.BasePricingId
      AND pp.Deleted = 0
    ORDER BY pp.Updated DESC, pp.ID DESC
) pp
LEFT JOIN Pricing pricing ON pricing.ID = rc.PricingId AND pricing.Deleted = 0
WHERE p.Deleted = 0
ORDER BY p.ID";

        IEnumerable<ProductSyncData> products = await connection.QueryAsync<ProductSyncData>(sql, new { Ids = ids }, commandTimeout: 120);
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
    public string RetailCurrencyCode { get; set; } = "EUR";

    public DateTime Updated { get; set; }
}
