using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GBA.Domain.EntityHelpers.ProductForecastApi;

public class ModelMetadata {
    [JsonPropertyName("model_type")]
    public string ModelType { get; set; }

    [JsonPropertyName("training_customers")]
    public int TrainingCustomers { get; set; }

    [JsonPropertyName("forecast_accuracy_estimate")]
    public double ForecastAccuracyEstimate { get; set; }

    [JsonPropertyName("seasonality_detected")]
    public bool SeasonalityDetected { get; set; }

    [JsonPropertyName("model_version")]
    public string ModelVersion { get; set; }

    [JsonPropertyName("statistical_methods")]
    public List<string> StatisticalMethods { get; set; }
}