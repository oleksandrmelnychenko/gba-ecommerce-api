namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class SupplyPaymentTaskDocument : BaseDocument {
    public long SupplyPaymentTaskId { get; set; }

    public SupplyPaymentTask SupplyPaymentTask { get; set; }
}