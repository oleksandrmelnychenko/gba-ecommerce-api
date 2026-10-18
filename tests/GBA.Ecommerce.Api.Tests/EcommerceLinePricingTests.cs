using GBA.Domain.Entities.Products;
using GBA.Services.Services.Offers;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;

namespace GBA.Ecommerce.Api.Tests;

public sealed class EcommerceLinePricingTests {
    [Fact]
    public void A_line_is_rounded_once_from_the_unrounded_unit_price_like_the_sale() {
        decimal eurUnit = 0.3m;
        decimal rate = 50.7374m;

        Assert.Equal(15.22m, EcommerceLinePricing.Amount(EcommerceLinePricing.UnitLocal(eurUnit, rate)));
        Assert.Equal(7153.97m, EcommerceLinePricing.LineLocal(eurUnit, rate, 470));
        Assert.NotEqual(decimal.Round(decimal.Round(eurUnit * rate, 2) * 470m, 2), EcommerceLinePricing.LineLocal(eurUnit, rate, 470));
    }

    [Fact]
    public void The_euro_unit_price_is_never_rounded_to_cents_before_conversion() {
        decimal eurUnit = 10.123456789m;
        decimal rate = 50.1234m;

        Assert.Equal(10.123456789m, EcommerceLinePricing.Unit(eurUnit));
        Assert.Equal(507.42m, EcommerceLinePricing.Amount(EcommerceLinePricing.UnitLocal(eurUnit, rate)));
        Assert.Equal(507.25m, decimal.Round(decimal.Round(eurUnit, 2) * rate, 2));
    }

    [Fact]
    public void Midpoints_round_away_from_zero_like_gba_server() {
        Assert.Equal(15.23m, EcommerceLinePricing.Amount(EcommerceLinePricing.UnitLocal(0.3m, 50.75m)));
        Assert.Equal(-0.01m, EcommerceLinePricing.Amount(-0.005m));
    }

    [Fact]
    public void Offer_totals_round_the_sum_of_unrounded_lines() {
        ClientShoppingCart offer = new();
        foreach (double qty in new[] { 1d, 1d, 1d })
            offer.OrderItems.Add(new OrderItem {
                Qty = qty,
                Product = new Product { CurrentPrice = 0.3m, CurrentLocalPrice = 15.224m }
            });

        OfferService.PriceOfferLines(offer);

        Assert.All(offer.OrderItems, item => Assert.Equal(15.22m, item.TotalAmountLocal));
        Assert.Equal(45.67m, offer.TotalLocalAmount);
        Assert.Equal(0.9m, offer.TotalAmount);
    }
}
