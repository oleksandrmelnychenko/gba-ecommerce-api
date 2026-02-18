using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Products;

public sealed class ProductLocation : EntityBase {
    public double Qty { get; set; }
    public double InvoiceDocumentQty { get; set; }

    public long StorageId { get; set; }

    public long ProductPlacementId { get; set; }

    public long? OrderItemId { get; set; }

    public long? DepreciatedOrderItemId { get; set; }

    public long? ProductTransferItemId { get; set; }

    public Storage Storage { get; set; }

    public ProductPlacement ProductPlacement { get; set; }

    public OrderItem OrderItem { get; set; }

    public DepreciatedOrderItem DepreciatedOrderItem { get; set; }

    public ProductTransferItem ProductTransferItem { get; set; }
}