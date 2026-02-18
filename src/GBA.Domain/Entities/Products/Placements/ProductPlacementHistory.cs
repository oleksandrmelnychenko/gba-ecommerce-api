namespace GBA.Domain.Entities.Products;

public sealed class ProductPlacementHistory : EntityBase {
    public string Placement { get; set; }
    public long ProductId { get; set; }
    public Product Product { get; set; }
    public long StorageId { get; set; }
    public Storage Storage { get; set; }
    public double TotalRowsQty { get; set; }
    public double Qty { get; set; }
    public StorageLocationType StorageLocationType { get; set; }
    public AdditionType AdditionType { get; set; }
    public long UserId { get; set; }
    public User User { get; set; }
}