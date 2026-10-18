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

    [Fact]
    public void A_catalog_change_invalidates_signed_in_prices_too() {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        PriceCacheService cache = new(memoryCache, NullLogger<PriceCacheService>.Instance);
        Guid client = Guid.NewGuid();
        int fetchCount = 0;
        ProductPriceInfo Fetch(decimal price) => cache.GetPrices(
            [7], client, false, "uk", "agreement", ids => {
                fetchCount++;
                return ids.ToDictionary(id => id, _ => new ProductPriceInfo { Price = price });
            })[7];

        Assert.Equal(10m, Fetch(10m).Price);
        cache.InvalidateForClient(Guid.Empty);
        Assert.Equal(10m, Fetch(11m).Price);
        Assert.Equal(1, fetchCount);

        cache.InvalidateAll();

        Assert.Equal(11m, Fetch(11m).Price);
        Assert.Equal(2, fetchCount);
    }

    [Fact]
    public void Cached_prices_expire_well_before_the_old_half_hour() {
        Assert.True(PriceCacheService.CacheDuration <= TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task The_search_sync_invalidates_every_clients_prices() {
        Moq.Mock<IPriceCacheService> prices = new();
        Moq.Mock<Microsoft.AspNetCore.OutputCaching.IOutputCacheStore> outputCache = new();
        GBA.Ecommerce.Background.OutputCacheSearchInvalidator invalidator = new(outputCache.Object, prices.Object);

        await invalidator.InvalidateProductsAsync(CancellationToken.None);

        prices.Verify(cache => cache.InvalidateAll(), Moq.Times.Once);
        prices.Verify(cache => cache.InvalidateForClient(Moq.It.IsAny<Guid>()), Moq.Times.Never);
    }
}
