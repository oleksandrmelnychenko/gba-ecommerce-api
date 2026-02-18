using System;

namespace GBA.Domain.Messages.Clients;

public sealed class UpdateClientPasswordMessage {
    public UpdateClientPasswordMessage(
        Guid clientNetId,
        string password,
        string login,
        string mobileNumber) {
        ClientNetId = clientNetId;

        Password = password;

        Login = login;

        MobileNumber = mobileNumber;
    }

    public Guid ClientNetId { get; }

    public string Password { get; }

    public string Login { get; }
    public string MobileNumber { get; }
}