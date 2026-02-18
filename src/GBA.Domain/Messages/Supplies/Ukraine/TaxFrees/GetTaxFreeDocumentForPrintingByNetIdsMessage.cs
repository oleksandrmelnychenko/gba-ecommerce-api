using System;
using System.Collections.Generic;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFrees;

public sealed class GetTaxFreeDocumentForPrintingByNetIdsMessage {
    public GetTaxFreeDocumentForPrintingByNetIdsMessage(IEnumerable<Guid> netIds, string folderPath) {
        NetIds = netIds;

        FolderPath = folderPath;
    }

    public IEnumerable<Guid> NetIds { get; }

    public string FolderPath { get; }
}