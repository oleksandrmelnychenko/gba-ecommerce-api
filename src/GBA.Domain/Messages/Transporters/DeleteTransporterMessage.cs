using System;

namespace GBA.Domain.Messages.Transporters;

public sealed class DeleteTransporterMessage {
    public DeleteTransporterMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}