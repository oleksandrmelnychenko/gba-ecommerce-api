namespace GBA.Domain.EntityHelpers;

public sealed class ShiftedOrderItem {
    public ShiftedOrderItem(long productId, long orderItemId, double qty, long userId, long? oldOrderItem = null) {
        ProductId = productId;

        OrderItemId = orderItemId;
        UserId = userId;

        Qty = qty;

        OldOrderItem = oldOrderItem;
    }

    public long ProductId { get; set; }

    public long OrderItemId { get; set; }

    public double Qty { get; set; }

    public long? OldOrderItem { get; set; }
    public long UserId { get; set; }
}