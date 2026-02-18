namespace GBA.Domain.Entities.Supplies.Ukraine.Documents;

public sealed class SadDocument : BaseDocument {
    public long SadId { get; set; }

    public Sad Sad { get; set; }
}