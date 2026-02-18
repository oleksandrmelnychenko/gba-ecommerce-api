using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Models;

namespace GBA.Search.Services;

public interface IProductSearchService {
    Task<ProductSearchResult> SearchAsync(
        string query,
        string locale = "uk",
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default);

    Task<ProductSearchResultWithDocs> SearchWithDocsAsync(
        string query,
        string locale = "uk",
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
