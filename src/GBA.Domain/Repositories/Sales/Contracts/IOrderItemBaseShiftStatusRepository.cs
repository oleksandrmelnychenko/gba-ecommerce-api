using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IOrderItemBaseShiftStatusRepository {
    long Add(OrderItemBaseShiftStatus orderItemBaseShiftStatus);

    void Update(OrderItemBaseShiftStatus orderItemBaseShiftStatus);

    OrderItemBaseShiftStatus GetByIdForConsignment(long id);
}