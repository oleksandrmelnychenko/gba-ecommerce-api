using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IOutcomePaymentOrderConsumablesOrderRepository {
    void Add(OutcomePaymentOrderConsumablesOrder order);

    void Add(IEnumerable<OutcomePaymentOrderConsumablesOrder> orders);

    void Update(IEnumerable<OutcomePaymentOrderConsumablesOrder> orders);
}