using Dapper;
using GBA.Domain;
using GBA.Domain.Entities.Products;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Api.Tests;

public sealed class LegacyNumericSqlFactAttribute : FactAttribute {
    public LegacyNumericSqlFactAttribute() {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(CheckoutSqlFixture.ConnectionStringVariable)))
            Skip = "A SQL connection is required to read float columns through Dapper.";
    }
}

public sealed class LegacyNumericTests {
    [Theory]
    [InlineData(0.3, "0.3")]
    [InlineData(1.23, "1.23")]
    [InlineData(36.96, "36.96")]
    [InlineData(3.0749999999999997, "3.075")]
    [InlineData(123456.78901234567, "123456.789012346")]
    [InlineData(-0.1, "-0.1")]
    public void A_double_becomes_a_decimal_with_fifteen_significant_digits(double value, string expected) {
        Assert.Equal(expected, LegacyNumeric.ToDecimal(value).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(expected, LegacyNumeric.ToDecimal((object)value).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void A_float_becomes_a_decimal_with_seven_significant_digits() {
        Assert.Equal(14.1m, LegacyNumeric.ToDecimal(14.1f));
        Assert.Equal(0.3m, LegacyNumeric.ToDecimal((object)0.3f));
    }

    [Fact]
    public void Values_that_are_not_binary_floating_point_convert_like_the_runtime() {
        Assert.Equal(12.3400m, LegacyNumeric.ToDecimal((object)12.3400m));
        Assert.Equal(7m, LegacyNumeric.ToDecimal((object)7));
        Assert.Equal(7m, LegacyNumeric.ToDecimal((object)7L));
    }

    [Theory]
    [InlineData(2.675, 2, 2.68)]
    [InlineData(1.005, 2, 1.0)]
    [InlineData(0.125, 2, 0.12)]
    [InlineData(1234.5678, 0, 1235)]
    public void Rounding_a_double_scales_rounds_and_divides(double value, int digits, double expected) {
        Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(LegacyNumeric.Round(value, digits)));
    }

    [Fact]
    public void Rounding_a_double_away_from_zero_keeps_the_scaled_midpoint() {
        Assert.Equal(0.13, LegacyNumeric.Round(0.125, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(1.0, LegacyNumeric.Round(1.005, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(2.68, LegacyNumeric.Round(2.675, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void A_long_decimal_becomes_the_double_of_mantissa_over_scale() {
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(10000000000000.09765625),
            BitConverter.DoubleToInt64Bits(LegacyNumeric.ToDouble(10000000000000.099609375m)));
        Assert.Equal(36.96, LegacyNumeric.ToDouble(36.96m));
    }

    [Fact]
    public void A_line_total_uses_the_fifteen_digit_quantity() {
        Assert.Equal(3.08m, EcommerceLinePricing.Line(10.25m, 0.3));
        Assert.Equal(3.075m, EcommerceLinePricing.UnroundedLine(10.25m, 0.3));
        Assert.Equal(126.80m, EcommerceLinePricing.LineLocal(10.25m, 41.2345m, 0.3));
        Assert.Equal(330.246910575m, EcommerceLinePricing.UnroundedLine(123.456789m, 2.675));
    }

    [Fact]
    public void Dapper_reads_decimals_through_the_legacy_handler() {
        Assert.True(SqlMapper.HasTypeHandler(typeof(decimal)));
    }

    [LegacyNumericSqlFact]
    public void A_float_column_lands_in_a_decimal_member_with_fifteen_digits() {
        using SqlConnection connection = new(Environment.GetEnvironmentVariable(CheckoutSqlFixture.ConnectionStringVariable));
        connection.Open();

        Assert.Equal("36.96", connection.QuerySingle<decimal>("SELECT CAST(36.96 AS float);").ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("12.33", connection.QuerySingle<decimal?>("SELECT CAST(0.3 AS float) * 41.1;")!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Null(connection.QuerySingle<decimal?>("SELECT CAST(NULL AS float);"));
        Assert.Equal(12.3400m, connection.QuerySingle<decimal>("SELECT CAST(12.34 AS decimal(18, 4));"));

        FloatRow row = connection.QuerySingle<FloatRow>("SELECT CAST(13.57 AS float) AS Price, CAST(0.3 AS float) AS Qty;");
        Assert.Equal("13.57", row.Price.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(0.3, row.Qty);

        dynamic loose = connection.QuerySingle("SELECT ROUND(CAST(12.34 AS decimal(18, 2)) * CAST(1.1 AS float), 2) AS Price;");
        Assert.Equal("13.57", LegacyNumeric.ToDecimal((object)loose.Price).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private sealed class FloatRow {
        public decimal Price { get; set; }

        public double Qty { get; set; }
    }
}
