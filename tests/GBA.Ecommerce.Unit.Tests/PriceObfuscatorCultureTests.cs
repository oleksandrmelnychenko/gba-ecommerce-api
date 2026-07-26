using System.Globalization;
using System.Threading;
using GBA.Common.Helpers;
using Xunit;

namespace GBA.Ecommerce.Unit.Tests;

/// <summary>
/// The protected price payload is a wire format: a locale that formats decimals with a comma
/// (uk-UA serves every /api/v1/uk request) would collide with the comma that separates prices
/// and leave the storefront unable to parse any price at all.
/// </summary>
public sealed class PriceObfuscatorCultureTests {
    [Theory]
    [InlineData("uk-UA")]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    public void EncodeMultiple_UsesInvariantDecimalSeparator(string culture) {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

        try {
            string encoded = PriceObfuscator.EncodeMultiple([1.31m, 1.31m, 0m, 0m], 1785064585);
            string decoded = TestPricePayload.Decrypt(encoded);

            Assert.Equal("1.31,1.31,0.00,0.00|1785064585", decoded);
        } finally {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Decode_ReadsBackASinglePriceUnderACommaCulture() {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new CultureInfo("uk-UA");

        try {
            string encoded = PriceObfuscator.Encode(12.34m, 1785064585);

            (decimal price, long timestamp) = Assert.NotNull(PriceObfuscator.Decode(encoded));

            Assert.Equal(12.34m, price);
            Assert.Equal(1785064585, timestamp);
        } finally {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
