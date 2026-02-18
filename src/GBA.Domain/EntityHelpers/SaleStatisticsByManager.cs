namespace GBA.Domain.EntityHelpers;

public sealed class SaleStatisticsByManager {
    public int PackagingSalesCount { get; set; }

    public int NewSalesCount { get; set; }

    public int OrderItemsCount { get; set; }

    public double OrderItemsTotalQty { get; set; }

    public decimal OrderItemsTotalAmount { get; set; }
}