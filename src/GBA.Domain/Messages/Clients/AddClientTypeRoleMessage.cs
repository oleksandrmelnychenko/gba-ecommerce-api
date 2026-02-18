using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients;

public sealed class AddClientTypeRoleMessage {
    public AddClientTypeRoleMessage(ClientTypeRole clientTypeRole) {
        ClientTypeRole = clientTypeRole;
    }

    public ClientTypeRole ClientTypeRole { get; set; }
}