using System.Net;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Ecommerce.Controllers.UserManagement;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.UserManagement.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class UserManagementLogoutTests {
    [Fact]
    public async Task Concurrent_session_change_returns_conflict_instead_of_logout_success() {
        Mock<IRequestTokenService> tokens = new();
        tokens.Setup(service => service.RevokeRefreshToken("refresh-token")).ReturnsAsync(false);
        UserManagementController controller = Controller(tokens.Object);

        IActionResult result = await controller.LogoutAsync(
            new UserManagementController.RefreshTokenRequest {Token = "refresh-token"});

        ConflictObjectResult response = Assert.IsType<ConflictObjectResult>(result);
        IWebResponse body = Assert.IsAssignableFrom<IWebResponse>(response.Value);
        Assert.Equal(HttpStatusCode.Conflict, body.StatusCode);
    }

    [Fact]
    public async Task Identity_store_failure_returns_service_unavailable() {
        Mock<IRequestTokenService> tokens = new();
        tokens.Setup(service => service.RevokeRefreshToken("refresh-token"))
            .ThrowsAsync(new InvalidOperationException("identity store unavailable"));
        UserManagementController controller = Controller(tokens.Object);

        IActionResult result = await controller.LogoutAsync(
            new UserManagementController.RefreshTokenRequest {Token = "refresh-token"});

        ObjectResult response = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, response.StatusCode);
        IWebResponse body = Assert.IsAssignableFrom<IWebResponse>(response.Value);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, body.StatusCode);
        Assert.DoesNotContain("identity store unavailable", body.Message, StringComparison.Ordinal);
    }

    private static UserManagementController Controller(IRequestTokenService tokens) => new(
        new ResponseFactory(),
        Mock.Of<ISignUpService>(),
        tokens,
        Mock.Of<IEmailAvailabilityService>(),
        Mock.Of<IEmailValidationService>(),
        Mock.Of<IClientRegistrationTaskService>());
}
