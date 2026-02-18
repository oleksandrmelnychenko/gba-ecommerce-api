using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class WeeklyData {
    [JsonPropertyName("week_start")]
    public DateTime WeekStart { get; set; }

    [JsonPropertyName("week_end")]
    public DateTime WeekEnd { get; set; }

    [JsonPropertyName("quantity")]
    public double Quantity { get; set; }

    [JsonPropertyName("revenue")]
    public double Revenue { get; set; }

    [JsonPropertyName("orders")]
    public double Orders { get; set; }

    [JsonPropertyName("data_type")]
    public string DataType { get; set; }

    [JsonPropertyName("confidence_lower")]
    public double? ConfidenceLower { get; set; }

    [JsonPropertyName("confidence_upper")]
    public double? ConfidenceUpper { get; set; }

    [JsonPropertyName("expected_customers")]
    public List<ExpectedCustomer> ExpectedCustomers { get; set; }
}