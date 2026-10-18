namespace GBA.Domain.EntityHelpers.ClientShoppingCartModels;

public sealed class ShoppingCartReservation {
    public long Id { get; set; }

    public long ProductAvailabilityId { get; set; }

    public double Qty { get; set; }
}
