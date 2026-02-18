using System.Collections.Generic;
using GBA.Domain.EntityHelpers.TotalDashboards.SupplyInvoices;

namespace GBA.Domain.EntityHelpers.TotalDashboards;

public sealed class TotalDashboard {
    public TotalDashboardItem TotalSales { get; set; }

    public TotalDashboardItem TotalIncomes { get; set; }

    public TotalDashboardItem TotalOutcomes { get; set; }

    public Dictionary<string, decimal> BalanceByCurrency { get; set; }

    public TotalInvoicesItem TotalInvoices { get; set; }
}