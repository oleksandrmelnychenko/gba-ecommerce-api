using System.Data;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IRetailClientRepositoriesFactory {
    IRetailClientRepository NewRetailClientRepository(IDbConnection connection);

    IRetailClientPaymentImageRepository NewRetailClientPaymentImageRepository(IDbConnection connection);

    IRetailClientPaymentImageItemRepository NewRetailClientPaymentImageItemRepository(IDbConnection connection);

    IRetailPaymentStatusRepository NewRetailPaymentStatusRepository(IDbConnection connection);
}