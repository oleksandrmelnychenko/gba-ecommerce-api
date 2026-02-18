using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients;

public sealed class UpdateClientGroupMessage {
    public UpdateClientGroupMessage(ClientGroup clientGroup) {
        ClientGroup = clientGroup;
    }

    public ClientGroup ClientGroup { get; }
}