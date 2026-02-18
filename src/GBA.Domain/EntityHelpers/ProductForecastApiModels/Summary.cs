using System.Text.Json.Serialization;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class Summary {
    [JsonPropertyName("total_predicted_quantity")]
    public double TotalPredictedQuantity { get; set; }

    [JsonPropertyName("total_predicted_revenue")]
    public double TotalPredictedRevenue { get; set; }

    [JsonPropertyName("total_predicted_orders")]
    public double TotalPredictedOrders { get; set; }

    [JsonPropertyName("active_customers")]
    public int ActiveCustomers { get; set; }

    [JsonPropertyName("at_risk_customers")]
    public int AtRiskCustomers { get; set; }
}