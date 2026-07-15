using System.Reflection;
using GBA.Search.Sync;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class ProductSyncRepositoryTests {
    [Theory]
    [InlineData(null, null)]
    [InlineData("ChangedProductIds AS (SELECT 1 AS ID)", "INNER JOIN ChangedProductIds c ON c.ID = p.ID")]
    [InlineData("RequestedProductIds AS (SELECT 1 AS ID)", "INNER JOIN RequestedProductIds c ON c.ID = p.ID")]
    public void ProductProjection_UsesFenixSourceAwarePricesForEverySyncPath(
        string? firstCte,
        string? productJoin) {
        string sql = BuildProductProjectionSql(firstCte, productJoin);

        Assert.Equal(2, CountOccurrences(sql, "GetCalculatedProductPriceForPricingSource"));
        Assert.Contains("o.PriceSourceIsAmg = 0", sql, StringComparison.Ordinal);
        Assert.Contains("ca.Deleted = 0", sql, StringComparison.Ordinal);
        Assert.Contains("a.IsActive = 1", sql, StringComparison.Ordinal);
        Assert.Contains("a.Deleted = 0", sql, StringComparison.Ordinal);
        Assert.Contains("DATALENGTH(a.SourceFenixID), 0) > 0 OR a.SourceFenixCode IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ISNULL(DATALENGTH(a.SourceAmgID), 0) = 0", sql, StringComparison.Ordinal);
        Assert.Contains("a.SourceAmgCode IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("pr.Deleted = 0", sql, StringComparison.Ordinal);
        Assert.Contains("currency.Deleted = 0", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("currency.Code = 'UAH'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("pr.ID = 853", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("pr.ID = 848", sql, StringComparison.Ordinal);
        Assert.Contains("CROSS JOIN FenixRetailAgreementPricing vat", sql, StringComparison.Ordinal);
        Assert.Contains("nonVat.RowNumber = 1", sql, StringComparison.Ordinal);
        Assert.Contains("vat.RowNumber = 1", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.NonVatAgreementNetUid", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.VatAgreementNetUid", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.CatalogOrganizationIdNonVat", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.CatalogOrganizationIdVat", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.CatalogAgreementSourceNonVat", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.CatalogAgreementSourceVat", sql, StringComparison.Ordinal);
        Assert.Contains("END AS ProductSourceFenix", sql, StringComparison.Ordinal);
        Assert.Contains("END AS ProductSourceAmg", sql, StringComparison.Ordinal);
        Assert.Contains("AS IsCanonicalFenix", sql, StringComparison.Ordinal);
        Assert.Contains("AS IsCanonicalAmg", sql, StringComparison.Ordinal);
        Assert.Contains("canonicalProduct.ID < p.ID", sql, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "= rpc.CatalogAgreementSourceNonVat",
            sql,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "= rpc.CatalogAgreementSourceVat",
            sql,
            StringComparison.Ordinal);
        Assert.Contains("rpc.CatalogAgreementNetUidNonVat", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.CatalogAgreementNetUidVat", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.CatalogPricingIdNonVat", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.CatalogPricingIdVat", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.CatalogCurrencyIdNonVat", sql, StringComparison.Ordinal);
        Assert.Contains("rpc.CatalogCurrencyIdVat", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY CASE WHEN a.IsSelected = 1 THEN 0 ELSE 1 END", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductProjection_IndexesSourceNeutralAvailabilityScopesForEveryEcommerceOrganization() {
        string sql = BuildProductProjectionSql(null, null);

        Assert.Contains("CatalogAvailability AS", sql, StringComparison.Ordinal);
        Assert.Contains("INNER JOIN Organization o ON o.ID = s.OrganizationID", sql, StringComparison.Ordinal);
        Assert.Contains("CASE WHEN o.PriceSourceIsAmg = 1 THEN 'amg' ELSE 'fenix' END", sql, StringComparison.Ordinal);
        Assert.Contains("availability.OrganizationId AS organizationId", sql, StringComparison.Ordinal);
        Assert.Contains("availability.SourceSystem AS sourceSystem", sql, StringComparison.Ordinal);
        Assert.Contains("FOR JSON PATH", sql, StringComparison.Ordinal);
        Assert.Contains("AS CatalogScopesJson", sql, StringComparison.Ordinal);
        Assert.Contains("FROM CatalogAvailability availability", sql, StringComparison.Ordinal);
        Assert.Contains("availability.SourceSystem = 'amg'", sql, StringComparison.Ordinal);
        Assert.Contains("SourceAmgID", sql, StringComparison.Ordinal);
        Assert.Contains("EXISTS (", sql, StringComparison.Ordinal);

        // Anonymous indexed prices retain their exact selected Fenix availability projection.
        Assert.Contains("FenixEcommerceStorages AS", sql, StringComparison.Ordinal);
        Assert.Contains(
            "INNER JOIN FenixRetailStorage selected",
            sql,
            StringComparison.Ordinal);
        Assert.Contains("s.Deleted = 0", sql, StringComparison.Ordinal);
        Assert.Contains("s.ForEcommerce = 1", sql, StringComparison.Ordinal);
        Assert.Contains("s.ForDefective = 0", sql, StringComparison.Ordinal);
        Assert.Contains(
            "INNER JOIN FenixEcommerceStorages s ON s.ID = pa.StorageID",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "LEFT JOIN FenixProductAvailability nonVatAvailability",
            sql,
            StringComparison.Ordinal);
        Assert.Contains("HasNonVatCatalogAvailability", sql, StringComparison.Ordinal);
        Assert.Contains("HasVatCatalogAvailability", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT SUM(pa.Amount)", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductProjection_HasSingleCardinalityWithMultiplePricingsGroupsAndSlugs() {
        string sql = BuildProductProjectionSql(null, null);

        Assert.DoesNotContain("JOIN ProductPricing ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ProductProductGroup", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LEFT JOIN ProductSlug", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ROW_NUMBER() OVER", sql, StringComparison.Ordinal);
        Assert.Contains("nonVat.RowNumber = 1", sql, StringComparison.Ordinal);
        Assert.Contains("vat.RowNumber = 1", sql, StringComparison.Ordinal);
        Assert.Contains("OUTER APPLY", sql, StringComparison.Ordinal);
        Assert.Contains("SELECT TOP (1) slug.ID", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void IncrementalChangeDetection_CoversProjectionAndPricingDependencies() {
        string sql = InvokePrivateSqlBuilder("BuildChangedProductIdsSql");

        string[] targetedDependencies = [
            "ProductOriginalNumber",
            "OriginalNumber",
            "ProductAvailability",
            "ProductSlug",
            "ProductPricing",
            "ProductProductGroup",
            "PricingProductGroupDiscount",
            "ProductGroupDiscount",
            "ProductGroup changedGroup"
        ];

        foreach (string dependency in targetedDependencies) {
            Assert.Contains(dependency, sql, StringComparison.Ordinal);
        }

        Assert.Contains("@Since", sql, StringComparison.Ordinal);
        Assert.Contains("@ForceFullRefresh", sql, StringComparison.Ordinal);
        Assert.Contains("FROM Pricing pricing", sql, StringComparison.Ordinal);
        Assert.Contains("FROM Client retailClient", sql, StringComparison.Ordinal);
        Assert.Contains("FROM Storage storage", sql, StringComparison.Ordinal);
        Assert.Contains("o.PriceSourceIsAmg = 0", sql, StringComparison.Ordinal);
        Assert.Contains("CROSS JOIN GlobalRetailDependencyChanges", sql, StringComparison.Ordinal);
        Assert.Contains(
            "FROM Product p\n    CROSS JOIN GlobalRetailDependencyChanges dependencyChange\n)",
            sql,
            StringComparison.Ordinal);
        Assert.Contains("SourceChangedProducts AS", sql, StringComparison.Ordinal);
        Assert.Contains("INNER JOIN SourceChangedProducts changed", sql, StringComparison.Ordinal);
        Assert.Contains("candidate.Deleted = 0", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void RetailConfigurationSignature_CoversUntimestampedSelectionChanges() {
        string sql = GetPrivateConstant("RetailConfigurationSignatureSql");

        Assert.Contains("storage.RetailPriority", sql, StringComparison.Ordinal);
        Assert.Contains("storage.ForEcommerce", sql, StringComparison.Ordinal);
        Assert.Contains("organization.PriceSourceIsAmg", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.IsActive", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.WithVATAccounting", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.PricingID", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.SourceFenixID", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.SourceFenixCode", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.SourceAmgID", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.SourceAmgCode", sql, StringComparison.Ordinal);
        Assert.Contains("pricing.BasePricingID", sql, StringComparison.Ordinal);
        Assert.Contains("pricing.CalculatedExtraCharge", sql, StringComparison.Ordinal);
        Assert.Contains("(SELECT COUNT(*) FROM FenixRetailStorage) = 2", sql, StringComparison.Ordinal);
        Assert.Contains("EXISTS (SELECT 1 FROM RetailPricingConfig)", sql, StringComparison.Ordinal);
        Assert.Contains("AS IsValid", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ByIdProjection_BatchesBelowSqlServerParameterLimit() {
        FieldInfo field = typeof(ProductSyncRepository).GetField(
            "ProductIdsBatchSize",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Product ID batch size was not found.");

        int batchSize = (int)(field.GetRawConstantValue()
            ?? throw new InvalidOperationException("Product ID batch size was null."));

        Assert.InRange(batchSize, 1, 2000);
    }

    private static string BuildProductProjectionSql(string? firstCte, string? productJoin) {
        MethodInfo method = GetPrivateStaticMethod("BuildProductProjectionSql");

        return (string)(method.Invoke(null, [firstCte, productJoin, true])
            ?? throw new InvalidOperationException("Product projection builder returned null."));
    }

    private static string InvokePrivateSqlBuilder(string methodName) {
        MethodInfo method = GetPrivateStaticMethod(methodName);

        return (string)(method.Invoke(null, null)
            ?? throw new InvalidOperationException($"{methodName} returned null."));
    }

    private static MethodInfo GetPrivateStaticMethod(string methodName) {
        return typeof(ProductSyncRepository).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{methodName} was not found.");
    }

    private static string GetPrivateConstant(string fieldName) {
        FieldInfo field = typeof(ProductSyncRepository).GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{fieldName} was not found.");

        return (string)((field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null))
            ?? throw new InvalidOperationException($"{fieldName} was null."));
    }

    private static int CountOccurrences(string value, string fragment) {
        return value.Split(fragment, StringSplitOptions.None).Length - 1;
    }
}
