using GBA.Domain.Entities.Clients.OrganizationClients;

namespace GBA.Domain.Messages.Clients.OrganizationClients;

public sealed class AddNewOrganizationClientMessage {
    public AddNewOrganizationClientMessage(OrganizationClient client) {
        Client = client;
    }

    public OrganizationClient Client { get; }
}