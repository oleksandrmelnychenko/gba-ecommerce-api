using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products;

public sealed class AddProductMessage {
    public AddProductMessage(Product product, Guid createdByNetId) {
        Product = product;

        CreatedByNetId = createdByNetId;
    }

    public Product Product { get; set; }

    public Guid CreatedByNetId { get; }
}