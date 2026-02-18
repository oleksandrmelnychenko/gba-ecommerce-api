using System;

namespace GBA.Domain.Messages.Supplies.HelperServices;

public sealed class DeleteServiceDetailItemMessage {
    public DeleteServiceDetailItemMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}