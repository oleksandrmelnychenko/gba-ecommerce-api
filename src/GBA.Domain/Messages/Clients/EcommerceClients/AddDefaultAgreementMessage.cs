using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients.EcommerceClients;

public sealed class AddDefaultAgreementMessage {
    public AddDefaultAgreementMessage(Client client, bool isLocalPayment) {
        Client = client;
        IsLocalPayment = isLocalPayment;
    }

    public Client Client { get; }
    public bool IsLocalPayment { get; }
}