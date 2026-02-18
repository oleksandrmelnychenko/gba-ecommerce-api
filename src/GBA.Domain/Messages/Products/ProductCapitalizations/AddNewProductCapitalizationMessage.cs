using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductCapitalizations;

public sealed class AddNewProductCapitalizationMessage {
    public AddNewProductCapitalizationMessage(ProductCapitalization productCapitalization, Guid userNetId) {
        ProductCapitalization = productCapitalization;

        UserNetId = userNetId;
    }

    public ProductCapitalization ProductCapitalization { get; }

    public Guid UserNetId { get; }
}