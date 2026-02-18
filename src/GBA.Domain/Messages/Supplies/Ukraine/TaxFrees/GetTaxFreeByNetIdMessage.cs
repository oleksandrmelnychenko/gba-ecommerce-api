using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFrees;

public sealed class GetTaxFreeByNetIdMessage {
    public GetTaxFreeByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}