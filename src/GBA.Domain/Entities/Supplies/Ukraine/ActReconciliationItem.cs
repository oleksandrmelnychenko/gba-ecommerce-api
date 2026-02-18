using System.Collections.Generic;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.EntityHelpers.Supplies;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class ActReconciliationItem : EntityBase {
    public ActReconciliationItem() {
        DepreciatedOrderItems = new HashSet<DepreciatedOrderItem>();

        ProductIncomeItems = new HashSet<ProductIncomeItem>();

        ProductTransferItems = new HashSet<ProductTransferItem>();

        Availabilities = new List<ActReconciliationItemStorageAvailability>();
    }

    public bool HasDifference { get; set; }

    public bool NegativeDifference { get; set; }

    public double OrderedQty { get; set; }

    public double ActualQty { get; set; }

    public double QtyDifference { get; set; }

    public double ToOperationQty { get; set; }

    public double NetWeight { get; set; }

    public double TotalNetWeight { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public string CommentUA { get; set; }

    public string CommentPL { get; set; }

    public string Comment { get; set; }

    public string Reason { get; set; }

    public long ProductId { get; set; }

    public long ActReconciliationId { get; set; }

    public long? SupplyOrderUkraineItemId { get; set; }

    public long? SupplyInvoiceOrderItemId { get; set; }

    public Product Product { get; set; }

    public ActReconciliation ActReconciliation { get; set; }

    public SupplyOrderUkraineItem SupplyOrderUkraineItem { get; set; }

    public SupplyInvoiceOrderItem SupplyInvoiceOrderItem { get; set; }

    public ICollection<DepreciatedOrderItem> DepreciatedOrderItems { get; set; }

    public ICollection<ProductIncomeItem> ProductIncomeItems { get; set; }

    public ICollection<ProductTransferItem> ProductTransferItems { get; set; }

    public List<ActReconciliationItemStorageAvailability> Availabilities { get; set; }
}