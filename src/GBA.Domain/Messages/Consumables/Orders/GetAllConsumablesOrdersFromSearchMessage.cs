namespace GBA.Domain.Messages.Consumables.Orders;

public sealed class GetAllConsumablesOrdersFromSearchMessage {
    public GetAllConsumablesOrdersFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}