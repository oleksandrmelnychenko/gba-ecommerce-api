using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFreePackLists;

public class GetTaxFreePackListByNetIdMessage {
    public GetTaxFreePackListByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}