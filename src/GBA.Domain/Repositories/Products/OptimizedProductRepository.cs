using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Repositories.Products;

/// <summary>
/// Optimized repository for fetching products by IDs with set-based price calculation.
/// Implements FULL business logic from GetCalculatedProductPriceWithSharesAndVat UDF:
/// - Promotional products (IsForSale, IsForZeroSale, Top='X9'/'Х9')
/// - PromotionalPricingId from Agreement for promotional items
/// - Client-specific ProductGroupDiscount
/// - PricingProductGroupDiscount per product group
/// - Full BasePricingId hierarchy traversal
/// </summary>
public sealed class OptimizedProductRepository {
    private readonly IDbConnection _connection;

    public OptimizedProductRepository(IDbConnection connection) {
        _connection = connection;
    }

    /// <summary>
    /// Gets only calculated prices for products (lightweight query for V3 search).
    /// Uses the same UDF as V1 to ensure price consistency.
    /// </summary>
    public Dictionary<long, ProductPriceInfo> GetPricesOnly(
        List<long> productIds,
        Guid clientAgreementNetId,
        long? organizationId,
        bool withVat,
        string culture = "uk") {

        if (productIds == null || productIds.Count == 0)
            return new Dictionary<long, ProductPriceInfo>();

        var tempTableSql = new System.Text.StringBuilder();
        tempTableSql.AppendLine("CREATE TABLE #ProductIds (Id BIGINT PRIMARY KEY);");

        const int batchSize = 1000;
        for (int i = 0; i < productIds.Count; i += batchSize) {
            var batch = productIds.Skip(i).Take(batchSize).ToList();
            var values = string.Join(",", batch.Select(id => $"({id})"));
            tempTableSql.AppendLine($"INSERT INTO #ProductIds (Id) VALUES {values};");
        }

        // Use the same UDF as V1 for consistent price calculation
        const string sql = @"
SELECT
    p.ID AS Id,
    dbo.GetCalculatedProductPriceWithSharesAndVat(p.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS Price,
    (SELECT Code FROM Currency c
     INNER JOIN Agreement a ON a.CurrencyID = c.ID
     INNER JOIN ClientAgreement ca ON ca.AgreementID = a.ID
     WHERE ca.NetUID = @ClientAgreementNetId AND c.Deleted = 0) AS CurrencyCode
FROM Product p
INNER JOIN #ProductIds ids ON ids.Id = p.ID
WHERE p.Deleted = 0;

DROP TABLE #ProductIds;";

        var fullSql = tempTableSql.ToString() + sql;

        var results = _connection.Query<dynamic>(fullSql, new {
            ClientAgreementNetId = clientAgreementNetId,
            Culture = culture,
            WithVat = withVat
        }).ToList();

        return results.ToDictionary(
            r => (long)r.Id,
            r => new ProductPriceInfo {
                Price = (decimal)(r.Price ?? 0m),
                CurrencyCode = r.CurrencyCode ?? "UAH"
            });
    }

    /// <summary>
    /// Gets prices for retail (anonymous) users using default pricing.
    /// Uses the same UDF as V1 to ensure price consistency.
    /// </summary>
    public Dictionary<long, ProductPriceInfo> GetPricesOnlyForRetail(
        List<long> productIds,
        Guid retailAgreementNetId,
        long currencyId,
        bool withVat,
        string culture = "uk") {

        if (productIds == null || productIds.Count == 0)
            return new Dictionary<long, ProductPriceInfo>();

        var tempTableSql = new System.Text.StringBuilder();
        tempTableSql.AppendLine("CREATE TABLE #ProductIds (Id BIGINT PRIMARY KEY);");

        const int batchSize = 1000;
        for (int i = 0; i < productIds.Count; i += batchSize) {
            var batch = productIds.Skip(i).Take(batchSize).ToList();
            var values = string.Join(",", batch.Select(id => $"({id})"));
            tempTableSql.AppendLine($"INSERT INTO #ProductIds (Id) VALUES {values};");
        }

        // Use the same UDF as V1 for consistent price calculation
        const string sql = @"
SELECT
    p.ID AS Id,
    dbo.GetCalculatedProductPriceWithSharesAndVat(p.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS Price,
    (SELECT Code FROM Currency WHERE ID = @CurrencyId AND Deleted = 0) AS CurrencyCode
FROM Product p
INNER JOIN #ProductIds ids ON ids.Id = p.ID
WHERE p.Deleted = 0;

DROP TABLE #ProductIds;";

        var fullSql = tempTableSql.ToString() + sql;

        var results = _connection.Query<dynamic>(fullSql, new {
            ClientAgreementNetId = retailAgreementNetId,
            CurrencyId = currencyId,
            Culture = culture,
            WithVat = withVat
        }).ToList();

        return results.ToDictionary(
            r => (long)r.Id,
            r => new ProductPriceInfo {
                Price = (decimal)(r.Price ?? 0m),
                CurrencyCode = r.CurrencyCode ?? "UAH"
            });
    }

    /// <summary>
    /// Gets products by IDs with prices calculated using set-based operations.
    /// Implements FULL pricing logic including:
    /// - Promotional products with special pricing
    /// - Client-specific discounts (ProductGroupDiscount)
    /// - Product group extra charges (PricingProductGroupDiscount)
    /// - Full pricing hierarchy (BasePricingId)
    /// </summary>
    public List<FromSearchProduct> GetProductsByIdsWithPrices(
        List<long> productIds,
        Guid clientAgreementNetId,
        string culture,
        bool withVat,
        long? organizationId) {

        if (productIds == null || productIds.Count == 0)
            return new List<FromSearchProduct>();

        // Build temp table creation and inserts
        var tempTableSql = new System.Text.StringBuilder();
        tempTableSql.AppendLine("CREATE TABLE #ProductIds (Id BIGINT PRIMARY KEY, RowNum INT);");

        const int batchSize = 1000;
        for (int i = 0; i < productIds.Count; i += batchSize) {
            var batch = productIds.Skip(i).Take(batchSize).ToList();
            var values = string.Join(",", batch.Select((id, idx) => $"({id},{i + idx + 1})"));
            tempTableSql.AppendLine($"INSERT INTO #ProductIds (Id, RowNum) VALUES {values};");
        }

        // Full business logic SQL matching the original UDF
        const string sql = @"
-- Get agreement data
;WITH AgreementData AS (
    SELECT
        ca.ID AS ClientAgreementId,
        ca.NetUID AS ClientAgreementNetId,
        a.PricingID,
        a.PromotionalPricingID,
        a.OrganizationID,
        a.CurrencyID,
        a.WithVATAccounting
    FROM ClientAgreement ca
    INNER JOIN Agreement a ON a.ID = ca.AgreementID
    WHERE ca.NetUID = @ClientAgreementNetId
),
-- Recursive CTE to find base pricing ID (follows BasePricingID chain)
BasePricingHierarchy AS (
    -- Starting point: the pricing from agreement
    SELECT
        p.ID AS PricingId,
        p.BasePricingID,
        p.ID AS RootPricingId,
        1 AS Level
    FROM Pricing p
    WHERE p.Deleted = 0

    UNION ALL

    -- Follow the chain
    SELECT
        p.ID,
        p.BasePricingID,
        bph.RootPricingId,
        bph.Level + 1
    FROM Pricing p
    INNER JOIN BasePricingHierarchy bph ON p.ID = bph.BasePricingID
    WHERE p.Deleted = 0 AND bph.Level < 10  -- Safety limit
),
-- Get the final base pricing ID for each pricing
FinalBasePricing AS (
    SELECT
        RootPricingId AS OriginalPricingId,
        PricingId AS BasePricingId
    FROM BasePricingHierarchy bph
    WHERE BasePricingID IS NULL  -- This is the root (no parent)
),
-- Product data with promotional flag
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
        ids.RowNum AS SearchRowNumber,
        -- Promotional product flag (matches UDF logic exactly)
        CASE
            WHEN p.IsForSale = 1 THEN 1
            WHEN p.IsForZeroSale = 1 THEN 1
            WHEN p.[Top] = N'X9' THEN 1
            WHEN p.[Top] = N'Х9' THEN 1  -- Cyrillic X
            ELSE 0
        END AS IsPromotionalProduct
    FROM Product p
    INNER JOIN #ProductIds ids ON ids.Id = p.ID
    WHERE p.Deleted = 0
),
-- Product groups
ProductGroups AS (
    SELECT ppg.ProductID, ppg.ProductGroupID
    FROM ProductProductGroup ppg
    WHERE ppg.Deleted = 0
    AND ppg.ProductID IN (SELECT Id FROM #ProductIds)
),
-- Get the correct PricingId for price lookup based on promotional status
-- For promotional products with PromotionalPricingId: use BasePricingId(PromotionalPricingId)
-- Otherwise: use BasePricingId(Agreement.PricingId)
ProductPricingIds AS (
    SELECT
        pd.ID AS ProductID,
        pd.IsPromotionalProduct,
        CASE
            WHEN pd.IsPromotionalProduct = 1 AND ad.PromotionalPricingID IS NOT NULL
                THEN COALESCE(fbp_promo.BasePricingId, ad.PromotionalPricingID)
            ELSE COALESCE(fbp_regular.BasePricingId, ad.PricingID)
        END AS EffectivePricingId,
        CASE
            WHEN pd.IsPromotionalProduct = 1 AND ad.PromotionalPricingID IS NOT NULL
                THEN 1  -- Has promotional pricing
            ELSE 0
        END AS HasPromotionalPricing
    FROM ProductData pd
    CROSS JOIN AgreementData ad
    LEFT JOIN FinalBasePricing fbp_regular ON fbp_regular.OriginalPricingId = ad.PricingID
    LEFT JOIN FinalBasePricing fbp_promo ON fbp_promo.OriginalPricingId = ad.PromotionalPricingID
),
-- Get base product prices from ProductPricing
ProductPrices AS (
    SELECT
        ppi.ProductID,
        pp.Price
    FROM ProductPricingIds ppi
    INNER JOIN ProductPricing pp ON pp.ProductID = ppi.ProductID
        AND pp.PricingID = ppi.EffectivePricingId
        AND pp.Deleted = 0
),
-- Calculate ExtraCharge based on UDF logic
-- For promotional with PromotionalPricingId: use PromotionalPricing.CalculatedExtraCharge
-- For others: use PricingProductGroupDiscount or Pricing.CalculatedExtraCharge
ExtraCharges AS (
    SELECT
        pd.ID AS ProductID,
        CASE
            -- Promotional product with PromotionalPricingId
            WHEN pd.IsPromotionalProduct = 1 AND ad.PromotionalPricingID IS NOT NULL THEN
                COALESCE(
                    (SELECT TOP 1
                        CASE
                            WHEN ppgd.CalculatedExtraCharge IS NOT NULL THEN ppgd.CalculatedExtraCharge
                            ELSE pr.CalculatedExtraCharge
                        END
                    FROM Pricing pr
                    LEFT JOIN PricingProductGroupDiscount ppgd
                        ON ppgd.PricingID = pr.ID
                        AND ppgd.ProductGroupID = pg.ProductGroupID
                        AND ppgd.Deleted = 0
                    WHERE pr.ID = ad.PromotionalPricingID AND pr.Deleted = 0),
                    0.00
                )
            -- Logged-in client (not anonymous): use their agreement pricing
            WHEN @ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000' THEN
                COALESCE(
                    (SELECT TOP 1
                        CASE
                            WHEN ppgd.CalculatedExtraCharge IS NOT NULL THEN ppgd.CalculatedExtraCharge
                            ELSE pr.CalculatedExtraCharge
                        END
                    FROM Pricing pr
                    LEFT JOIN PricingProductGroupDiscount ppgd
                        ON ppgd.PricingID = pr.ID
                        AND ppgd.ProductGroupID = pg.ProductGroupID
                        AND ppgd.Deleted = 0
                    WHERE pr.ID = ad.PricingID AND pr.Deleted = 0),
                    -- Fallback to default pricing if agreement pricing not found
                    (SELECT TOP 1
                        CASE
                            WHEN ppgd.CalculatedExtraCharge IS NOT NULL THEN ppgd.CalculatedExtraCharge
                            ELSE pr.CalculatedExtraCharge
                        END
                    FROM Pricing pr
                    LEFT JOIN PricingProductGroupDiscount ppgd
                        ON ppgd.PricingID = pr.ID
                        AND ppgd.ProductGroupID = pg.ProductGroupID
                        AND ppgd.Deleted = 0
                    WHERE pr.Deleted = 0 AND pr.Culture = @Culture AND pr.ForVat = 0
                    ORDER BY pr.CalculatedExtraCharge DESC),
                    0.00
                )
            -- Anonymous user: use default pricing with highest extra charge
            ELSE
                COALESCE(
                    (SELECT TOP 1
                        CASE
                            WHEN ppgd.CalculatedExtraCharge IS NOT NULL THEN ppgd.CalculatedExtraCharge
                            ELSE pr.CalculatedExtraCharge
                        END
                    FROM Pricing pr
                    LEFT JOIN PricingProductGroupDiscount ppgd
                        ON ppgd.PricingID = pr.ID
                        AND ppgd.ProductGroupID = pg.ProductGroupID
                        AND ppgd.Deleted = 0
                    WHERE pr.Deleted = 0 AND pr.Culture = @Culture AND pr.ForVat = 0
                    ORDER BY pr.CalculatedExtraCharge DESC),
                    0.00
                )
        END AS ExtraCharge
    FROM ProductData pd
    CROSS JOIN AgreementData ad
    LEFT JOIN ProductGroups pg ON pg.ProductID = pd.ID
),
-- Calculate DiscountRate based on UDF logic
-- For promotional with PromotionalPricingId: discount = 0
-- For others: use ProductGroupDiscount if client is logged in
Discounts AS (
    SELECT
        pd.ID AS ProductID,
        CASE
            -- Promotional product with PromotionalPricingId: NO discount
            WHEN pd.IsPromotionalProduct = 1 AND ad.PromotionalPricingID IS NOT NULL THEN 0.00
            -- Anonymous user: NO discount
            WHEN @ClientAgreementNetId = '00000000-0000-0000-0000-000000000000' THEN 0.00
            -- Logged-in client: get their ProductGroupDiscount
            ELSE COALESCE(
                (SELECT TOP 1 pgd.DiscountRate
                FROM ProductGroupDiscount pgd
                WHERE pgd.ClientAgreementID = ad.ClientAgreementId
                AND pgd.ProductGroupID = pg.ProductGroupID
                AND pgd.IsActive = 1),
                0.00
            )
        END AS DiscountRate
    FROM ProductData pd
    CROSS JOIN AgreementData ad
    LEFT JOIN ProductGroups pg ON pg.ProductID = pd.ID
),
-- Calculate final prices (matching UDF formula exactly)
CalculatedPrices AS (
    SELECT
        pp.ProductID,
        -- Formula: (Price + Price * ExtraCharge/100) - (Price + Price * ExtraCharge/100) * DiscountRate/100
        ROUND(
            ROUND(pp.Price + (pp.Price * ISNULL(ec.ExtraCharge, 0) / 100), 14)
            - ROUND(pp.Price + (pp.Price * ISNULL(ec.ExtraCharge, 0) / 100), 14) * ISNULL(d.DiscountRate, 0) / 100
        , 2) AS CalculatedPrice,
        pp.Price AS BasePrice,
        ec.ExtraCharge,
        d.DiscountRate
    FROM ProductPrices pp
    LEFT JOIN ExtraCharges ec ON ec.ProductID = pp.ProductID
    LEFT JOIN Discounts d ON d.ProductID = pp.ProductID
),
-- Availability
Availability AS (
    SELECT
        pa.ProductID,
        SUM(CASE WHEN s.Locale = 'uk' AND s.ForVatProducts = 1 THEN pa.Amount ELSE 0 END) AS AvailableQtyUkVAT,
        SUM(CASE WHEN s.Locale = 'uk' AND s.ForVatProducts = 0 THEN pa.Amount ELSE 0 END) AS AvailableQtyUkNonVAT,
        SUM(CASE WHEN s.Locale = 'pl' AND s.ForVatProducts = 1 THEN pa.Amount ELSE 0 END) AS AvailableQtyPlVAT,
        SUM(CASE WHEN s.Locale = 'pl' AND s.ForVatProducts = 0 THEN pa.Amount ELSE 0 END) AS AvailableQtyPlNonVAT
    FROM ProductAvailability pa
    INNER JOIN Storage s ON s.ID = pa.StorageID
        AND s.ForDefective = 0
        AND s.OrganizationID = @OrganizationId
    WHERE pa.ProductID IN (SELECT Id FROM #ProductIds)
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
    ISNULL(cp.CalculatedPrice, 0) AS CurrentPrice,
    ISNULL(cp.CalculatedPrice, 0) AS CurrentLocalPrice,
    ISNULL(cp.CalculatedPrice, 0) AS CurrentWithVatPrice,
    ISNULL(cp.CalculatedPrice, 0) AS CurrentLocalWithVatPrice,
    (SELECT Code FROM Currency WHERE ID = (SELECT CurrencyID FROM AgreementData) AND Deleted = 0) AS CurrencyCode,
    pd.SearchRowNumber,
    pd.IsPromotionalProduct,
    cp.BasePrice,
    cp.ExtraCharge,
    cp.DiscountRate,
    ps.ID AS SlugId,
    ps.NetUID AS SlugNetUid,
    ps.Url,
    ps.Locale,
    ps.ProductID AS SlugProductId,
    ps.Created AS SlugCreated,
    ps.Updated AS SlugUpdated,
    ps.Deleted AS SlugDeleted
FROM ProductData pd
LEFT JOIN CalculatedPrices cp ON cp.ProductID = pd.ID
LEFT JOIN Availability av ON av.ProductID = pd.ID
LEFT JOIN ProductSlug ps ON ps.ProductID = pd.ID AND ps.Locale = @Culture AND ps.Deleted = 0
ORDER BY pd.SearchRowNumber;

DROP TABLE #ProductIds;";

        // Get agreement data for parameters
        var agreementData = _connection.QueryFirstOrDefault<dynamic>(@"
SELECT
    a.OrganizationID,
    a.CurrencyID
FROM ClientAgreement ca
INNER JOIN Agreement a ON a.ID = ca.AgreementID
WHERE ca.NetUID = @ClientAgreementNetId",
            new { ClientAgreementNetId = clientAgreementNetId });

        var fullSql = tempTableSql.ToString() + sql;

        var results = _connection.Query<dynamic>(fullSql, new {
            ClientAgreementNetId = clientAgreementNetId,
            Culture = culture,
            OrganizationId = organizationId ?? agreementData?.OrganizationID,
            WithVat = withVat
        }).ToList();

        var products = results.Select(r => {
            var product = new FromSearchProduct {
                Id = (long)r.ID,
                NetUid = (Guid)r.NetUid,
                VendorCode = r.VendorCode,
                Name = r.Name,
                Description = r.Description,
                Size = r.Size,
                PackingStandard = r.PackingStandard,
                OrderStandard = r.OrderStandard,
                UCGFEA = r.UCGFEA,
                Volume = r.Volume,
                Top = r.Top,
                AvailableQtyUk = (double)r.AvailableQtyUk,
                AvailableQtyRoad = (double)r.AvailableQtyRoad,
                AvailableQtyUkVAT = (double)r.AvailableQtyUkVAT,
                AvailableQtyPl = (double)r.AvailableQtyPl,
                AvailableQtyPlVAT = (double)r.AvailableQtyPlVAT,
                Weight = (double)r.Weight,
                HasAnalogue = (bool)r.HasAnalogue,
                HasComponent = (bool)r.HasComponent,
                HasImage = (bool)r.HasImage,
                IsForWeb = (bool)r.IsForWeb,
                IsForSale = (bool)r.IsForSale,
                IsForZeroSale = (bool)r.IsForZeroSale,
                MainOriginalNumber = r.MainOriginalNumber,
                Image = r.Image,
                MeasureUnitId = (long)r.MeasureUnitId,
                CurrentPrice = (decimal)r.CurrentPrice,
                CurrentLocalPrice = (decimal)r.CurrentLocalPrice,
                CurrentWithVatPrice = (decimal)r.CurrentWithVatPrice,
                CurrentLocalWithVatPrice = (decimal)r.CurrentLocalWithVatPrice,
                CurrencyCode = r.CurrencyCode,
                SearchRowNumber = (long)r.SearchRowNumber
            };

            if (r.SlugId != null && (long)r.SlugId > 0) {
                product.ProductSlug = new ProductSlug {
                    Id = (long)r.SlugId,
                    NetUid = (Guid)r.SlugNetUid,
                    Url = r.Url,
                    Locale = r.Locale,
                    ProductId = (long)r.SlugProductId,
                    Created = (DateTime)r.SlugCreated,
                    Updated = (DateTime)r.SlugUpdated,
                    Deleted = (bool)r.SlugDeleted
                };
            }

            return product;
        }).ToList();

        return products;
    }

    /// <summary>
    /// For retail (anonymous) users - uses default pricing without client-specific discounts.
    /// </summary>
    public List<FromSearchProduct> GetProductsByIdsForRetail(
        List<long> productIds,
        Guid clientAgreementNetId,
        string culture,
        long organizationId,
        long currencyId,
        long pricingId) {

        if (productIds == null || productIds.Count == 0)
            return new List<FromSearchProduct>();

        var tempTableSql = new System.Text.StringBuilder();
        tempTableSql.AppendLine("CREATE TABLE #ProductIds (Id BIGINT PRIMARY KEY, RowNum INT);");

        const int batchSize = 1000;
        for (int i = 0; i < productIds.Count; i += batchSize) {
            var batch = productIds.Skip(i).Take(batchSize).ToList();
            var values = string.Join(",", batch.Select((id, idx) => $"({id},{i + idx + 1})"));
            tempTableSql.AppendLine($"INSERT INTO #ProductIds (Id, RowNum) VALUES {values};");
        }

        // Full business logic SQL for retail - same as logged-in users but with retail agreement
        const string sql = @"
;WITH AgreementData AS (
    SELECT
        ca.ID AS ClientAgreementId,
        ca.NetUID AS ClientAgreementNetId,
        a.PricingID,
        a.PromotionalPricingID,
        a.OrganizationID,
        a.CurrencyID
    FROM ClientAgreement ca
    INNER JOIN Agreement a ON a.ID = ca.AgreementID
    WHERE ca.NetUID = @ClientAgreementNetId
),
BasePricingHierarchy AS (
    SELECT
        p.ID AS PricingId,
        p.BasePricingID,
        p.ID AS RootPricingId,
        1 AS Level
    FROM Pricing p
    WHERE p.Deleted = 0

    UNION ALL

    SELECT
        p.ID,
        p.BasePricingID,
        bph.RootPricingId,
        bph.Level + 1
    FROM Pricing p
    INNER JOIN BasePricingHierarchy bph ON p.ID = bph.BasePricingID
    WHERE p.Deleted = 0 AND bph.Level < 10
),
FinalBasePricing AS (
    SELECT
        RootPricingId AS OriginalPricingId,
        PricingId AS BasePricingId
    FROM BasePricingHierarchy bph
    WHERE BasePricingID IS NULL
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
        ids.RowNum AS SearchRowNumber,
        CASE
            WHEN p.IsForSale = 1 THEN 1
            WHEN p.IsForZeroSale = 1 THEN 1
            WHEN p.[Top] = N'X9' THEN 1
            WHEN p.[Top] = N'Х9' THEN 1
            ELSE 0
        END AS IsPromotionalProduct
    FROM Product p
    INNER JOIN #ProductIds ids ON ids.Id = p.ID
    WHERE p.Deleted = 0
),
ProductGroups AS (
    SELECT ppg.ProductID, ppg.ProductGroupID
    FROM ProductProductGroup ppg
    WHERE ppg.Deleted = 0
    AND ppg.ProductID IN (SELECT Id FROM #ProductIds)
),
ProductPricingIds AS (
    SELECT
        pd.ID AS ProductID,
        pd.IsPromotionalProduct,
        CASE
            WHEN pd.IsPromotionalProduct = 1 AND ad.PromotionalPricingID IS NOT NULL
                THEN COALESCE(fbp_promo.BasePricingId, ad.PromotionalPricingID)
            ELSE COALESCE(fbp_regular.BasePricingId, ad.PricingID)
        END AS EffectivePricingId,
        CASE
            WHEN pd.IsPromotionalProduct = 1 AND ad.PromotionalPricingID IS NOT NULL
                THEN 1
            ELSE 0
        END AS HasPromotionalPricing
    FROM ProductData pd
    CROSS JOIN AgreementData ad
    LEFT JOIN FinalBasePricing fbp_regular ON fbp_regular.OriginalPricingId = ad.PricingID
    LEFT JOIN FinalBasePricing fbp_promo ON fbp_promo.OriginalPricingId = ad.PromotionalPricingID
),
ProductPrices AS (
    SELECT
        ppi.ProductID,
        pp.Price
    FROM ProductPricingIds ppi
    INNER JOIN ProductPricing pp ON pp.ProductID = ppi.ProductID
        AND pp.PricingID = ppi.EffectivePricingId
        AND pp.Deleted = 0
),
ExtraCharges AS (
    SELECT
        pd.ID AS ProductID,
        CASE
            WHEN pd.IsPromotionalProduct = 1 AND ad.PromotionalPricingID IS NOT NULL THEN
                COALESCE(
                    (SELECT TOP 1
                        CASE
                            WHEN ppgd.CalculatedExtraCharge IS NOT NULL THEN ppgd.CalculatedExtraCharge
                            ELSE pr.CalculatedExtraCharge
                        END
                    FROM Pricing pr
                    LEFT JOIN PricingProductGroupDiscount ppgd
                        ON ppgd.PricingID = pr.ID
                        AND ppgd.ProductGroupID = pg.ProductGroupID
                        AND ppgd.Deleted = 0
                    WHERE pr.ID = ad.PromotionalPricingID AND pr.Deleted = 0),
                    0.00
                )
            WHEN @ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000' THEN
                COALESCE(
                    (SELECT TOP 1
                        CASE
                            WHEN ppgd.CalculatedExtraCharge IS NOT NULL THEN ppgd.CalculatedExtraCharge
                            ELSE pr.CalculatedExtraCharge
                        END
                    FROM Pricing pr
                    LEFT JOIN PricingProductGroupDiscount ppgd
                        ON ppgd.PricingID = pr.ID
                        AND ppgd.ProductGroupID = pg.ProductGroupID
                        AND ppgd.Deleted = 0
                    WHERE pr.ID = ad.PricingID AND pr.Deleted = 0),
                    (SELECT TOP 1
                        CASE
                            WHEN ppgd.CalculatedExtraCharge IS NOT NULL THEN ppgd.CalculatedExtraCharge
                            ELSE pr.CalculatedExtraCharge
                        END
                    FROM Pricing pr
                    LEFT JOIN PricingProductGroupDiscount ppgd
                        ON ppgd.PricingID = pr.ID
                        AND ppgd.ProductGroupID = pg.ProductGroupID
                        AND ppgd.Deleted = 0
                    WHERE pr.Deleted = 0 AND pr.Culture = @Culture AND pr.ForVat = 0
                    ORDER BY pr.CalculatedExtraCharge DESC),
                    0.00
                )
            ELSE
                COALESCE(
                    (SELECT TOP 1
                        CASE
                            WHEN ppgd.CalculatedExtraCharge IS NOT NULL THEN ppgd.CalculatedExtraCharge
                            ELSE pr.CalculatedExtraCharge
                        END
                    FROM Pricing pr
                    LEFT JOIN PricingProductGroupDiscount ppgd
                        ON ppgd.PricingID = pr.ID
                        AND ppgd.ProductGroupID = pg.ProductGroupID
                        AND ppgd.Deleted = 0
                    WHERE pr.Deleted = 0 AND pr.Culture = @Culture AND pr.ForVat = 0
                    ORDER BY pr.CalculatedExtraCharge DESC),
                    0.00
                )
        END AS ExtraCharge
    FROM ProductData pd
    CROSS JOIN AgreementData ad
    LEFT JOIN ProductGroups pg ON pg.ProductID = pd.ID
),
Discounts AS (
    SELECT
        pd.ID AS ProductID,
        CASE
            WHEN pd.IsPromotionalProduct = 1 AND ad.PromotionalPricingID IS NOT NULL THEN 0.00
            WHEN @ClientAgreementNetId = '00000000-0000-0000-0000-000000000000' THEN 0.00
            ELSE COALESCE(
                (SELECT TOP 1 pgd.DiscountRate
                FROM ProductGroupDiscount pgd
                WHERE pgd.ClientAgreementID = ad.ClientAgreementId
                AND pgd.ProductGroupID = pg.ProductGroupID
                AND pgd.IsActive = 1),
                0.00
            )
        END AS DiscountRate
    FROM ProductData pd
    CROSS JOIN AgreementData ad
    LEFT JOIN ProductGroups pg ON pg.ProductID = pd.ID
),
CalculatedPrices AS (
    SELECT
        pp.ProductID,
        ROUND(
            ROUND(pp.Price + (pp.Price * ISNULL(ec.ExtraCharge, 0) / 100), 14)
            - ROUND(pp.Price + (pp.Price * ISNULL(ec.ExtraCharge, 0) / 100), 14) * ISNULL(d.DiscountRate, 0) / 100
        , 2) AS CalculatedPrice
    FROM ProductPrices pp
    LEFT JOIN ExtraCharges ec ON ec.ProductID = pp.ProductID
    LEFT JOIN Discounts d ON d.ProductID = pp.ProductID
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
        AND s.ForDefective = 0
        AND s.OrganizationID = @OrganizationId
    WHERE pa.ProductID IN (SELECT Id FROM #ProductIds)
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
    ISNULL(cp.CalculatedPrice, 0) AS CurrentPrice,
    ISNULL(cp.CalculatedPrice, 0) AS CurrentLocalPrice,
    ISNULL(cp.CalculatedPrice, 0) AS CurrentWithVatPrice,
    ISNULL(cp.CalculatedPrice, 0) AS CurrentLocalWithVatPrice,
    (SELECT Code FROM Currency WHERE ID = @CurrencyId AND Deleted = 0) AS CurrencyCode,
    pd.SearchRowNumber,
    ps.ID AS SlugId,
    ps.NetUID AS SlugNetUid,
    ps.Url,
    ps.Locale,
    ps.ProductID AS SlugProductId,
    ps.Created AS SlugCreated,
    ps.Updated AS SlugUpdated,
    ps.Deleted AS SlugDeleted
FROM ProductData pd
LEFT JOIN CalculatedPrices cp ON cp.ProductID = pd.ID
LEFT JOIN Availability av ON av.ProductID = pd.ID
LEFT JOIN ProductSlug ps ON ps.ProductID = pd.ID AND ps.Locale = @Culture AND ps.Deleted = 0
ORDER BY pd.SearchRowNumber;

DROP TABLE #ProductIds;";

        var fullSql = tempTableSql.ToString() + sql;

        var results = _connection.Query<dynamic>(fullSql, new {
            ClientAgreementNetId = clientAgreementNetId,
            Culture = culture,
            OrganizationId = organizationId,
            CurrencyId = currencyId
        }).ToList();

        var products = results.Select(r => {
            var product = new FromSearchProduct {
                Id = (long)r.ID,
                NetUid = (Guid)r.NetUid,
                VendorCode = r.VendorCode,
                Name = r.Name,
                Description = r.Description,
                Size = r.Size,
                PackingStandard = r.PackingStandard,
                OrderStandard = r.OrderStandard,
                UCGFEA = r.UCGFEA,
                Volume = r.Volume,
                Top = r.Top,
                AvailableQtyUk = (double)r.AvailableQtyUk,
                AvailableQtyRoad = (double)r.AvailableQtyRoad,
                AvailableQtyUkVAT = (double)r.AvailableQtyUkVAT,
                AvailableQtyPl = (double)r.AvailableQtyPl,
                AvailableQtyPlVAT = (double)r.AvailableQtyPlVAT,
                Weight = (double)r.Weight,
                HasAnalogue = (bool)r.HasAnalogue,
                HasComponent = (bool)r.HasComponent,
                HasImage = (bool)r.HasImage,
                IsForWeb = (bool)r.IsForWeb,
                IsForSale = (bool)r.IsForSale,
                IsForZeroSale = (bool)r.IsForZeroSale,
                MainOriginalNumber = r.MainOriginalNumber,
                Image = r.Image,
                MeasureUnitId = (long)r.MeasureUnitId,
                CurrentPrice = (decimal)r.CurrentPrice,
                CurrentLocalPrice = (decimal)r.CurrentLocalPrice,
                CurrentWithVatPrice = (decimal)r.CurrentWithVatPrice,
                CurrentLocalWithVatPrice = (decimal)r.CurrentLocalWithVatPrice,
                CurrencyCode = r.CurrencyCode,
                SearchRowNumber = (long)r.SearchRowNumber
            };

            if (r.SlugId != null && (long)r.SlugId > 0) {
                product.ProductSlug = new ProductSlug {
                    Id = (long)r.SlugId,
                    NetUid = (Guid)r.SlugNetUid,
                    Url = r.Url,
                    Locale = r.Locale,
                    ProductId = (long)r.SlugProductId,
                    Created = (DateTime)r.SlugCreated,
                    Updated = (DateTime)r.SlugUpdated,
                    Deleted = (bool)r.SlugDeleted
                };
            }

            return product;
        }).ToList();

        return products;
    }
}

public sealed class ProductPriceInfo {
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "UAH";
}
