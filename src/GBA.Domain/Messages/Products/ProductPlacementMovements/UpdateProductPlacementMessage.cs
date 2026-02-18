using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductPlacementMovements;

public sealed class UpdateProductPlacementMessage {
    public UpdateProductPlacementMessage(List<ProductPlacement> productPlacements, Guid userNetId) {
        ProductPlacements = productPlacements;
        UserNetId = userNetId;
    }

    public List<ProductPlacement> ProductPlacements { get; }
    public Guid UserNetId { get; set; }
}