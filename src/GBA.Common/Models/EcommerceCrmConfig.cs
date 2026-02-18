using System.Text.Json.Serialization;

namespace GBA.Common.Models;

public class EcommerceCrmConfig {
    [JsonPropertyName("CrmServerUrl")]
    public string? CrmServerUrl { get; set; }

    [JsonPropertyName("CrmServerUrlRelease")]
    public string? CrmServerUrlRelease { get; set; }

    [JsonPropertyName("EcommerceClientUrl")]
    public string? EcommerceClientUrl { get; set; }

    [JsonPropertyName("EcommerceClientUrlRelease")]
    public string? EcommerceClientUrlRelease { get; set; }
}
