using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
    private static readonly Regex _phoneNumberPattern = new(@"^\d{9,15}$", RegexOptions.Compiled);

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.SIGN_UP)]
    [EnableRateLimiting("auth")]
    [Consumes("application/json")]
    [RequestSizeLimit(131072)]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request) {
        if (request?.Client == null) {
            return BadRequest(ErrorResponseBody("Client payload is required", HttpStatusCode.BadRequest));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ErrorResponseBody("Password is required", HttpStatusCode.BadRequest));
        if (request.EcommerceRegionNetId == Guid.Empty)
            return BadRequest(ErrorResponseBody("Ecommerce region is required", HttpStatusCode.BadRequest));

        Client client = BuildSignUpClient(request.Client);
        Tuple<IdentityResponse, Client> identityResponse = await signUpService.SignUp(
            client,
            request.Password,
            request.EcommerceRegionNetId
        );

        if (identityResponse.Item1.Succeeded) {
            await clientRegistrationTaskService.Add(identityResponse.Item2);

            Tuple<bool, string, CompleteAccessToken> result =
                await requestTokenService.RequestToken(client.MobileNumber, request.Password);

            return Ok(SuccessResponseBody(result.Item3));
        }

        return BadRequest(ErrorResponseBody(identityResponse.Item1.Errors.FirstOrDefault()?.Description, HttpStatusCode.BadRequest));
    }

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.GET_TOKEN)]
    [EnableRateLimiting("auth")]
    [Consumes("application/json")]
    [RequestSizeLimit(16384)]
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
        public SignUpClientRequest? Client { get; set; }
        public string Password { get; set; } = string.Empty;
        public Guid EcommerceRegionNetId { get; set; }
    }

    public sealed class SignUpClientRequest {
        public bool IsIndividual { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
    }

    [HttpPost]
    [AssignActionRoute(UserManagementSegments.REFRESH_TOKEN)]
    [EnableRateLimiting("auth")]
    [Consumes("application/json")]
    [RequestSizeLimit(8192)]
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
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> CheckIsEmailAvaliable([FromQuery] string email) {
        bool isEmailValid = emailValidationService.IsEmailValid(email);

        if (!isEmailValid) return BadRequest(ErrorResponseBody("Email is not valid", HttpStatusCode.BadRequest));

        return Ok(SuccessResponseBody(await emailAvailabilityService.IsEmailAvailableAsync(email)));
    }

    private Client BuildSignUpClient(SignUpClientRequest request) {
        string name = request.Name?.Trim() ?? string.Empty;
        string fullName = request.FullName?.Trim() ?? string.Empty;
        string firstName = request.FirstName?.Trim() ?? string.Empty;
        string middleName = request.MiddleName?.Trim() ?? string.Empty;
        string lastName = request.LastName?.Trim() ?? string.Empty;
        string email = request.EmailAddress?.Trim() ?? string.Empty;
        string mobileNumber = request.MobileNumber?.Trim() ?? string.Empty;

        if (name.Length is < 1 or > 200 ||
            fullName.Length > 250 ||
            firstName.Length > 100 ||
            middleName.Length > 100 ||
            lastName.Length > 100)
            throw new ArgumentException("Client name is invalid.");
        if (email.Length > 254 || !emailValidationService.IsEmailValid(email))
            throw new ArgumentException("Email is invalid.");
        if (!_phoneNumberPattern.IsMatch(mobileNumber))
            throw new ArgumentException("Mobile number is invalid.");

        return new Client {
            IsIndividual = request.IsIndividual,
            IsTemporaryClient = true,
            IsActive = false,
            IsBlocked = false,
            IsSubClient = false,
            IsTradePoint = false,
            IsForRetail = false,
            Name = name,
            FullName = fullName,
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            EmailAddress = email,
            MobileNumber = mobileNumber
        };
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
