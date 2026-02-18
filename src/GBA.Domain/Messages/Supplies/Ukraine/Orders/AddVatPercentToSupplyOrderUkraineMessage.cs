using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class AddVatPercentToSupplyOrderUkraineMessage {
    public AddVatPercentToSupplyOrderUkraineMessage(
        SupplyOrderUkraine supplyOrderUkraine) {
        SupplyOrderUkraine = supplyOrderUkraine;
    }

    public SupplyOrderUkraine SupplyOrderUkraine { get; }
}