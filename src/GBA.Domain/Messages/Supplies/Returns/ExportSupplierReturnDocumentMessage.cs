using System;

namespace GBA.Domain.Messages.Supplies.Returns;

public sealed class ExportSupplierReturnDocumentMessage {
    public ExportSupplierReturnDocumentMessage(
        string pathToFolder,
        Guid netId) {
        PathToFolder = pathToFolder;
        NetId = netId;
    }

    public string PathToFolder { get; }

    public Guid NetId { get; }
}