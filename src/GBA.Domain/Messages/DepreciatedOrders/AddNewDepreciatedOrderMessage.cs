using System;
using GBA.Domain.Entities.DepreciatedOrders;

namespace GBA.Domain.Messages.DepreciatedOrders;

public sealed class AddNewDepreciatedOrderMessage {
    public AddNewDepreciatedOrderMessage(DepreciatedOrder depreciatedOrder, Guid userNetId) {
        DepreciatedOrder = depreciatedOrder;

        UserNetId = userNetId;
    }

    public DepreciatedOrder DepreciatedOrder { get; }

    public Guid UserNetId { get; }
}