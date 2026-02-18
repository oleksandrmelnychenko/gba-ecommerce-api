using System;

namespace GBA.Domain.Messages.Sales;

public sealed class DeleteSaleMessage {
    public DeleteSaleMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}