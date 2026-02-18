namespace GBA.Domain.Messages.Products;

public sealed class GetAllProductGroupsFilteredMessage {
    public GetAllProductGroupsFilteredMessage(
        string value) {
        Value = value ?? string.Empty;
    }

    public string Value { get; }
}