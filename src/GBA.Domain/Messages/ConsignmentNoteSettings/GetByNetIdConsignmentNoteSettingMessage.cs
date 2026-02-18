using System;

namespace GBA.Domain.Messages.ConsignmentNoteSettings;

public sealed class GetByNetIdConsignmentNoteSettingMessage {
    public GetByNetIdConsignmentNoteSettingMessage(
        Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}