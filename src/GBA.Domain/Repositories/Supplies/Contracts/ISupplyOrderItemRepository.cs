using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrderItemRepository {
    List<SupplyOrderItem> GetAllBySupplyOrderNetId(Guid netId);

    List<SupplyOrderItem> GetAll();

    SupplyOrderItem GetByNetId(Guid netId);

    SupplyOrderItem GetByOrderAndProductIdWithInvoiceItemsIfExists(long orderId, long productId);
    SupplyOrderItem GetByOrderAndProductIdWithInvoiceItemsIfExistsAndQty(long orderId, long productId, double qty);

    void Add(IEnumerable<SupplyOrderItem> supplyOrderItems);

    long Add(SupplyOrderItem supplyOrderItem);

    void UpdateQty(SupplyOrderItem supplyOrderItem);

    void Update(SupplyOrderItem supplyOrderItem);

    void Update(IEnumerable<SupplyOrderItem> supplyOrderItems);

    SupplyOrderItem GetByOrderAndProductIdAndQtyWithInvoiceItemsIfExists(long supplyOrderId, long productId, double qty, decimal unitPrice);
}