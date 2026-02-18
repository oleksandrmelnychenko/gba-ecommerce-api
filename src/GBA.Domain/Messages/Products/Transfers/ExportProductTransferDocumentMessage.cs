using System;

namespace GBA.Domain.Messages.Products.Transfers;

public sealed class ExportProductTransferDocumentMessage {
    public ExportProductTransferDocumentMessage(
        string pathToFolder,
        Guid netId) {
        PathToFolder = pathToFolder;
        NetId = netId;
    }

    public string PathToFolder { get; }

    public Guid NetId { get; }
}