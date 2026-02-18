using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFrees;

public sealed class UploadTaxFreeDocumentsByTaxFreeNetIdMessage {
    public UploadTaxFreeDocumentsByTaxFreeNetIdMessage(Guid netId, List<TaxFreeDocument> taxFreeDocuments) {
        NetId = netId;

        TaxFreeDocuments = taxFreeDocuments;
    }

    public Guid NetId { get; }

    public List<TaxFreeDocument> TaxFreeDocuments { get; }
}