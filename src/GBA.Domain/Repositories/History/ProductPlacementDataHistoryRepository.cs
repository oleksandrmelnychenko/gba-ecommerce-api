using System;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.History.Contracts;

namespace GBA.Domain.Repositories.History;

public class ProductPlacementDataHistoryRepository : IProductPlacementDataHistoryRepository {
    private readonly IDbConnection _connection;

    public ProductPlacementDataHistoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductPlacementDataHistory productPlacementDataHistory) {
        return _connection.Query<long>(
            "INSERT INTO ProductPlacementDataHistory (Qty, StorageNumber, RowNumber, CellNumber, ProductAvailabilityDataHistoryID, VendorCode, NameUA, MainOriginalNumber, ProductId, StorageId, ConsignmentItemId, Updated) " +
            "VALUES (@Qty, @StorageNumber, @RowNumber, @CellNumber, @ProductAvailabilityDataHistoryID, @VendorCode, @NameUA, @MainOriginalNumber,  @ProductId, @StorageId, @ConsignmentItemId, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            productPlacementDataHistory
        ).Single();
    }

    public ProductPlacementDataHistory GetId(long Id) {
        throw new NotImplementedException();
    }

    public ProductPlacementDataHistory GetNetId(Guid NetId) {
        throw new NotImplementedException();
    }
}