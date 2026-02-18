namespace GBA.Domain.Messages.ConsignmentNoteSettings;

public sealed class GetAllConsignmentNoteSettingsMessage {
    public GetAllConsignmentNoteSettingsMessage(
        bool forReSale) {
        ForReSale = forReSale;
    }

    public bool ForReSale { get; }
}