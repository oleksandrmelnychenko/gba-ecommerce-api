using System;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using GBA.Common.Configuration;

namespace GBA.Common.Helpers;

/// <summary>
/// High-performance price obfuscation using Span, stackalloc, and ArrayPool.
/// </summary>
public static class PriceObfuscator {
    // Cache key/IV bytes to avoid repeated encoding
    private static byte[]? _cachedKey;
    private static byte[]? _cachedIv;

    private static byte[] Key => _cachedKey ??= Encoding.UTF8.GetBytes(SecuritySettings.Instance.PriceEncryptionKey);
    private static byte[] Iv => _cachedIv ??= Encoding.UTF8.GetBytes(SecuritySettings.Instance.PriceEncryptionIV);

    public static string Encode(decimal price, long timestamp) {
        // Format: "price|timestamp" - max ~30 chars
        Span<char> dataBuffer = stackalloc char[64];
        int written = 0;

        price.TryFormat(dataBuffer, out int priceLen, "F2");
        written += priceLen;
        dataBuffer[written++] = '|';
        timestamp.TryFormat(dataBuffer[written..], out int tsLen);
        written += tsLen;

        // Convert to UTF8 bytes
        Span<byte> utf8Buffer = stackalloc byte[written * 2];
        int utf8Len = Encoding.UTF8.GetBytes(dataBuffer[..written], utf8Buffer);

        return EncryptAndEncode(utf8Buffer[..utf8Len]);
    }

    public static string EncodeMultiple(decimal[] prices, long timestamp) {
        // Estimate buffer size: ~10 chars per price + separators + timestamp
        int estimatedSize = prices.Length * 12 + 20;

        Span<char> dataBuffer = estimatedSize <= 256
            ? stackalloc char[256]
            : new char[estimatedSize];

        int written = 0;

        for (int i = 0; i < prices.Length; i++) {
            if (i > 0) dataBuffer[written++] = ',';
            prices[i].TryFormat(dataBuffer[written..], out int len, "F2");
            written += len;
        }

        dataBuffer[written++] = '|';
        timestamp.TryFormat(dataBuffer[written..], out int tsLen);
        written += tsLen;

        // Convert to UTF8 bytes
        int maxUtf8 = Encoding.UTF8.GetMaxByteCount(written);
        byte[]? rentedBytes = null;
        Span<byte> utf8Buffer = maxUtf8 <= 512
            ? stackalloc byte[512]
            : (rentedBytes = ArrayPool<byte>.Shared.Rent(maxUtf8));

        try {
            int utf8Len = Encoding.UTF8.GetBytes(dataBuffer[..written], utf8Buffer);
            return EncryptAndEncode(utf8Buffer[..utf8Len]);
        } finally {
            if (rentedBytes != null) ArrayPool<byte>.Shared.Return(rentedBytes);
        }
    }

    public static (decimal price, long timestamp)? Decode(string encoded) {
        try {
            // Convert URL-safe base64 to standard base64 using Span
            int base64Len = encoded.Length + (4 - encoded.Length % 4) % 4;
            Span<char> base64Buffer = base64Len <= 256
                ? stackalloc char[base64Len]
                : new char[base64Len];

            for (int i = 0; i < encoded.Length; i++) {
                base64Buffer[i] = encoded[i] switch {
                    '-' => '+',
                    '_' => '/',
                    _ => encoded[i]
                };
            }

            // Add padding
            for (int i = encoded.Length; i < base64Len; i++) {
                base64Buffer[i] = '=';
            }

            byte[] decrypted = DecryptAes(Convert.FromBase64String(new string(base64Buffer[..base64Len])));

            // Parse the decrypted data
            ReadOnlySpan<char> data = Encoding.UTF8.GetString(decrypted).AsSpan();
            int pipeIndex = data.IndexOf('|');

            if (pipeIndex < 0) return null;

            return (decimal.Parse(data[..pipeIndex]), long.Parse(data[(pipeIndex + 1)..]));
        } catch {
            return null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string EncryptAndEncode(ReadOnlySpan<byte> data) {
        byte[] encrypted = EncryptAes(data);

        // Convert to URL-safe base64 without padding
        int base64Len = ((encrypted.Length + 2) / 3) * 4;
        Span<char> result = base64Len <= 256
            ? stackalloc char[base64Len]
            : new char[base64Len];

        Convert.TryToBase64Chars(encrypted, result, out int charsWritten);

        // Replace in-place for URL-safe base64 and remove padding
        int finalLen = charsWritten;
        for (int i = 0; i < charsWritten; i++) {
            if (result[i] == '+') result[i] = '-';
            else if (result[i] == '/') result[i] = '_';
            else if (result[i] == '=') { finalLen = i; break; }
        }

        return new string(result[..finalLen]);
    }

    private static byte[] EncryptAes(ReadOnlySpan<byte> data) {
        using Aes aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Use pooled array for input
        byte[] inputArray = ArrayPool<byte>.Shared.Rent(data.Length);
        try {
            data.CopyTo(inputArray);
            using ICryptoTransform encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(inputArray, 0, data.Length);
        } finally {
            ArrayPool<byte>.Shared.Return(inputArray, clearArray: true);
        }
    }

    private static byte[] DecryptAes(byte[] data) {
        using Aes aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTimestampValid(long timestamp, int maxAgeSeconds = 300) {
        long now = GetTimestamp();
        return Math.Abs(now - timestamp) <= maxAgeSeconds;
    }
}
