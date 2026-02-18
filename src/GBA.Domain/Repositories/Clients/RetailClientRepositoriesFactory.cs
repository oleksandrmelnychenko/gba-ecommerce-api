using System.Data;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Clients.RetailClients;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class RetailClientRepositoriesFactory : IRetailClientRepositoriesFactory {
    public IRetailClientRepository NewRetailClientRepository(IDbConnection connection) {
        return new RetailClientRepository(connection);
    }

    public IRetailClientPaymentImageRepository NewRetailClientPaymentImageRepository(IDbConnection connection) {
        return new RetailClientPaymentImageRepository(connection);
    }

    public IRetailClientPaymentImageItemRepository NewRetailClientPaymentImageItemRepository(IDbConnection connection) {
        return new RetailClientPaymentImageItemRepository(connection);
    }

    public IRetailPaymentStatusRepository NewRetailPaymentStatusRepository(IDbConnection connection) {
        return new RetailPaymentStatusRepository(connection);
    }
}