using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyInvoiceOrderItemRepository {
    long Add(SupplyInvoiceOrderItem supplyInvoiceOrderItem);

    void Add(IEnumerable<SupplyInvoiceOrderItem> supplyInvoiceOrderItems);

    void Update(SupplyInvoiceOrderItem supplyInvoiceOrderItem);

    void Update(IEnumerable<SupplyInvoiceOrderItem> supplyInvoiceOrderItems);

    void UpdateSupplyInvoiceId(IEnumerable<SupplyInvoiceOrderItem> supplyInvoiceOrderItems);

    void RemoveAllByInvoiceId(long invoiceId);

    void RemoveAllByInvoiceIdExceptProvided(long invoiceId, IEnumerable<long> ids);

    SupplyInvoiceOrderItem GetById(long id);

    SupplyInvoiceOrderItem GetByInvoiceAndSupplyOrderItemIds(long invoiceId, long orderItemId);

    SupplyInvoiceOrderItem GetByInvoiceAndProductIds(long invoiceId, long productId, double qty, decimal unitPrice);
}