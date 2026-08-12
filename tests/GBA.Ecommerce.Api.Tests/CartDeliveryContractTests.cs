using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class CartDeliveryContractTests(EcommerceApiFixture fixture) : IClassFixture<EcommerceApiFixture> {
    private readonly HttpClient _client = fixture.Client;
    private readonly ApiTestConfig _config = fixture.Config;

    [Theory]
    [InlineData("clients/shoppingcart/items/new?withVat=0", "POST")]
    [InlineData("clients/shoppingcart/items/new/with/verify?withVat=0", "POST")]
    [InlineData("clients/shoppingcart/items/new/many?withVat=0", "POST")]
    [InlineData("clients/shoppingcart/items/update?withVat=0", "POST")]
    [InlineData("clients/shoppingcart/items/update/many?withVat=0", "POST")]
    [InlineData("clients/shoppingcart/items/verify", "POST")]
    [InlineData("clients/shoppingcart/items/delete?netId=00000000-0000-0000-0000-000000000000&withVat=0", "DELETE")]
    [InlineData("clients/shoppingcart/items/delete/all?withVat=0", "DELETE")]
    public async Task Cart_write_and_verify_endpoints_require_bearer_token(string relativePath, string method) {
        using HttpRequestMessage request = new(new HttpMethod(method), _config.ApiPath(relativePath));

        if (method == "POST") {
            request.Content = JsonContent.Create(new { });
        }

        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Theory]
    [InlineData("deliveries/recipients/all/current")]
    [InlineData("deliveries/recipients/addresses/all/recipient?netId=00000000-0000-0000-0000-000000000000")]
    public async Task Delivery_recipient_endpoints_require_bearer_token(string relativePath) {
        using HttpResponseMessage response = await _client.GetAsync(_config.ApiPath(relativePath));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Authenticated_delivery_recipients_contract_matches_current_client_when_credentials_are_configured() {
        if (!_config.HasCredentials) return;

        LoginTokens tokens = await LoginAsync();
        using HttpRequestMessage request = AuthorizedRequest(HttpMethod.Get, _config.ApiPath("deliveries/recipients/all/current"), tokens.AccessToken);
        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);

        JsonElement[] recipients = envelope.Body.EnumerateArray().ToArray();
        if (recipients.Length == 0) return;

        Guid recipientNetUid = ApiAssertions.RequiredGuid(recipients[0], "NetUid");
        using HttpRequestMessage addressRequest = AuthorizedRequest(
            HttpMethod.Get,
            _config.ApiPath($"deliveries/recipients/addresses/all/recipient?netId={recipientNetUid}"),
            tokens.AccessToken);
        using HttpResponseMessage addressResponse = await _client.SendAsync(addressRequest);

        Assert.Equal(HttpStatusCode.OK, addressResponse.StatusCode);

        ApiEnvelope addressEnvelope = await ApiAssertions.ReadEnvelopeAsync(addressResponse);
        ApiAssertions.AssertSuccessEnvelope(addressEnvelope);
        Assert.Equal(JsonValueKind.Array, addressEnvelope.Body.ValueKind);
    }

    [Fact]
    public async Task Authenticated_cart_verify_checks_real_product_availability_when_credentials_are_configured() {
        if (!_config.HasCredentials) return;

        LoginTokens tokens = await LoginAsync();
        JsonElement product = await GetFirstAvailableProductAsync();
        int productId = ApiAssertions.RequiredInt32(product, "Id");
        Guid productNetUid = ApiAssertions.RequiredGuid(product, "NetUid");

        using HttpResponseMessage response = await SendAuthorizedJsonAsync(
            HttpMethod.Post,
            _config.ApiPath("clients/shoppingcart/items/verify"),
            new {
                Id = 0,
                Qty = 1,
                OverLordQty = 1,
                ProductId = productId,
                Product = new {
                    Id = productId,
                    NetUid = productNetUid
                }
            },
            tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        ApiAssertions.RequiredGuid(envelope.Body, "NetUid");
    }

    [Fact]
    public async Task Authenticated_cart_verify_rejects_quantity_above_available_without_cart_mutation_when_credentials_are_configured() {
        if (!_config.HasCredentials) return;

        LoginTokens tokens = await LoginAsync();
        JsonElement product = await GetFirstAvailableProductAsync();
        int productId = ApiAssertions.RequiredInt32(product, "Id");
        Guid productNetUid = ApiAssertions.RequiredGuid(product, "NetUid");

        using HttpResponseMessage response = await SendAuthorizedJsonAsync(
            HttpMethod.Post,
            _config.ApiPath("clients/shoppingcart/items/verify"),
            new {
                Id = 0,
                Qty = 1_000_000,
                OverLordQty = 1_000_000,
                ProductId = productId,
                Product = new {
                    Id = productId,
                    NetUid = productNetUid
                }
            },
            tokens.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
    }

    [Fact]
    public async Task Real_cart_add_with_verify_and_delete_flow_runs_only_with_explicit_write_flag() {
        if (!_config.HasCredentials || !_config.RunRealCartWrites) return;
        _config.EnsureSafeWriteTarget();

        LoginTokens tokens = await LoginAsync();
        JsonElement product = await GetFirstAvailableProductAsync();
        int productId = ApiAssertions.RequiredInt32(product, "Id");
        Guid productNetUid = ApiAssertions.RequiredGuid(product, "NetUid");

        using HttpResponseMessage addResponse = await SendAuthorizedJsonAsync(
            HttpMethod.Post,
            _config.ApiPath("clients/shoppingcart/items/new/with/verify?withVat=0"),
            new {
                Id = 0,
                Qty = 1,
                OverLordQty = 1,
                ProductId = productId,
                Product = new {
                    Id = productId,
                    NetUid = productNetUid
                }
            },
            tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        ApiEnvelope addEnvelope = await ApiAssertions.ReadEnvelopeAsync(addResponse);
        ApiAssertions.AssertSuccessEnvelope(addEnvelope);
        Assert.True(addEnvelope.Body.TryGetProperty("orderItem", out JsonElement orderItem));
        Assert.NotEqual(JsonValueKind.Null, orderItem.ValueKind);

        Guid orderItemNetUid = ApiAssertions.RequiredGuid(orderItem, "NetUid");

        using HttpRequestMessage deleteRequest = AuthorizedRequest(
            HttpMethod.Delete,
            _config.ApiPath($"clients/shoppingcart/items/delete?netId={orderItemNetUid}&withVat=0"),
            tokens.AccessToken);
        using HttpResponseMessage deleteResponse = await _client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        ApiEnvelope deleteEnvelope = await ApiAssertions.ReadEnvelopeAsync(deleteResponse);
        ApiAssertions.AssertSuccessEnvelope(deleteEnvelope);
        Assert.Equal(orderItemNetUid, deleteEnvelope.Body.GetGuid());
    }

    [Fact]
    public async Task Real_cart_add_many_update_many_delete_created_items_runs_only_with_explicit_cart_write_flag() {
        if (!_config.HasCredentials || !_config.RunRealCartWrites) return;
        _config.EnsureSafeWriteTarget();

        LoginTokens tokens = await LoginAsync();
        JsonElement[] currentCartItems = await GetCurrentCartItemsAsync(tokens.AccessToken);
        HashSet<int> existingProductIds = currentCartItems
            .Select(TryGetProductId)
            .Where(id => id > 0)
            .ToHashSet();

        JsonElement[] products = await GetAvailableProductsAsync(20);
        JsonElement[] candidateProducts = products
            .Where(product => TryGetProductId(product) > 0)
            .Where(product => !existingProductIds.Contains(TryGetProductId(product)))
            .Where(product => TryGetAvailableQtyUk(product) >= 2)
            .Take(2)
            .ToArray();

        if (candidateProducts.Length < 2) return;

        List<Guid> createdItemNetUids = new();

        try {
            object[] addPayload = candidateProducts
                .Select(product => {
                    int productId = ApiAssertions.RequiredInt32(product, "Id");
                    Guid productNetUid = ApiAssertions.RequiredGuid(product, "NetUid");

                    return new {
                        Id = 0,
                        Qty = 1,
                        OverLordQty = 1,
                        ProductId = productId,
                        Product = new {
                            Id = productId,
                            NetUid = productNetUid
                        }
                    };
                })
                .Cast<object>()
                .ToArray();

            using HttpResponseMessage addResponse = await SendAuthorizedJsonAsync(
                HttpMethod.Post,
                _config.ApiPath("clients/shoppingcart/items/new/many?withVat=0"),
                addPayload,
                tokens.AccessToken);

            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            ApiEnvelope addEnvelope = await ApiAssertions.ReadEnvelopeAsync(addResponse);
            ApiAssertions.AssertSuccessEnvelope(addEnvelope);
            Assert.Equal(JsonValueKind.Array, addEnvelope.Body.ValueKind);

            JsonElement[] createdItems = addEnvelope.Body.EnumerateArray()
                .Where(item => !existingProductIds.Contains(TryGetProductId(item)))
                .Where(item => TryGetNetUid(item) != Guid.Empty)
                .ToArray();

            Assert.True(createdItems.Length >= 2, "Expected add-many to return at least the two newly-created cart items.");
            createdItemNetUids.AddRange(createdItems.Select(item => ApiAssertions.RequiredGuid(item, "NetUid")));

            object[] updatePayload = createdItems
                .Select(item => new {
                    Id = ApiAssertions.RequiredInt32(item, "Id"),
                    NetUid = ApiAssertions.RequiredGuid(item, "NetUid"),
                    ProductId = ApiAssertions.RequiredInt32(item, "ProductId"),
                    Qty = 2,
                    OverLordQty = 2
                })
                .Cast<object>()
                .ToArray();

            using HttpResponseMessage updateResponse = await SendAuthorizedJsonAsync(
                HttpMethod.Post,
                _config.ApiPath("clients/shoppingcart/items/update/many?withVat=0"),
                updatePayload,
                tokens.AccessToken);

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            ApiEnvelope updateEnvelope = await ApiAssertions.ReadEnvelopeAsync(updateResponse);
            ApiAssertions.AssertSuccessEnvelope(updateEnvelope);
            Assert.Equal(JsonValueKind.Array, updateEnvelope.Body.ValueKind);

            JsonElement[] updatedItems = updateEnvelope.Body.EnumerateArray()
                .Where(item => createdItemNetUids.Contains(TryGetNetUid(item)))
                .ToArray();

            Assert.Equal(createdItemNetUids.Count, updatedItems.Length);
            Assert.All(updatedItems, item => Assert.True(TryGetNumber(item, "Qty") >= 1));
        } finally {
            foreach (Guid itemNetUid in createdItemNetUids.Distinct()) {
                using HttpRequestMessage deleteRequest = AuthorizedRequest(
                    HttpMethod.Delete,
                    _config.ApiPath($"clients/shoppingcart/items/delete?netId={itemNetUid}&withVat=0"),
                    tokens.AccessToken);

                using HttpResponseMessage deleteResponse = await _client.SendAsync(deleteRequest);
                Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
            }
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

    private async Task<JsonElement> GetFirstAvailableProductAsync() {
        using HttpResponseMessage response = await _client.GetAsync(
            _config.ApiPath($"products/search?value={Uri.EscapeDataString(_config.SearchQuery)}&limit=10&offset=0&withVat=0"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        JsonElement[] products = envelope.Body.EnumerateArray()
            .Where(product =>
                product.TryGetProperty("AvailableQtyUk", out JsonElement available)
                && available.ValueKind == JsonValueKind.Number
                && available.GetDouble() > 0)
            .ToArray();

        Assert.NotEmpty(products);

        return products[0];
    }

    private async Task<JsonElement[]> GetAvailableProductsAsync(int limit) {
        using HttpResponseMessage response = await _client.GetAsync(
            _config.ApiPath($"products/search?value={Uri.EscapeDataString(_config.SearchQuery)}&limit={limit}&offset=0&withVat=0"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        return envelope.Body.EnumerateArray()
            .Where(product => TryGetAvailableQtyUk(product) > 0)
            .ToArray();
    }

    private async Task<JsonElement[]> GetCurrentCartItemsAsync(string accessToken) {
        using HttpRequestMessage request = AuthorizedRequest(
            HttpMethod.Get,
            _config.ApiPath("clients/shoppingcart/items/all?withVat=0"),
            accessToken);
        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);

        return envelope.Body.EnumerateArray().ToArray();
    }

    private async Task<HttpResponseMessage> SendAuthorizedJsonAsync(HttpMethod method, string url, object payload, string accessToken) {
        HttpRequestMessage request = new(method, url) {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request);
    }

    private static HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, string accessToken) {
        HttpRequestMessage request = new(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
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

    private static int TryGetProductId(JsonElement item) {
        if (item.ValueKind != JsonValueKind.Object) return 0;

        int id;
        if (item.TryGetProperty("ProductId", out JsonElement productId)
            && productId.ValueKind == JsonValueKind.Number
            && productId.TryGetInt32(out id)) {
            return id;
        }

        if (item.TryGetProperty("Id", out JsonElement idElement)
            && idElement.ValueKind == JsonValueKind.Number
            && idElement.TryGetInt32(out id)
            && item.TryGetProperty("VendorCode", out _)) {
            return id;
        }

        if (item.TryGetProperty("Product", out JsonElement product)
            && product.ValueKind == JsonValueKind.Object
            && product.TryGetProperty("Id", out JsonElement nestedId)
            && nestedId.ValueKind == JsonValueKind.Number
            && nestedId.TryGetInt32(out id)) {
            return id;
        }

        return 0;
    }

    private static Guid TryGetNetUid(JsonElement item) {
        if (item.ValueKind != JsonValueKind.Object
            || !item.TryGetProperty("NetUid", out JsonElement netUid)
            || netUid.ValueKind != JsonValueKind.String
            || !Guid.TryParse(netUid.GetString(), out Guid parsed)) {
            return Guid.Empty;
        }

        return parsed;
    }

    private static double TryGetAvailableQtyUk(JsonElement product) {
        return TryGetNumber(product, "AvailableQtyUk");
    }

    private static double TryGetNumber(JsonElement item, string propertyName) {
        if (item.ValueKind != JsonValueKind.Object
            || !item.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind != JsonValueKind.Number) {
            return 0d;
        }

        return value.TryGetDouble(out double number) ? number : 0d;
    }

    private sealed record LoginTokens(string AccessToken, string RefreshToken);
}
