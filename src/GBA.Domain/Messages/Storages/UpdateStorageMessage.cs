using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Storages;

public sealed class UpdateStorageMessage {
    public UpdateStorageMessage(Storage storage) {
        Storage = storage;
    }

    public Storage Storage { get; set; }
}