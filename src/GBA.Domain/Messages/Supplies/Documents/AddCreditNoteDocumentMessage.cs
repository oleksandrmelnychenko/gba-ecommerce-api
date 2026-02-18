using System;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Messages.Supplies.Documents;

public sealed class AddCreditNoteDocumentMessage {
    public AddCreditNoteDocumentMessage(Guid netId, CreditNoteDocument creditNoteDocument) {
        NetId = netId;
        CreditNoteDocument = creditNoteDocument;
    }

    public Guid NetId { get; set; }

    public CreditNoteDocument CreditNoteDocument { get; set; }
}