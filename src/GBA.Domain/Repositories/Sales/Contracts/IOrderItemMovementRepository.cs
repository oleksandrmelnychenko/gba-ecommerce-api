using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IOrderItemMovementRepository {
    void Add(OrderItemMovement movement);

    void Update(OrderItemMovement movement);

    void Remove(long id);

    void Remove(IEnumerable<long> ids);

    void RemoveAllByOrderItemId(long orderItemId);

    IEnumerable<OrderItemMovement> GetAllByOrderItemId(long orderItemId);
}