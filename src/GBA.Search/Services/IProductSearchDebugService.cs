using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Models;

namespace GBA.Search.Services;

public interface IProductSearchDebugService {
    Task<ProductSearchDebugResult> SearchDebugAsync(
        string query,
        string locale = "uk",
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default);
}
