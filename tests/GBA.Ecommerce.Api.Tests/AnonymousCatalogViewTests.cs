using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Ecommerce.Controllers;

namespace GBA.Ecommerce.Api.Tests;

public sealed class AnonymousCatalogViewTests {
    [Fact]
    public void Anonymous_product_detail_carries_the_price_only_as_the_search_token() {
        PriceTokenTestSupport.EnsureKeys();
        Product product = new() {
            Id = 3,
            CurrentPrice = 18.1066m,
            CurrentLocalPrice = 925.4234m,
            CurrentWithVatPrice = 21.73m,
            CurrentLocalWithVatPrice = 1110.51m,
            CurrentPriceEurToUah = 925.42m,
            AvailableQtyUk = 7,
            MeasureUnit = new MeasureUnit { CodeOneC = "2009" }
        };
        product.ProductAvailabilities.Add(new ProductAvailability { StorageId = 2968, Amount = 7 });
        product.ProductProductGroups.Add(new ProductProductGroup { ProductGroup = new ProductGroup() });
        product.AnalogueProducts.Add(new ProductAnalogue { AnalogueProduct = new Product { CurrentLocalPrice = 99m } });
        product.ProductImages.Add(new ProductImage { ImageUrl = "image.png" });

        AnonymousCatalogView.Protect(product, 1_700_000_000L);

        Assert.Equal(new[] { 18.1066m, 925.4234m, 18.1066m, 925.4234m }, PriceTokenTestSupport.DecodePrices(product.P));
        Assert.Equal(1_700_000_000L, product.T);
        Assert.Equal(0m, product.CurrentPrice);
        Assert.Equal(0m, product.CurrentLocalPrice);
        Assert.Equal(0m, product.CurrentWithVatPrice);
        Assert.Equal(0m, product.CurrentLocalWithVatPrice);
        Assert.Equal(0m, product.CurrentPriceEurToUah);
        Assert.Empty(product.ProductAvailabilities);
        Assert.Empty(product.ProductProductGroups);
        Assert.Empty(product.AnalogueProducts);
        Assert.Null(product.MeasureUnit);
        Assert.Equal(7, product.AvailableQtyUk);
        Assert.Single(product.ProductImages);
    }

    [Fact]
    public void Anonymous_related_products_carry_prices_only_as_tokens() {
        PriceTokenTestSupport.EnsureKeys();
        List<FromSearchProduct> products = [new() { Id = 1, CurrentPrice = 2.5m, CurrentLocalPrice = 125.3m, AvailableQtyUk = 3 }];

        AnonymousCatalogView.Protect(products, 1_700_000_000L);

        Assert.Equal(new[] { 2.5m, 125.3m, 2.5m, 125.3m }, PriceTokenTestSupport.DecodePrices(products[0].P));
        Assert.Equal(0m, products[0].CurrentPrice);
        Assert.Equal(0m, products[0].CurrentLocalPrice);
        Assert.Equal(3, products[0].AvailableQtyUk);
    }

    [Fact]
    public void A_missing_product_stays_missing() {
        Assert.Null(AnonymousCatalogView.Protect((Product)null!, 1L));
    }
}
