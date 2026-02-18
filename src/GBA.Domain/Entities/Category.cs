using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities;

public sealed class Category : EntityBase {
    public Category() {
        SubCategories = new HashSet<Category>();

        ProductCategories = new HashSet<ProductCategory>();
    }

    public string Name { get; set; }

    public string Description { get; set; }

    public long? RootCategoryId { get; set; }

    public Category RootCategory { get; set; }

    public ICollection<Category> SubCategories { get; set; }

    public ICollection<ProductCategory> ProductCategories { get; set; }
}