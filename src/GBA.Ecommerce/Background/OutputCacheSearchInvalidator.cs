using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Common.Search;
using GBA.Services.Services.Products;
using Microsoft.AspNetCore.OutputCaching;

namespace GBA.Ecommerce.Background;

public sealed class OutputCacheSearchInvalidator(
    IOutputCacheStore outputCacheStore,
    IPriceCacheService priceCacheService) : ISearchCacheInvalidator {

    public async ValueTask InvalidateProductsAsync(CancellationToken cancellationToken) {
        priceCacheService.InvalidateForClient(Guid.Empty);
        await outputCacheStore.EvictByTagAsync("products", cancellationToken);
    }
}
