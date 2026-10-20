using System.Text.RegularExpressions;
using GBA.Search.Sync;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ProductSyncRepositoryQueryPlanTests {
    [Fact]
    public void Direct_change_detection_does_not_expand_global_retail_dependencies() {
        Assert.DoesNotContain(
            "PricingProductGroupDiscount",
            ProductSyncRepository.DirectChangedProductIdsSql,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "ProductGroupDiscount pgd",
            ProductSyncRepository.DirectChangedProductIdsSql,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "ClientAgreement",
            ProductSyncRepository.DirectChangedProductIdsSql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Global_dependency_probe_is_separate_and_uses_existence_checks() {
        Assert.Contains(
            "EXISTS",
            ProductSyncRepository.HasGlobalRetailDependencyChangesSql,
            StringComparison.Ordinal);
        Assert.Contains(
            "PricingProductGroupDiscount",
            ProductSyncRepository.HasGlobalRetailDependencyChangesSql,
            StringComparison.Ordinal);
        Assert.Contains(
            "ProductGroupDiscount",
            ProductSyncRepository.HasGlobalRetailDependencyChangesSql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Global_dependency_expansion_keeps_both_pricing_sources() {
        Assert.Contains(
            "PricingProductGroupDiscount",
            ProductSyncRepository.GlobalRetailDependencyProductIdsSql,
            StringComparison.Ordinal);
        Assert.Contains(
            "ProductGroupDiscount",
            ProductSyncRepository.GlobalRetailDependencyProductIdsSql,
            StringComparison.Ordinal);
        Assert.Contains(
            "client.IsForRetail = 1",
            ProductSyncRepository.GlobalRetailDependencyProductIdsSql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Change_detection_uses_one_timestamp_range_per_branch() {
        foreach (string sql in new[] {
                     ProductSyncRepository.DirectChangedProductIdsSql,
                     ProductSyncRepository.HasGlobalRetailDependencyChangesSql
                 }) {
            Assert.DoesNotMatch(new Regex(@"@Since\s+OR\b", RegexOptions.IgnoreCase), sql);
            Assert.DoesNotMatch(new Regex(@"\bOR\s+[\w.]*(Updated|Created)\s*>", RegexOptions.IgnoreCase), sql);
        }

        string direct = Regex.Replace(ProductSyncRepository.DirectChangedProductIdsSql, @"\s+", " ");
        foreach (string branch in new[] {
                     "SELECT ID FROM Product WHERE Updated > @Since",
                     "SELECT ProductID FROM ProductOriginalNumber WHERE Updated > @Since",
                     "SELECT ProductID FROM ProductOriginalNumber WHERE Created > @Since",
                     "SELECT pon.ProductID FROM OriginalNumber on_ INNER JOIN ProductOriginalNumber pon ON pon.OriginalNumberID = on_.ID AND pon.Deleted = 0 WHERE on_.Updated > @Since",
                     "SELECT ProductID FROM ProductAvailability WHERE Updated > @Since",
                     "SELECT ProductID FROM ProductPricing WHERE Updated > @Since",
                     "SELECT ProductID FROM ProductPricing WHERE Created > @Since",
                     "SELECT ProductID FROM ProductProductGroup WHERE Updated > @Since",
                     "SELECT ProductID FROM ProductProductGroup WHERE Created > @Since",
                     "SELECT ProductID FROM ProductSlug WHERE Updated > @Since",
                     "SELECT ProductID FROM ProductSlug WHERE Created > @Since"
                 }) {
            Assert.Contains(branch, direct, StringComparison.Ordinal);
        }

        Assert.Equal(11, Regex.Matches(direct, @"> @Since").Count);
        Assert.Equal(10, Regex.Matches(direct, @"\bUNION SELECT\b").Count);
        Assert.DoesNotContain("UNION ALL", direct, StringComparison.Ordinal);
        Assert.Contains("SELECT TOP (1) p.ID FROM Product p WHERE p.ID = changed.ID AND p.Deleted = 0", direct, StringComparison.Ordinal);

        string gate = Regex.Replace(ProductSyncRepository.HasGlobalRetailDependencyChangesSql, @"\s+", " ");
        Assert.Equal(4, Regex.Matches(gate, @"EXISTS \(SELECT 1 FROM (Pricing)?ProductGroupDiscount WHERE (Updated|Created) > @Since\)").Count);
    }

    [Fact]
    public void Change_detection_plan_does_not_depend_on_the_first_watermark() {
        string direct = Regex.Replace(ProductSyncRepository.DirectChangedProductIdsSql, @"\s+", " ");

        Assert.EndsWith(
            "OPTION (MAXDOP 1, OPTIMIZE FOR (@Since = '9999-12-31T23:59:59.997'), " +
            "USE HINT ('DISABLE_ROW_MODE_MEMORY_GRANT_FEEDBACK', 'DISABLE_BATCH_MODE_MEMORY_GRANT_FEEDBACK'))",
            direct,
            StringComparison.Ordinal);
    }
}
