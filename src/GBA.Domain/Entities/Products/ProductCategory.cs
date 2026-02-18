namespace GBA.Domain.Entities.Products;

public sealed class ProductCategory : EntityBase {
    public long CategoryId { get; set; }

    public long ProductId { get; set; }

    public Category Category { get; set; }

    public Product Product { get; set; }
}