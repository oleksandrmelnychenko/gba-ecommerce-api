using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products;

public sealed class AddNewProductSpecificationForCurrentProductMessage {
    public AddNewProductSpecificationForCurrentProductMessage(
        ProductSpecification productSpecification,
        Guid productNetId,
        Guid userNetId
    ) {
        ProductSpecification = productSpecification;

        ProductNetId = productNetId;

        UserNetId = userNetId;
    }

    public ProductSpecification ProductSpecification { get; }

    public Guid ProductNetId { get; }

    public Guid UserNetId { get; }
}