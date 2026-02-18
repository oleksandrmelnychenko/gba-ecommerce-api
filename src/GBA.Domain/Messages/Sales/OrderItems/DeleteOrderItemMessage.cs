using System;

namespace GBA.Domain.Messages.Sales.OrderItems;

public sealed class DeleteOrderItemMessage {
    public DeleteOrderItemMessage(Guid orderItemNetId, Guid userNetId) {
        OrderItemNetId = orderItemNetId;

        UserNetId = userNetId;
    }

    public Guid OrderItemNetId { get; }

    public Guid UserNetId { get; }
}