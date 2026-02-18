namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncOriginalNumber {
    public long ProductCode { get; set; }

    public string OriginalNumber { get; set; }

    public bool IsMainNumber { get; set; }
}