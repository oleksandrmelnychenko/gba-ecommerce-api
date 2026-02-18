using System.Data;
using GBA.Domain.Repositories.History.Contracts;

namespace GBA.Domain.Repositories.History;

public class HistoryRepositoryFactory : IHistoryRepositoryFactory {
    public IProductAvailabilityDataHistoryRepository NewIProductAvailabilityDataHistoryRepository(IDbConnection connection) {
        return new ProductAvailabilityDataHistoryRepository(connection);
    }

    public IProductPlacementDataHistoryRepository NewIProductPlacementDataHistoryRepository(IDbConnection connection) {
        return new ProductPlacementDataHistoryRepository(connection);
    }

    public IStockStateStorageRepository NewStockStateStorageRepository(IDbConnection connection) {
        return new StockStateStorageRepository(connection);
    }
}