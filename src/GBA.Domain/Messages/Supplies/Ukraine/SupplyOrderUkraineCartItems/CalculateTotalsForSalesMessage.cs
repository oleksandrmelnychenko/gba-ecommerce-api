using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Supplies.Ukraine.SupplyOrderUkraineCartItems;

public sealed class CalculateTotalsForSalesMessage {
    public CalculateTotalsForSalesMessage(IEnumerable<Sale> sales) {
        Sales = sales;
    }

    public IEnumerable<Sale> Sales { get; }
}