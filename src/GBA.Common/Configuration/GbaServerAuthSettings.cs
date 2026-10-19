using System;
using System.Text;

namespace GBA.Common.Configuration;

public sealed class GbaServerAuthSettings {
    public const string SectionName = "GbaServerAuth";
    public const int MinimumKeyBytes = 32;

    public string JwtKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;

    private static GbaServerAuthSettings _instance = new();

    public static GbaServerAuthSettings Instance => _instance;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(JwtKey);

    public static GbaServerAuthSettings Initialize(GbaServerAuthSettings settings, SecuritySettings security) {
        ArgumentNullException.ThrowIfNull(security);

        GbaServerAuthSettings normalized = new() {
            JwtKey = settings?.JwtKey?.Trim() ?? string.Empty,
            JwtIssuer = settings?.JwtIssuer?.Trim() ?? string.Empty,
            JwtAudience = settings?.JwtAudience?.Trim() ?? string.Empty
        };

        if (normalized.IsConfigured) {
            if (Encoding.UTF8.GetByteCount(normalized.JwtKey) < MinimumKeyBytes)
                throw new InvalidOperationException($"{SectionName}:JwtKey must be at least {MinimumKeyBytes} bytes (256 bits)");
            if (string.IsNullOrWhiteSpace(normalized.JwtIssuer))
                throw new InvalidOperationException($"{SectionName}:JwtIssuer is not configured");
            if (string.IsNullOrWhiteSpace(normalized.JwtAudience))
                throw new InvalidOperationException($"{SectionName}:JwtAudience is not configured");
            if (string.Equals(normalized.JwtKey, security.JwtKey?.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"{SectionName}:JwtKey must differ from Security:JwtKey; the shop signs its own tokens");
            if (string.Equals(normalized.JwtAudience, security.JwtAudience?.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"{SectionName}:JwtAudience must differ from Security:JwtAudience");
        }

        if (security.AcceptLegacySharedTokens && !normalized.IsConfigured)
            throw new InvalidOperationException($"Security:AcceptLegacySharedTokens requires {SectionName} to name the former shared key");

        _instance = normalized;
        return normalized;
    }
}
