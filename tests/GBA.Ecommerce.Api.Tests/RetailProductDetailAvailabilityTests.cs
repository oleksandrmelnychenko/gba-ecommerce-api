using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products;
using GBA.Services.Services.Products;

namespace GBA.Ecommerce.Api.Tests;

public sealed class RetailProductDetailAvailabilityTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Anonymous_detail_routes_selected_ukrainian_storage_to_its_vat_bucket(
        bool forVatProducts) {
        Product product = new() { AvailableQtyUk = 12.5 };

        GetSingleProductRepository.RouteRetailAvailability(
            product,
            forVatProducts);

        Assert.Equal(forVatProducts ? 0 : 12.5, product.AvailableQtyUk);
        Assert.Equal(forVatProducts ? 12.5 : 0, product.AvailableQtyUkVAT);
    }

    [Fact]
    public void Anonymous_detail_hides_positive_stock_when_live_price_is_missing() {
        Product product = new() {
            IsForWeb = true,
            CurrentPrice = 0,
            AvailableQtyUk = 8,
            AvailableQtyUkVAT = 9,
            AvailableQtyRoad = 3
        };

        Product result = ProductService.EnsureRetailProductCanBeOffered(product);

        Assert.Same(product, result);
        Assert.Equal(0, result.AvailableQtyUk);
        Assert.Equal(0, result.AvailableQtyUkVAT);
        Assert.Equal(0, result.AvailableQtyRoad);
    }

    [Fact]
    public void Anonymous_detail_preserves_exact_stock_when_live_price_is_sellable() {
        Product product = new() {
            IsForWeb = true,
            CurrentPrice = 25,
            AvailableQtyUk = 12.5
        };

        Product result = ProductService.EnsureRetailProductCanBeOffered(product);

        Assert.Same(product, result);
        Assert.Equal(12.5, result.AvailableQtyUk);
    }
}
