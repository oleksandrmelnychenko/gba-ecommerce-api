using System;

namespace GBA.Domain.Messages.Products.Transfers;

public sealed class GetProductTransferByNetIdMessage {
    public GetProductTransferByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}