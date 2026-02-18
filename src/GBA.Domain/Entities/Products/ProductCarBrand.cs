namespace GBA.Domain.Entities.Products;

public sealed class ProductCarBrand : EntityBase {
    public long CarBrandId { get; set; }

    public long ProductId { get; set; }

    public CarBrand CarBrand { get; set; }

    public Product Product { get; set; }
}