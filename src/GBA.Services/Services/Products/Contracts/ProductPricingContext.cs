namespace GBA.Services.Services.Products.Contracts;

/// <summary>
/// Identifies the agreement-backed pricing context used by the ecommerce surface.
/// </summary>
public sealed record ProductPricingContext(
    bool WithVat,
    string CurrencyCode,
    string CacheKey);
