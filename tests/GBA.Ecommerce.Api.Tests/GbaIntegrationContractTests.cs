using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class GbaIntegrationContractTests(EcommerceApiFixture fixture) : IClassFixture<EcommerceApiFixture> {
    private static readonly Guid MissingNetId = Guid.Empty;

    private readonly HttpClient _client = fixture.Client;
    private readonly ApiTestConfig _config = fixture.Config;

    [Fact]
    public async Task Gba_health_endpoint_is_reachable_on_dev_base_url() {
        using HttpResponseMessage response = await _client.GetAsync(new Uri(_config.GbaApiBaseUri, "/health"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("sales/get?netId=00000000-0000-0000-0000-000000000000", "GET")]
    [InlineData("sales/get/merged?netId=00000000-0000-0000-0000-000000000000", "GET")]
    [InlineData("sales/get/current?netId=00000000-0000-0000-0000-000000000000", "GET")]
    [InlineData("sales/carts/all", "GET")]
    [InlineData("sales/all/client/pl-uk", "GET")]
    [InlineData("sales/all/items/locations?netId=00000000-0000-0000-0000-000000000000", "GET")]
    [InlineData("sales/discount/calculate", "POST")]
    [InlineData("orders/items/new?clientAgreementNetId=00000000-0000-0000-0000-000000000000&saleNetId=00000000-0000-0000-0000-000000000000", "POST")]
    [InlineData("orders/items/update", "POST")]
    [InlineData("orders/items/delete?orderItemNetId=00000000-0000-0000-0000-000000000000", "DELETE")]
    [InlineData("orders/items/shift/current", "POST")]
    [InlineData("orders/items/shift/specific?saleFromNetId=00000000-0000-0000-0000-000000000000&saleToNetId=00000000-0000-0000-0000-000000000000", "POST")]
    public async Task Gba_sales_and_order_item_routes_require_bearer_token(string relativePath, string method) {
        using HttpRequestMessage request = new(new HttpMethod(method), _config.GbaApiPath(relativePath));

        if (method == "POST") {
            request.Content = JsonContent.Create(new { });
        }

        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Theory]
    [InlineData("sales/update/ecommerce")]
    [InlineData("sales/payment/save?saleNetId=00000000-0000-0000-0000-000000000000&clientNetId=00000000-0000-0000-0000-000000000000")]
    public async Task Gba_anonymous_ecommerce_bridge_rejects_empty_json_payload(string relativePath) {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            _config.GbaApiPath(relativePath),
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
    }

    [Fact]
    public async Task Gba_anonymous_ttn_upload_rejects_missing_file_payload() {
        using HttpRequestMessage request = new(HttpMethod.Post, _config.GbaApiPath("sales/save/ttn"));
        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
    }

    [Fact]
    public async Task Gba_legacy_allegro_reservation_route_is_not_exposed_on_dev() {
        using HttpResponseMessage response = await _client.GetAsync(
            _config.GbaApiPath($"allegro/reservations/get?netId={MissingNetId}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
