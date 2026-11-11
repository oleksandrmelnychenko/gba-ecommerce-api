using System;

namespace GBA.Domain;

public static class LegacyNumeric {
    private const int MaxScale = 28;
    private const double RoundLimit = 1E16;
    private const double TwoPower64 = 1.8446744073709552E+19;

    private static readonly double[] DoublePowers10 =
    [
        1, 1E1, 1E2, 1E3, 1E4, 1E5, 1E6, 1E7, 1E8, 1E9,
        1E10, 1E11, 1E12, 1E13, 1E14, 1E15, 1E16, 1E17, 1E18, 1E19,
        1E20, 1E21, 1E22, 1E23, 1E24, 1E25, 1E26, 1E27, 1E28, 1E29,
        1E30, 1E31, 1E32, 1E33, 1E34, 1E35, 1E36, 1E37, 1E38, 1E39,
        1E40, 1E41, 1E42, 1E43, 1E44, 1E45, 1E46, 1E47, 1E48, 1E49,
        1E50, 1E51, 1E52, 1E53, 1E54, 1E55, 1E56, 1E57, 1E58, 1E59,
        1E60, 1E61, 1E62, 1E63, 1E64, 1E65, 1E66, 1E67, 1E68, 1E69,
        1E70, 1E71, 1E72, 1E73, 1E74, 1E75, 1E76, 1E77, 1E78, 1E79,
        1E80
    ];

    private static readonly decimal[] DecimalPowers10 =
    [
        1m, 1E1m, 1E2m, 1E3m, 1E4m, 1E5m, 1E6m, 1E7m, 1E8m, 1E9m,
        1E10m, 1E11m, 1E12m, 1E13m, 1E14m, 1E15m, 1E16m, 1E17m, 1E18m, 1E19m,
        1E20m, 1E21m, 1E22m
    ];

    public static decimal ToDecimal(double value) {
        int exponent = (int)((BitConverter.DoubleToUInt64Bits(value) >> 52) & 0x7FF) - 1022;
        if (exponent < -94) return decimal.Zero;
        if (exponent > 96) throw new OverflowException("Value was either too large or too small for a Decimal.");

        bool negative = value < 0;
        double scaled = negative ? -value : value;
        int power = 14 - ((exponent * 19728) >> 16);

        if (power >= 0) {
            if (power > MaxScale) power = MaxScale;
            scaled *= DoublePowers10[power];
        } else if (power != -1 || scaled >= 1E15) {
            scaled /= DoublePowers10[-power];
        } else {
            power = 0;
        }

        if (scaled < 1E14 && power < MaxScale) {
            scaled *= 10;
            power++;
        }

        return Compose((ulong)(long)Math.Round(scaled), power, 14, negative);
    }

    public static decimal ToDecimal(float value) {
        int exponent = (int)((BitConverter.SingleToUInt32Bits(value) >> 23) & 0xFF) - 126;
        if (exponent < -94) return decimal.Zero;
        if (exponent > 96) throw new OverflowException("Value was either too large or too small for a Decimal.");

        bool negative = value < 0;
        double scaled = negative ? -value : value;
        int power = 6 - ((exponent * 19728) >> 16);

        if (power >= 0) {
            if (power > MaxScale) power = MaxScale;
            scaled *= DoublePowers10[power];
        } else if (power != -1 || scaled >= 1E7) {
            scaled /= DoublePowers10[-power];
        } else {
            power = 0;
        }

        if (scaled < 1E6 && power < MaxScale) {
            scaled *= 10;
            power++;
        }

        return Compose((ulong)(long)Math.Round(scaled), power, 6, negative);
    }

    public static decimal? ToNullableDecimal(double? value) => value.HasValue ? ToDecimal(value.GetValueOrDefault()) : null;

    public static decimal? ToNullableDecimal(float? value) => value.HasValue ? ToDecimal(value.GetValueOrDefault()) : null;

    public static decimal ToDecimal(object value) =>
        value switch {
            double number => ToDecimal(number),
            float number => ToDecimal(number),
            _ => Convert.ToDecimal(value)
        };

    public static decimal ToDecimal(object value, IFormatProvider provider) =>
        value switch {
            double number => ToDecimal(number),
            float number => ToDecimal(number),
            _ => Convert.ToDecimal(value, provider)
        };

    public static object ChangeType(object value, Type conversionType, IFormatProvider provider) =>
        value switch {
            double or float when conversionType == typeof(decimal) => ToDecimal(value),
            decimal number when conversionType == typeof(double) => ToDouble(number),
            decimal number when conversionType == typeof(float) => ToSingle(number),
            _ => Convert.ChangeType(value, conversionType, provider)
        };

    public static double ToDouble(decimal value) {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        ulong low = (uint)bits[0] | ((ulong)(uint)bits[1] << 32);
        double result = (low + (uint)bits[2] * TwoPower64) / DoublePowers10[(bits[3] >> 16) & 0xFF];
        return bits[3] < 0 ? -result : result;
    }

    public static double? ToNullableDouble(decimal? value) => value.HasValue ? ToDouble(value.GetValueOrDefault()) : null;

    public static float ToSingle(decimal value) => (float)ToDouble(value);

    public static float? ToNullableSingle(decimal? value) => value.HasValue ? ToSingle(value.GetValueOrDefault()) : null;

    public static double Round(double value, int digits) => Round(value, digits, MidpointRounding.ToEven);

    public static double Round(double value, int digits, MidpointRounding mode) {
        if (digits is < 0 or > 15) throw new ArgumentOutOfRangeException(nameof(digits), "Rounding digits must be between 0 and 15, inclusive.");
        if (mode is < MidpointRounding.ToEven or > MidpointRounding.ToPositiveInfinity)
            throw new ArgumentException($"The value '{mode}' is not valid for this usage of the type {nameof(MidpointRounding)}.", nameof(mode));

        if (Math.Abs(value) < RoundLimit) {
            double power10 = DoublePowers10[digits];
            value = Math.Round(value * power10, mode) / power10;
        }

        return value;
    }

    private static decimal Compose(ulong mantissa, int power, int maxFactored, bool negative) {
        if (mantissa == 0) return decimal.Zero;

        if (power < 0) {
            decimal whole = mantissa * DecimalPowers10[-power];
            return negative ? -whole : whole;
        }

        int limit = Math.Min(power, maxFactored);
        while (limit > 0 && mantissa % 10 == 0) {
            mantissa /= 10;
            power--;
            limit--;
        }

        return new decimal((int)(uint)mantissa, (int)(uint)(mantissa >> 32), 0, negative, (byte)power);
    }
}
