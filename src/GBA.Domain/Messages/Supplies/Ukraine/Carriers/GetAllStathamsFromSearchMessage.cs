namespace GBA.Domain.Messages.Supplies.Ukraine.Carriers;

public sealed class GetAllStathamsFromSearchMessage {
    public GetAllStathamsFromSearchMessage(string value) {
        Value = string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    public string Value { get; }
}