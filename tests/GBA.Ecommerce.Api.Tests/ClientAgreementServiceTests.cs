using System;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
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
}
