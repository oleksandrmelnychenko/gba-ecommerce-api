using System.Collections.Generic;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Products;

public sealed class ProductAvailability : EntityBase {
    public ProductAvailability() {
        ProductReservations = new HashSet<ProductReservation>();

        SupplyOrderUkraineCartItemReservations = new HashSet<SupplyOrderUkraineCartItemReservation>();

        ReSaleAvailabilities = new HashSet<ReSaleAvailability>();
    }

    public long ProductId { get; set; }

    public long StorageId { get; set; }

    public double Amount { get; set; }

    public bool IsReSaleAvailability { get; set; }

    public Product Product { get; set; }

    public Storage Storage { get; set; }

    public ICollection<ProductReservation> ProductReservations { get; set; }

    public ICollection<SupplyOrderUkraineCartItemReservation> SupplyOrderUkraineCartItemReservations { get; set; }

    public ICollection<ReSaleAvailability> ReSaleAvailabilities { get; set; }
}