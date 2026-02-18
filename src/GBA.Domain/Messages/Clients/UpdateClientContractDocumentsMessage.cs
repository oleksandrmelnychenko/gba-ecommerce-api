using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients;

public sealed class UpdateClientContractDocumentsMessage {
    public UpdateClientContractDocumentsMessage(Client client) {
        Client = client;
    }

    public Client Client { get; set; }
}