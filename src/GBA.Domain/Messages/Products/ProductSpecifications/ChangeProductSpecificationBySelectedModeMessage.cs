using System;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductSpecifications;

public sealed class ChangeProductSpecificationBySelectedModeMessage {
    public ChangeProductSpecificationBySelectedModeMessage(
        ProductSpecification specification,
        ProductSpecificationChangeMode specificationChangeMode,
        Guid userNetId) {
        Specification = specification;
        SpecificationChangeMode = specificationChangeMode;
        UserNetId = userNetId;
    }

    public ProductSpecification Specification { get; }
    public ProductSpecificationChangeMode SpecificationChangeMode { get; }
    public Guid UserNetId { get; }
}