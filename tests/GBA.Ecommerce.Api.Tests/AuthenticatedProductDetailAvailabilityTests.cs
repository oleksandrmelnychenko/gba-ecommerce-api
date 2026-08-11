using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products;

namespace GBA.Ecommerce.Api.Tests;

public sealed class AuthenticatedProductDetailAvailabilityTests {
    [Fact]
    public void Detail_ignores_availability_when_the_storage_join_is_outside_the_agreement_scope() {
        Product product = new();
        ProductAvailability availability = new() { Id = 17, Amount = 8.25 };

        GetSingleProductRepository.IncludeAvailabilityFromJoinedStorage(product, availability, null);

        Assert.Equal(0, product.AvailableQtyUk);
        Assert.Equal(0, product.AvailableQtyPl);
        Assert.Empty(product.ProductAvailabilities);
    }

    [Fact]
    public void Detail_includes_the_exact_availability_when_the_storage_join_matches_the_agreement_scope() {
        Product product = new();
        ProductAvailability availability = new() { Id = 18, Amount = 12.5 };
        Storage storage = new() { Locale = "uk" };

        GetSingleProductRepository.IncludeAvailabilityFromJoinedStorage(product, availability, storage);

        Assert.Equal(12.5, product.AvailableQtyUk);
        Assert.Equal(0, product.AvailableQtyPl);
        Assert.Same(availability, Assert.Single(product.ProductAvailabilities));
    }
}
