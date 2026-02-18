using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFrees;

public sealed class GetTaxFreeDocumentForPrintingByNetIdMessage {
    public GetTaxFreeDocumentForPrintingByNetIdMessage(Guid netId, string folderPath) {
        NetId = netId;

        FolderPath = folderPath;
    }

    public Guid NetId { get; }

    public string FolderPath { get; }
}