using System;

namespace GBA.Domain.Messages.SaleReturns;

public sealed class CancelSaleReturnMessage {
    public CancelSaleReturnMessage(Guid saleReturnNetId, Guid userNetId) {
        SaleReturnNetId = saleReturnNetId;

        UserNetId = userNetId;
    }

    public Guid SaleReturnNetId { get; }

    public Guid UserNetId { get; }
}