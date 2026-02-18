using GBA.Domain.Entities.Products.Incomes;

namespace GBA.Domain.Entities.Products;

public sealed class ProductCapitalizationItem : EntityBase {
    public double Qty { get; set; }

    public double RemainingQty { get; set; }

    public double Weight { get; set; }

    public double TotalNetWeight { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public long ProductId { get; set; }

    public long ProductCapitalizationId { get; set; }

    public Product Product { get; set; }

    public ProductCapitalization ProductCapitalization { get; set; }

    public ProductIncomeItem ProductIncomeItem { get; set; }
}