using System.Text.Json.Serialization;

namespace GBA.Domain.EntityHelpers.ClientPredictionsDtos;

public class Recommendation {
    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("rank")]
    public double Rank { get; set; }

    [JsonPropertyName("segment")]
    public string Segment { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }
}