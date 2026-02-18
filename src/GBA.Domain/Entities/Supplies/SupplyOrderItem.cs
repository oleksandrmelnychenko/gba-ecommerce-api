using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyOrderItem : EntityBase {
    public SupplyOrderItem() {
        SupplyInvoiceOrderItems = new HashSet<SupplyInvoiceOrderItem>();
    }

    public string StockNo { get; set; }

    public string ItemNo { get; set; }

    public double Qty { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public double GrossWeight { get; set; }

    public double NetWeight { get; set; }

    public string Description { get; set; }

    public long? SupplyOrderId { get; set; }

    public bool IsPacked { get; set; }

    public long ProductId { get; set; }

    public bool IsUpdated { get; set; }

    public SupplyOrder SupplyOrder { get; set; }

    public Product Product { get; set; }

    public ICollection<SupplyInvoiceOrderItem> SupplyInvoiceOrderItems { get; set; }
}