namespace GBA.Domain.Messages.Consumables.Storages;

public sealed class GetAllConsumablesStoragesFromSearchMessage {
    public GetAllConsumablesStoragesFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}