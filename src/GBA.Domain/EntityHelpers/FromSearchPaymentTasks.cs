using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.EntityHelpers;

public sealed class FromSearchPaymentTasks {
    public FromSearchPaymentTasks() {
        SupplyPaymentTasks = new List<SupplyPaymentTask>();

        Total = decimal.Zero;

        TotalByRange = decimal.Zero;
    }

    public List<SupplyPaymentTask> SupplyPaymentTasks { get; set; }

    public decimal Total { get; set; }

    public decimal TotalByRange { get; set; }
}