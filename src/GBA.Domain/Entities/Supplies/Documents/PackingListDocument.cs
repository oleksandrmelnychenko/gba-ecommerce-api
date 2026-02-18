namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class PackingListDocument : BaseDocument {
    public long SupplyOrderId { get; set; }

    public SupplyOrder SupplyOrder { get; set; }
}