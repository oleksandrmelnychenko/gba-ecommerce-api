using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class WeeklyData {
    [JsonProperty("week_start")]
    public DateTime WeekStart { get; set; }

    [JsonProperty("week_end")]
    public DateTime WeekEnd { get; set; }

    [JsonProperty("quantity")]
    public double Quantity { get; set; }

    [JsonProperty("revenue")]
    public double Revenue { get; set; }

    [JsonProperty("orders")]
    public double Orders { get; set; }

    [JsonProperty("data_type")]
    public string DataType { get; set; }

    [JsonProperty("confidence_lower")]
    public double? ConfidenceLower { get; set; }

    [JsonProperty("confidence_upper")]
    public double? ConfidenceUpper { get; set; }

    [JsonProperty("expected_customers")]
    public List<ExpectedCustomer> ExpectedCustomers { get; set; }
}