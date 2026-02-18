using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class RemoveSadDocumentByNetIdMessage {
    public RemoveSadDocumentByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}