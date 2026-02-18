namespace GBA.Domain.Messages.Communications.Hubs;

public sealed class PushDataSyncNotificationMessage {
    public PushDataSyncNotificationMessage(
        string message,
        bool stopProgressBar = false,
        bool isError = false) {
        Message = message;

        StopProgressBar = stopProgressBar;

        IsError = isError;
    }

    public string Message { get; }

    public bool StopProgressBar { get; }

    public bool IsError { get; }
}