using GBA.Domain.Entities.SaleReturns;

namespace GBA.Domain.Repositories.SaleReturns.Contracts;

public interface ISaleReturnItemProductPlacementRepository {
    void Add(SaleReturnItemProductPlacement saleReturnItem);
}