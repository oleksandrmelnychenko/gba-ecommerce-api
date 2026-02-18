using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductSpecifications;

public sealed class AddOrUpdateProductSpecificationMessage {
    public AddOrUpdateProductSpecificationMessage(
        Guid supplyInvoiceNetId,
        Guid sadNetId,
        Guid userNetId,
        ProductSpecification specification) {
        SupplyInvoiceNetId = supplyInvoiceNetId;
        SadNetId = sadNetId;
        UserNetId = userNetId;
        Specification = specification;
    }

    public Guid SupplyInvoiceNetId { get; }
    public Guid SadNetId { get; }
    public Guid UserNetId { get; }
    public ProductSpecification Specification { get; }
}