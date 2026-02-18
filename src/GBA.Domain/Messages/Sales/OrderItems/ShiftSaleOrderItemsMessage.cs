using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales.OrderItems;

public sealed class ShiftSaleOrderItemsMessage {
    public ShiftSaleOrderItemsMessage(Sale sale, Guid userNetId, bool billReturn = false) {
        Sale = sale;

        UserNetId = userNetId;
        BillReturn = billReturn;
    }

    public Sale Sale { get; set; }

    public Guid UserNetId { get; set; }

    public bool BillReturn { get; }
}