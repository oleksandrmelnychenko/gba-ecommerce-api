namespace GBA.Domain.Entities.Products;

public sealed class ProductImage : EntityBase {
    public ProductImage() { }

    public ProductImage(string imageUrl) {
        ImageUrl = imageUrl;
    }

    public string ImageUrl { get; set; }

    public bool IsMainImage { get; set; }

    public long ProductId { get; set; }

    public Product Product { get; set; }
}