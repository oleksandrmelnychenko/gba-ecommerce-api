namespace GBA.Domain.Messages.Measures;

public sealed class GetAllFromSearchMessage {
    public GetAllFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}