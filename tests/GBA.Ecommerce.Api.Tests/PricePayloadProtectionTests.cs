using System;
using System.Globalization;
using System.IO;
using System.Threading;
using GBA.Common.Configuration;
using GBA.Common.Helpers;

namespace GBA.Ecommerce.Api.Tests;

public sealed class PricePayloadProtectionTests {
    private const string _key = "GBA_Test_Key_16!";
    private const string _iv = "GBA_Test_IV__16!";

    private static readonly Lock _initLock = new();
    private static bool _initialized;

    private static void EnsureKeys() {
        lock (_initLock) {
            if (_initialized) return;

            SecuritySettings.Initialize(new SecuritySettings {
                JwtKey = new string('k', 64),
                JwtIssuer = "tests",
                JwtAudience = "tests",
                PriceEncryptionKey = _key,
                PriceEncryptionIV = _iv,
                CorsOrigins = ["http://localhost"]
            });

            _initialized = true;
        }
    }

    [Fact]
    public void Encoded_payload_is_not_readable_cleartext() {
        EnsureKeys();

        string encoded = PriceObfuscator.EncodeMultiple([1.28m, 65.79m, 1.28m, 65.79m], 1_700_000_000L);

        Assert.DoesNotContain("1.28", encoded, StringComparison.Ordinal);
        Assert.DoesNotContain("65.79", encoded, StringComparison.Ordinal);
        Assert.DoesNotContain("|", encoded, StringComparison.Ordinal);
    }

    [Fact]
    public void Prices_round_trip_under_the_ukrainian_request_culture() {
        EnsureKeys();

        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            // Every request runs under "uk", whose decimal separator is ',' - the same character
            // that separates the prices. The wire format must stay InvariantCulture regardless.
            CultureInfo.CurrentCulture = new CultureInfo("uk");

            string encoded = PriceObfuscator.Encode(1.28m, 1_700_000_000L);
            (decimal price, long timestamp)? decoded = PriceObfuscator.Decode(encoded);

            Assert.NotNull(decoded);
            Assert.Equal(1.28m, decoded!.Value.price);
            Assert.Equal(1_700_000_000L, decoded.Value.timestamp);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Multi_price_payload_uses_a_dot_decimal_mark_so_the_comma_stays_a_separator() {
        EnsureKeys();

        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("uk");

            string encoded = PriceObfuscator.EncodeMultiple([1.28m, 65.79m, 1.28m, 65.79m], 1_700_000_000L);
            string plaintext = DecryptForTest(encoded);

            Assert.Equal("1.28,65.79,1.28,65.79|1700000000", plaintext);
            Assert.Equal(4, plaintext.Split('|')[0].Split(',').Length);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void The_search_endpoint_encrypts_prices_and_never_ships_them_raw() {
        string source = File.ReadAllText(
            RepositoryPath("src/GBA.Ecommerce/Controllers/ProductsController.cs"));

        Assert.Contains("PriceObfuscator.EncodeMultiple", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DEBUG: raw prices", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new[] { price, price, 0m, 0m }", source, StringComparison.Ordinal);
    }

    private static string DecryptForTest(string encoded) {
        string base64 = encoded.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        using System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Text.Encoding.UTF8.GetBytes(_key);
        aes.IV = System.Text.Encoding.UTF8.GetBytes(_iv);
        aes.Mode = System.Security.Cryptography.CipherMode.CBC;
        aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

        byte[] cipher = Convert.FromBase64String(base64);
        using System.Security.Cryptography.ICryptoTransform decryptor = aes.CreateDecryptor();

        return System.Text.Encoding.UTF8.GetString(decryptor.TransformFinalBlock(cipher, 0, cipher.Length));
    }

    private static string RepositoryPath(string relativePath) {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null) {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate repository file: {relativePath}");
    }
}
