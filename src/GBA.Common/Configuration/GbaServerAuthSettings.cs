using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GBA.Common.Configuration;

public sealed class GbaServerAuthSettings {
    public const string SectionName = "GbaServerAuth";
    public const int MinimumRsaKeyBits = 2048;

    private static readonly string[] PublicKeyLabels = ["PUBLIC KEY", "RSA PUBLIC KEY"];

    public string JwtPublicKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public string JwtKey { get; set; } = string.Empty;

    public IReadOnlyList<RsaSecurityKey> ValidationKeys { get; private set; } = Array.Empty<RsaSecurityKey>();

    private static GbaServerAuthSettings _instance = new();

    public static GbaServerAuthSettings Instance => _instance;

    public bool IsConfigured => ValidationKeys.Count > 0;

    public static GbaServerAuthSettings Initialize(GbaServerAuthSettings settings, SecuritySettings security) {
        GbaServerAuthSettings normalized = Create(settings, security);
        _instance = normalized;
        return normalized;
    }

    public static GbaServerAuthSettings Create(GbaServerAuthSettings settings, SecuritySettings security) {
        ArgumentNullException.ThrowIfNull(security);

        if (!string.IsNullOrWhiteSpace(settings?.JwtKey))
            throw new InvalidOperationException(
                $"{SectionName}:JwtKey is no longer accepted: the shop must not hold gba-server's signing secret. " +
                $"Mount gba-server's RS256 public key as {SectionName}:JwtPublicKey instead.");

        GbaServerAuthSettings normalized = new() {
            JwtPublicKey = settings?.JwtPublicKey?.Trim() ?? string.Empty,
            JwtIssuer = settings?.JwtIssuer?.Trim() ?? string.Empty,
            JwtAudience = settings?.JwtAudience?.Trim() ?? string.Empty
        };

        if (normalized.JwtPublicKey.Length > 0) {
            if (string.IsNullOrWhiteSpace(normalized.JwtIssuer))
                throw new InvalidOperationException($"{SectionName}:JwtIssuer is not configured");
            if (string.IsNullOrWhiteSpace(normalized.JwtAudience))
                throw new InvalidOperationException($"{SectionName}:JwtAudience is not configured");
            if (string.Equals(normalized.JwtAudience, security.JwtAudience?.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"{SectionName}:JwtAudience must differ from Security:JwtAudience");
            if (string.Equals(normalized.JwtIssuer, security.JwtIssuer?.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"{SectionName}:JwtIssuer must differ from Security:JwtIssuer");

            normalized.ValidationKeys = ReadPublicKeys(normalized.JwtPublicKey);
        }

        return normalized;
    }

    public static IReadOnlyList<RsaSecurityKey> ReadPublicKeys(string pem) {
        string text = pem?.Trim() ?? string.Empty;
        if (text.Contains("-----BEGIN", StringComparison.Ordinal) && !text.Contains('\n'))
            text = text.Replace("\\n", "\n", StringComparison.Ordinal);

        List<RsaSecurityKey> keys = new();
        int offset = 0;

        while (offset < text.Length && PemEncoding.TryFind(text.AsSpan(offset), out PemFields fields)) {
            string label = text.Substring(offset + fields.Label.Start.Value, fields.Label.End.Value - fields.Label.Start.Value);
            string block = text.Substring(offset + fields.Location.Start.Value,
                fields.Location.End.Value - fields.Location.Start.Value);
            offset += fields.Location.End.Value;

            if (label.Contains("PRIVATE", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"{SectionName}:JwtPublicKey contains a private key; the shop may hold only gba-server's public key.");
            if (!PublicKeyLabels.Contains(label, StringComparer.Ordinal))
                throw new InvalidOperationException($"Unsupported PEM block '{label}' in {SectionName}:JwtPublicKey.");

            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(block);
            if (rsa.KeySize < MinimumRsaKeyBits)
                throw new InvalidOperationException($"{SectionName}:JwtPublicKey must be at least {MinimumRsaKeyBits} bits.");

            RSAParameters parameters = rsa.ExportParameters(false);
            string keyId = ComputeKeyId(parameters);
            if (keys.All(existing => existing.KeyId != keyId))
                keys.Add(new RsaSecurityKey(parameters) { KeyId = keyId });
        }

        if (keys.Count == 0)
            throw new InvalidOperationException($"{SectionName}:JwtPublicKey contains no RSA public key.");

        return keys;
    }

    public static string ComputeKeyId(RSAParameters parameters) {
        string canonical =
            "{\"e\":\"" + Base64UrlEncoder.Encode(parameters.Exponent) +
            "\",\"kty\":\"RSA\",\"n\":\"" + Base64UrlEncoder.Encode(parameters.Modulus) + "\"}";

        return Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
