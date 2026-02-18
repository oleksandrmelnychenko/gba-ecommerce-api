namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class UpdateSupplyOrderUkraineItemPriceMessage {
    public UpdateSupplyOrderUkraineItemPriceMessage(long supplyOrderUkraineId) {
        SupplyOrderUkraineId = supplyOrderUkraineId;
    }

    public long SupplyOrderUkraineId { get; }
}