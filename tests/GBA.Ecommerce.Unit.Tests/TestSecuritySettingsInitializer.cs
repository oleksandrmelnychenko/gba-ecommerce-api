using System.Runtime.CompilerServices;
using GBA.Common.Configuration;

namespace GBA.Ecommerce.Unit.Tests;

/// <summary>
/// Price payloads are AES-protected in production, so the suite must run with a real
/// key/IV — otherwise the encryption path silently drops out of test coverage.
/// </summary>
internal static class TestSecuritySettingsInitializer {
    [ModuleInitializer]
    internal static void Initialize() {
        SecuritySettings.Initialize(new SecuritySettings {
            JwtKey = "unit-test-jwt-key-with-at-least-32-bytes",
            JwtIssuer = "unit-tests",
            JwtAudience = "unit-tests",
            PriceEncryptionKey = "0123456789abcdef0123456789abcdef",
            PriceEncryptionIV = "0123456789abcdef",
            CorsOrigins = ["http://localhost"]
        });
    }
}
