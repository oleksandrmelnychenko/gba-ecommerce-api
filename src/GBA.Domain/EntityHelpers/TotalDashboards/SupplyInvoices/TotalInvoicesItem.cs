namespace GBA.Domain.EntityHelpers.TotalDashboards.SupplyInvoices;

public sealed class TotalInvoicesItem {
    public double QtyInRoad { get; set; }

    public double QtyInSupplier { get; set; }

    public double TotalQty =>
        QtyInRoad + QtyInSupplier;
}