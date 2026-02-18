using System;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.History.Contracts;

namespace GBA.Domain.Repositories.History;

public class ProductAvailabilityDataHistoryRepository : IProductAvailabilityDataHistoryRepository {
    private readonly IDbConnection _connection;

    public ProductAvailabilityDataHistoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductAvailabilityDataHistory productAvailabilityDataHistory) {
        return _connection.Query<long>(
            "INSERT INTO ProductAvailabilityDataHistory (Amount, StorageId, StockStateStorageID, Updated) " +
            "VALUES (@Amount, @StorageId, @StockStateStorageID, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            productAvailabilityDataHistory
        ).Single();
    }

    public ProductAvailabilityDataHistory GetId(long Id) {
        throw new NotImplementedException();
    }

    public ProductAvailabilityDataHistory GetNetId(Guid NetId) {
        throw new NotImplementedException();
    }
}