namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class FinishAddOrUpdateSadMessage {
    public FinishAddOrUpdateSadMessage(long sadId) {
        SadId = sadId;
    }

    public long SadId { get; }
}