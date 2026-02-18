using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface IActReconciliationRepository {
    long Add(ActReconciliation actReconciliation);

    void Remove(long id);

    ActReconciliation GetLastRecord();

    ActReconciliation GetBySupplyOrderUkraineId(long id);

    ActReconciliation GetBySupplyInvoiceId(long id);

    ActReconciliation GetById(long id);

    ActReconciliation GetByIdIfExists(long id);

    ActReconciliation GetByNetId(Guid netId);

    List<ActReconciliation> GetAll();

    List<ActReconciliation> GetAllFiltered(DateTime from, DateTime to);
}