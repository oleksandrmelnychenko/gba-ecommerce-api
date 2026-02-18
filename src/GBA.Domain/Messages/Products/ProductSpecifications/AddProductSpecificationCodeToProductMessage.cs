using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products;

public sealed class AddProductSpecificationCodeToProductMessage {
    public AddProductSpecificationCodeToProductMessage(Product product, Guid userNetId) {
        Product = product;

        UserNetId = userNetId;
    }

    public Product Product { get; set; }

    public Guid UserNetId { get; set; }
}