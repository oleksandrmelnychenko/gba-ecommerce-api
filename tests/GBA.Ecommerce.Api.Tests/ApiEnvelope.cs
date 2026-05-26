using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

internal sealed class ApiEnvelope {
    public JsonElement Body { get; init; }
    public string Message { get; init; } = string.Empty;
    public int StatusCode { get; init; }
}

internal static class ApiAssertions {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ApiEnvelope> ReadEnvelopeAsync(HttpResponseMessage response) {
        string json = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(json), "Expected an API response envelope, but response body was empty.");

        ApiEnvelope? envelope = JsonSerializer.Deserialize<ApiEnvelope>(json, JsonOptions);
        Assert.NotNull(envelope);

        return envelope;
    }

    public static void AssertSuccessEnvelope(ApiEnvelope envelope) {
        Assert.Equal(200, envelope.StatusCode);
        Assert.True(string.IsNullOrEmpty(envelope.Message), $"Expected empty message, got '{envelope.Message}'.");
    }

    public static string RequiredString(JsonElement element, string propertyName) {
        Assert.True(element.TryGetProperty(propertyName, out JsonElement property), $"Missing '{propertyName}' property.");
        Assert.Equal(JsonValueKind.String, property.ValueKind);

        string? value = property.GetString();
        Assert.False(string.IsNullOrWhiteSpace(value), $"Expected '{propertyName}' to be a non-empty string.");

        return value!;
    }

    public static int RequiredInt32(JsonElement element, string propertyName) {
        Assert.True(element.TryGetProperty(propertyName, out JsonElement property), $"Missing '{propertyName}' property.");
        Assert.True(property.TryGetInt32(out int value), $"Expected '{propertyName}' to be an int.");

        return value;
    }

    public static decimal RequiredDecimal(JsonElement element, string propertyName) {
        Assert.True(element.TryGetProperty(propertyName, out JsonElement property), $"Missing '{propertyName}' property.");
        Assert.True(property.TryGetDecimal(out decimal value), $"Expected '{propertyName}' to be a decimal.");

        return value;
    }

    public static Guid RequiredGuid(JsonElement element, string propertyName) {
        string value = RequiredString(element, propertyName);
        Assert.True(Guid.TryParse(value, out Guid guid), $"Expected '{propertyName}' to be a GUID.");
        Assert.NotEqual(Guid.Empty, guid);

        return guid;
    }
}
