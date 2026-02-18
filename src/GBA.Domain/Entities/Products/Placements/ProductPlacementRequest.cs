using System.Collections.Generic;

namespace GBA.Domain.Entities.Products;

public class ProductPlacementRequest {
    public List<ProductPlacementStorage> ProductPlacementStorages { get; set; }
    public long StorageId { get; set; }
}