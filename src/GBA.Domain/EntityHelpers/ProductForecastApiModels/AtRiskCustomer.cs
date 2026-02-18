using System;
using System.Text.Json.Serialization;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class AtRiskCustomer {
    [JsonPropertyName("customer_id")]
    public long CustomerId { get; set; }

    [JsonPropertyName("customer_name")]
    public string CustomerName { get; set; }

    [JsonPropertyName("last_order")]
    public DateTime LastOrder { get; set; }

    [JsonPropertyName("expected_reorder")]
    public DateTime ExpectedReorder { get; set; }

    [JsonPropertyName("days_overdue")]
    public int DaysOverdue { get; set; }

    [JsonPropertyName("churn_probability")]
    public double ChurnProbability { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; }
}