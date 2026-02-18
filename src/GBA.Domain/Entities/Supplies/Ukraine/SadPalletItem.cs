namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class SadPalletItem : EntityBase {
    public decimal TotalAmount { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public double TotalNetWeight { get; set; }

    public double TotalGrossWeight { get; set; }

    public double Qty { get; set; }

    public long SadItemId { get; set; }

    public long SadPalletId { get; set; }

    public SadItem SadItem { get; set; }

    public SadPallet SadPallet { get; set; }
}