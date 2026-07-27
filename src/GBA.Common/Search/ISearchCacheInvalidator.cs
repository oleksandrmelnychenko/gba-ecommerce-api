using System.Threading;
using System.Threading.Tasks;

namespace GBA.Common.Search;

/// <summary>
/// Invalidates application-level catalog caches after the Elasticsearch projection changes.
/// Search infrastructure depends on this abstraction instead of an ASP.NET cache implementation.
/// </summary>
public interface ISearchCacheInvalidator {
    ValueTask InvalidateProductsAsync(CancellationToken cancellationToken);
}

public sealed class NoOpSearchCacheInvalidator : ISearchCacheInvalidator {
    public ValueTask InvalidateProductsAsync(CancellationToken cancellationToken) {
        return ValueTask.CompletedTask;
    }
}
