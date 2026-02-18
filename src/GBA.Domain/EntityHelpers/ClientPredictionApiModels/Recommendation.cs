using Newtonsoft.Json;

namespace GBA.Domain.EntityHelpers.ClientPredictionsDtos;

public class Recommendation {
    [JsonProperty("product_id")]
    public long ProductId { get; set; }

    [JsonProperty("score")]
    public double Score { get; set; }

    [JsonProperty("rank")]
    public double Rank { get; set; }

    [JsonProperty("segment")]
    public string Segment { get; set; }

    [JsonProperty("source")]
    public string Source { get; set; }
}