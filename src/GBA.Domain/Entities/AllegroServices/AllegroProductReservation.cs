using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities.AllegroServices;

public sealed class AllegroProductReservation : EntityBase {
    public long ProductId { get; set; }

    public double Qty { get; set; }

    public long AllegroItemId { get; set; }

    public Product Product { get; set; }
}