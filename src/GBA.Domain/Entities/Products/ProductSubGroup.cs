namespace GBA.Domain.Entities.Products;

public sealed class ProductSubGroup : EntityBase {
    public long RootProductGroupId { get; set; }

    public long SubProductGroupId { get; set; }

    public ProductGroup RootProductGroup { get; set; }

    public ProductGroup SubProductGroup { get; set; }
}