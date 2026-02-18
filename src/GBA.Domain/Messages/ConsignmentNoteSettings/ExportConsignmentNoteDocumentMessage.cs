using System;
using GBA.Domain.Entities.ConsignmentNoteSettings;

namespace GBA.Domain.Messages.ConsignmentNoteSettings;

public sealed class ExportConsignmentNoteDocumentMessage {
    public ExportConsignmentNoteDocumentMessage(
        string pathToFolder,
        Guid netId,
        ConsignmentNoteSetting setting,
        bool forReSale) {
        PathToFolder = pathToFolder;
        NetId = netId;
        Setting = setting;
        ForReSale = forReSale;
    }

    public string PathToFolder { get; }
    public Guid NetId { get; }
    public ConsignmentNoteSetting Setting { get; }
    public bool ForReSale { get; }
}