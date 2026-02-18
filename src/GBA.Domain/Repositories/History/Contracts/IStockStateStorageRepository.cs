using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.History.Contracts;

public interface IStockStateStorageRepository {
    long Add(StockStateStorage StockStateStorage);
    StockStateStorage GetId(long Id);
    StockStateStorage GetNetId(Guid NetId);
    List<StockStateStorage> GetAllFiltered(long[] storageId, DateTime from, DateTime to, long limit, long offset, string value);
    List<StockStateStorage> GetVerificationAllFiltered(long[] storageId, DateTime from, DateTime to, long limit, long offset, string value);
    List<ProductPlacementDataHistory> GetVerificationAllFilteredProductPlacementHistory(long[] storageId, DateTime from, DateTime to, long limit, long offset, string value);
    List<StockStateStorage> GetAll(long[] storageId, DateTime to, string value);
}