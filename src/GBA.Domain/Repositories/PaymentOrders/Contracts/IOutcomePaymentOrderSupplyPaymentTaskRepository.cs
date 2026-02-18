using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IOutcomePaymentOrderSupplyPaymentTaskRepository {
    void Add(IEnumerable<OutcomePaymentOrderSupplyPaymentTask> tasks);
}