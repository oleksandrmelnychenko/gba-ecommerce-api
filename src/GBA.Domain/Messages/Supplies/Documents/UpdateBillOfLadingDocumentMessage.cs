using System;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Messages.Supplies.Documents;

public sealed class UpdateBillOfLadingDocumentMessage {
    public UpdateBillOfLadingDocumentMessage(Guid supplyOrderNetId, BillOfLadingDocument billOfLadingDocument) {
        SupplyOrderNetId = supplyOrderNetId;
        BillOfLadingDocument = billOfLadingDocument;
    }

    public BillOfLadingDocument BillOfLadingDocument { get; set; }

    public Guid SupplyOrderNetId { get; set; }
}