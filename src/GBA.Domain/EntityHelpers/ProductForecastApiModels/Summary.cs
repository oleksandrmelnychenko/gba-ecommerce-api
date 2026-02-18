using Newtonsoft.Json;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class Summary {
    [JsonProperty("total_predicted_quantity")]
    public double TotalPredictedQuantity { get; set; }

    [JsonProperty("total_predicted_revenue")]
    public double TotalPredictedRevenue { get; set; }

    [JsonProperty("total_predicted_orders")]
    public double TotalPredictedOrders { get; set; }

    [JsonProperty("active_customers")]
    public int ActiveCustomers { get; set; }

    [JsonProperty("at_risk_customers")]
    public int AtRiskCustomers { get; set; }
}