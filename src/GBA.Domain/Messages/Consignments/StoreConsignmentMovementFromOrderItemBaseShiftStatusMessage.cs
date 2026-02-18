namespace GBA.Domain.Messages.Consignments;

public sealed class StoreConsignmentMovementFromOrderItemBaseShiftStatusMessage {
    public StoreConsignmentMovementFromOrderItemBaseShiftStatusMessage(long shiftStatusId) {
        ShiftStatusId = shiftStatusId;
    }

    public long ShiftStatusId { get; }
}