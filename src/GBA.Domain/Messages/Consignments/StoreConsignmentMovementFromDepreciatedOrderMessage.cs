namespace GBA.Domain.Messages.Consignments;

public sealed class StoreConsignmentMovementFromDepreciatedOrderMessage {
    public StoreConsignmentMovementFromDepreciatedOrderMessage(long depreciatedOrderId) {
        DepreciatedOrderId = depreciatedOrderId;
    }

    public long DepreciatedOrderId { get; }
}