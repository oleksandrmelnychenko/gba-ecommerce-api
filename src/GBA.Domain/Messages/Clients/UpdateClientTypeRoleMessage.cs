using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients;

public sealed class UpdateClientTypeRoleMessage {
    public UpdateClientTypeRoleMessage(ClientTypeRole clientTypeRole) {
        ClientTypeRole = clientTypeRole;
    }

    public ClientTypeRole ClientTypeRole { get; set; }
}