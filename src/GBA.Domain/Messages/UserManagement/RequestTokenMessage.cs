namespace GBA.Domain.Messages.UserManagement;

public sealed class RequestTokenMessage {
    public RequestTokenMessage(string userName, string password) {
        UserName = userName;

        Password = password;
    }

    public string UserName { get; set; }

    public string Password { get; set; }
}