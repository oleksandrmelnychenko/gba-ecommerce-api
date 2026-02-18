using System.Collections.Generic;

namespace GBA.Domain.Entities.Products;

public sealed class CarBrand : EntityBase {
    public CarBrand() {
        ProductCarBrands = new HashSet<ProductCarBrand>();
    }

    public string Name { get; set; }

    public string Alias { get; set; }

    public string Description { get; set; }

    public string ImageUrl { get; set; }

    public ICollection<ProductCarBrand> ProductCarBrands { get; set; }
}