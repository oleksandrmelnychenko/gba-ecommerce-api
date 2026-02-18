using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using GBA.Common.IdentityConfiguration;
using Microsoft.IO;

namespace GBA.Common.Helpers;

/// <summary>
/// High-performance AES encryption using ArrayPool and RecyclableMemoryStream.
/// </summary>
public static class AesManager {
    private static readonly byte[] Salt = [
        0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
    ];

    private const int Iterations = 1000;

    // Cache derived key bytes - they're always the same for the same KEY
    private static byte[]? _cachedKey;
    private static byte[]? _cachedIv;

    // Recyclable memory stream manager for reduced allocations
    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();

    private static (byte[] key, byte[] iv) GetKeyAndIv() {
        if (_cachedKey == null || _cachedIv == null) {
            byte[] derivedBytes = Rfc2898DeriveBytes.Pbkdf2(
                AuthOptions.KEY,
                Salt,
                Iterations,
                HashAlgorithmName.SHA1,
                48);

            _cachedKey = derivedBytes[..32];
            _cachedIv = derivedBytes[32..48];
        }

        return (_cachedKey, _cachedIv);
    }

    public static string Encrypt(string toEncryptString) {
        int maxByteCount = Encoding.Unicode.GetMaxByteCount(toEncryptString.Length);
        byte[] rentedArray = ArrayPool<byte>.Shared.Rent(maxByteCount);

        try {
            int actualBytes = Encoding.Unicode.GetBytes(toEncryptString, rentedArray);
            return EncryptBytes(rentedArray.AsSpan(0, actualBytes));
        } finally {
            ArrayPool<byte>.Shared.Return(rentedArray, clearArray: true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string EncryptBytes(ReadOnlySpan<byte> clearBytes) {
        var (key, iv) = GetKeyAndIv();

        using Aes encryptor = Aes.Create();
        encryptor.Key = key;
        encryptor.IV = iv;

        using RecyclableMemoryStream ms = MemoryStreamManager.GetStream();
        using (CryptoStream cs = new(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true)) {
            cs.Write(clearBytes);
            cs.FlushFinalBlock();
        }

        return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
    }

    public static string Decrypt(string cipherText) {
        // Handle URL-encoded spaces using Span
        ReadOnlySpan<char> input = cipherText.AsSpan();
        int spaceCount = 0;
        foreach (char c in input) {
            if (c == ' ') spaceCount++;
        }

        string normalizedText;
        if (spaceCount > 0) {
            Span<char> buffer = cipherText.Length <= 256
                ? stackalloc char[cipherText.Length]
                : new char[cipherText.Length];

            for (int i = 0; i < input.Length; i++) {
                buffer[i] = input[i] == ' ' ? '+' : input[i];
            }

            normalizedText = new string(buffer);
        } else {
            normalizedText = cipherText;
        }

        byte[] cipherBytes = Convert.FromBase64String(normalizedText);
        return DecryptBytes(cipherBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string DecryptBytes(byte[] cipherBytes) {
        var (key, iv) = GetKeyAndIv();

        using Aes encryptor = Aes.Create();
        encryptor.Key = key;
        encryptor.IV = iv;

        using RecyclableMemoryStream ms = MemoryStreamManager.GetStream();
        using (CryptoStream cs = new(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write, leaveOpen: true)) {
            cs.Write(cipherBytes, 0, cipherBytes.Length);
            cs.FlushFinalBlock();
        }

        return Encoding.Unicode.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }
}