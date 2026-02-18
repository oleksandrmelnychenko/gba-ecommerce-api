using System.Data;

namespace GBA.Domain.Repositories.ReSales.Contracts;

public interface IReSaleRepositoriesFactory {
    IReSaleItemRepository NewReSaleItemRepository(IDbConnection connection);

    IReSaleRepository NewReSaleRepository(IDbConnection connection);

    IReSaleAvailabilityRepository NewReSaleAvailabilityRepository(IDbConnection connection);
}