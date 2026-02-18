using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSaleByNetIdMessage {
    public GetSaleByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}