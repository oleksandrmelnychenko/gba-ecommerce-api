namespace GBA.Domain.Messages.Logging;

public sealed class AddExpiredBillLogMessage {
    public AddExpiredBillLogMessage(string message, string serializedException) {
        Message = message;
        SerializedException = serializedException;
    }

    public string Message { get; }
    public string SerializedException { get; }
}