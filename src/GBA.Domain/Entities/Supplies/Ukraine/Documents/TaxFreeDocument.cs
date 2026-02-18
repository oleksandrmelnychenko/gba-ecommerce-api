namespace GBA.Domain.Entities.Supplies.Ukraine.Documents;

public sealed class TaxFreeDocument : BaseDocument {
    public long TaxFreeId { get; set; }

    public TaxFree TaxFree { get; set; }
}