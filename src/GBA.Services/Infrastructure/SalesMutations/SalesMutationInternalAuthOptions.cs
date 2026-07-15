using System;

namespace GBA.Services.Infrastructure.SalesMutations;

/// <summary>
/// Provides the shared credential used only for durable ecommerce-to-CRM sales mutations.
/// </summary>
public sealed class SalesMutationInternalAuthOptions {
    private const int MinimumApiKeyLength = 32;

    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "EcommerceInternalAuth";

    /// <summary>Gets the HTTP header carrying the shared internal-service credential.</summary>
    public const string HeaderName = "X-Internal-Api-Key";

    /// <summary>Gets or sets the shared internal-service credential.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Validates the credential and returns its normalized in-memory value.</summary>
    /// <returns>The validated API key without surrounding file whitespace.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the API key is absent or too short.</exception>
    public string GetValidatedApiKey() {
        string apiKey = ApiKey?.Trim() ?? string.Empty;
        if (apiKey.Length < MinimumApiKeyLength)
            throw new InvalidOperationException(
                $"{SectionName}:ApiKey must contain at least {MinimumApiKeyLength} characters.");

        return apiKey;
    }
}
