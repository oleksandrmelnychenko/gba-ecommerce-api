using System;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ApiTestConfig {
    public const string DefaultBaseUrl = "https://ecom-api-dev.85.17.167.167.nip.io";
    public const string DefaultGbaApiBaseUrl = "https://gba-api-dev.85.17.167.167.nip.io";

    public Uri BaseUri { get; } = BuildBaseUriFrom("ECOM_API_BASE_URL", DefaultBaseUrl);
    public Uri GbaApiBaseUri { get; } = BuildBaseUriFrom("GBA_API_BASE_URL", DefaultGbaApiBaseUrl);
    public string Culture { get; } = GetString("ECOM_API_CULTURE", "uk");
    public string SearchQuery { get; } = GetString("ECOM_API_SEARCH_QUERY", "oil");
    public bool AllowInsecureTls { get; } = GetBool("ECOM_API_ALLOW_INSECURE_TLS", true);
    public string? Username { get; } = GetOptionalString("ECOM_API_USERNAME");
    public string? Password { get; } = GetOptionalString("ECOM_API_PASSWORD");
    public bool RunRealCartWrites { get; } = GetBool("ECOM_API_RUN_REAL_CART", false);
    public bool RunRealSaleWrites { get; } = GetBool("ECOM_API_RUN_REAL_SALE", false);
    public bool RunRealOrderWrites { get; } = GetBool("ECOM_API_RUN_REAL_ORDER", false) || GetBool("ECOM_API_RUN_REAL_SALE", false);
    public bool RunRealAuthLifecycleWrites { get; } = GetBool("ECOM_API_RUN_REAL_AUTH", false);
    public bool RunRealRegistrationWrites { get; } = GetBool("ECOM_API_RUN_REAL_REGISTRATION", false);

    public bool HasCredentials => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    public void EnsureSafeWriteTarget() {
        string host = BaseUri.Host;

        bool isSafeDevTarget = host.Contains("-dev.", StringComparison.OrdinalIgnoreCase)
            || host.Contains(".dev.", StringComparison.OrdinalIgnoreCase)
            || host.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

        if (!isSafeDevTarget) {
            throw new InvalidOperationException($"Refusing to run write-capable live tests against '{BaseUri}'. Point ECOM_API_BASE_URL at DEV/local first.");
        }
    }

    public string ApiPath(string relativePath) {
        return $"/api/v1/{Culture}/{relativePath.TrimStart('/')}";
    }

    public string GbaApiPath(string relativePath) {
        return new Uri(GbaApiBaseUri, $"/api/v1/{Culture}/{relativePath.TrimStart('/')}").ToString();
    }

    private static Uri BuildBaseUriFrom(string environmentVariableName, string fallback) {
        string value = GetString(environmentVariableName, fallback).TrimEnd('/');
        return new Uri(value, UriKind.Absolute);
    }

    private static string GetString(string name, string fallback) {
        string? value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? GetOptionalString(string name) {
        string? value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool GetBool(string name, bool fallback) {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value)) return fallback;

        return value.Trim().Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
