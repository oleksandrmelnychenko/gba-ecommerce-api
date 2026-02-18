using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients;

public sealed class AddClientGroupMessage {
    public AddClientGroupMessage(ClientGroup clientGroup) {
        ClientGroup = clientGroup;
    }

    public ClientGroup ClientGroup { get; }
}