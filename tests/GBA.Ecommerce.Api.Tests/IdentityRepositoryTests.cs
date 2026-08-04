using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using GBA.Common.Helpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Repositories.Identities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class IdentityRepositoryTests {
    [Fact]
    public async Task Failed_client_claim_assignment_cannot_be_masked_by_a_successful_role_assignment() {
        UserIdentity user = new() { NetId = Guid.NewGuid() };
        IdentityResult claimsFailure = IdentityResult.Failed(
            new IdentityError {
                Code = "ClaimWriteFailed",
                Description = "The client claim could not be stored."
            });
        Mock<UserManager<UserIdentity>> userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.AddClaimsAsync(
                user,
                It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(claimsFailure);
        userManager
            .Setup(manager => manager.AddToRoleAsync(user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        IdentityRepository repository = new(
            Mock.Of<IStringLocalizer<SharedResource>>(),
            userManager.Object);

        IdentityResult result = await repository.AddUserRoleAndClaims(
            user,
            "ClientUa");

        Assert.Same(claimsFailure, result);
        userManager.Verify(
            manager => manager.AddToRoleAsync(
                It.IsAny<UserIdentity>(),
                It.IsAny<string>()),
            Times.Never);
    }

    private static Mock<UserManager<UserIdentity>> CreateUserManager() {
        return new Mock<UserManager<UserIdentity>>(
            Mock.Of<IUserStore<UserIdentity>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<UserIdentity>>(),
            Array.Empty<IUserValidator<UserIdentity>>(),
            Array.Empty<IPasswordValidator<UserIdentity>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<UserIdentity>>>());
    }
}
