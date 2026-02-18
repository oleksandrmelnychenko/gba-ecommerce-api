using System;

namespace GBA.Domain.Messages.Sales.Orders;

public sealed class GetAllShopOrdersByUserNetIdMessage {
    public GetAllShopOrdersByUserNetIdMessage(Guid userNetId) {
        UserNetId = userNetId;
    }

    public Guid UserNetId { get; set; }
}