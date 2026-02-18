using System;

namespace GBA.Domain.Messages.Sales;

public sealed class UnlockVatSaleByNetIdMessage {
    public UnlockVatSaleByNetIdMessage(Guid saleNetId, Guid userNetId) {
        SaleNetId = saleNetId;

        UserNetId = userNetId;
    }

    public Guid SaleNetId { get; }

    public Guid UserNetId { get; }
}