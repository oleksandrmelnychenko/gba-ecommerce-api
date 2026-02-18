using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFrees;

public sealed class RemoveTaxFreeDocumentByNetIdMessage {
    public RemoveTaxFreeDocumentByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}