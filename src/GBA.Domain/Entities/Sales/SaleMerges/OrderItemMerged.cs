namespace GBA.Domain.Entities.Sales.SaleMerges;

public sealed class OrderItemMerged : EntityBase {
    public long OldOrderId { get; set; }

    public long OrderItemId { get; set; }

    public long OldOrderItemId { get; set; }

    public OrderItem OrderItem { get; set; }

    public OrderItem OldOrderItem { get; set; }

    public Order OldOrder { get; set; }
}