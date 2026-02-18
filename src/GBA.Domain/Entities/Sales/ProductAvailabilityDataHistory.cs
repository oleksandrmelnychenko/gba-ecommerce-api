using System.Collections.Generic;

namespace GBA.Domain.Entities.Sales;

public sealed class ProductAvailabilityDataHistory : EntityBase {
    public ProductAvailabilityDataHistory() {
        ProductPlacementDataHistory = new HashSet<ProductPlacementDataHistory>();
    }

    public ICollection<ProductPlacementDataHistory> ProductPlacementDataHistory { get; set; }
    public double Amount { get; set; }
    public long? StorageId { get; set; }
    public Storage Storage { get; set; }
    public long StockStateStorageID { get; set; }
    public StockStateStorage StockStateStorage { get; set; }
}