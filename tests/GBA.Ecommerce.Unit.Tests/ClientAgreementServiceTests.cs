using System.Data;
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
using GBA.Services.Services.Products;
using Moq;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class ClientAgreementServiceTests {
    [Fact]
    public async Task UpdateSelectedClientAgreement_InvalidatesLivePriceCacheForClient() {
        Guid clientNetId = Guid.NewGuid();
        Guid selectedAgreementNetId = Guid.NewGuid();
        ClientAgreement selected = new() {
            NetUid = selectedAgreementNetId,
            Agreement = new Agreement()
        };
        ClientAgreement unselected = new() {
            NetUid = Guid.NewGuid(),
            Agreement = new Agreement { IsSelected = true }
        };
        Client client = new() {
            NetUid = clientNetId,
            ClientAgreements = [selected, unselected]
        };
        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewSqlConnection()).Returns(connection.Object);
        Mock<IClientRepository> clients = new();
        clients.Setup(repository => repository.GetByNetId(clientNetId, true)).Returns(client);
        Mock<IClientRepositoriesFactory> clientRepositories = new();
        clientRepositories.Setup(factory => factory.NewClientRepository(connection.Object)).Returns(clients.Object);
        Mock<IAgreementRepository> agreements = new();
        Mock<IAgreementRepositoriesFactory> agreementRepositories = new();
        agreementRepositories.Setup(factory => factory.NewAgreementRepository(connection.Object)).Returns(agreements.Object);
        Mock<IPriceCacheService> priceCache = new();
        ClientAgreementService service = new(
            connectionFactory.Object,
            clientRepositories.Object,
            Mock.Of<IOrganizationRepositoriesFactory>(),
            Mock.Of<ICurrencyRepositoriesFactory>(),
            Mock.Of<IPricingRepositoriesFactory>(),
            agreementRepositories.Object,
            Mock.Of<IStorageRepositoryFactory>(),
            priceCache.Object);

        Client result = await service.UpdateSelectedClientAgreement(clientNetId, selectedAgreementNetId);

        Assert.Same(client, result);
        Assert.True(selected.Agreement.IsSelected);
        Assert.False(unselected.Agreement.IsSelected);
        agreements.Verify(repository => repository.Update(It.IsAny<Agreement>()), Times.Exactly(2));
        priceCache.Verify(cache => cache.InvalidateForClient(clientNetId), Times.Once);
    }
}
