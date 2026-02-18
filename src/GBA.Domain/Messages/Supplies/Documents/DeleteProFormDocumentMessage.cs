using System;

namespace GBA.Domain.Messages.Supplies.Documents;

public sealed class DeleteProFormDocumentMessage {
    public DeleteProFormDocumentMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}