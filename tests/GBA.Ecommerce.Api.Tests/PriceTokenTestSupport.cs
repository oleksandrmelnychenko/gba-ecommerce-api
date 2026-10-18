using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using GBA.Common.Configuration;

namespace GBA.Ecommerce.Api.Tests;

internal static class PriceTokenTestSupport {
    private const string Key = "GBA_Test_Key_16!";
    private const string Iv = "GBA_Test_IV__16!";

    private static readonly Lock InitLock = new();
    private static bool _initialized;

    public static void EnsureKeys() {
        lock (InitLock) {
            if (_initialized) return;

            SecuritySettings.Initialize(new SecuritySettings {
                JwtKey = new string('k', 64),
                JwtIssuer = "tests",
                JwtAudience = "tests",
                PriceEncryptionKey = Key,
                PriceEncryptionIV = Iv,
                CorsOrigins = ["http://localhost"]
            });

            _initialized = true;
        }
    }

    public static decimal[] DecodePrices(string encoded) {
        string base64 = encoded.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.IV = Encoding.UTF8.GetBytes(Iv);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        byte[] cipher = Convert.FromBase64String(base64);
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        string plaintext = Encoding.UTF8.GetString(decryptor.TransformFinalBlock(cipher, 0, cipher.Length));

        return Array.ConvertAll(
            plaintext.Split('|')[0].Split(','),
            part => decimal.Parse(part, System.Globalization.CultureInfo.InvariantCulture));
    }
}
