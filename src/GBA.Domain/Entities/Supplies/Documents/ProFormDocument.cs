namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class ProFormDocument : BaseDocument {
    public long SupplyProFormId { get; set; }

    public SupplyProForm SupplyProForm { get; set; }
}