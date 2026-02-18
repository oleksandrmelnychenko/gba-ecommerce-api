using System;

namespace GBA.Domain.Messages.SaleReturns;

public sealed class GetProductQtyForVatStorageMessage {
    public GetProductQtyForVatStorageMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}