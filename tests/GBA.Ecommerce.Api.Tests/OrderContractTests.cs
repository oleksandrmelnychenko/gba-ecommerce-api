using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class OrderContractTests(EcommerceApiFixture fixture) : IClassFixture<EcommerceApiFixture> {
    private readonly HttpClient _client = fixture.Client;
    private readonly ApiTestConfig _config = fixture.Config;

    [Fact]
    public async Task Order_calculate_never_prices_an_order_from_the_request_body() {
        // /orders/calculate is anonymous. It re-resolves every line from the database, so a body
        // that names no real product cannot be priced at all - and the prices it carries must
        // never reach the totals.
        var payload = new {
            OrderItems = new[] {
                new {
                    Qty = 2,
                    OverLordQty = 3,
                    Product = new {
                        NetUid = Guid.Empty,
                        CurrentPrice = 10.25m,
                        CurrentLocalPrice = 410.50m
                    }
                }
            }
        };

        using HttpResponseMessage response = await _client.PostAsJsonAsync(_config.ApiPath("orders/calculate"), payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("20.50", body, StringComparison.Ordinal);
        Assert.DoesNotContain("921.25", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Order_calculate_rejects_an_empty_order() {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            _config.ApiPath("orders/calculate"),
            new { OrderItems = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Offer_calculate_requires_bearer_token_for_order_payload() {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            _config.ApiPath("orders/calculate/offer"),
            new {
                OrderItems = new[] {
                    new {
                        Qty = 3,
                        OverLordQty = 4,
                        Product = new {
                            CurrentPrice = 7.50m,
                            CurrentLocalPrice = 300m
                        }
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(
            "Bearer",
            response.Headers.WwwAuthenticate.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Offer_calculate_rejects_unauthenticated_pricing_requests() {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            _config.ApiPath("orders/calculate/offer"),
            new {
                OrderItems = new[] {
                    new {
                        Qty = 99,
                        ChangedQty = 3,
                        Product = new {
                            CurrentPrice = 7.50m,
                            CurrentLocalPrice = 300.005m
                        }
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(
            "Bearer",
            response.Headers.WwwAuthenticate.ToString(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("orders/new?withVat=0", "GET")]
    [InlineData("orders/new/invoice?withVat=false", "POST")]
    [InlineData("orders/new/offer?addCartItems=0", "POST")]
    [InlineData("orders/offer/all/available", "GET")]
    public async Task Protected_order_write_endpoints_require_bearer_token(string relativePath, string method) {
        using HttpRequestMessage request = new(new HttpMethod(method), _config.ApiPath(relativePath));
        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Authenticated_invoice_rejects_missing_invoice_without_creating_sale_when_credentials_are_configured() {
        if (!_config.HasCredentials) return;

        LoginTokens tokens = await LoginAsync();
        using MultipartFormDataContent form = new();
        using HttpRequestMessage request = new(HttpMethod.Post, _config.ApiPath("orders/new/invoice?withVat=false")) {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Equal("ShoppingCart entity can not be empty", envelope.Message);
    }

    [Fact]
    public async Task Authenticated_invoice_rejects_malformed_invoice_without_creating_sale_when_credentials_are_configured() {
        if (!_config.HasCredentials) return;

        LoginTokens tokens = await LoginAsync();
        using MultipartFormDataContent form = new() {
            { new StringContent("{bad-json"), "invoice" }
        };
        using HttpRequestMessage request = new(HttpMethod.Post, _config.ApiPath("orders/new/invoice?withVat=false")) {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Anonymous_quick_invoice_with_random_client_rejects_empty_sale_without_creating_order() {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            _config.ApiPath($"orders/new/quick/invoice?clientNetId={Guid.NewGuid()}&fullPayment=0"),
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
    }

    [Fact]
    public async Task Real_retail_quick_invoice_flow_runs_only_with_explicit_order_write_flag() {
        if (!_config.RunRealOrderWrites) return;
        _config.EnsureSafeWriteTarget();

        JsonElement region = await GetFirstArrayItemAsync("regions/ecommerce/all");
        JsonElement product = await GetFirstAvailableProductAsync();
        JsonElement transporterType = await GetFirstArrayItemAsync("transporters/types/all");
        Guid transporterTypeNetUid = ApiAssertions.RequiredGuid(transporterType, "NetUid");
        JsonElement transporter = await GetFirstArrayItemAsync($"transporters/all/type?netId={transporterTypeNetUid}");

        string testId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        using HttpResponseMessage clientResponse = await _client.PostAsJsonAsync(
            _config.ApiPath("retail/clients/new"),
            new {
                Name = $"E2E DEV Order {testId}",
                PhoneNumber = $"+38067{testId[^7..]}",
                EcommerceRegion = region,
                ShoppingCartJson = "[]"
            });

        Assert.Equal(HttpStatusCode.OK, clientResponse.StatusCode);

        ApiEnvelope clientEnvelope = await ApiAssertions.ReadEnvelopeAsync(clientResponse);
        ApiAssertions.AssertSuccessEnvelope(clientEnvelope);
        Guid retailClientNetUid = ApiAssertions.RequiredGuid(clientEnvelope.Body, "NetUid");

        int productId = ApiAssertions.RequiredInt32(product, "Id");
        Guid productNetUid = ApiAssertions.RequiredGuid(product, "NetUid");
        int transporterId = ApiAssertions.RequiredInt32(transporter, "Id");
        Guid transporterNetUid = ApiAssertions.RequiredGuid(transporter, "NetUid");

        var salePayload = new {
            Comment = $"E2E DEV quick invoice {testId}",
            ShipmentDate = DateTime.UtcNow.Date.AddDays(1).ToString("O"),
            Transporter = new {
                Id = transporterId,
                NetUid = transporterNetUid
            },
            DeliveryRecipient = new {
                FullName = $"E2E DEV Recipient {testId}",
                MobilePhone = $"+38067{testId[^7..]}"
            },
            DeliveryRecipientAddress = new {
                Value = "E2E DEV address",
                Department = "E2E DEV department",
                City = "Kyiv"
            },
            Order = new {
                OrderItems = new[] {
                    new {
                        Id = 0,
                        Qty = 1,
                        OverLordQty = 1,
                        ProductId = productId,
                        Product = new {
                            Id = productId,
                            NetUid = productNetUid
                        }
                    }
                }
            }
        };

        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            _config.ApiPath($"orders/new/quick/invoice?clientNetId={retailClientNetUid}&fullPayment=0"),
            salePayload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.String, envelope.Body.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Body.GetString()));
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

    private async Task<JsonElement> GetFirstArrayItemAsync(string relativePath) {
        using HttpResponseMessage response = await _client.GetAsync(_config.ApiPath(relativePath));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);

        JsonElement[] items = envelope.Body.EnumerateArray().ToArray();
        Assert.NotEmpty(items);

        return items[0];
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
