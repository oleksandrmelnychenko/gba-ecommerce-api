using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Messages.Supplies.Documents;

public sealed class AddPackingListDocumentsMessage {
    public AddPackingListDocumentsMessage(
        Guid supplyOrderNetId,
        List<PackingListDocument> packingListDocuments) {
        SupplyOrderNetId = supplyOrderNetId;
        PackingListDocuments = packingListDocuments;
    }

    public Guid SupplyOrderNetId { get; set; }

    public List<PackingListDocument> PackingListDocuments { get; set; }
}