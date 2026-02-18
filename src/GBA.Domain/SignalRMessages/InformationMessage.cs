namespace GBA.Domain.SignalRMessages;

public sealed class InformationMessage {
    public string Title { get; set; }

    public string Message { get; set; }

    public string CreatedBy { get; set; }

    public string Amount { get; set; }
}