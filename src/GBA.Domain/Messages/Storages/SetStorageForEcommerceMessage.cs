using System;

namespace GBA.Domain.Messages.Storages;

public sealed class SetStorageForEcommerceMessage {
    public SetStorageForEcommerceMessage(Guid storageNetId) {
        StorageNetId = storageNetId;
    }

    public Guid StorageNetId { get; }
}