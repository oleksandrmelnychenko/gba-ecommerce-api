using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.NumeratorMessages;

public sealed class CountSaleMessage : EntityBase {
    public long SaleId { get; set; }

    public long SaleMessageNumeratorId { get; set; }

    public bool Transfered { get; set; }

    public Sale Sale { get; set; }

    public SaleMessageNumerator SaleMessageNumerator { get; set; }
}