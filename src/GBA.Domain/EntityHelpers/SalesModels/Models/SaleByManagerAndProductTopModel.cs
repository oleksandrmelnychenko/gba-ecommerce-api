using System;
using System.Collections.Generic;
using System.Linq;

namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class SaleByManagerAndProductTopModel {
    public SaleByManagerAndProductTopModel() {
        SalesValueByProductTop = new Dictionary<string, decimal>();
    }

    public Guid ManagerNetId { get; set; }

    public string ManagerName { get; set; }

    public decimal TotalValueSales =>
        SalesValueByProductTop.Sum(x => x.Value);

    public Dictionary<string, decimal> SalesValueByProductTop { get; }
}