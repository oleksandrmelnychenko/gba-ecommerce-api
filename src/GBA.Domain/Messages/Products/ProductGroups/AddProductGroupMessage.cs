using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products;

public sealed class AddProductGroupMessage {
    public AddProductGroupMessage(ProductGroup productGroup) {
        ProductGroup = productGroup;
    }

    public ProductGroup ProductGroup { get; set; }
}