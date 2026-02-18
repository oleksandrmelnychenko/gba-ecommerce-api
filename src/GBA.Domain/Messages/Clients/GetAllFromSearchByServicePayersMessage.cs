namespace GBA.Domain.Messages.Clients;

public sealed class GetAllFromSearchByServicePayersMessage {
    public GetAllFromSearchByServicePayersMessage(string value, long limit, long offset) {
        Value = value;

        Limit = limit;

        Offset = offset;
    }

    public string Value { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }
}