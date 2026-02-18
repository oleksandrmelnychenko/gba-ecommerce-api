using System.Text.Json.Serialization;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class TopCustomerByVolume {
    [JsonPropertyName("customer_id")]
    public long CustomerId { get; set; }

    [JsonPropertyName("customer_name")]
    public string CustomerName { get; set; }

    [JsonPropertyName("predicted_quantity")]
    public double PredictedQuantity { get; set; }

    [JsonPropertyName("contribution_pct")]
    public double ContributionPct { get; set; }
}