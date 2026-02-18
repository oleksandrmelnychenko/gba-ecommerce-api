using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers;

public sealed class PreviewCartItem {
    public SupplyOrderUkraineCartItem SupplyOrderUkraineCartItem { get; set; }

    public Product Product { get; set; }

    public double Qty { get; set; }

    public double AvailableQty { get; set; }

    public bool HasError { get; set; }

    public bool ZeroAvailable { get; set; }

    public bool LessAvailable { get; set; }

    public bool NoCartItem { get; set; }
}