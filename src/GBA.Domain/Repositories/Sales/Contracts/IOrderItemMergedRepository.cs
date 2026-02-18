using System.Collections.Generic;
using GBA.Domain.Entities.Sales.SaleMerges;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IOrderItemMergedRepository {
    long Add(OrderItemMerged orderItemMerged);

    void Add(List<OrderItemMerged> orderItemsMerged);
}