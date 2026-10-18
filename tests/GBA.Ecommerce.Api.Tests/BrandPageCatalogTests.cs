using System.Collections.Generic;
using System.Linq;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Products;
using GBA.Ecommerce.Controllers;

namespace GBA.Ecommerce.Api.Tests;

public sealed class BrandPageCatalogTests {
    [Fact]
    public void Anonymous_brand_cards_carry_the_retail_price_token_and_retail_stock_like_search() {
        PriceTokenTestSupport.EnsureKeys();
        List<FromSearchProduct> products = [
            new() { Id = 1, Name = "B", VendorCode = "PRICED" },
            new() { Id = 2, Name = "A", VendorCode = "UNPRICED" }
        ];
        Dictionary<long, ProductPriceInfo> prices = new() {
            [1] = new ProductPriceInfo { Price = 12.345m, LocalPrice = 600.12345678m, CurrencyCode = "UAH" }
        };
        Dictionary<long, double> stock = new() { [1] = 5, [2] = 3 };

        List<ProtectedSearchProduct> cards = ProductsController.ProtectCatalogCards(
            products, prices, stock, "UAH", isAuthenticated: false, timestamp: 1_700_000_000L);

        ProtectedSearchProduct priced = cards[0];
        Assert.Equal("PRICED", priced.VendorCode);
        Assert.Equal(new[] { 12.345m, 600.12345678m, 12.345m, 600.12345678m }, PriceTokenTestSupport.DecodePrices(priced.P));
        Assert.Equal(5, priced.AvailableQtyUk);
        Assert.Equal(5, priced.AvailableQtyUkVAT);
        Assert.Equal("UAH", priced.CurrencyCode);

        ProtectedSearchProduct unpriced = cards[1];
        Assert.Equal(new[] { 0m, 0m, 0m, 0m }, PriceTokenTestSupport.DecodePrices(unpriced.P));
        Assert.Equal(0, unpriced.AvailableQtyUk);
    }

    [Fact]
    public void Signed_in_brand_cards_show_the_agreement_stock_even_without_a_price() {
        PriceTokenTestSupport.EnsureKeys();
        List<FromSearchProduct> products = [new() { Id = 7, VendorCode = "X" }];

        ProtectedSearchProduct card = Assert.Single(ProductsController.ProtectCatalogCards(
            products,
            new Dictionary<long, ProductPriceInfo>(),
            new Dictionary<long, double> { [7] = 4 },
            "EUR",
            isAuthenticated: true,
            timestamp: 1_700_000_000L));

        Assert.Equal(4, card.AvailableQtyUk);
        Assert.Equal("EUR", card.CurrencyCode);
    }
}
