namespace GBA.Search.Sync;

public sealed class SyncResult {
    public bool Success { get; set; }
    public int DocumentsIndexed { get; set; }
    public int DocumentsDeleted { get; set; }
    public long ElapsedMs { get; set; }
    public string? Error { get; set; }

    public static SyncResult Failed(string error) => new() {
        Success = false,
        Error = error
    };
}
