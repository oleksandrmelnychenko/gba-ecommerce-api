using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GBA.Domain.EntityHelpers.ClientPredictionsDtos;

public class ProductRecommendation {
    [JsonProperty("customer_id")]
    public long CustomerId { get; set; }

    [JsonProperty("recommendations")]
    public List<Recommendation> Recommendations { get; set; }

    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("discovery_count")]
    public int DiscoveryCount { get; set; }

    [JsonProperty("precision_estimate")]
    public double PrecisionEstimate { get; set; }

    [JsonProperty("latency_ms")]
    public double LatencyMs { get; set; }

    [JsonProperty("cached")]
    public bool Cached { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }
}