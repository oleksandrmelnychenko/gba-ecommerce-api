using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products;

public sealed class AddNewProductSpecificationForAllProductsMessage {
    public AddNewProductSpecificationForAllProductsMessage(
        ProductSpecification productSpecification,
        string oldCode,
        Guid userNetId
    ) {
        ProductSpecification = productSpecification;

        OldCode = oldCode;

        UserNetId = userNetId;
    }

    public ProductSpecification ProductSpecification { get; }

    public string OldCode { get; }

    public Guid UserNetId { get; }
}