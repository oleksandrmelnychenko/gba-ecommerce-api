namespace GBA.Search.Elasticsearch;

public sealed class ElasticsearchSettings {
    public string Url { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "products";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}
