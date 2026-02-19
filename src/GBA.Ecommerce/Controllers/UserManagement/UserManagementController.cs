using System;
using System.Linq;
using System.Net;
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

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.SIGN_UP)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request) {
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

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.GET_TOKEN)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> GetTokenPostAsync([FromBody] LoginRequest request) {
        if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request?.Password))
            return BadRequest(ErrorResponseBody("Username and password are required", HttpStatusCode.BadRequest));

        Tuple<bool, string, CompleteAccessToken> result = await requestTokenService.RequestToken(request.Username, request.Password);

        if (!result.Item1) return BadRequest(ErrorResponseBody(result.Item2, HttpStatusCode.BadRequest));

        return Ok(SuccessResponseBody(result.Item3));
    }

    public sealed class LoginRequest {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public sealed class SignUpRequest {
        public Client Client { get; set; } = new();
        public string Password { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public int IsLocalPayment { get; set; }
    }

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.REFRESH_TOKEN)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request) {
        if (string.IsNullOrEmpty(request?.Token))
            return BadRequest(ErrorResponseBody("Refresh token is required", HttpStatusCode.BadRequest));

        Tuple<bool, string, CompleteAccessToken> result = await requestTokenService.RefreshToken(request.Token);

        if (!result.Item1) return BadRequest(ErrorResponseBody(result.Item2, HttpStatusCode.BadRequest));

        return Ok(SuccessResponseBody(result.Item3));
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
}