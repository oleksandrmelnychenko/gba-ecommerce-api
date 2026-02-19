using System;
using System.Collections.Generic;
using System.Linq;
using GBA.Domain.Repositories.Products;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GBA.Services.Services.Products;

public interface IPriceCacheService {
    Dictionary<long, ProductPriceInfo> GetPrices(
        List<long> productIds,
        Guid clientNetId,
        bool withVat,
        string locale,
        Func<List<long>, Dictionary<long, ProductPriceInfo>> fetchFromDb);

    void InvalidateForClient(Guid clientNetId);
}

public sealed class PriceCacheService : IPriceCacheService {
    private readonly IMemoryCache _cache;
    private readonly ILogger<PriceCacheService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public PriceCacheService(IMemoryCache cache, ILogger<PriceCacheService> logger) {
        _cache = cache;
        _logger = logger;
    }

    public Dictionary<long, ProductPriceInfo> GetPrices(
        List<long> productIds,
        Guid clientNetId,
        bool withVat,
        string locale,
        Func<List<long>, Dictionary<long, ProductPriceInfo>> fetchFromDb) {

        if (productIds == null || productIds.Count == 0)
            return new Dictionary<long, ProductPriceInfo>();

        Dictionary<long, ProductPriceInfo> result = new Dictionary<long, ProductPriceInfo>();
        List<long> missingIds = new List<long>();

        // Check cache for each product
        foreach (long productId in productIds) {
            string cacheKey = BuildCacheKey(clientNetId, productId, withVat, locale);
            if (_cache.TryGetValue(cacheKey, out ProductPriceInfo? cachedPrice) && cachedPrice != null) {
                result[productId] = cachedPrice;
            } else {
                missingIds.Add(productId);
            }
        }

        // Fetch missing prices from DB
        if (missingIds.Count > 0) {
            Dictionary<long, ProductPriceInfo> fetchedPrices = fetchFromDb(missingIds);

            foreach (KeyValuePair<long, ProductPriceInfo> kvp in fetchedPrices) {
                result[kvp.Key] = kvp.Value;

                // Cache the fetched price
                string cacheKey = BuildCacheKey(clientNetId, kvp.Key, withVat, locale);
                _cache.Set(cacheKey, kvp.Value, CacheDuration);
            }

            _logger.LogDebug(
                "Price cache: {CacheHits} hits, {CacheMisses} misses for client {ClientNetId}",
                productIds.Count - missingIds.Count, missingIds.Count, clientNetId);
        }

        return result;
    }

    public void InvalidateForClient(Guid clientNetId) {
        // Note: IMemoryCache doesn't support prefix-based invalidation
        // For production with Redis, use SCAN + DEL pattern
        // For now, we rely on TTL-based expiration
        _logger.LogDebug("Cache invalidation requested for client {ClientNetId}", clientNetId);
    }

    private static string BuildCacheKey(Guid clientNetId, long productId, bool withVat, string locale) {
        return $"price:{clientNetId}:{productId}:{(withVat ? 1 : 0)}:{locale}";
    }
}
