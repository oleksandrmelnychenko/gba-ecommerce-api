namespace GBA.Domain.Entities.Synchronizations;

public sealed class DataSyncOperation : EntityBase {
    public DataSyncOperationType OperationType { get; set; }

    public long UserId { get; set; }

    public User User { get; set; }

    public bool ForAmg { get; set; }
}