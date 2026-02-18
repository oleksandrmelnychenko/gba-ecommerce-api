using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class ForecastResponse {
    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; set; }

    [JsonPropertyName("forecast_period_weeks")]
    public int ForecastPeriodWeeks { get; set; }

    [JsonPropertyName("historical_weeks")]
    public int HistoricalWeeks { get; set; }

    [JsonPropertyName("summary")]
    public Summary Summary { get; set; }

    [JsonPropertyName("weekly_data")]
    public List<WeeklyData> WeeklyData { get; set; }

    [JsonPropertyName("top_customers_by_volume")]
    public List<TopCustomerByVolume> TopCustomersByVolume { get; set; }

    [JsonPropertyName("at_risk_customers")]
    public List<AtRiskCustomer> AtRiskCustomers { get; set; }

    [JsonPropertyName("model_metadata")]
    public ModelMetadata ModelMetadata { get; set; }
}