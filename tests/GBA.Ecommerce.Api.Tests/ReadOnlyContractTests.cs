using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ReadOnlyContractTests(EcommerceApiFixture fixture) : IClassFixture<EcommerceApiFixture> {
    private readonly HttpClient _client = fixture.Client;
    private readonly ApiTestConfig _config = fixture.Config;

    [Fact]
    public async Task Health_endpoint_reports_api_and_database_ready() {
        using HttpResponseMessage response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = json.RootElement;

        Assert.Equal("Healthy", ApiAssertions.RequiredString(root, "status"));
        Assert.True(root.TryGetProperty("checks", out JsonElement checks));
        Assert.Equal(JsonValueKind.Array, checks.ValueKind);
        Assert.Contains(checks.EnumerateArray(), check =>
            check.TryGetProperty("name", out JsonElement name)
            && name.GetString() == "db-main"
            && check.TryGetProperty("status", out JsonElement status)
            && status.GetString() == "Healthy");
    }

    [Fact]
    public async Task Security_headers_are_present_on_public_api_responses() {
        using HttpResponseMessage response = await _client.GetAsync(_config.ApiPath("elasticsearch/health"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
    }

    [Fact]
    public async Task Elasticsearch_health_reports_product_index_ready() {
        using HttpResponseMessage response = await _client.GetAsync(_config.ApiPath("elasticsearch/health"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        Assert.True(envelope.Body.TryGetProperty("healthy", out JsonElement healthy));
        Assert.True(healthy.GetBoolean());
    }

    [Fact]
    public async Task Anonymous_product_search_returns_shop_product_contract() {
        string url = _config.ApiPath($"products/search?value={Uri.EscapeDataString(_config.SearchQuery)}&limit=2&offset=0&withVat=0");
        using HttpResponseMessage response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);

        JsonElement[] products = envelope.Body.EnumerateArray().ToArray();
        Assert.NotEmpty(products);
        Assert.True(products.Length <= 2);

        JsonElement product = products[0];
        Assert.True(ApiAssertions.RequiredInt32(product, "Id") > 0);
        ApiAssertions.RequiredGuid(product, "NetUid");
        ApiAssertions.RequiredString(product, "VendorCode");
        ApiAssertions.RequiredString(product, "Name");
        ApiAssertions.RequiredString(product, "CurrencyCode");
        ApiAssertions.RequiredString(product, "P");

        Assert.True(product.TryGetProperty("AvailableQtyUk", out JsonElement availableQty));
        Assert.Equal(JsonValueKind.Number, availableQty.ValueKind);
    }

    [Fact]
    public async Task Anonymous_elasticsearch_search_returns_ids_and_total_count() {
        string url = _config.ApiPath($"elasticsearch/search?query={Uri.EscapeDataString(_config.SearchQuery)}&limit=2&offset=0");
        using HttpResponseMessage response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        Assert.True(envelope.Body.TryGetProperty("ProductIds", out JsonElement ids));
        Assert.Equal(JsonValueKind.Array, ids.ValueKind);
        Assert.NotEmpty(ids.EnumerateArray());
        Assert.True(ids.EnumerateArray().All(id => id.ValueKind == JsonValueKind.Number && id.GetInt32() > 0));

        Assert.True(ApiAssertions.RequiredInt32(envelope.Body, "TotalCount") >= ids.GetArrayLength());
        Assert.True(ApiAssertions.RequiredInt32(envelope.Body, "SearchTimeMs") >= 0);
    }

    [Fact]
    public async Task Order_calculate_uses_request_prices_without_creating_sale() {
        var payload = new {
            OrderItems = new[] {
                new {
                    Qty = 2,
                    OverLordQty = 2,
                    Product = new {
                        CurrentPrice = 10,
                        CurrentLocalPrice = 400
                    }
                }
            }
        };

        using HttpResponseMessage response = await _client.PostAsJsonAsync(_config.ApiPath("orders/calculate"), payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        Assert.Equal(20m, ApiAssertions.RequiredDecimal(envelope.Body, "TotalAmount"));
        Assert.Equal(800m, ApiAssertions.RequiredDecimal(envelope.Body, "TotalAmountLocal"));

        Assert.True(envelope.Body.TryGetProperty("OrderItems", out JsonElement items));
        Assert.Single(items.EnumerateArray());
    }

    [Fact]
    public async Task Shop_bootstrap_lookups_are_available() {
        await AssertNonEmptyArrayEndpoint("regions/all");
        await AssertNonEmptyArrayEndpoint("car/brands/all");
        await AssertNonEmptyArrayEndpoint("exchangerates/get/current");
        await AssertNonEmptyArrayEndpoint("transporters/types/all");

        using HttpResponseMessage seoResponse = await _client.GetAsync(_config.ApiPath("seo/info/all"));
        Assert.Equal(HttpStatusCode.OK, seoResponse.StatusCode);

        ApiEnvelope seoEnvelope = await ApiAssertions.ReadEnvelopeAsync(seoResponse);
        ApiAssertions.AssertSuccessEnvelope(seoEnvelope);
        Assert.True(seoEnvelope.Body.TryGetProperty("EcommerceContactInfo", out JsonElement contactInfo));
        ApiAssertions.RequiredString(contactInfo, "Phone");
        ApiAssertions.RequiredString(contactInfo, "Email");
    }

    private async Task AssertNonEmptyArrayEndpoint(string relativePath) {
        using HttpResponseMessage response = await _client.GetAsync(_config.ApiPath(relativePath));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);
        Assert.NotEmpty(envelope.Body.EnumerateArray());
    }
}
