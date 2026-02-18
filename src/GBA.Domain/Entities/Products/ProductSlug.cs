namespace GBA.Domain.Entities.Products;

public sealed class ProductSlug : EntityBase {
    public string Url { get; set; }

    public string Locale { get; set; }

    public long ProductId { get; set; }

    public Product Product { get; set; }
}