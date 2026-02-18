using System;
using System.Text.Json.Serialization;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class ExpectedCustomer {
    [JsonPropertyName("customer_id")]
    public long CustomerId { get; set; }

    [JsonPropertyName("customer_name")]
    public string CustomerName { get; set; }

    [JsonPropertyName("probability")]
    public double Probability { get; set; }

    [JsonPropertyName("expected_quantity")]
    public double ExpectedQuantity { get; set; }

    [JsonPropertyName("expected_date")]
    public DateTime ExpectedDate { get; set; }

    [JsonPropertyName("days_since_last_order")]
    public int DaysSinceLastOrder { get; set; }

    [JsonPropertyName("avg_reorder_cycle")]
    public double AvgReorderCycle { get; set; }
}