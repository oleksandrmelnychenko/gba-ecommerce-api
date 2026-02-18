using System.Collections.Generic;
using GBA.Domain.Entities.ReSales;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class ReSaleItemsWithTotalsModel {
    public IEnumerable<ReSaleItem> ReSaleItems { get; set; }

    public double TotalQty { get; set; }

    public decimal TotalValueWithVat { get; set; }

    public decimal TotalWithExtraValue { get; set; }
}