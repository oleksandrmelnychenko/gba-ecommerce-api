using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.EntityHelpers.Accounting;

public sealed class GroupedPaymentTask {
    public GroupedPaymentTask() {
        SupplyPaymentTasks = new List<SupplyPaymentTask>();

        PriceTotals = new List<PriceTotal>();
    }

    public DateTime PayToDate { get; set; }

    public TaskStatus TaskStatus { get; set; }

    public bool IsFutureTask { get; set; }

    public decimal TotalNetAmount { get; set; }

    public decimal TotalGrossAmount { get; set; }

    public List<SupplyPaymentTask> SupplyPaymentTasks { get; set; }

    public List<PriceTotal> PriceTotals { get; set; }
}