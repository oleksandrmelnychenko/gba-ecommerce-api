using GBA.Services.Services.Products;

namespace GBA.Search.Elasticsearch;

/// <summary>Defines the durable schema version required by every served search generation.</summary>
public static class SearchIndexSchema {
    public const string CurrentVersion = EcommercePricingSchema.Version;
}
