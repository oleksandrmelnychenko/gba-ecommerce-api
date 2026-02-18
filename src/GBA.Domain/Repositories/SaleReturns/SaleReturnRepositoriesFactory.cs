using System.Data;
using GBA.Domain.Repositories.SaleReturns.Contracts;

namespace GBA.Domain.Repositories.SaleReturns;

public sealed class SaleReturnRepositoriesFactory : ISaleReturnRepositoriesFactory {
    public ISaleReturnItemRepository NewSaleReturnItemRepository(IDbConnection connection) {
        return new SaleReturnItemRepository(connection);
    }

    public ISaleReturnItemProductPlacementRepository NewSaleReturnItemProductPlacementRepository(IDbConnection connection) {
        return new SaleReturnItemProductPlacementRepository(connection);
    }

    public ISaleReturnRepository NewSaleReturnRepository(IDbConnection connection) {
        return new SaleReturnRepository(connection);
    }

    public ISaleReturnItemStatusNameRepository NewSaleReturnItemStatusNameRepository(IDbConnection connection) {
        return new SaleReturnItemStatusNameRepository(connection);
    }
}