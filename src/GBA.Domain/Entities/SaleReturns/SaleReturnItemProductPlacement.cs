using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities.SaleReturns;

public sealed class SaleReturnItemProductPlacement : EntityBase {
    public long? ProductPlacementId { get; set; }
    public ProductPlacement ProductPlacement { get; set; }
    public long SaleReturnItemId { get; set; }
    public double Qty { get; set; }
    public SaleReturnItem SaleReturnItem { get; set; }
}