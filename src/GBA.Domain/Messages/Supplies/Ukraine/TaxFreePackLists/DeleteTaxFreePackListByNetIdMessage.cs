using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFreePackLists;

public sealed class DeleteTaxFreePackListByNetIdMessage {
    public DeleteTaxFreePackListByNetIdMessage(Guid netId, Guid userNetId) {
        NetId = netId;

        UserNetId = userNetId;
    }

    public Guid NetId { get; }

    public Guid UserNetId { get; }
}