using System.Data;

namespace GBA.Domain.Repositories.SaleReturns.Contracts;

public interface ISaleReturnRepositoriesFactory {
    ISaleReturnRepository NewSaleReturnRepository(IDbConnection connection);

    ISaleReturnItemRepository NewSaleReturnItemRepository(IDbConnection connection);
    ISaleReturnItemProductPlacementRepository NewSaleReturnItemProductPlacementRepository(IDbConnection connection);

    ISaleReturnItemStatusNameRepository NewSaleReturnItemStatusNameRepository(IDbConnection connection);
}