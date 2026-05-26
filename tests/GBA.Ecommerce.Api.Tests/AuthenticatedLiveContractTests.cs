using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class AuthenticatedLiveContractTests(EcommerceApiFixture fixture) : IClassFixture<EcommerceApiFixture> {
    private readonly HttpClient _client = fixture.Client;
    private readonly ApiTestConfig _config = fixture.Config;

    [Fact]
    public async Task Login_with_real_dev_credentials_returns_access_and_refresh_tokens_when_configured() {
        if (!_config.HasCredentials) return;

        LoginTokens tokens = await LoginAsync();

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Authenticated_cart_read_contract_matches_current_client_when_credentials_are_configured() {
        if (!_config.HasCredentials) return;

        LoginTokens tokens = await LoginAsync();
        using HttpRequestMessage request = new(HttpMethod.Get, _config.ApiPath("clients/shoppingcart/items/all?withVat=0"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);
    }

    [Fact]
    public async Task Authenticated_cart_read_contract_includes_expected_item_shape_when_non_empty() {
        if (!_config.HasCredentials) return;

        LoginTokens tokens = await LoginAsync();
        JsonElement[] cartItems = await GetCurrentCartItemsAsync(tokens.AccessToken);
        if (cartItems.Length == 0) return;

        JsonElement item = cartItems[0];
        Assert.True(ApiAssertions.RequiredInt32(item, "Id") > 0);
        ApiAssertions.RequiredGuid(item, "NetUid");
        AssertNumberProperty(item, "Qty");
        AssertNumberProperty(item, "OverLordQty");
        Assert.True(ApiAssertions.RequiredInt32(item, "ProductId") > 0);

        Assert.True(item.TryGetProperty("Product", out JsonElement product));
        Assert.Equal(JsonValueKind.Object, product.ValueKind);
        Assert.True(ApiAssertions.RequiredInt32(product, "Id") > 0);
        ApiAssertions.RequiredGuid(product, "NetUid");
    }

    [Fact]
    public async Task Real_cart_add_update_delete_flow_runs_only_with_explicit_write_flag() {
        if (!_config.HasCredentials || !_config.RunRealCartWrites) return;
        if (_config.RunRealSaleWrites) throw new InvalidOperationException("Cart test does not create sales. Use a separate explicit sale test for invoices.");
        _config.EnsureSafeWriteTarget();

        LoginTokens tokens = await LoginAsync();
        JsonElement product = await GetFirstProductAsync();

        int productId = ApiAssertions.RequiredInt32(product, "Id");
        Guid productNetUid = ApiAssertions.RequiredGuid(product, "NetUid");

        var addPayload = new {
            Id = 0,
            Qty = 1,
            OverLordQty = 1,
            ProductId = productId,
            Product = new {
                Id = productId,
                NetUid = productNetUid
            }
        };

        using HttpResponseMessage addResponse = await SendAuthorizedJsonAsync(
            HttpMethod.Post,
            _config.ApiPath("clients/shoppingcart/items/new?withVat=0"),
            addPayload,
            tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        ApiEnvelope addEnvelope = await ApiAssertions.ReadEnvelopeAsync(addResponse);
        ApiAssertions.AssertSuccessEnvelope(addEnvelope);

        JsonElement createdItem = ExtractCartItem(addEnvelope.Body);
        int orderItemId = ApiAssertions.RequiredInt32(createdItem, "Id");
        Guid orderItemNetUid = ApiAssertions.RequiredGuid(createdItem, "NetUid");

        try {
            var updatePayload = new {
                Id = orderItemId,
                NetUid = orderItemNetUid,
                ProductId = productId,
                Qty = 2,
                OverLordQty = 2
            };

            using HttpResponseMessage updateResponse = await SendAuthorizedJsonAsync(
                HttpMethod.Post,
                _config.ApiPath("clients/shoppingcart/items/update?withVat=0"),
                updatePayload,
                tokens.AccessToken);

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        } finally {
            using HttpRequestMessage deleteRequest = new(
                HttpMethod.Delete,
                _config.ApiPath($"clients/shoppingcart/items/delete?netId={orderItemNetUid}&withVat=0"));
            deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            using HttpResponseMessage deleteResponse = await _client.SendAsync(deleteRequest);
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        }
    }

    private async Task<LoginTokens> LoginAsync() {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            _config.ApiPath("usermanagement/token"),
            new {
                username = _config.Username,
                password = _config.Password
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        string? accessToken = FindStringValue(envelope.Body, "AccessToken")
            ?? FindStringValue(envelope.Body, "accessToken")
            ?? FindStringValue(envelope.Body, "Token")
            ?? FindStringValue(envelope.Body, "token");

        string? refreshToken = FindStringValue(envelope.Body, "RefreshToken")
            ?? FindStringValue(envelope.Body, "refreshToken");

        Assert.False(string.IsNullOrWhiteSpace(accessToken), "Login response did not include an access token.");
        Assert.False(string.IsNullOrWhiteSpace(refreshToken), "Login response did not include a refresh token.");

        return new LoginTokens(accessToken!, refreshToken!);
    }

    private async Task<JsonElement> GetFirstProductAsync() {
        using HttpResponseMessage response = await _client.GetAsync(
            _config.ApiPath($"products/search?value={Uri.EscapeDataString(_config.SearchQuery)}&limit=1&offset=0&withVat=0"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        JsonElement[] products = envelope.Body.EnumerateArray().ToArray();
        Assert.NotEmpty(products);

        return products[0];
    }

    private async Task<JsonElement[]> GetCurrentCartItemsAsync(string accessToken) {
        using HttpRequestMessage request = new(HttpMethod.Get, _config.ApiPath("clients/shoppingcart/items/all?withVat=0"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);

        return envelope.Body.EnumerateArray().ToArray();
    }

    private async Task<HttpResponseMessage> SendAuthorizedJsonAsync(HttpMethod method, string url, object payload, string accessToken) {
        JsonContent content = JsonContent.Create(payload);
        HttpRequestMessage request = new(method, url) {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request);
    }

    private static JsonElement ExtractCartItem(JsonElement body) {
        if (body.ValueKind == JsonValueKind.Object) {
            if (body.TryGetProperty("OrderItem", out JsonElement orderItem)) return orderItem;
            if (body.TryGetProperty("ClientShoppingCartItem", out JsonElement cartItem)) return cartItem;
            if (body.TryGetProperty("Id", out _)) return body;
            if (body.TryGetProperty("OrderItems", out JsonElement nestedItems) && nestedItems.ValueKind == JsonValueKind.Array) {
                return nestedItems.EnumerateArray().First();
            }
        }

        if (body.ValueKind == JsonValueKind.Array) return body.EnumerateArray().First();

        throw new InvalidOperationException($"Could not extract a cart item from API response body kind '{body.ValueKind}'.");
    }

    private static void AssertNumberProperty(JsonElement element, string propertyName) {
        Assert.True(element.TryGetProperty(propertyName, out JsonElement property), $"Missing '{propertyName}' property.");
        Assert.Equal(JsonValueKind.Number, property.ValueKind);
    }

    private static string? FindStringValue(JsonElement element, string propertyName) {
        if (element.ValueKind == JsonValueKind.Object) {
            foreach (JsonProperty property in element.EnumerateObject()) {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.String) {
                    return property.Value.GetString();
                }

                string? nested = FindStringValue(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }

        if (element.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement item in element.EnumerateArray()) {
                string? nested = FindStringValue(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }

        return null;
    }

    private sealed record LoginTokens(string AccessToken, string RefreshToken);
}
