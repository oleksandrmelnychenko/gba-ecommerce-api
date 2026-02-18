using System;
using GBA.Domain.Entities.SaleReturns;

namespace GBA.Domain.Messages.SaleReturns;

public sealed class UpdateSaleReturnMessage {
    public UpdateSaleReturnMessage(SaleReturn saleReturn, Guid userNetId) {
        SaleReturn = saleReturn;

        UserNetId = userNetId;
    }

    public SaleReturn SaleReturn { get; }

    public Guid UserNetId { get; }
}