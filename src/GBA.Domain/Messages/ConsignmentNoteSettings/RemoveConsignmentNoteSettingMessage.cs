using System;

namespace GBA.Domain.Messages.ConsignmentNoteSettings;

public sealed class RemoveConsignmentNoteSettingMessage {
    public RemoveConsignmentNoteSettingMessage(
        bool forReSale,
        Guid netId) {
        ForReSale = forReSale;
        NetId = netId;
    }

    public bool ForReSale { get; }
    public Guid NetId { get; }
}