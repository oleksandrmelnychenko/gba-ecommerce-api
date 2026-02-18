using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.Accounting;

public sealed class GroupedPaymentTaskWithTotals {
    public GroupedPaymentTaskWithTotals() {
        GroupedPaymentTasks = new List<GroupedPaymentTask>();

        PriceTotals = new List<PriceTotal>();
    }

    public List<GroupedPaymentTask> GroupedPaymentTasks { get; set; }

    public List<PriceTotal> PriceTotals { get; set; }

    public decimal TotalGrossPrice { get; set; }
}