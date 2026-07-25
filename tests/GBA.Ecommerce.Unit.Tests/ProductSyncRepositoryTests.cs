using System.Reflection;
using GBA.Search.Sync;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class ProductSyncRepositoryTests {
    [Theory]
    [InlineData(null, null)]
    [InlineData("ChangedProductIds AS (SELECT 1 AS ID)", "INNER JOIN ChangedProductIds c ON c.ID = p.ID")]
    [InlineData("RequestedProductIds AS (SELECT 1 AS ID)", "INNER JOIN RequestedProductIds c ON c.ID = p.ID")]
    public void ProductProjection_IndexesOnlyStableWebCatalogFieldsForEverySyncPath(
        string? firstCte,
        string? productJoin) {
        string sql = BuildProductProjectionSql(firstCte, productJoin);

        Assert.DoesNotContain("GetCalculatedProductPrice", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("RetailPricing", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductAvailability", sql, StringComparison.Ordinal);
        Assert.Contains("p.IsForWeb = 1", sql, StringComparison.Ordinal);
        Assert.Contains("CAST(0 AS decimal(19, 4)) AS RetailPrice", sql, StringComparison.Ordinal);
        Assert.Contains("CAST(0 AS decimal(19, 4)) AS RetailPriceVat", sql, StringComparison.Ordinal);
        Assert.Contains("END AS ProductSourceFenix", sql, StringComparison.Ordinal);
        Assert.Contains("END AS ProductSourceAmg", sql, StringComparison.Ordinal);
        Assert.Contains("AS IsCanonicalFenix", sql, StringComparison.Ordinal);
        Assert.Contains("AS IsCanonicalAmg", sql, StringComparison.Ordinal);
        Assert.Contains("canonicalProduct.ID < p.ID", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductProjection_DoesNotUseStockAsCatalogEligibility() {
        string sql = BuildProductProjectionSql(null, null);

        Assert.DoesNotContain("CatalogAvailability AS (", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductAvailability", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("HAVING SUM", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("EXISTS (\n      SELECT 1\n      FROM CatalogAvailability", sql, StringComparison.Ordinal);
        Assert.Contains("N'[]' AS CatalogScopesJson", sql, StringComparison.Ordinal);
        Assert.Contains("CAST(0 AS float) AS AvailableQty", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductProjection_HasSingleCardinalityWithMultiplePricingsGroupsAndSlugs() {
        string sql = BuildProductProjectionSql(null, null);

        Assert.DoesNotContain("JOIN ProductPricing ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ProductProductGroup", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LEFT JOIN ProductSlug", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OUTER APPLY", sql, StringComparison.Ordinal);
        Assert.Contains("SELECT TOP (1) slug.ID", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void IncrementalChangeDetection_CoversOnlyIndexedSearchDependencies() {
        string sql = InvokePrivateSqlBuilder("BuildChangedProductIdsSql");

        string[] targetedDependencies = [
            "ProductOriginalNumber",
            "OriginalNumber",
            "ProductSlug"
        ];

        foreach (string dependency in targetedDependencies) {
            Assert.Contains(dependency, sql, StringComparison.Ordinal);
        }

        Assert.Contains("@Since", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("@ForceFullRefresh", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductAvailability", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductPricing", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("PricingProductGroupDiscount", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("GlobalRetailDependencyChanges", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE #DirectChangedProductIds", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE #ChangedProductIds", sql, StringComparison.Ordinal);
        Assert.Contains("IF EXISTS (SELECT 1 FROM #DirectChangedProductIds)", sql, StringComparison.Ordinal);
        Assert.Contains("candidate.SourceFenixCode = changed.SourceFenixCode", sql, StringComparison.Ordinal);
        Assert.Contains("candidate.SourceAmgCode = changed.SourceAmgCode", sql, StringComparison.Ordinal);
        Assert.Contains("candidate.SourceFenixID = changed.SourceFenixID", sql, StringComparison.Ordinal);
        Assert.Contains("candidate.SourceAmgID = changed.SourceAmgID", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("SourceChangedProducts AS", sql, StringComparison.Ordinal);
        Assert.Contains("candidate.Deleted = 0", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void IncrementalBatch_UsesMaterializedKeysetAndStablePagination() {
        string sql = InvokePrivateSqlBuilder("BuildChangedProductIdBatchSql");

        Assert.Contains("CREATE TABLE #DirectChangedProductIds", sql, StringComparison.Ordinal);
        Assert.Contains("SELECT TOP (@Take) ID", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE ID > @AfterProductId", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY ID", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogConfigurationSignature_IsIndependentFromPricingAndStock() {
        string sql = GetPrivateConstant("RetailConfigurationSignatureSql");

        Assert.Contains("web-catalog-live-sql-v1", sql, StringComparison.Ordinal);
        Assert.Contains("AS IsValid", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("Storage", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("Pricing", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("Agreement", sql, StringComparison.Ordinal);
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
}
