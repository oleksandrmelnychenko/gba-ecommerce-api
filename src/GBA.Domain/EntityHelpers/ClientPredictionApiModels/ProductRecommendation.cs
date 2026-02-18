using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GBA.Domain.EntityHelpers.ClientPredictionsDtos;

public class ProductRecommendation {
    [JsonPropertyName("customer_id")]
    public long CustomerId { get; set; }

    [JsonPropertyName("recommendations")]
    public List<Recommendation> Recommendations { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("discovery_count")]
    public int DiscoveryCount { get; set; }

    [JsonPropertyName("precision_estimate")]
    public double PrecisionEstimate { get; set; }

    [JsonPropertyName("latency_ms")]
    public double LatencyMs { get; set; }

    [JsonPropertyName("cached")]
    public bool Cached { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}