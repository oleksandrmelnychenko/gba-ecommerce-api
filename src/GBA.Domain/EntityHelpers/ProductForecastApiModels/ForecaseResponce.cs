using System.Collections.Generic;
using Newtonsoft.Json;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class ForecastResponse {
    [JsonProperty("product_id")]
    public long ProductId { get; set; }

    [JsonProperty("product_name")]
    public string ProductName { get; set; }

    [JsonProperty("forecast_period_weeks")]
    public int ForecastPeriodWeeks { get; set; }

    [JsonProperty("historical_weeks")]
    public int HistoricalWeeks { get; set; }

    [JsonProperty("summary")]
    public Summary Summary { get; set; }

    [JsonProperty("weekly_data")]
    public List<WeeklyData> WeeklyData { get; set; }

    [JsonProperty("top_customers_by_volume")]
    public List<TopCustomerByVolume> TopCustomersByVolume { get; set; }

    [JsonProperty("at_risk_customers")]
    public List<AtRiskCustomer> AtRiskCustomers { get; set; }

    [JsonProperty("model_metadata")]
    public ModelMetadata ModelMetadata { get; set; }
}