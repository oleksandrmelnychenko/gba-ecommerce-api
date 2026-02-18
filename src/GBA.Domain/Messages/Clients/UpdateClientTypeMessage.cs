using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients;

public sealed class UpdateClientTypeMessage {
    public UpdateClientTypeMessage(ClientType clientType) {
        ClientType = clientType;
    }

    public ClientType ClientType { get; set; }
}