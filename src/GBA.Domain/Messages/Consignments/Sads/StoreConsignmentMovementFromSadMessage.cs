namespace GBA.Domain.Messages.Consignments.Sads;

public sealed class StoreConsignmentMovementFromSadMessage {
    public StoreConsignmentMovementFromSadMessage(long sadId) {
        SadId = sadId;
    }

    public long SadId { get; }
}