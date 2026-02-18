using System.Data;
using GBA.Domain.Repositories.ReSales.Contracts;

namespace GBA.Domain.Repositories.ReSales;

public sealed class ReSaleRepositoriesFactory : IReSaleRepositoriesFactory {
    public IReSaleItemRepository NewReSaleItemRepository(IDbConnection connection) {
        return new ReSaleItemRepository(connection);
    }

    public IReSaleRepository NewReSaleRepository(IDbConnection connection) {
        return new ReSaleRepository(connection);
    }

    public IReSaleAvailabilityRepository NewReSaleAvailabilityRepository(IDbConnection connection) {
        return new ReSaleAvailabilityRepository(connection);
    }
}