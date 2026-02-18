namespace GBA.Domain.Messages.Storages;

public sealed class SetStoragePriorityMessage {
    public SetStoragePriorityMessage(long storageId, int priority) {
        StorageId = storageId;
        Priority = priority;
    }

    public long StorageId { get; }
    public int Priority { get; }
}