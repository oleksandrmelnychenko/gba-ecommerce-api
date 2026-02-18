using System;

namespace GBA.Domain.Messages.Supplies.Documents;

public sealed class DeleteInvoiceDocumentMessage {
    public DeleteInvoiceDocumentMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}