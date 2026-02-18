using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class DeleteSupplyInvoiceByNetIdMessage {
    public DeleteSupplyInvoiceByNetIdMessage(
        Guid netId,
        Guid userNetId) {
        NetId = netId;
        UserNetId = userNetId;
    }

    public Guid NetId { get; }
    public Guid UserNetId { get; }
}