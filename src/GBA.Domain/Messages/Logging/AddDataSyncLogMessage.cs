namespace GBA.Domain.Messages.Logging;

public sealed class AddDataSyncLogMessage {
    public AddDataSyncLogMessage(string message, string userFullName, string serializedException) {
        Message = message;

        UserFullName = userFullName;

        SerializedException = serializedException;
    }

    public string Message { get; }

    public string UserFullName { get; }

    public string SerializedException { get; }
}