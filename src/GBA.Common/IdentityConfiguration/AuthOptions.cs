using System.Text;
using GBA.Common.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GBA.Common.IdentityConfiguration;

public sealed class AuthOptions {
    public const string ISSUER = "ConcordCRM";

    public const string AUDIENCE_LOCAL = "http://localhost:4200/";
    public const string AUDIENCE_REMOTE = "http://localhost:4200/";

    public const string DEFAULT_PASSWORD_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string KEY => SecuritySettings.Instance.JwtKey;

#if DEBUG
    public const int LIFETIME = 32400;
#else
    public const int LIFETIME = 32400;
#endif

    public const int REFRESH_LIFETIME = 43200;

    public static SymmetricSecurityKey GetSymmetricSecurityKey() {
        return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
    }
}