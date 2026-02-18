using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItemReservation : EntityBase {
    public SupplyOrderUkraineCartItemReservation() {
        SupplyOrderUkraineCartItemReservationProductPlacements = new HashSet<SupplyOrderUkraineCartItemReservationProductPlacement>();
    }

    public double Qty { get; set; }

    public long ProductAvailabilityId { get; set; }

    public long SupplyOrderUkraineCartItemId { get; set; }

    public long? ConsignmentItemId { get; set; }

    public ProductAvailability ProductAvailability { get; set; }

    public SupplyOrderUkraineCartItem SupplyOrderUkraineCartItem { get; set; }

    public ConsignmentItem ConsignmentItem { get; set; }

    public ICollection<SupplyOrderUkraineCartItemReservationProductPlacement> SupplyOrderUkraineCartItemReservationProductPlacements { get; set; }
}