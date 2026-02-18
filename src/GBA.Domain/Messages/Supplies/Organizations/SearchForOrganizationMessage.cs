namespace GBA.Domain.Messages.Supplies;

public sealed class SearchForOrganizationMessage {
    public SearchForOrganizationMessage(string value) {
        Value = !string.IsNullOrEmpty(value) ? value : string.Empty;
    }

    public string Value { get; set; }
}