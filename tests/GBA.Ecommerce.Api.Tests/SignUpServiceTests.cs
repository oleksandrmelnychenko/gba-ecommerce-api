using System;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Ecommerce.Contracts;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Domain.Repositories.Regions.Contracts;
using GBA.Services.Services.Clients;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.UserManagement;
using Moq;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class SignUpServiceTests {
    [Fact]
    public async Task Missing_retail_template_fails_before_any_client_or_identity_write() {
        DefaultAgreementTemplateUnavailableException expected =
            new("Ecommerce retail template is unavailable.");
        Mock<IClientAgreementService> agreementService = new();
        agreementService
            .Setup(service => service.EnsureDefaultAgreementTemplateAvailable())
            .ThrowsAsync(expected);

        Mock<IDbConnectionFactory> connectionFactory = new();
        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        Mock<IIdentityRepositoriesFactory> identityRepositoriesFactory = new();
        SignUpService service = new(
            connectionFactory.Object,
            identityRepositoriesFactory.Object,
            clientRepositoriesFactory.Object,
            Mock.Of<IRegionRepositoriesFactory>(),
            Mock.Of<IEcommerceAdminPanelRepositoriesFactory>(),
            agreementService.Object);

        DefaultAgreementTemplateUnavailableException actual =
            await Assert.ThrowsAsync<DefaultAgreementTemplateUnavailableException>(
                () => service.SignUp(
                    new Client(),
                    "StrongPassword!2026",
                    Guid.NewGuid()));

        Assert.Same(expected, actual);
        connectionFactory.Verify(
            factory => factory.NewSqlConnection(),
            Times.Never);
        clientRepositoriesFactory.Verify(
            factory => factory.NewClientRepository(It.IsAny<IDbConnection>()),
            Times.Never);
        identityRepositoriesFactory.Verify(
            factory => factory.NewIdentityRepository(),
            Times.Never);
    }
}
