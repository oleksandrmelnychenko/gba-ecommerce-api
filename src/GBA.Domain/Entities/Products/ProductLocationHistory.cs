using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Products;

public sealed class ProductLocationHistory : EntityBase {
    public double Qty { get; set; }
    public long StorageId { get; set; }
    public long ProductPlacementId { get; set; }

    public long? OrderItemId { get; set; }

    public long? DepreciatedOrderItemId { get; set; }

    public long? HistoryInvoiceEditId { get; set; }

    public Storage Storage { get; set; }

    public ProductPlacement ProductPlacement { get; set; }

    public OrderItem OrderItem { get; set; }

    public DepreciatedOrderItem DepreciatedOrderItem { get; set; }

    public HistoryInvoiceEdit HistoryInvoiceEdit { get; set; }

    public TypeOfMovement TypeOfMovement { get; set; }
}