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
            [29279710], retailClient, true, "uk", "agreement-v1", FirstFetch)[29279710];
        ProductPriceInfo cached = cache.GetPrices(
            [29279710], retailClient, true, "uk", "agreement-v1", UpdatedFetch)[29279710];

        Assert.Equal(42.55m, initial.Price);
        Assert.Equal(42.55m, cached.Price);
        Assert.Equal(1, fetchCount);

        cache.InvalidateForClient(retailClient);

        ProductPriceInfo refreshed = cache.GetPrices(
            [29279710], retailClient, true, "uk", "agreement-v1", UpdatedFetch)[29279710];

        Assert.Equal(43.10m, refreshed.Price);
        Assert.Equal("EUR", refreshed.CurrencyCode);
        Assert.Equal(2, fetchCount);
    }

    [Fact]
    public void Changed_pricing_context_bypasses_stale_retail_prices() {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        PriceCacheService cache = new(
            memoryCache,
            NullLogger<PriceCacheService>.Instance);

        int fetchCount = 0;
        ProductPriceInfo Fetch(string currency, decimal price) {
            return cache.GetPrices(
                [29279710],
                Guid.Empty,
                true,
                "uk",
                $"agreement:{currency}",
                ids => {
                    fetchCount++;
                    return ids.ToDictionary(
                        id => id,
                        id => new ProductPriceInfo {
                            Price = price,
                            CurrencyCode = currency
                        });
                })[29279710];
        }

        ProductPriceInfo eur = Fetch("EUR", 10m);
        ProductPriceInfo uah = Fetch("UAH", 515m);

        Assert.Equal("EUR", eur.CurrencyCode);
        Assert.Equal("UAH", uah.CurrencyCode);
        Assert.Equal(515m, uah.Price);
        Assert.Equal(2, fetchCount);
    }
}
