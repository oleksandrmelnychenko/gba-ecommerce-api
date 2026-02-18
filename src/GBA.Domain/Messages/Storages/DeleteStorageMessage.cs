using System;

namespace GBA.Domain.Messages.Storages;

public sealed class DeleteStorageMessage {
    public DeleteStorageMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}