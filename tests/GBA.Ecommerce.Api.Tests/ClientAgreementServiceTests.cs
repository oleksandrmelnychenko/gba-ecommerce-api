using System;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Clients;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ClientAgreementServiceTests {
    [Fact]
    public async Task Missing_retail_template_fails_preflight_before_client_graph_access() {
        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IStorageRepository> storageRepository = new();
        storageRepository
            .Setup(repository => repository.GetWithHighestPriority(null))
            .Returns((GBA.Domain.Entities.Storage)null!);
        Mock<IStorageRepositoryFactory> storageFactory = new();
        storageFactory
            .Setup(factory => factory.NewStorageRepository(connection.Object))
            .Returns(storageRepository.Object);

        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        ClientAgreementService service = new(
            connectionFactory.Object,
            clientRepositoriesFactory.Object,
            Mock.Of<IOrganizationRepositoriesFactory>(),
            Mock.Of<ICurrencyRepositoriesFactory>(),
            Mock.Of<IPricingRepositoriesFactory>(),
            Mock.Of<IAgreementRepositoriesFactory>(),
            storageFactory.Object,
            Mock.Of<ILogger<ClientAgreementService>>());

        DefaultAgreementTemplateUnavailableException exception =
            await Assert.ThrowsAsync<DefaultAgreementTemplateUnavailableException>(
                service.EnsureDefaultAgreementTemplateAvailable);

        Assert.Equal(
            "Ecommerce retail storage and agreement template are not configured.",
            exception.Message);
        clientRepositoriesFactory.Verify(
            factory => factory.NewClientRepository(It.IsAny<IDbConnection>()),
            Times.Never);
        clientRepositoriesFactory.Verify(
            factory => factory.NewClientAgreementRepository(It.IsAny<IDbConnection>()),
            Times.Never);
    }

    [Fact]
    public async Task Sub_client_selects_the_root_clients_fenix_agreement() {
        Guid actorNetId = Guid.NewGuid();
        Guid rootClientNetId = Guid.NewGuid();
        Guid fenixAgreementNetId = Guid.NewGuid();
        Agreement fenixAgreement = new() { IsSelected = false };
        Agreement otherAgreement = new() { IsSelected = true };
        Client rootClient = new() { NetUid = rootClientNetId };
        rootClient.ClientAgreements.Add(new ClientAgreement {
            NetUid = fenixAgreementNetId,
            Agreement = fenixAgreement
        });
        rootClient.ClientAgreements.Add(new ClientAgreement {
            NetUid = Guid.NewGuid(),
            Agreement = otherAgreement
        });

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository
            .Setup(repository => repository.GetRootNetIdBySubClientNetId(actorNetId))
            .Returns(rootClientNetId);
        clientRepository
            .Setup(repository => repository.GetByNetId(rootClientNetId, true))
            .Returns(rootClient);
        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);

        Mock<IAgreementRepository> agreementRepository = new();
        Mock<IAgreementRepositoriesFactory> agreementRepositoriesFactory = new();
        agreementRepositoriesFactory
            .Setup(factory => factory.NewAgreementRepository(connection.Object))
            .Returns(agreementRepository.Object);

        ClientAgreementService service = CreateService(
            connectionFactory.Object,
            clientRepositoriesFactory.Object,
            agreementRepositoriesFactory.Object);

        Client result = await service.UpdateSelectedClientAgreement(
            actorNetId,
            fenixAgreementNetId);

        Assert.Same(rootClient, result);
        Assert.True(fenixAgreement.IsSelected);
        Assert.False(otherAgreement.IsSelected);
        clientRepository.Verify(
            repository => repository.GetByNetId(actorNetId, true),
            Times.Never);
        agreementRepository.Verify(
            repository => repository.Update(It.IsAny<Agreement>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Direct_client_keeps_its_own_agreement_context() {
        Guid actorNetId = Guid.NewGuid();
        Guid agreementNetId = Guid.NewGuid();
        Agreement agreement = new() { IsSelected = false };
        Client directClient = new() { NetUid = actorNetId };
        directClient.ClientAgreements.Add(new ClientAgreement {
            NetUid = agreementNetId,
            Agreement = agreement
        });

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IClientRepository> clientRepository = new();
        clientRepository
            .Setup(repository => repository.GetRootNetIdBySubClientNetId(actorNetId))
            .Returns(Guid.Empty);
        clientRepository
            .Setup(repository => repository.GetByNetId(actorNetId, true))
            .Returns(directClient);
        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(connection.Object))
            .Returns(clientRepository.Object);

        Mock<IAgreementRepository> agreementRepository = new();
        Mock<IAgreementRepositoriesFactory> agreementRepositoriesFactory = new();
        agreementRepositoriesFactory
            .Setup(factory => factory.NewAgreementRepository(connection.Object))
            .Returns(agreementRepository.Object);

        ClientAgreementService service = CreateService(
            connectionFactory.Object,
            clientRepositoriesFactory.Object,
            agreementRepositoriesFactory.Object);

        Client result = await service.UpdateSelectedClientAgreement(
            actorNetId,
            agreementNetId);

        Assert.Same(directClient, result);
        Assert.True(agreement.IsSelected);
        clientRepository.Verify(
            repository => repository.GetByNetId(actorNetId, true),
            Times.Once);
        agreementRepository.Verify(
            repository => repository.Update(agreement),
            Times.Once);
    }

    private static ClientAgreementService CreateService(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory) {
        return new ClientAgreementService(
            connectionFactory,
            clientRepositoriesFactory,
            Mock.Of<IOrganizationRepositoriesFactory>(),
            Mock.Of<ICurrencyRepositoriesFactory>(),
            Mock.Of<IPricingRepositoriesFactory>(),
            agreementRepositoriesFactory,
            Mock.Of<IStorageRepositoryFactory>(),
            Mock.Of<ILogger<ClientAgreementService>>());
    }
}
