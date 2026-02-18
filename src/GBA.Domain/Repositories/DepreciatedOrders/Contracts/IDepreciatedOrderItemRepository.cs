using System.Collections.Generic;
using GBA.Domain.Entities.DepreciatedOrders;

namespace GBA.Domain.Repositories.DepreciatedOrders.Contracts;

public interface IDepreciatedOrderItemRepository {
    long Add(DepreciatedOrderItem item);

    void Add(IEnumerable<DepreciatedOrderItem> items);
}