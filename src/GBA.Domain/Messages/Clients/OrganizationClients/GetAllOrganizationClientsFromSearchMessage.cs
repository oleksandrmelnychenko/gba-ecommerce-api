namespace GBA.Domain.Messages.Clients.OrganizationClients;

public sealed class GetAllOrganizationClientsFromSearchMessage {
    public GetAllOrganizationClientsFromSearchMessage(string value) {
        Value =
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value;
    }

    public string Value { get; }
}