using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class ManageSupplyOrderUkraineDocumentMessage {
    public ManageSupplyOrderUkraineDocumentMessage(
        SupplyOrderUkraine supplyOrderUkraine) {
        SupplyOrderUkraine = supplyOrderUkraine;
    }

    public SupplyOrderUkraine SupplyOrderUkraine { get; }
}