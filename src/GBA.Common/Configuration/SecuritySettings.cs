using System;
using System.Text;
using GBA.Common.Helpers;

namespace GBA.Common.Configuration;

public class SecuritySettings {
    public string JwtKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public string PriceEncryptionKey { get; set; } = string.Empty;
    public string PriceEncryptionIV { get; set; } = string.Empty;
    public string[] CorsOrigins { get; set; } = Array.Empty<string>();
    public bool EnableSwagger { get; set; }
    public bool AcceptLegacySharedTokens { get; set; }
    public string LegacyRefreshTokenKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeMinutes { get; set; } = 10080;

    public byte[] LegacyRefreshKeyMaterial { get; private set; } = Array.Empty<byte>();

    private static SecuritySettings _instance = new();

    public static SecuritySettings Instance => _instance;

    public static void Initialize(SecuritySettings settings) {
        if (settings is null) throw new InvalidOperationException("Security settings missing");
        if (string.IsNullOrWhiteSpace(settings.JwtKey)) throw new InvalidOperationException("Security:JwtKey is not configured");
        if (Encoding.UTF8.GetByteCount(settings.JwtKey) < 32) throw new InvalidOperationException("Security:JwtKey must be at least 32 bytes (256 bits)");
        if (string.IsNullOrWhiteSpace(settings.JwtIssuer)) throw new InvalidOperationException("Security:JwtIssuer is not configured");
        if (string.IsNullOrWhiteSpace(settings.JwtAudience)) throw new InvalidOperationException("Security:JwtAudience is not configured");
        if (string.IsNullOrWhiteSpace(settings.PriceEncryptionKey)) throw new InvalidOperationException("Security:PriceEncryptionKey is not configured");
        if (string.IsNullOrWhiteSpace(settings.PriceEncryptionIV)) throw new InvalidOperationException("Security:PriceEncryptionIV is not configured");
        if (settings.CorsOrigins is null || settings.CorsOrigins.Length == 0) throw new InvalidOperationException("Security:CorsOrigins is not configured");
        if (settings.AccessTokenLifetimeMinutes is < 1 or > 60)
            throw new InvalidOperationException("Security:AccessTokenLifetimeMinutes must be between 1 and 60");
        if (settings.RefreshTokenLifetimeMinutes < settings.AccessTokenLifetimeMinutes || settings.RefreshTokenLifetimeMinutes > 43200)
            throw new InvalidOperationException("Security:RefreshTokenLifetimeMinutes must be at least the access token lifetime and at most 43200");
        settings.LegacyRefreshKeyMaterial = settings.AcceptLegacySharedTokens
            ? DecodeLegacyRefreshKey(settings.LegacyRefreshTokenKey)
            : Array.Empty<byte>();
        _instance = settings;
    }

    private static byte[] DecodeLegacyRefreshKey(string value) {
        byte[] material;
        try {
            material = Convert.FromBase64String(value?.Trim() ?? string.Empty);
        } catch (FormatException) {
            material = Array.Empty<byte>();
        }

        if (material.Length != AesManager.DerivedKeyMaterialBytes)
            throw new InvalidOperationException(
                "Security:AcceptLegacySharedTokens requires Security:LegacyRefreshTokenKey: the base64 AES key material " +
                $"({AesManager.DerivedKeyMaterialBytes} bytes) derived from the former shared key, never the key itself");

        return material;
    }
}
