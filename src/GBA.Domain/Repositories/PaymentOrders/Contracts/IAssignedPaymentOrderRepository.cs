using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IAssignedPaymentOrderRepository {
    void Add(AssignedPaymentOrder assignedPaymentOrder);

    void Remove(long id);
}