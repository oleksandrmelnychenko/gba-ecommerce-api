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
}
