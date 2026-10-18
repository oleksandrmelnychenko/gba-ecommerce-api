using System;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Ecommerce.Contracts;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Domain.Repositories.Regions.Contracts;
using GBA.Services.Services.Clients;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.UserManagement;
using Microsoft.AspNetCore.Identity;
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

    [Fact]
    public async Task Identity_without_required_claims_and_role_is_not_reported_as_a_usable_account() {
        const long clientId = 42;
        const string password = "StrongPassword!2026";
        Guid regionNetId = Guid.NewGuid();
        Client client = new() {
            Id = clientId,
            NetUid = Guid.NewGuid(),
            EmailAddress = "new-client@example.com",
            MobileNumber = "0990000001"
        };

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository
            .Setup(repository => repository.Add(client))
            .Returns(clientId);
        clientRepository
            .Setup(repository => repository.GetById(clientId))
            .Returns(client);
        Mock<IClientInRoleRepository> clientInRoleRepository = new();
        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientInRoleRepository(connection.Object))
            .Returns(clientInRoleRepository.Object);

        Mock<IIdentityRepository> identityRepository = new();
        identityRepository
            .Setup(repository => repository.CreateUser(
                It.IsAny<GBA.Domain.IdentityEntities.UserIdentity>(),
                password,
                false))
            .ReturnsAsync(new IdentityResponse { Succeeded = true });
        identityRepository
            .Setup(repository => repository.AddUserRoleAndClaims(
                It.IsAny<GBA.Domain.IdentityEntities.UserIdentity>(),
                It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError {
                    Code = "RoleMissing",
                    Description = "Required client role is unavailable."
                }));
        Mock<IIdentityRepositoriesFactory> identityRepositoriesFactory = new();
        identityRepositoriesFactory
            .Setup(factory => factory.NewIdentityRepository())
            .Returns(identityRepository.Object);

        Mock<IEcommerceRegionRepository> ecommerceRegionRepository = new();
        ecommerceRegionRepository
            .Setup(repository => repository.GetByNetId(regionNetId))
            .Returns(new EcommerceRegion { IsLocalPayment = true });
        Mock<IEcommerceAdminPanelRepositoriesFactory> ecommerceRepositoriesFactory = new();
        ecommerceRepositoriesFactory
            .Setup(factory => factory.NewEcommerceRegionRepository(connection.Object))
            .Returns(ecommerceRegionRepository.Object);

        Mock<IClientAgreementService> agreementService = new();
        agreementService
            .Setup(service => service.EnsureDefaultAgreementTemplateAvailable())
            .Returns(Task.CompletedTask);
        agreementService
            .Setup(service => service.AddDefaultAgreementForClient(client, true))
            .Returns(Task.CompletedTask);

        SignUpService service = new(
            connectionFactory.Object,
            identityRepositoriesFactory.Object,
            clientRepositoriesFactory.Object,
            Mock.Of<IRegionRepositoriesFactory>(),
            ecommerceRepositoriesFactory.Object,
            agreementService.Object);

        Tuple<IdentityResponse, Client> result = await service.SignUp(
            client,
            password,
            regionNetId);

        Assert.False(result.Item1.Succeeded);
        Assert.Contains(
            result.Item1.Errors,
            error => error.Code == "RoleMissing"
                     && error.Description == "Required client role is unavailable.");
        agreementService.Verify(
            agreement => agreement.AddDefaultAgreementForClient(
                It.IsAny<Client>(),
                It.IsAny<bool>()),
            Times.Never);
        identityRepository.Verify(
            repository => repository.DeleteUser(
                It.Is<GBA.Domain.IdentityEntities.UserIdentity>(
                    user => user.NetId == client.NetUid)),
            Times.Once);
        clientRepository.Verify(repository => repository.Remove(clientId), Times.Once);
    }
}
