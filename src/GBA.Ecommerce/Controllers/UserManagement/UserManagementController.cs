using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration;
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
using NLog;

namespace GBA.Ecommerce.Controllers.UserManagement;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.UserManagement)]
public sealed class UserManagementController(
    IResponseFactory responseFactory,
    ISignUpService signUpService,
    IRequestTokenService requestTokenService,
    IEmailAvailabilityService emailAvailabilityService,
    IEmailValidationService emailValidationService,
    IClientRegistrationTaskService clientRegistrationTaskService,
    IClientAgreementService clientAgreementService)
    : WebApiControllerBase(responseFactory) {
    private readonly IClientAgreementService _clientAgreementService = clientAgreementService;

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.SIGN_UP)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SignUp([FromBody] Client client, [FromQuery] string password, [FromQuery] string login, [FromQuery] int isLocalPayment) {
        try {
            if (string.IsNullOrEmpty(password)) {
                Random random = new();

                password = new string(Enumerable.Repeat(AuthOptions.DEFAULT_PASSWORD_CHARS, 12)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                //ToDo: send password to email after success registration.
            }

            Tuple<IdentityResponse, Client> identityResponse = await signUpService.SignUp(client, password, login, isLocalPayment.Equals(1));

            if (identityResponse.Item1.Succeeded) {
                await clientRegistrationTaskService.Add(identityResponse.Item2);

                Tuple<bool, string, CompleteAccessToken> result = await requestTokenService.RequestToken(client.EmailAddress, password);

                return Ok(SuccessResponseBody(result.Item3));
            }

            return BadRequest(ErrorResponseBody(identityResponse.Item1.Errors.FirstOrDefault()?.Description, HttpStatusCode.BadRequest));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(UserManagementSegments.GET_TOKEN)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> GetTokenAsync([FromQuery] string username, [FromQuery] string password) {
        try {
            Tuple<bool, string, CompleteAccessToken> result = await requestTokenService.RequestToken(username, password);

            if (!result.Item1) return BadRequest(ErrorResponseBody(result.Item2, HttpStatusCode.BadRequest));

            return Ok(SuccessResponseBody(result.Item3));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.GET_TOKEN)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> GetTokenPostAsync([FromBody] LoginRequest request) {
        try {
            if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request?.Password))
                return BadRequest(ErrorResponseBody("Username and password are required", HttpStatusCode.BadRequest));

            Tuple<bool, string, CompleteAccessToken> result = await requestTokenService.RequestToken(request.Username, request.Password);

            if (!result.Item1) return BadRequest(ErrorResponseBody(result.Item2, HttpStatusCode.BadRequest));

            return Ok(SuccessResponseBody(result.Item3));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    public class LoginRequest {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    [HttpGet]
    [AssignActionRoute(UserManagementSegments.REFRESH_TOKEN)]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RefreshTokenAsync([FromQuery] string token) {
        try {
            Tuple<bool, string, CompleteAccessToken> result = await requestTokenService.RefreshToken(token);

            if (!result.Item1) return BadRequest(ErrorResponseBody(result.Item2, HttpStatusCode.BadRequest));

            return Ok(SuccessResponseBody(result.Item3));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(UserManagementSegments.IS_EMAIL_AVAILABLE)]
    public async Task<IActionResult> CheckIsEmailAvaliable([FromQuery] string email) {
        try {
            bool isEmailValid = emailValidationService.IsEmailValid(email);

            if (!isEmailValid) return BadRequest(ErrorResponseBody("Email is not valid", HttpStatusCode.BadRequest));

            return Ok(SuccessResponseBody(await emailAvailabilityService.IsEmailAvailableAsync(email)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}