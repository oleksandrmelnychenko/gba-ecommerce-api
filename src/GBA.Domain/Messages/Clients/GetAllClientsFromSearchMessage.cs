namespace GBA.Domain.Messages.Clients;

public sealed class GetAllClientsFromSearchMessage {
    public GetAllClientsFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; }
}