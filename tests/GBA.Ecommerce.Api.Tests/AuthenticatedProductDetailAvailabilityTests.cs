using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Products;

namespace GBA.Ecommerce.Api.Tests;

public sealed class AuthenticatedProductDetailAvailabilityTests {
    [Fact]
    public void Detail_ignores_availability_when_the_storage_join_is_outside_the_agreement_scope() {
        Product product = new();
        ProductAvailability availability = new() { Id = 17, Amount = 8.25 };

        GetSingleProductRepository.IncludeAvailabilityFromJoinedStorage(
            product,
            availability,
            null,
            withVat: false);

        Assert.Equal(0, product.AvailableQtyUk);
        Assert.Equal(0, product.AvailableQtyPl);
        Assert.Empty(product.ProductAvailabilities);
    }

    [Fact]
    public void Detail_includes_the_exact_availability_when_the_storage_join_matches_the_agreement_scope() {
        Product product = new();
        ProductAvailability availability = new() { Id = 18, Amount = 12.5 };
        Storage storage = new() { Locale = "uk" };

        GetSingleProductRepository.IncludeAvailabilityFromJoinedStorage(
            product,
            availability,
            storage,
            withVat: false);

        Assert.Equal(12.5, product.AvailableQtyUk);
        Assert.Equal(0, product.AvailableQtyUkVAT);
        Assert.Equal(0, product.AvailableQtyPl);
        Assert.Same(storage, Assert.Single(product.ProductAvailabilities).Storage);
    }

    [Fact]
    public void Detail_does_not_let_a_negative_storage_row_cancel_sellable_stock() {
        Product product = new();
        Storage storage = new() { Locale = "uk" };

        GetSingleProductRepository.IncludeAvailabilityFromJoinedStorage(
            product,
            new ProductAvailability { Id = 20, Amount = 3 },
            storage,
            withVat: false);
        GetSingleProductRepository.IncludeAvailabilityFromJoinedStorage(
            product,
            new ProductAvailability { Id = 21, Amount = -3 },
            storage,
            withVat: false);

        Assert.Equal(3, product.AvailableQtyUk);
        Assert.Equal(2, product.ProductAvailabilities.Count);
    }

    [Fact]
    public void Detail_keeps_contract_stock_independent_from_the_storage_VAT_flag() {
        Product product = new();
        ProductAvailability availability = new() { Id = 19, Amount = 16 };
        Storage storage = new() { Locale = "uk", ForVatProducts = false };

        GetSingleProductRepository.IncludeAvailabilityFromJoinedStorage(
            product,
            availability,
            storage,
            withVat: true);

        Assert.Equal(0, product.AvailableQtyUk);
        Assert.Equal(16, product.AvailableQtyUkVAT);
        Assert.Same(storage, Assert.Single(product.ProductAvailabilities).Storage);
    }

    [Fact]
    public void Fenix_primary_storage_link_preserves_the_exact_quantity_in_the_selected_price_bucket() {
        const long fenixOrganizationId = 41;
        const long fenixPrimaryStorageId = 73;
        Product product = new();
        ProductAvailability availability = new() { Id = 20, Amount = 16 };
        Storage storage = new() {
            Id = fenixPrimaryStorageId,
            Locale = "uk",
            OrganizationId = null,
            ForVatProducts = false,
            AvailableForReSale = false
        };

        Assert.True(EcommerceStorageScope.MatchesOrganization(
            storage.OrganizationId,
            storage.Id,
            fenixOrganizationId,
            organizationStorageId: fenixPrimaryStorageId));

        GetSingleProductRepository.IncludeAvailabilityFromJoinedStorage(
            product,
            availability,
            storage,
            withVat: true);

        Assert.Equal(0, product.AvailableQtyUk);
        Assert.Equal(16, product.AvailableQtyUkVAT);
        Assert.Same(storage, Assert.Single(product.ProductAvailabilities).Storage);
    }

    [Theory]
    [InlineData(false, 16, 0)]
    [InlineData(true, 0, 16)]
    public void Analogue_quantity_uses_the_selected_price_bucket_without_changing_storage_scope(
        bool withVat,
        double expectedNonVat,
        double expectedVat) {
        FromSearchProduct product = new();
        ProductAvailability availability = new() { Id = 21, Amount = 16 };
        Storage fenixStorage = new() {
            Id = 73,
            Locale = "uk",
            OrganizationId = 41,
            ForVatProducts = false
        };

        GetMultipleProductsRepository.AddAvailabilityToRequestedBucket(
            product,
            availability,
            fenixStorage,
            withVat);

        Assert.Equal(expectedNonVat, product.AvailableQtyUk);
        Assert.Equal(expectedVat, product.AvailableQtyUkVAT);
    }

    [Fact]
    public void Unrelated_storage_is_not_included_in_the_agreement_organization_scope() {
        Assert.False(EcommerceStorageScope.MatchesOrganization(
            storageOrganizationId: 42,
            storageId: 73,
            organizationId: 41,
            organizationStorageId: 74));
    }

    [Fact]
    public void Fenix_scope_does_not_mix_resale_stock_from_AMG() {
        const long fenixOrganizationId = 10487;
        const long fenixPrimaryStorageId = 2625;
        var stock = new[] {
            new { StorageOrganizationId = (long?)null, StorageId = fenixPrimaryStorageId, Amount = 16d },
            new { StorageOrganizationId = (long?)10485, StorageId = 2618L, Amount = 39d }
        };

        double availableForFenix = stock
            .Where(item => EcommerceStorageScope.MatchesOrganization(
                item.StorageOrganizationId,
                item.StorageId,
                fenixOrganizationId,
                fenixPrimaryStorageId))
            .Sum(item => item.Amount);

        Assert.Equal(16, availableForFenix);
    }
}
