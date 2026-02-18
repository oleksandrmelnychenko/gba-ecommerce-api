using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients;

public sealed class AddClientTypeMessage {
    public AddClientTypeMessage(ClientType clientType) {
        ClientType = clientType;
    }

    public ClientType ClientType { get; set; }
}