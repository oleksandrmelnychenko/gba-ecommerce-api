using System;
using Newtonsoft.Json;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class ExpectedCustomer {
    [JsonProperty("customer_id")]
    public long CustomerId { get; set; }

    [JsonProperty("customer_name")]
    public string CustomerName { get; set; }

    [JsonProperty("probability")]
    public double Probability { get; set; }

    [JsonProperty("expected_quantity")]
    public double ExpectedQuantity { get; set; }

    [JsonProperty("expected_date")]
    public DateTime ExpectedDate { get; set; }

    [JsonProperty("days_since_last_order")]
    public int DaysSinceLastOrder { get; set; }

    [JsonProperty("avg_reorder_cycle")]
    public double AvgReorderCycle { get; set; }
}