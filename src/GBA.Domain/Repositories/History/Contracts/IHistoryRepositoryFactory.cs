using System.Data;

namespace GBA.Domain.Repositories.History.Contracts;

public interface IHistoryRepositoryFactory {
    IProductPlacementDataHistoryRepository NewIProductPlacementDataHistoryRepository(IDbConnection connection);
    IProductAvailabilityDataHistoryRepository NewIProductAvailabilityDataHistoryRepository(IDbConnection connection);
    IStockStateStorageRepository NewStockStateStorageRepository(IDbConnection connection);
}