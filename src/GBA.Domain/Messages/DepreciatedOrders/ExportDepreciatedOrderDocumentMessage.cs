using System;

namespace GBA.Domain.Messages.DepreciatedOrders;

public sealed class ExportDepreciatedOrderDocumentMessage {
    public ExportDepreciatedOrderDocumentMessage(
        string pathToFolder,
        Guid netId) {
        PathToFolder = pathToFolder;
        NetId = netId;
    }

    public string PathToFolder { get; }
    public Guid NetId { get; }
}