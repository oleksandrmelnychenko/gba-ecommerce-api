using System;

namespace GBA.Domain.Messages.ConsignmentNoteSettings;

public sealed class PrintConsignmentNoteDocumentMessage {
    public PrintConsignmentNoteDocumentMessage(
        string pathToFolder,
        Guid saleNetId,
        Guid settingNetId) {
        PathToFolder = pathToFolder;
        SaleNetId = saleNetId;
        SettingNetId = settingNetId;
    }

    public string PathToFolder { get; }
    public Guid SaleNetId { get; }
    public Guid SettingNetId { get; }
}