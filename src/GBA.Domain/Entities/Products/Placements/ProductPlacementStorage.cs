namespace GBA.Domain.Entities.Products;

public sealed class ProductPlacementStorage : EntityBase {
    public double Qty { get; set; }
    public string Placement { get; set; }
    public string VendorCode { get; set; }
    public int TotalRowsQty { get; set; }
    public long ProductPlacementId { get; set; }
    public long ProductId { get; set; }
    public long StorageId { get; set; }
    public ProductPlacement ProductPlacement { get; set; }
    public Product Product { get; set; }
    public Storage Storage { get; set; }
    public string ErrorMessage { get; set; }
}