using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products;

public sealed class UpdateProductGroupWithContentMessage {
    public UpdateProductGroupWithContentMessage(
        ProductGroup productGroup) {
        ProductGroup = productGroup;
    }

    public ProductGroup ProductGroup { get; }
}