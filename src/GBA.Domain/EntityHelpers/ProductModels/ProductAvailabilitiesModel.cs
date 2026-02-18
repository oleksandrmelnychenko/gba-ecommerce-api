using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers.ProductModels;

public sealed class ProductAvailabilitiesModel {
    public double AvailableQtyPl { get; set; }

    public double AvailableQtyPlVAT { get; set; }

    public double AvailableQtyUk { get; set; }

    public double AvailableQtyUkVAT { get; set; }

    public double AvailableQtyUkReSale { get; set; }

    public IEnumerable<ProductAvailability> ProductAvailabilities { get; set; }
}