using System.Threading;
using System.Threading.Tasks;

namespace GBA.Search.Sync;

public interface IProductSyncService {
    Task<SyncResult> IncrementalSyncAsync(CancellationToken cancellationToken = default);
    Task<SyncResult> FullRebuildAsync(CancellationToken cancellationToken = default);
    Task EnsureCollectionExistsAsync(string collectionName, CancellationToken cancellationToken = default);
}

public sealed class SyncResult {
    public bool Success { get; set; }
    public int DocumentsIndexed { get; set; }
    public int DocumentsDeleted { get; set; }
    public long ElapsedMs { get; set; }
    public string? ErrorMessage { get; set; }

    public static SyncResult Failed(string error) => new() { Success = false, ErrorMessage = error };
}
