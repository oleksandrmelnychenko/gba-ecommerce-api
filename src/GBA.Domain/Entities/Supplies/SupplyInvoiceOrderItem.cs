using System.Collections.Generic;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyInvoiceOrderItem : EntityBase {
    public SupplyInvoiceOrderItem() {
        PackingListPackageOrderItems = new HashSet<PackingListPackageOrderItem>();

        ActReconciliationItems = new HashSet<ActReconciliationItem>();
    }

    public double Qty { get; set; }

    public double Weight { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal GrossUnitPrice { get; set; }

    public long? SupplyOrderItemId { get; set; }

    public long SupplyInvoiceId { get; set; }

    public long ProductId { get; set; }

    public int RowNumber { get; set; }

    public bool ProductIsImported { get; set; }

    public SupplyOrderItem SupplyOrderItem { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public Product Product { get; set; }

    public ProductSpecification ProductSpecification { get; set; }

    public ProductSpecification PlProductSpecification { get; set; }

    public ICollection<PackingListPackageOrderItem> PackingListPackageOrderItems { get; set; }

    public ICollection<ActReconciliationItem> ActReconciliationItems { get; set; }
}