using GBA.Domain.Repositories.Products;
using GBA.Ecommerce.Controllers;

namespace GBA.Ecommerce.Api.Tests;

public sealed class SearchPriceResolverTests {
    [Fact]
    public void Positive_calculated_price_remains_authoritative() {
        ProductPriceInfo calculated = new() {
            Price = 31.25m,
            CurrencyCode = "EUR"
        };
        ProductPriceInfo result = ProductsController.ResolveSearchPrice(
            calculated,
            configuredCurrencyCode: "EUR");

        Assert.Same(calculated, result);
        Assert.Equal(31.25m, result.Price);
    }

    [Fact]
    public void Zero_authoritative_price_never_uses_the_search_projection() {
        ProductPriceInfo result = ProductsController.ResolveSearchPrice(
            new ProductPriceInfo { Price = 0, CurrencyCode = "EUR" },
            configuredCurrencyCode: "EUR");

        Assert.Equal(0, result.Price);
        Assert.Equal(0, result.LocalPrice);
        Assert.Equal("EUR", result.CurrencyCode);
    }

    [Fact]
    public void Authenticated_zero_calculation_never_uses_public_retail_projection() {
        ProductPriceInfo calculated = new() {
            Price = 0,
            CurrencyCode = "EUR"
        };
        ProductPriceInfo result = ProductsController.ResolveSearchPrice(
            calculated,
            configuredCurrencyCode: "EUR");

        Assert.Same(calculated, result);
        Assert.Equal(0, result.Price);
    }

    [Fact]
    public void Missing_authoritative_price_uses_only_the_configured_currency() {
        ProductPriceInfo result = ProductsController.ResolveSearchPrice(
            calculatedPrice: null,
            configuredCurrencyCode: "UAH");

        Assert.Equal(0, result.Price);
        Assert.Equal("UAH", result.CurrencyCode);
    }

    [Fact]
    public void Positive_stock_is_hidden_without_an_authoritative_sellable_price() {
        Assert.Equal(0, ProductsController.ResolveVisibleSearchQuantity(
            new ProductPriceInfo { Price = 0 },
            12));
        Assert.Equal(0, ProductsController.ResolveVisibleSearchQuantity(null, 12));
        Assert.Equal(12, ProductsController.ResolveVisibleSearchQuantity(
            new ProductPriceInfo { Price = 31.25m },
            12));
    }
}
