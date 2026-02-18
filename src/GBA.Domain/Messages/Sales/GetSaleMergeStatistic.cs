using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSaleMergeStatistic {
    public GetSaleMergeStatistic(Guid saleNetId) {
        SaleNetId = saleNetId;
    }

    public Guid SaleNetId { get; set; }
}