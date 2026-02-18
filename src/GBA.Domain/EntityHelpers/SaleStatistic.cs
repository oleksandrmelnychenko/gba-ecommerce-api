using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.EntityHelpers;

public sealed class SaleStatistic {
    public Sale Sale { get; set; }

    public List<dynamic> LifeCycleLine { get; set; }

    public List<SaleExchangeRate> SaleExchangeRates { get; set; }
}