using System;

namespace GBA.Domain.Messages.SaleReturns;

public sealed class GetSaleReturnByNetIdMessage {
    public GetSaleReturnByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}