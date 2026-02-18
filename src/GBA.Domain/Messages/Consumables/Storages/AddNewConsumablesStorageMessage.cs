using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.Storages;

public sealed class AddNewConsumablesStorageMessage {
    public AddNewConsumablesStorageMessage(ConsumablesStorage consumablesStorage) {
        ConsumablesStorage = consumablesStorage;
    }

    public ConsumablesStorage ConsumablesStorage { get; set; }
}