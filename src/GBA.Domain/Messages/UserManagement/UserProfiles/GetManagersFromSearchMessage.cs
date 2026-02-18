namespace GBA.Domain.Messages.UserManagement.UserProfiles;

public sealed class GetManagersFromSearchMessage {
    public GetManagersFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; }
}