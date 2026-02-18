using GBA.Domain.Entities.ConsignmentNoteSettings;

namespace GBA.Domain.Messages.ConsignmentNoteSettings;

public sealed class AddConsignmentNoteSettingMessage {
    public AddConsignmentNoteSettingMessage(
        bool forReSale,
        ConsignmentNoteSetting consignmentNoteSetting) {
        ForReSale = forReSale;
        ConsignmentNoteSetting = consignmentNoteSetting;
    }

    public bool ForReSale { get; }
    public ConsignmentNoteSetting ConsignmentNoteSetting { get; }
}