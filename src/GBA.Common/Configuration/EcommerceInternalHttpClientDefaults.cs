using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GBA.Common.Configuration;

public static class EcommerceInternalHttpClientDefaults {
    public const string ClientName = "gba-internal";
    public const string HeaderName = "X-Internal-Api-Key";
    public const string SectionName = "EcommerceInternalAuth";
    public const int MinimumApiKeyLength = 32;

    public static string GetValidatedApiKey(string configuredApiKey) {
        string apiKey = configuredApiKey?.Trim() ?? string.Empty;
        if (apiKey.Length < MinimumApiKeyLength)
            throw new InvalidOperationException(
                $"{SectionName}:ApiKey must contain at least {MinimumApiKeyLength} characters.");

        return apiKey;
    }

    public static IServiceCollection AddEcommerceInternalHttpClient(
        this IServiceCollection services,
        IConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string apiKey = GetValidatedApiKey(
            configuration[$"{SectionName}:ApiKey"]);
        services.AddHttpClient(ClientName, client => {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderName, apiKey);
        });
        return services;
    }
}
