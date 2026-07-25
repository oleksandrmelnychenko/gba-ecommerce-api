using GBA.Services.Services.Products;

namespace GBA.Search.Elasticsearch;

/// <summary>Defines the durable schema version required by every served search generation.</summary>
public static class SearchIndexSchema {
    public const string CurrentVersion = "web-catalog-v5-live-sql";

    public static PricingDependencyRevisions LiveHydrationMarker { get; } = new(
        $"{CurrentVersion}:product-pricing",
        $"{CurrentVersion}:pricing-hierarchy",
        $"{CurrentVersion}:discounts",
        $"{CurrentVersion}:exchange-rates");
}
