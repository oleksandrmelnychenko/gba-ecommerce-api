using System.Collections.Generic;
using GBA.Domain.Entities.SaleReturns;

namespace GBA.Domain.Repositories.SaleReturns.Contracts;

public interface ISaleReturnItemRepository {
    long Add(SaleReturnItem saleReturnItem);

    void Add(IEnumerable<SaleReturnItem> saleReturnItems);

    void Update(IEnumerable<SaleReturnItem> saleReturnItems);

    void Update(SaleReturnItem saleReturnItem);

    void UpdateProductPlacement(SaleReturnItem saleReturnItem);

    void RemoveAllByIds(IEnumerable<long> ids, long updatedById);

    void RemoveById(long id);
}