using System;

namespace GBA.Domain.Messages.Sales.OrderItems;

public sealed class SetActForEditMessage {
    public SetActForEditMessage(Guid netId, Guid userNetId) {
        NetId = netId;
        UserNetId = userNetId;
    }

    public Guid NetId { get; }
    public Guid UserNetId { get; }
}