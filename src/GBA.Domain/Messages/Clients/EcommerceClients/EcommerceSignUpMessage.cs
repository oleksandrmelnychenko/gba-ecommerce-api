using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients.EcommerceClients;

public sealed class EcommerceSignUpMessage {
    public EcommerceSignUpMessage(Client client, string login, string password, bool isLocalPayment) {
        Client = client;
        Login = login;
        Password = password;
        IsLocalPayment = isLocalPayment;
    }

    public Client Client { get; }
    public string Login { get; }
    public string Password { get; }
    public bool IsLocalPayment { get; }
}