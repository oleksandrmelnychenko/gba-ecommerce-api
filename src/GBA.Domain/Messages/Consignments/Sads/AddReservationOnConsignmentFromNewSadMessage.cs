namespace GBA.Domain.Messages.Consignments.Sads;

public sealed class AddReservationOnConsignmentFromNewSadMessage {
    public AddReservationOnConsignmentFromNewSadMessage(long createdSadId, object originalSender) {
        CreatedSadId = createdSadId;

        OriginalSender = originalSender;
    }

    public long CreatedSadId { get; }

    public object OriginalSender { get; }
}