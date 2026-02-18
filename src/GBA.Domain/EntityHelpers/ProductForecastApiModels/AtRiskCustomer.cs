using System;
using Newtonsoft.Json;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class AtRiskCustomer {
    [JsonProperty("customer_id")]
    public long CustomerId { get; set; }

    [JsonProperty("customer_name")]
    public string CustomerName { get; set; }

    [JsonProperty("last_order")]
    public DateTime LastOrder { get; set; }

    [JsonProperty("expected_reorder")]
    public DateTime ExpectedReorder { get; set; }

    [JsonProperty("days_overdue")]
    public int DaysOverdue { get; set; }

    [JsonProperty("churn_probability")]
    public double ChurnProbability { get; set; }

    [JsonProperty("action")]
    public string Action { get; set; }
}