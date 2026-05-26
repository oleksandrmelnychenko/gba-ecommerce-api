using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class AuthAndBoundaryContractTests(EcommerceApiFixture fixture) : IClassFixture<EcommerceApiFixture> {
    private readonly HttpClient _client = fixture.Client;
    private readonly ApiTestConfig _config = fixture.Config;

    [Fact]
    public async Task Login_validation_rejects_missing_credentials() {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            _config.ApiPath("usermanagement/token"),
            new { username = string.Empty, password = string.Empty });

        if (await AssertRateLimitEnvelopeAndReturnAsync(response)) return;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Equal("Username and password are required", envelope.Message);
    }

    [Fact]
    public async Task Refresh_token_validation_rejects_missing_token() {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            _config.ApiPath("usermanagement/token/refresh"),
            new { token = string.Empty });

        if (await AssertRateLimitEnvelopeAndReturnAsync(response)) return;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Equal("Refresh token is required", envelope.Message);
    }

    [Fact]
    public async Task Email_validation_rejects_malformed_email() {
        using HttpResponseMessage response = await _client.GetAsync(
            _config.ApiPath("usermanagement/check/email?email=not-an-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Equal("Email is not valid", envelope.Message);
    }

    [Theory]
    [InlineData("clients/shoppingcart/items/all?withVat=0")]
    [InlineData("deliveries/recipients/all/current")]
    [InlineData("products/all/ordered?from=2026-01-01&to=2026-01-31&limit=10&offset=0")]
    [InlineData("orders/offer/all/available")]
    public async Task Customer_endpoints_require_bearer_token(string relativePath) {
        using HttpResponseMessage response = await _client.GetAsync(_config.ApiPath(relativePath));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Theory]
    [InlineData("elasticsearch/index/delete", "DELETE")]
    [InlineData("elasticsearch/index/create", "POST")]
    [InlineData("elasticsearch/sync/full", "POST")]
    [InlineData("elasticsearch/search/debug?query=oil&limit=1&offset=0", "GET")]
    public async Task Elasticsearch_admin_endpoints_require_admin_token(string relativePath, string method) {
        using HttpRequestMessage request = new(new HttpMethod(method), _config.ApiPath(relativePath));
        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    private static async Task<bool> AssertRateLimitEnvelopeAndReturnAsync(HttpResponseMessage response) {
        if (response.StatusCode != HttpStatusCode.TooManyRequests) return false;

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(429, envelope.StatusCode);
        Assert.Contains("Too many requests", envelope.Message, System.StringComparison.OrdinalIgnoreCase);
        return true;
    }
}
