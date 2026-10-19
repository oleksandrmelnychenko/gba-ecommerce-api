using System.Text;
using GBA.Common.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GBA.Common.IdentityConfiguration;

public sealed class AuthOptions {
    public const string DEFAULT_PASSWORD_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static int LIFETIME => SecuritySettings.Instance.AccessTokenLifetimeMinutes;
    public static int REFRESH_LIFETIME => SecuritySettings.Instance.RefreshTokenLifetimeMinutes;

    public static string ISSUER => SecuritySettings.Instance.JwtIssuer;
    public static string AUDIENCE => SecuritySettings.Instance.JwtAudience;
    public static string KEY => SecuritySettings.Instance.JwtKey;

    public static SymmetricSecurityKey GetSymmetricSecurityKey() {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
    }
}