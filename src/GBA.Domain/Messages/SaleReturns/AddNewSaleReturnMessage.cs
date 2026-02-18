using System;
using GBA.Domain.Entities.SaleReturns;

namespace GBA.Domain.Messages.SaleReturns;

public sealed class AddNewSaleReturnMessage {
    public AddNewSaleReturnMessage(SaleReturn saleReturn, Guid userNetId) {
        SaleReturn = saleReturn;

        UserNetId = userNetId;
    }

    public SaleReturn SaleReturn { get; set; }

    public Guid UserNetId { get; }
}