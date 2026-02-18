namespace GBA.Domain.Messages.UserManagement.UserProfiles;

public sealed class GetAllFromSearchMessage {
    public GetAllFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}