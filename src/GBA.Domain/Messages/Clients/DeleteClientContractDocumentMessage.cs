using System;

namespace GBA.Domain.Messages.Clients;

public sealed class DeleteClientContractDocumentMessage {
    public DeleteClientContractDocumentMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}