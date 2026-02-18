using System;

namespace GBA.Domain.Messages.Sales.Orders;

public sealed class GetAllShopOrdersByClientNetIdMessage {
    public GetAllShopOrdersByClientNetIdMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; set; }
}