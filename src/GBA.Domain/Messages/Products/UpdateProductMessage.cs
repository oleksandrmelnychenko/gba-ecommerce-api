using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products;

public sealed class UpdateProductMessage {
    public UpdateProductMessage(Product product, Guid updatedByNetId, bool descriptionOnly) {
        Product = product;

        UpdatedByNetId = updatedByNetId;

        DescriptionOnly = descriptionOnly;
    }

    public Product Product { get; }

    public Guid UpdatedByNetId { get; }

    public bool DescriptionOnly { get; }
}