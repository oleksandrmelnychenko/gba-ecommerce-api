using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales.OrderItems;

public sealed class ShiftSaleOrderItemMessage {
    public ShiftSaleOrderItemMessage(Guid saleFromNetId, Guid saleToNetId, OrderItem orderItem, Guid userNetId) {
        SaleFromNetId = saleFromNetId;

        SaleToNetId = saleToNetId;

        OrderItem = orderItem;

        UserNetId = userNetId;
    }

    public Guid SaleFromNetId { get; set; }

    public Guid SaleToNetId { get; set; }

    public OrderItem OrderItem { get; set; }

    public Guid UserNetId { get; set; }

    public Sale Sale { get; set; }
}