using GBA.Domain.Repositories.Products;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace GBA.Ecommerce.Api.Tests;

public sealed class PriceCacheServiceTests {
    [Fact]
    public void InvalidateForClient_forces_canonical_prices_to_be_reloaded() {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        PriceCacheService cache = new(
            memoryCache,
            NullLogger<PriceCacheService>.Instance);

        Guid retailClient = Guid.Empty;
        int fetchCount = 0;

        Dictionary<long, ProductPriceInfo> FirstFetch(List<long> ids) {
            fetchCount++;
            return ids.ToDictionary(
                id => id,
                id => new ProductPriceInfo { Price = 42.55m, CurrencyCode = "EUR" });
        }

        Dictionary<long, ProductPriceInfo> UpdatedFetch(List<long> ids) {
            fetchCount++;
            return ids.ToDictionary(
                id => id,
                id => new ProductPriceInfo { Price = 43.10m, CurrencyCode = "EUR" });
        }

        ProductPriceInfo initial = cache.GetPrices(
            [29279710], retailClient, true, "uk", FirstFetch)[29279710];
        ProductPriceInfo cached = cache.GetPrices(
            [29279710], retailClient, true, "uk", UpdatedFetch)[29279710];

        Assert.Equal(42.55m, initial.Price);
        Assert.Equal(42.55m, cached.Price);
        Assert.Equal(1, fetchCount);

        cache.InvalidateForClient(retailClient);

        ProductPriceInfo refreshed = cache.GetPrices(
            [29279710], retailClient, true, "uk", UpdatedFetch)[29279710];

        Assert.Equal(43.10m, refreshed.Price);
        Assert.Equal("EUR", refreshed.CurrencyCode);
        Assert.Equal(2, fetchCount);
    }
}
