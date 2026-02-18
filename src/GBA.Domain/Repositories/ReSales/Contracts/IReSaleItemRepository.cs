using System.Collections.Generic;
using GBA.Domain.Entities.ReSales;

namespace GBA.Domain.Repositories.ReSales.Contracts;

public interface IReSaleItemRepository {
    long Add(ReSaleItem item);

    void Update(ReSaleItem item);

    void UpdateMany(IEnumerable<ReSaleItem> items);

    void Delete(long id);

    ReSaleItem GetById(long id);

    ReSaleItem GetEmptyReSaleItemsByProductId(long reSaleId, long productId);

    int GetSoldInReSaleByProductIdAndReSaleId(long productId, long reSaleId);

    void DeleteByReSale(long reSaleId);
}