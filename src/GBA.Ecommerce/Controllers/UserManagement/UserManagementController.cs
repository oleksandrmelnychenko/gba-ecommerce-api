using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration.Entities;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.UserManagement.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GBA.Ecommerce.Controllers.UserManagement;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.UserManagement)]
public sealed class UserManagementController(
    IResponseFactory responseFactory,
    ISignUpService signUpService,
    IRequestTokenService requestTokenService,
    IEmailAvailabilityService emailAvailabilityService,
    IEmailValidationService emailValidationService,
    IClientRegistrationTaskService clientRegistrationTaskService)
    : WebApiControllerBase(responseFactory) {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.SIGN_UP)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SignUp(
        [FromQuery] string password = "",
        [FromQuery] string login = "",
        [FromQuery] int? isLocalPayment = null) {
        SignUpRequest? request = await ReadSignUpRequestAsync(password, login, isLocalPayment);

        if (request?.Client == null) {
            return BadRequest(ErrorResponseBody("Client payload is required", HttpStatusCode.BadRequest));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ErrorResponseBody("Password is required", HttpStatusCode.BadRequest));

        string password = request.Password;

        Tuple<IdentityResponse, Client> identityResponse = await signUpService.SignUp(
            request.Client,
            password,
            request.Login,
            request.IsLocalPayment.Equals(1)
        );

        if (identityResponse.Item1.Succeeded) {
            await clientRegistrationTaskService.Add(identityResponse.Item2);

            Tuple<bool, string, CompleteAccessToken> result = await requestTokenService.RequestToken(request.Client.EmailAddress, password);

            return Ok(SuccessResponseBody(result.Item3));
        }

        return BadRequest(ErrorResponseBody(identityResponse.Item1.Errors.FirstOrDefault()?.Description, HttpStatusCode.BadRequest));
    }

    [HttpGet]
    [AssignActionRoute(UserManagementSegments.GET_TOKEN)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> GetTokenAsync([FromQuery] string username, [FromQuery] string password) {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return BadRequest(ErrorResponseBody("Username and password are required", HttpStatusCode.BadRequest));

        return await RequestTokenAsync(username, password);
    }

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.GET_TOKEN)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> GetTokenPostAsync([FromBody] LoginRequest request) {
        if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request?.Password))
            return BadRequest(ErrorResponseBody("Username and password are required", HttpStatusCode.BadRequest));

        return await RequestTokenAsync(request.Username, request.Password);
    }

    public sealed class LoginRequest {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public sealed class SignUpRequest {
        public Client? Client { get; set; } = new();
        public string Password { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public int IsLocalPayment { get; set; }
    }

    [HttpGet]
    [AssignActionRoute(UserManagementSegments.REFRESH_TOKEN)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RefreshTokenAsync([FromQuery] string token) {
        if (string.IsNullOrEmpty(token))
            return BadRequest(ErrorResponseBody("Refresh token is required", HttpStatusCode.BadRequest));

        return await RefreshTokenCoreAsync(token);
    }

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.REFRESH_TOKEN)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request) {
        if (string.IsNullOrEmpty(request?.Token))
            return BadRequest(ErrorResponseBody("Refresh token is required", HttpStatusCode.BadRequest));

        return await RefreshTokenCoreAsync(request.Token);
    }

    public sealed class RefreshTokenRequest {
        public string Token { get; set; } = string.Empty;
    }

    [HttpGet]
    [AssignActionRoute(UserManagementSegments.IS_EMAIL_AVAILABLE)]
    public async Task<IActionResult> CheckIsEmailAvaliable([FromQuery] string email) {
        bool isEmailValid = emailValidationService.IsEmailValid(email);

        if (!isEmailValid) return BadRequest(ErrorResponseBody("Email is not valid", HttpStatusCode.BadRequest));

        return Ok(SuccessResponseBody(await emailAvailabilityService.IsEmailAvailableAsync(email)));
    }

    private async Task<SignUpRequest?> ReadSignUpRequestAsync(string queryPassword, string queryLogin, int? queryIsLocalPayment) {
        string json;
        using (StreamReader reader = new(Request.Body)) {
            json = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(json)) return null;

        try {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            bool isWrappedRequest = root.TryGetProperty("Client", out _)
                || root.TryGetProperty("client", out _);

            if (isWrappedRequest) {
                return root.Deserialize<SignUpRequest>(JsonOptions);
            }

            Client? legacyClient = root.Deserialize<Client>(JsonOptions);
            return new SignUpRequest {
                Client = legacyClient,
                Password = queryPassword ?? string.Empty,
                Login = queryLogin ?? string.Empty,
                IsLocalPayment = queryIsLocalPayment ?? 0
            };
        } catch (JsonException) {
            return null;
        }
    }

    private async Task<IActionResult> RequestTokenAsync(string username, string password) {
        Tuple<bool, string, CompleteAccessToken> result = await requestTokenService.RequestToken(username, password);

        if (!result.Item1) return BadRequest(ErrorResponseBody(result.Item2, HttpStatusCode.BadRequest));

        return Ok(SuccessResponseBody(result.Item3));
    }

    private async Task<IActionResult> RefreshTokenCoreAsync(string token) {
        Tuple<bool, string, CompleteAccessToken> result = await requestTokenService.RefreshToken(token);

        if (!result.Item1) return BadRequest(ErrorResponseBody(result.Item2, HttpStatusCode.BadRequest));

        return Ok(SuccessResponseBody(result.Item3));
    }
}
