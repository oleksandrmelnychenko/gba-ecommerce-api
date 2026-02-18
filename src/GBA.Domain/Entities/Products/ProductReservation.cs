using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Products;

public sealed class ProductReservation : EntityBase {
    public ProductReservation() {
        ReSaleAvailabilities = new HashSet<ReSaleAvailability>();
    }

    public double Qty { get; set; }

    public long OrderItemId { get; set; }

    public long ProductAvailabilityId { get; set; }

    public long? ConsignmentItemId { get; set; }

    public bool IsReSaleReservation { get; set; }

    public string RegionCode { get; set; }

    public OrderItem OrderItem { get; set; }

    public ProductAvailability ProductAvailability { get; set; }

    public ConsignmentItem ConsignmentItem { get; set; }

    public ICollection<ReSaleAvailability> ReSaleAvailabilities { get; set; }
}