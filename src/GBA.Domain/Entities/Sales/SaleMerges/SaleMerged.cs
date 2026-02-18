namespace GBA.Domain.Entities.Sales.SaleMerges;

public sealed class SaleMerged : EntityBase {
    public long OutputSaleId { get; set; }

    public long InputSaleId { get; set; }

    public Sale OutputSale { get; set; }

    public Sale InputSale { get; set; }
}