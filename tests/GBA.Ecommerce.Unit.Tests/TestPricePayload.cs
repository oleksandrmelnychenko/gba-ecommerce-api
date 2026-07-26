using System;
using System.Security.Cryptography;
using System.Text;
using GBA.Common.Configuration;

namespace GBA.Ecommerce.Unit.Tests;

/// <summary>
/// Mirrors the AES-CBC unwrap the storefront performs on the protected price payload, so the
/// tests assert the real wire format instead of trusting the encoder.
/// </summary>
internal static class TestPricePayload {
    internal static string Decrypt(string encoded) {
        string base64 = encoded.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(SecuritySettings.Instance.PriceEncryptionKey);
        aes.IV = Encoding.UTF8.GetBytes(SecuritySettings.Instance.PriceEncryptionIV);
        using ICryptoTransform decryptor = aes.CreateDecryptor();

        byte[] cipher = Convert.FromBase64String(base64);
        byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
