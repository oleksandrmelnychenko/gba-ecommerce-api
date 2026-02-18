using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyPaymentTaskRepository {
    long Add(SupplyPaymentTask supplyPaymentTask);

    void Update(IEnumerable<SupplyPaymentTask> supplyPaymentTasks);

    void Update(SupplyPaymentTask supplyPaymentTask);

    void UpdateTaskStatus(SupplyPaymentTask supplyPaymentTask);

    void UpdateTaskPrices(SupplyPaymentTask supplyPaymentTask);

    void SetTaskAvailableForPayment(SupplyPaymentTask supplyPaymentTask);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveById(long taskId, long deletedById);

    List<SupplyPaymentTask> GetAllByIds(IEnumerable<long> ids);

    SupplyPaymentTask GetById(long id);

    SupplyPaymentTask GetByNetId(Guid netId);

    SupplyPaymentTask GetByIdWithCalculatedGrossPrice(long id);
}