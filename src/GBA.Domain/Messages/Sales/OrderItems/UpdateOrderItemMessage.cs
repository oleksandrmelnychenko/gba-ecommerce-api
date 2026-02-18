using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales.OrderItems;

public sealed class UpdateOrderItemMessage {
    public UpdateOrderItemMessage(OrderItem orderItem, Guid userNetId) {
        OrderItem = orderItem;

        UserNetId = userNetId;
    }

    public OrderItem OrderItem { get; set; }

    public Guid UserNetId { get; set; }
}