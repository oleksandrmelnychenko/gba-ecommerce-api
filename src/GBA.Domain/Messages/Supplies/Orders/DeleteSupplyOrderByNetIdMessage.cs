using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class DeleteSupplyOrderByNetIdMessage {
    public DeleteSupplyOrderByNetIdMessage(Guid orderNetId, Guid userNetId) {
        OrderNetId = orderNetId;

        UserNetId = userNetId;
    }

    public Guid OrderNetId { get; }

    public Guid UserNetId { get; }
}