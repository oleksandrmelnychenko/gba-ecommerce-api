using GBA.Domain.Repositories.Products;
using GBA.Ecommerce.Controllers;
using GBA.Search.Models;

namespace GBA.Ecommerce.Api.Tests;

public sealed class SearchPriceResolverTests {
    [Fact]
    public void Positive_calculated_price_remains_authoritative() {
        ProductPriceInfo calculated = new() {
            Price = 31.25m,
            CurrencyCode = "EUR"
        };
        ProductSearchDocument document = new() {
            RetailPrice = 27.17m,
            RetailCurrencyCode = "EUR"
        };

        ProductPriceInfo result = ProductsController.ResolveSearchPrice(
            document,
            calculated,
            isAnonymous: true,
            withVat: false,
            configuredCurrencyCode: "EUR");

        Assert.Same(calculated, result);
        Assert.Equal(31.25m, result.Price);
    }

    [Fact]
    public void Anonymous_zero_calculation_uses_last_indexed_retail_price() {
        ProductSearchDocument document = new() {
            RetailPrice = 27.17m,
            RetailPriceVat = 32.60m,
            RetailCurrencyCode = "EUR"
        };

        ProductPriceInfo result = ProductsController.ResolveSearchPrice(
            document,
            new ProductPriceInfo { Price = 0, CurrencyCode = "EUR" },
            isAnonymous: true,
            withVat: false,
            configuredCurrencyCode: "EUR");
        ProductPriceInfo vatResult = ProductsController.ResolveSearchPrice(
            document,
            new ProductPriceInfo { Price = 0, CurrencyCode = "EUR" },
            isAnonymous: true,
            withVat: true,
            configuredCurrencyCode: "EUR");

        Assert.Equal(27.17m, result.Price);
        Assert.Equal(27.17m, result.LocalPrice);
        Assert.Equal(32.60m, vatResult.Price);
        Assert.Equal(32.60m, vatResult.LocalPrice);
        Assert.Equal("EUR", result.CurrencyCode);
    }

    [Fact]
    public void Authenticated_zero_calculation_never_uses_public_retail_projection() {
        ProductPriceInfo calculated = new() {
            Price = 0,
            CurrencyCode = "EUR"
        };
        ProductSearchDocument document = new() {
            RetailPrice = 27.17m,
            RetailCurrencyCode = "EUR"
        };

        ProductPriceInfo result = ProductsController.ResolveSearchPrice(
            document,
            calculated,
            isAnonymous: false,
            withVat: false,
            configuredCurrencyCode: "EUR");

        Assert.Same(calculated, result);
        Assert.Equal(0, result.Price);
    }

    [Fact]
    public void Stale_indexed_currency_cannot_override_current_store_agreement() {
        ProductSearchDocument document = new() {
            RetailPrice = 27.17m,
            RetailCurrencyCode = "EUR"
        };

        ProductPriceInfo result = ProductsController.ResolveSearchPrice(
            document,
            new ProductPriceInfo { Price = 0, CurrencyCode = "UAH" },
            isAnonymous: true,
            withVat: false,
            configuredCurrencyCode: "UAH");

        Assert.Equal(0, result.Price);
        Assert.Equal("UAH", result.CurrencyCode);
    }
}
