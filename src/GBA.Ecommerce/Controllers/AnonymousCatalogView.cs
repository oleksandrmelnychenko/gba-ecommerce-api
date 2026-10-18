using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;

namespace GBA.Ecommerce.Controllers;

public static class AnonymousCatalogView {
    public static Product Protect(Product product, long timestamp) {
        if (product == null) return null;

        product.P = PriceObfuscator.EncodeMultiple(
            new[] { product.CurrentPrice, product.CurrentLocalPrice, product.CurrentPrice, product.CurrentLocalPrice },
            timestamp);
        product.T = timestamp;
        product.CurrentPrice = 0;
        product.CurrentLocalPrice = 0;
        product.CurrentWithVatPrice = 0;
        product.CurrentLocalWithVatPrice = 0;
        product.CurrentPriceReSale = 0;
        product.CurrentLocalPriceReSale = 0;
        product.CurrentPriceEurToUah = 0;
        product.CurrentPriceReSaleEurToUah = 0;
        product.MeasureUnit = null;
        product.ProductAvailabilities?.Clear();
        product.ProductProductGroups?.Clear();
        product.AnalogueProducts?.Clear();
        product.BaseAnalogueProducts?.Clear();
        product.ComponentProducts?.Clear();
        product.BaseSetProducts?.Clear();
        product.ProductPricings?.Clear();
        product.CalculatedPrices?.Clear();

        return product;
    }

    public static List<FromSearchProduct> Protect(List<FromSearchProduct> products, long timestamp) {
        if (products == null) return null;

        foreach (FromSearchProduct product in products) {
            product.P = PriceObfuscator.EncodeMultiple(
                new[] { product.CurrentPrice, product.CurrentLocalPrice, product.CurrentPrice, product.CurrentLocalPrice },
                timestamp);
            product.T = timestamp;
            product.CurrentPrice = 0;
            product.CurrentLocalPrice = 0;
            product.CurrentWithVatPrice = 0;
            product.CurrentLocalWithVatPrice = 0;
            product.ProductPricings = null;
        }

        return products;
    }
}
