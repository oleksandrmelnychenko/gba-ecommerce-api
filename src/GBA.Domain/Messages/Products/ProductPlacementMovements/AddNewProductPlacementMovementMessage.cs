using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductPlacementMovements;

public sealed class AddNewProductPlacementMovementMessage {
    public AddNewProductPlacementMovementMessage(
        ProductPlacementMovement productPlacementMovement,
        Guid userNetId) {
        ProductPlacementMovement = productPlacementMovement;

        UserNetId = userNetId;
    }

    public ProductPlacementMovement ProductPlacementMovement { get; }

    public Guid UserNetId { get; }
}