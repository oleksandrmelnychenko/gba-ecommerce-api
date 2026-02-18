using System.Collections.Generic;
using Newtonsoft.Json;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class ModelMetadata {
    [JsonProperty("model_type")]
    public string ModelType { get; set; }

    [JsonProperty("training_customers")]
    public int TrainingCustomers { get; set; }

    [JsonProperty("forecast_accuracy_estimate")]
    public double ForecastAccuracyEstimate { get; set; }

    [JsonProperty("seasonality_detected")]
    public bool SeasonalityDetected { get; set; }

    [JsonProperty("model_version")]
    public string ModelVersion { get; set; }

    [JsonProperty("statistical_methods")]
    public List<string> StatisticalMethods { get; set; }
}