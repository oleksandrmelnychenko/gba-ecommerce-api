namespace GBA.Domain.Entities.Products;

public sealed class ProductOriginalNumber : EntityBase {
    public long OriginalNumberId { get; set; }

    public long ProductId { get; set; }

    public bool IsMainOriginalNumber { get; set; }

    public OriginalNumber OriginalNumber { get; set; }

    public Product Product { get; set; }
}