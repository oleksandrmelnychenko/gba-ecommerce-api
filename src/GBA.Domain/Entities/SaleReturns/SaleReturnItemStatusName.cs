namespace GBA.Domain.Entities.SaleReturns;

public sealed class SaleReturnItemStatusName : EntityBase {
    public SaleReturnItemStatus SaleReturnItemStatus { get; set; }

    public string NameUK { get; set; }

    public string NamePL { get; set; }
}