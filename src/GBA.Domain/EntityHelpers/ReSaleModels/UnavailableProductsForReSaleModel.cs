using System.Collections.Generic;
using GBA.Domain.EntityHelpers.ProductModels;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class UnavailableProductsForReSaleModel {
    public UnavailableProductsForReSaleModel(string message, List<ProductWithAvailableQty> products) {
        Message = message;
        Products = products;
    }

    public string Message { get; }
    public List<ProductWithAvailableQty> Products { get; }
}