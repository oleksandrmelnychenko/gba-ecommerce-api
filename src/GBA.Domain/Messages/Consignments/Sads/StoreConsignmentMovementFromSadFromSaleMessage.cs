namespace GBA.Domain.Messages.Consignments.Sads;

public sealed class StoreConsignmentMovementFromSadFromSaleMessage {
    public StoreConsignmentMovementFromSadFromSaleMessage(long sadId) {
        SadId = sadId;
    }

    public long SadId { get; }
}