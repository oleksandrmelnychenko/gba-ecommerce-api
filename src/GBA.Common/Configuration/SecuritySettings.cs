using System;
using System.Text;

namespace GBA.Common.Configuration;

public class SecuritySettings {
    public string JwtKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public string PriceEncryptionKey { get; set; } = string.Empty;
    public string PriceEncryptionIV { get; set; } = string.Empty;
    public string[] CorsOrigins { get; set; } = Array.Empty<string>();

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
        _instance = settings;
    }
}
