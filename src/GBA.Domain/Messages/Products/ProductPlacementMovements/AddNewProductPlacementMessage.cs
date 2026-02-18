using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductPlacementMovements;

public sealed class AddNewProductPlacementMessage {
    public AddNewProductPlacementMessage(ProductPlacement productPlacement) {
        ProductPlacement = productPlacement;
    }

    public ProductPlacement ProductPlacement { get; }
}