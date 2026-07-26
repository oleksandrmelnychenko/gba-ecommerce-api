using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GBA.Common.Configuration;
using GBA.Common.Exceptions.GlobalHandler;
using GBA.Common.ResponseBuilder;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Ecommerce.Controllers;
using GBA.Ecommerce.Controllers.UserManagement;
using GBA.Services.Services.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GBA.Ecommerce.Api.Tests;

public sealed class SecurityRegressionTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    public void Internal_gba_client_fails_closed_without_a_strong_api_key(string? apiKey) {
        Assert.Throws<InvalidOperationException>(() =>
            EcommerceInternalHttpClientDefaults.GetValidatedApiKey(apiKey));
    }

    [Fact]
    public void Internal_gba_client_trims_and_accepts_a_32_character_api_key() {
        string apiKey = new('a', EcommerceInternalHttpClientDefaults.MinimumApiKeyLength);

        Assert.Equal(
            apiKey,
            EcommerceInternalHttpClientDefaults.GetValidatedApiKey($"  {apiKey}  "));
    }

    [Fact]
    public void Internal_gba_client_sends_only_the_dedicated_service_header() {
        string apiKey = new('b', EcommerceInternalHttpClientDefaults.MinimumApiKeyLength);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                [$"{EcommerceInternalHttpClientDefaults.SectionName}:ApiKey"] = apiKey
            })
            .Build();
        ServiceCollection services = new();
        services.AddEcommerceInternalHttpClient(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        using HttpClient client = factory.CreateClient(
            EcommerceInternalHttpClientDefaults.ClientName);

        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
        Assert.Equal(
            apiKey,
            Assert.Single(client.DefaultRequestHeaders.GetValues(
                EcommerceInternalHttpClientDefaults.HeaderName)));
    }

    [Theory]
    [InlineData(UserManagementSegments.SIGN_UP)]
    [InlineData(UserManagementSegments.GET_TOKEN)]
    [InlineData(UserManagementSegments.REFRESH_TOKEN)]
    public void Credential_endpoints_are_post_only(string route) {
        MethodInfo[] actions = typeof(UserManagementController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.GetCustomAttributes<AssignActionRouteAttribute>()
                .Any(attribute => attribute.Template == route))
            .ToArray();

        MethodInfo action = Assert.Single(actions);
        Assert.NotNull(action.GetCustomAttribute<HttpPostAttribute>());
        Assert.Null(action.GetCustomAttribute<HttpGetAttribute>());
    }

    [Fact]
    public void Signup_credentials_are_bound_from_the_json_body() {
        MethodInfo action = typeof(UserManagementController)
            .GetMethod(nameof(UserManagementController.SignUp))!;
        ParameterInfo request = Assert.Single(action.GetParameters());

        Assert.NotNull(request.GetCustomAttribute<FromBodyAttribute>());
        Assert.Null(request.GetCustomAttribute<FromQueryAttribute>());
    }

    [Fact]
    public async Task Unexpected_exceptions_never_return_stack_traces() {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        Exception exception = new InvalidOperationException("database-internal-message");
        ExceptionHandlerFeature feature = new() { Error = exception };

        GlobalExceptionHandler handler = new();
        await handler.HandleException(context, feature);

        context.Response.Body.Position = 0;
        using JsonDocument response =
            await JsonDocument.ParseAsync(context.Response.Body);
        JsonElement root = response.RootElement;

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("An unexpected error occurred.", root.GetProperty(nameof(ErrorResponse.Message)).GetString());
        Assert.DoesNotContain("database-internal-message", root.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), root.GetRawText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(ArgumentException), HttpStatusCode.BadRequest, "The request is invalid.")]
    [InlineData(typeof(UnauthorizedAccessException), HttpStatusCode.Forbidden, "Access is forbidden.")]
    public async Task Expected_security_exceptions_return_sanitized_statuses(
        Type exceptionType,
        HttpStatusCode expectedStatus,
        string expectedMessage) {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        Exception exception = (Exception)Activator.CreateInstance(exceptionType, "sensitive-internal-message")!;
        ExceptionHandlerFeature feature = new() { Error = exception };

        GlobalExceptionHandler handler = new();
        await handler.HandleException(context, feature);

        context.Response.Body.Position = 0;
        using JsonDocument response = await JsonDocument.ParseAsync(context.Response.Body);
        JsonElement root = response.RootElement;

        Assert.Equal((int)expectedStatus, context.Response.StatusCode);
        Assert.Equal(expectedMessage, root.GetProperty(nameof(ErrorResponse.Message)).GetString());
        Assert.DoesNotContain("sensitive-internal-message", root.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Signup_dto_cannot_mass_assign_server_controlled_client_flags() {
        string[] propertyNames = typeof(UserManagementController.SignUpClientRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("IsForRetail", propertyNames);
        Assert.DoesNotContain("IsActive", propertyNames);
        Assert.DoesNotContain("IsBlocked", propertyNames);
        Assert.DoesNotContain("IsSubClient", propertyNames);
        Assert.DoesNotContain("IsTradePoint", propertyNames);
        Assert.DoesNotContain("ClientAgreements", propertyNames);
    }

    [Theory]
    [InlineData(nameof(OrdersController.GetOfferByNetIdAsync))]
    [InlineData(nameof(OrdersController.CalculateTotalsForOrderAsOfferAsync))]
    public void Customer_offer_endpoints_require_authentication(string actionName) {
        MethodInfo action = typeof(OrdersController).GetMethod(actionName)!;

        Assert.NotNull(typeof(OrdersController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Preorders_require_authenticated_customer_role() {
        AuthorizeAttribute authorize =
            Assert.IsType<AuthorizeAttribute>(typeof(PreOrderController).GetCustomAttribute<AuthorizeAttribute>());

        Assert.Contains("ClientUa", authorize.Roles, StringComparison.Ordinal);
        Assert.Contains("Workplace", authorize.Roles, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("image/png", true, new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0 })]
    [InlineData("image/png", false, new byte[] { 0x89, 0x50, 0x4e, 0x46, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0 })]
    [InlineData("text/html", false, new byte[] { 0x3c, 0x68, 0x74, 0x6d, 0x6c, 0, 0, 0, 0, 0, 0, 0 })]
    public void Payment_upload_uses_file_signatures(
        string contentType,
        bool expected,
        byte[] bytes) {
        MethodInfo validator = typeof(OrdersController).GetMethod(
            "HasValidPaymentImageSignature",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        bool result = (bool)validator.Invoke(null, new object[] { bytes, contentType })!;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Refresh_tokens_are_matched_only_against_database_hashes() {
        const string token = "opaque-refresh-token";
        string storedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        MethodInfo matcher = typeof(RequestTokenService).GetMethod(
            "IsRefreshTokenMatch",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.True((bool)matcher.Invoke(null, new object[] { storedHash, token })!);
        Assert.False((bool)matcher.Invoke(null, new object[] { token, token })!);
    }
}
