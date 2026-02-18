using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface IDeliveryExpenseRepository {
    DeliveryExpense GetById(long id);

    long Add(DeliveryExpense deliveryExpense);

    void Update(DeliveryExpense deliveryExpense);
}