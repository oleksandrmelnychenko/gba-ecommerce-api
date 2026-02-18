using System;

namespace GBA.Domain.Messages.ColumnItems;

public sealed class DeleteColumnItemMessage {
    public DeleteColumnItemMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}