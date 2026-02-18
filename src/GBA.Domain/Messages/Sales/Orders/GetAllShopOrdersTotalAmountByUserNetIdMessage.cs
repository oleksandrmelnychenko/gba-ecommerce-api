using System;

namespace GBA.Domain.Messages.Sales.Orders;

public sealed class GetAllShopOrdersTotalAmountByUserNetIdMessage {
    public GetAllShopOrdersTotalAmountByUserNetIdMessage(Guid userNetId) {
        UserNetId = userNetId;
    }

    public Guid UserNetId { get; set; }
}