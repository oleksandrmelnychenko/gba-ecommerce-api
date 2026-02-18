using System;

namespace GBA.Domain.Messages.Storages;

public sealed class UnselectStorageForEcommerceMessage {
    public UnselectStorageForEcommerceMessage(Guid storageNetId) {
        StorageNetId = storageNetId;
    }

    public Guid StorageNetId { get; }
}