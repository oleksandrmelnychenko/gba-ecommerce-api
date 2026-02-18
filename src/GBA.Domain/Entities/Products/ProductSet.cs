namespace GBA.Domain.Entities.Products;

public sealed class ProductSet : EntityBase {
    public long BaseProductId { get; set; }

    public long ComponentProductId { get; set; }

    public int SetComponentsQty { get; set; }

    public Product BaseProduct { get; set; }

    public Product ComponentProduct { get; set; }
}