namespace GBA.Domain.Messages.Clients;

public sealed class GetAllFromSearchByNameAndRegionCodeMessage {
    public GetAllFromSearchByNameAndRegionCodeMessage(string value) {
        Value = string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    public string Value { get; }
}