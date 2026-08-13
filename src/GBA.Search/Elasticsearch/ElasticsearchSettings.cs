namespace GBA.Search.Elasticsearch;

public sealed class ElasticsearchSettings {
    public string Url { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "products";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of additional attempts made after an Elasticsearch HTTP timeout.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay for timeout retries. Each subsequent retry doubles this delay,
    /// up to the service's bounded maximum.
    /// </summary>
    public int RetryBaseDelayMilliseconds { get; set; } = 1000;
}
