using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class DeleteSupplyOrderUkraineFromSupplierMessage {
    public DeleteSupplyOrderUkraineFromSupplierMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}