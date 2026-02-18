using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class ExportPZDocumentBySupplyInvoiceNetIdMessage {
    public ExportPZDocumentBySupplyInvoiceNetIdMessage(Guid netId, string pathToFolder) {
        NetId = netId;

        PathToFolder = pathToFolder;
    }

    public Guid NetId { get; }

    public string PathToFolder { get; }
}