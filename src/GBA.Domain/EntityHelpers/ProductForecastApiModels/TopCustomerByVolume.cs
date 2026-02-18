using Newtonsoft.Json;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class TopCustomerByVolume {
    [JsonProperty("customer_id")]
    public long CustomerId { get; set; }

    [JsonProperty("customer_name")]
    public string CustomerName { get; set; }

    [JsonProperty("predicted_quantity")]
    public double PredictedQuantity { get; set; }

    [JsonProperty("contribution_pct")]
    public double ContributionPct { get; set; }
}