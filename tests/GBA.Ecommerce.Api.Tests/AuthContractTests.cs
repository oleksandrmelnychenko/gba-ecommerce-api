using System;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace GBA.Ecommerce.Api.Tests;

public sealed class AuthContractTests(EcommerceApiFixture fixture) : IClassFixture<EcommerceApiFixture> {
    private static readonly SemaphoreSlim AuthRateLimitLock = new(1, 1);
    private static DateTimeOffset _lastAuthRequestAt = DateTimeOffset.MinValue;

    private readonly HttpClient _client = fixture.Client;
    private readonly ApiTestConfig _config = fixture.Config;

    [Theory]
    [InlineData("{")]
    [InlineData("")]
    public async Task Token_endpoint_rejects_malformed_json_or_empty_body(string body) {
        await ThrottleAuthEndpointAsync();

        using HttpRequestMessage request = new(HttpMethod.Post, _config.ApiPath("usermanagement/token")) {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("")]
    public async Task Signup_endpoint_rejects_malformed_json_or_empty_body_without_creating_client(string body) {
        await ThrottleAuthEndpointAsync();

        using HttpRequestMessage request = new(HttpMethod.Post, _config.ApiPath("usermanagement/signup")) {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response = await _client.SendAsync(request);

        if (await AssertRateLimitEnvelopeAndReturnAsync(response)) return;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Equal("Client payload is required", envelope.Message);
        AssertNoTokenValues(envelope.Body);
    }

    [Fact]
    public async Task Invalid_login_returns_400_envelope_without_tokens() {
        using HttpResponseMessage response = await SendJsonAsync(
            _config.ApiPath("usermanagement/token"),
            new {
                username = $"missing-user-{Guid.NewGuid():N}@example.invalid",
                password = $"wrong-{Guid.NewGuid():N}"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
        AssertNoTokenValues(envelope.Body);
    }

    [Fact]
    public async Task Refresh_with_invalid_random_token_returns_400_envelope_without_tokens() {
        using HttpResponseMessage response = await SendJsonAsync(
            _config.ApiPath("usermanagement/token/refresh"),
            new { token = $"invalid-refresh-{Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
        AssertNoTokenValues(envelope.Body);
    }

    [Fact]
    public async Task Signup_missing_client_returns_400_client_payload_required() {
        using HttpResponseMessage response = await SendJsonAsync(
            _config.ApiPath("usermanagement/signup"),
            new {
                client = (object?)null,
                password = $"Password-{Guid.NewGuid():N}",
                login = $"signup-{Guid.NewGuid():N}@example.invalid"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);
        Assert.Equal("Client payload is required", envelope.Message);
    }

    [Fact]
    public async Task Signup_missing_password_with_minimal_client_returns_400_envelope() {
        using HttpResponseMessage response = await SendJsonAsync(
            _config.ApiPath("usermanagement/signup"),
            new {
                client = new {
                    emailAddress = $"signup-{Guid.NewGuid():N}@example.invalid"
                },
                login = $"signup-{Guid.NewGuid():N}@example.invalid"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(400, envelope.StatusCode);

        if (!string.IsNullOrWhiteSpace(envelope.Message)) {
            Assert.Equal("Password is required", envelope.Message);
        }
    }

    [Fact]
    public async Task Check_email_with_valid_random_email_returns_200_boolean_envelope() {
        string email = $"contract-{Guid.NewGuid():N}@example.invalid";

        using HttpResponseMessage response = await _client.GetAsync(
            _config.ApiPath($"usermanagement/check/email?email={Uri.EscapeDataString(email)}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        AssertEmailAvailabilityBody(envelope.Body);
    }

    [Fact]
    public async Task Login_with_real_credentials_returns_tokens_when_configured() {
        if (!_config.HasCredentials) return;

        using HttpResponseMessage response = await SendJsonAsync(
            _config.ApiPath("usermanagement/token"),
            new {
                username = _config.Username,
                password = _config.Password
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        Assert.False(string.IsNullOrWhiteSpace(FindStringValue(envelope.Body, "AccessToken")
            ?? FindStringValue(envelope.Body, "accessToken")
            ?? FindStringValue(envelope.Body, "Token")
            ?? FindStringValue(envelope.Body, "token")));
        Assert.False(string.IsNullOrWhiteSpace(FindStringValue(envelope.Body, "RefreshToken")
            ?? FindStringValue(envelope.Body, "refreshToken")));
    }

    [Fact]
    public async Task Authenticated_refresh_with_real_refresh_token_returns_new_tokens_only_with_explicit_auth_flag() {
        if (!_config.HasCredentials || !_config.RunRealAuthLifecycleWrites) return;
        _config.EnsureSafeWriteTarget();

        LoginTokens tokens = await LoginAsync();

        using HttpResponseMessage response = await SendJsonAsync(
            _config.ApiPath("usermanagement/token/refresh"),
            new { token = tokens.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        LoginTokens refreshedTokens = ExtractLoginTokens(envelope.Body);
        Assert.NotEqual(tokens.RefreshToken, refreshedTokens.RefreshToken);
    }

    [Fact]
    public async Task Real_registration_returns_tokens_and_marks_email_unavailable_only_with_explicit_registration_flag() {
        if (!_config.RunRealRegistrationWrites) return;
        _config.EnsureSafeWriteTarget();

        string unique = Guid.NewGuid().ToString("N");
        string email = $"e2e-dev-{unique}@example.invalid";
        string phone = $"+38067{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000000:0000000}";
        string password = $"Password-{unique[..12]}!1";

        ApiEnvelope beforeEnvelope = await CheckEmailAsync(email);
        Assert.True(IsEmailAvailable(beforeEnvelope.Body), "Disposable registration email should be available before signup.");

        using HttpResponseMessage signupResponse = await SendJsonAsync(
            _config.ApiPath("usermanagement/signup"),
            new {
                client = new {
                    emailAddress = email,
                    mobileNumber = phone,
                    firstName = "E2E",
                    lastName = "Registration",
                    name = $"E2E Registration {unique[..8]}",
                    fullName = $"E2E Registration {unique[..8]}",
                    isIndividual = true,
                    isActive = true
                },
                password,
                login = email,
                isLocalPayment = 0
            });

        Assert.Equal(HttpStatusCode.OK, signupResponse.StatusCode);

        ApiEnvelope signupEnvelope = await ApiAssertions.ReadEnvelopeAsync(signupResponse);
        ApiAssertions.AssertSuccessEnvelope(signupEnvelope);
        ExtractLoginTokens(signupEnvelope.Body);
        ApiAssertions.RequiredGuid(signupEnvelope.Body, "UserNetUid");

        ApiEnvelope afterEnvelope = await CheckEmailAsync(email);
        Assert.False(IsEmailAvailable(afterEnvelope.Body), "Registered email should become unavailable after signup.");
    }

    private static void AssertNoTokenValues(JsonElement element) {
        Assert.True(string.IsNullOrWhiteSpace(FindStringValue(element, "AccessToken")));
        Assert.True(string.IsNullOrWhiteSpace(FindStringValue(element, "accessToken")));
        Assert.True(string.IsNullOrWhiteSpace(FindStringValue(element, "RefreshToken")));
        Assert.True(string.IsNullOrWhiteSpace(FindStringValue(element, "refreshToken")));
        Assert.True(string.IsNullOrWhiteSpace(FindStringValue(element, "Token")));
        Assert.True(string.IsNullOrWhiteSpace(FindStringValue(element, "token")));
    }

    private async Task<HttpResponseMessage> SendJsonAsync(string url, object payload) {
        await ThrottleAuthEndpointAsync();

        JsonContent content = JsonContent.Create(payload);
        HttpRequestMessage request = new(HttpMethod.Post, url) {
            Content = content
        };

        return await _client.SendAsync(request);
    }

    private async Task<LoginTokens> LoginAsync() {
        using HttpResponseMessage response = await SendJsonAsync(
            _config.ApiPath("usermanagement/token"),
            new {
                username = _config.Username,
                password = _config.Password
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        return ExtractLoginTokens(envelope.Body);
    }

    private async Task<ApiEnvelope> CheckEmailAsync(string email) {
        using HttpResponseMessage response = await _client.GetAsync(
            _config.ApiPath($"usermanagement/check/email?email={Uri.EscapeDataString(email)}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);

        return envelope;
    }

    private static async Task<bool> AssertRateLimitEnvelopeAndReturnAsync(HttpResponseMessage response) {
        if (response.StatusCode != HttpStatusCode.TooManyRequests) return false;

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        Assert.Equal(429, envelope.StatusCode);
        Assert.Contains("Too many requests", envelope.Message, StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private static async Task ThrottleAuthEndpointAsync() {
        await AuthRateLimitLock.WaitAsync();
        try {
            TimeSpan elapsed = DateTimeOffset.UtcNow - _lastAuthRequestAt;
            TimeSpan delay = TimeSpan.FromSeconds(15) - elapsed;
            if (delay > TimeSpan.Zero) await Task.Delay(delay);

            _lastAuthRequestAt = DateTimeOffset.UtcNow;
        } finally {
            AuthRateLimitLock.Release();
        }
    }

    private static void AssertEmailAvailabilityBody(JsonElement body) {
        if (body.ValueKind == JsonValueKind.Object
            && body.TryGetProperty("Succeeded", out JsonElement succeeded)) {
            Assert.True(succeeded.ValueKind is JsonValueKind.True or JsonValueKind.False);

            if (body.TryGetProperty("Errors", out JsonElement errors)) {
                Assert.Equal(JsonValueKind.Array, errors.ValueKind);
            }

            return;
        }

        Assert.True(ContainsBooleanValue(body), $"Expected boolean or IdentityResponse body, got '{body.ValueKind}'.");
    }

    private static bool ContainsBooleanValue(JsonElement element) {
        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False) return true;

        if (element.ValueKind == JsonValueKind.Object) {
            foreach (JsonProperty property in element.EnumerateObject()) {
                if (ContainsBooleanValue(property.Value)) return true;
            }
        }

        if (element.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement item in element.EnumerateArray()) {
                if (ContainsBooleanValue(item)) return true;
            }
        }

        return false;
    }

    private static LoginTokens ExtractLoginTokens(JsonElement body) {
        string? accessToken = FindStringValue(body, "AccessToken")
            ?? FindStringValue(body, "accessToken")
            ?? FindStringValue(body, "Token")
            ?? FindStringValue(body, "token");

        string? refreshToken = FindStringValue(body, "RefreshToken")
            ?? FindStringValue(body, "refreshToken");

        Assert.False(string.IsNullOrWhiteSpace(accessToken), "Login response did not include an access token.");
        Assert.False(string.IsNullOrWhiteSpace(refreshToken), "Login response did not include a refresh token.");

        return new LoginTokens(accessToken!, refreshToken!);
    }

    private static bool IsEmailAvailable(JsonElement body) {
        Assert.Equal(JsonValueKind.Object, body.ValueKind);
        Assert.True(body.TryGetProperty("Succeeded", out JsonElement succeeded), "Email availability response should include Succeeded.");
        Assert.True(succeeded.ValueKind is JsonValueKind.True or JsonValueKind.False);

        return succeeded.GetBoolean();
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
