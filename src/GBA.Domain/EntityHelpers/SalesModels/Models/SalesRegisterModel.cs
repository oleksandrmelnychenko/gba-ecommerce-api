using GBA.Domain.Entities.SaleReturns;

namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class SalesRegisterModel {
    public SaleStatistic SaleStatistic { get; set; }

    public SaleReturn SaleReturn { get; set; }

    public int TotalRowsQty { get; set; }
}