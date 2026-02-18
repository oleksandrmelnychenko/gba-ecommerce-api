using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Returns;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyReturnItemRepository {
    long Add(SupplyReturnItem item);

    void Add(IEnumerable<SupplyReturnItem> items);

    void RemoveAllBySupplyReturnId(long id);
}