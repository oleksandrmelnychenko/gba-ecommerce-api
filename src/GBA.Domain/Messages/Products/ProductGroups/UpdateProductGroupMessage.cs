using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products;

public sealed class UpdateProductGroupMessage {
    public UpdateProductGroupMessage(ProductGroup productGroup) {
        ProductGroup = productGroup;
    }

    public ProductGroup ProductGroup { get; set; }
}