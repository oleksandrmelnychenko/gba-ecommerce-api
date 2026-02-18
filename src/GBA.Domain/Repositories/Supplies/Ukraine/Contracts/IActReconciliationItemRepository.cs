using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface IActReconciliationItemRepository {
    long Add(ActReconciliationItem actReconciliationItem);

    void Add(IEnumerable<ActReconciliationItem> actReconciliationItems);

    void Update(ActReconciliationItem actReconciliationItem);

    void FullUpdate(ActReconciliationItem actReconciliationItem);

    void RemoveAllBySupplyOrderUkraineItemIds(IEnumerable<long> ids);

    void RemoveAllByActReconciliationIdExceptProvidedSupplyOrderUkraineItemIds(long actReconciliationId, IEnumerable<long> ids);

    ActReconciliationItem GetById(long id);

    ActReconciliationItem GetByNetId(Guid netId);

    ActReconciliationItem GetBySupplyOrderUkraineItemId(long id);

    ActReconciliationItem GetBySupplyInvoiceOrderItemId(long id);

    IEnumerable<ActReconciliationItem> GetByIds(IEnumerable<long> ids);
}