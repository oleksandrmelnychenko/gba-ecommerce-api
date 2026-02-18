using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItemReservationProductPlacement : EntityBase {
    public double Qty { get; set; }

    public long ProductPlacementId { get; set; }

    public long SupplyOrderUkraineCartItemReservationId { get; set; }

    public ProductPlacement ProductPlacement { get; set; }

    public SupplyOrderUkraineCartItemReservation SupplyOrderUkraineCartItemReservation { get; set; }
}