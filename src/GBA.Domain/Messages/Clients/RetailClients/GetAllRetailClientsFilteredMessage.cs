namespace GBA.Domain.Messages.Clients.RetailClients;

public sealed class GetAllRetailClientsFilteredMessage {
    public GetAllRetailClientsFilteredMessage(string value, long limit, long offset) {
        Value = value;

        Limit = limit <= 0 ? 50 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public string Value { get; set; }

    public long Limit { get; }

    public long Offset { get; }
}