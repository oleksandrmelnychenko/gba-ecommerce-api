namespace GBA.Domain.Entities.Sales;

public sealed class OrderItemMovement : EntityBase {
    public double Qty { get; set; }

    public long UserId { get; set; }

    public long OrderItemId { get; set; }

    public OrderItemMovementType MovementType { get; set; }

    public User User { get; set; }

    public OrderItem OrderItem { get; set; }
}