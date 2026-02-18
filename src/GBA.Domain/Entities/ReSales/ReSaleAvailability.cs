using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies.Returns;

namespace GBA.Domain.Entities.ReSales;

public class ReSaleAvailability : EntityBase {
    public ReSaleAvailability() {
        ReSaleItems = new HashSet<ReSaleItem>();

        ProductLocations = new List<ProductLocation>();
    }

    public double Qty { get; set; }

    public double RemainingQty { get; set; }
    public double InvoiceQty { get; set; }

    public long ConsignmentItemId { get; set; }

    public long ProductAvailabilityId { get; set; }

    public long? OrderItemId { get; set; }

    public long? ProductTransferItemId { get; set; }

    public long? DepreciatedOrderItemId { get; set; }

    public long? ProductReservationId { get; set; }

    public long? SupplyReturnItemId { get; set; }

    public decimal PricePerItem { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal PricePerItemWithExtraCharge { get; set; }

    public decimal TotalPriceWithExtraCharge { get; set; }

    public bool IsSelected { get; set; }

    public decimal ExchangeRate { get; set; }

    public double QtyToReSale { get; set; }

    public ConsignmentItem ConsignmentItem { get; set; }

    public ProductTransferItem ProductTransferItem { get; set; }

    public DepreciatedOrderItem DepreciatedOrderItem { get; set; }

    public ProductAvailability ProductAvailability { get; set; }

    public OrderItem OrderItem { get; set; }

    public ProductReservation ProductReservation { get; set; }

    public SupplyReturnItem SupplyReturnItem { get; set; }

    public Product Product { get; set; }

    public ICollection<ReSaleItem> ReSaleItems { get; set; }

    public List<ProductLocation> ProductLocations { get; set; }
}