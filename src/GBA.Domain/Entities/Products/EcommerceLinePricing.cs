using System;

namespace GBA.Domain.Entities.Products;

public static class EcommerceLinePricing {
    public const int UnitPriceScale = 14;

    public const int AmountScale = 2;

    public static decimal Unit(decimal price) {
        return decimal.Round(price, UnitPriceScale, MidpointRounding.AwayFromZero);
    }

    public static decimal UnitLocal(decimal price, decimal exchangeRate) {
        return decimal.Round(Unit(price) * exchangeRate, UnitPriceScale, MidpointRounding.AwayFromZero);
    }

    public static decimal Amount(decimal value) {
        return decimal.Round(value, AmountScale, MidpointRounding.AwayFromZero);
    }

    public static decimal Line(decimal price, double qty) {
        return Amount(Unit(price) * Convert.ToDecimal(qty));
    }

    public static decimal LineLocal(decimal price, decimal exchangeRate, double qty) {
        return Amount(Unit(price) * exchangeRate * Convert.ToDecimal(qty));
    }

    public static decimal UnroundedLine(decimal unitPrice, double qty) {
        return unitPrice * Convert.ToDecimal(qty);
    }
}
