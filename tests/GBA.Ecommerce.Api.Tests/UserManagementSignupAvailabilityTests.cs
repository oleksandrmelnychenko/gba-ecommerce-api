using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration.Entities;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers;
using GBA.Ecommerce.Controllers.UserManagement;
using GBA.Services.Services.Clients;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.UserManagement.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class UserManagementSignupAvailabilityTests {
    [Fact]
    public async Task Signup_does_not_return_success_when_initial_session_cannot_be_started() {
        const string mobileNumber = "0990000001";
        const string password = "StrongPassword!2026";
        Client createdClient = new() {
            NetUid = Guid.NewGuid(),
            MobileNumber = mobileNumber
        };
        Mock<ISignUpService> signUpService = new();
        signUpService
            .Setup(service => service.SignUp(
                It.IsAny<Client>(),
                password,
                It.IsAny<Guid>()))
            .ReturnsAsync(
                new Tuple<IdentityResponse, Client>(
                    new IdentityResponse { Succeeded = true },
                    createdClient));
        Mock<IRequestTokenService> requestTokenService = new();
        requestTokenService
            .Setup(service => service.RequestToken(mobileNumber, password))
            .ReturnsAsync(
                new Tuple<bool, string, CompleteAccessToken>(
                    false,
                    "Session could not be started.",
                    null!));
        Mock<IClientRegistrationTaskService> registrationTaskService = new();
        registrationTaskService
            .Setup(service => service.Add(createdClient))
            .Returns(Task.CompletedTask);
        Mock<IEmailValidationService> emailValidationService = new();
        emailValidationService
            .Setup(service => service.IsEmailValid("new-client-token@example.com"))
            .Returns(true);
        UserManagementController controller = new(
            new ResponseFactory(),
            signUpService.Object,
            requestTokenService.Object,
            Mock.Of<IEmailAvailabilityService>(),
            emailValidationService.Object,
            registrationTaskService.Object);

        IActionResult result = await controller.SignUp(
            new UserManagementController.SignUpRequest {
                Client = new UserManagementController.SignUpClientRequest {
                    IsIndividual = true,
                    Name = "New Client",
                    FullName = "New Client",
                    FirstName = "New",
                    LastName = "Client",
                    EmailAddress = "new-client-token@example.com",
                    MobileNumber = mobileNumber
                },
                Password = password,
                EcommerceRegionNetId = Guid.NewGuid()
            });

        ObjectResult response = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.InternalServerError, response.StatusCode);
        IWebResponse body = Assert.IsAssignableFrom<IWebResponse>(response.Value);
        Assert.Equal(HttpStatusCode.InternalServerError, body.StatusCode);
        Assert.Equal(
            "Account was created, but the session could not be started. Sign in again.",
            body.Message);
        registrationTaskService.Verify(
            service => service.Add(createdClient),
            Times.Once);
    }

    [Fact]
    public async Task Missing_retail_template_returns_explicit_service_unavailable_response() {
        Mock<ISignUpService> signUpService = new();
        signUpService
            .Setup(service => service.SignUp(
                It.IsAny<GBA.Domain.Entities.Clients.Client>(),
                It.IsAny<string>(),
                It.IsAny<Guid>()))
            .ThrowsAsync(
                new DefaultAgreementTemplateUnavailableException(
                    "Retail template is unavailable."));
        Mock<IEmailValidationService> emailValidationService = new();
        emailValidationService
            .Setup(service => service.IsEmailValid("new-client@example.com"))
            .Returns(true);
        UserManagementController controller = new(
            new ResponseFactory(),
            signUpService.Object,
            Mock.Of<IRequestTokenService>(),
            Mock.Of<IEmailAvailabilityService>(),
            emailValidationService.Object,
            Mock.Of<IClientRegistrationTaskService>());

        IActionResult result = await controller.SignUp(
            new UserManagementController.SignUpRequest {
                Client = new UserManagementController.SignUpClientRequest {
                    IsIndividual = true,
                    Name = "New Client",
                    FullName = "New Client",
                    FirstName = "New",
                    LastName = "Client",
                    EmailAddress = "new-client@example.com",
                    MobileNumber = "0990000000"
                },
                Password = "StrongPassword!2026",
                EcommerceRegionNetId = Guid.NewGuid()
            });

        ObjectResult response = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, response.StatusCode);
        IWebResponse body = Assert.IsAssignableFrom<IWebResponse>(response.Value);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, body.StatusCode);
        Assert.Equal("Registration is temporarily unavailable.", body.Message);
    }
}
