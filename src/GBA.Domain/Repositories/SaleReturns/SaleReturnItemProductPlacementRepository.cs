using System.Data;
using Dapper;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Repositories.SaleReturns.Contracts;

namespace GBA.Domain.Repositories.SaleReturns;

public sealed class SaleReturnItemProductPlacementRepository : ISaleReturnItemProductPlacementRepository {
    private readonly IDbConnection _connection;

    public SaleReturnItemProductPlacementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(SaleReturnItemProductPlacement saleReturnItemProductPlacement) {
        _connection.Execute(
            "INSERT INTO [SaleReturnItemProductPlacement] (ProductPlacementId, SaleReturnItemId, Qty, Updated) " +
            "VALUES (@ProductPlacementId, @SaleReturnItemId, @Qty, GETUTCDATE())",
            saleReturnItemProductPlacement
        );
    }
}