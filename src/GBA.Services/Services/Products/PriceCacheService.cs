using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Products;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace GBA.Services.Services.Products;

public static class EcommercePricingSchema {
    public const string Version = "source-world-pricing-v4-ct-fenced";
}

public sealed record ProductPricingContext(
    Guid ClientAgreementNetId,
    long OrganizationId,
    bool WithVat,
    string Source,
    long? CurrencyId = null,
    long? PricingId = null,
    long SelectionVersion = 0,
    long DefinitionVersion = 0,
    string ProductPricingRevision = "",
    string PricingHierarchyRevision = "",
    string DiscountRevision = "",
    string ExchangeRateRevision = "") {
    public bool HasDurableDependencyRevisions => !string.IsNullOrWhiteSpace(ProductPricingRevision)
                                                  && !string.IsNullOrWhiteSpace(PricingHierarchyRevision)
                                                  && !string.IsNullOrWhiteSpace(DiscountRevision)
                                                  && !string.IsNullOrWhiteSpace(ExchangeRateRevision);

    public PricingDependencyRevisions DependencyRevisions => new(
        ProductPricingRevision,
        PricingHierarchyRevision,
        DiscountRevision,
        ExchangeRateRevision);
}

public interface IPriceCacheService {
    Dictionary<long, ProductPriceInfo> GetPrices(
        List<long> productIds,
        Guid clientNetId,
        ProductPricingContext pricingContext,
        string locale,
        Func<List<long>, Dictionary<long, ProductPriceInfo>> fetchFromDb);

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
    private readonly object _clientInvalidationSync = new();
    private readonly Dictionary<Guid, ClientInvalidationState> _clientInvalidationStates = new();
    private readonly LinkedList<Guid> _clientInvalidationLru = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private const int MaxClientInvalidationStates = 2_048;

    public PriceCacheService(IMemoryCache cache, ILogger<PriceCacheService> logger) {
        _cache = cache;
        _logger = logger;
    }

    public Dictionary<long, ProductPriceInfo> GetPrices(
        List<long> productIds,
        Guid clientNetId,
        ProductPricingContext pricingContext,
        string locale,
        Func<List<long>, Dictionary<long, ProductPriceInfo>> fetchFromDb) {

        if (productIds == null || productIds.Count == 0)
            return new Dictionary<long, ProductPriceInfo>();

        if (!HasCompletePricingIdentity(pricingContext))
            return new Dictionary<long, ProductPriceInfo>();

        // Change Tracking is an operational prerequisite for cross-replica cache reuse.
        // If it is not configured, preserve correct prices by reading through without caching.
        if (!HasDurableDependencyRevisions(pricingContext))
            return fetchFromDb(productIds);

        if (!TryBuildContextFingerprint(pricingContext, locale, out string contextFingerprint))
            return new Dictionary<long, ProductPriceInfo>();

        Dictionary<long, ProductPriceInfo> result = new();
        List<long> missingIds = new();

        foreach (long productId in productIds) {
            string cacheKey = BuildCacheKey(clientNetId, productId, contextFingerprint);

            if (_cache.TryGetValue(cacheKey, out ProductPriceInfo cachedPrice) && cachedPrice != null) {
                result[productId] = cachedPrice;
            } else {
                missingIds.Add(productId);
            }
        }

        if (missingIds.Count > 0) {
            Dictionary<long, ProductPriceInfo> fetchedPrices = fetchFromDb(missingIds);

            foreach (KeyValuePair<long, ProductPriceInfo> price in fetchedPrices) {
                result[price.Key] = price.Value;

                string cacheKey = BuildCacheKey(clientNetId, price.Key, contextFingerprint);
                SetCachedPrice(clientNetId, cacheKey, price.Value);
            }

            _logger.LogDebug(
                "Price cache: {CacheHits} hits, {CacheMisses} misses for client {ClientNetId}, agreement {ClientAgreementNetId}, source {Source}",
                productIds.Count - missingIds.Count,
                missingIds.Count,
                clientNetId,
                pricingContext.ClientAgreementNetId,
                pricingContext.Source);
        }

        return result;
    }

    public Dictionary<long, ProductPriceInfo> GetPrices(
        List<long> productIds,
        Guid clientNetId,
        bool withVat,
        string locale,
        Func<List<long>, Dictionary<long, ProductPriceInfo>> fetchFromDb) {

        if (productIds == null || productIds.Count == 0)
            return new Dictionary<long, ProductPriceInfo>();

        // The compatibility overload has no agreement/source identity. Caching it
        // could replay a price after the selected agreement changes.
        return fetchFromDb(productIds);
    }

    public void InvalidateForClient(Guid clientNetId) {
        CancellationTokenSource expiredSource;
        lock (_clientInvalidationSync) {
            ClientInvalidationState state = GetOrCreateClientStateLocked(clientNetId);
            expiredSource = state.TokenSource;
            state.TokenSource = new CancellationTokenSource();
        }

        expiredSource.Cancel();
        expiredSource.Dispose();
        _logger.LogDebug("Local price cache entries invalidated for client {ClientNetId}", clientNetId);
    }

    private void SetCachedPrice(Guid clientNetId, string cacheKey, ProductPriceInfo price) {
        lock (_clientInvalidationSync) {
            ClientInvalidationState state = GetOrCreateClientStateLocked(clientNetId);
            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .AddExpirationToken(new CancellationChangeToken(state.TokenSource.Token));
            _cache.Set(cacheKey, price, cacheEntryOptions);
        }
    }

    private ClientInvalidationState GetOrCreateClientStateLocked(Guid clientNetId) {
        if (_clientInvalidationStates.TryGetValue(clientNetId, out ClientInvalidationState existing)) {
            _clientInvalidationLru.Remove(existing.LruNode);
            _clientInvalidationLru.AddLast(existing.LruNode);
            return existing;
        }

        LinkedListNode<Guid> node = _clientInvalidationLru.AddLast(clientNetId);
        ClientInvalidationState state = new(node);
        _clientInvalidationStates.Add(clientNetId, state);

        while (_clientInvalidationStates.Count > MaxClientInvalidationStates) {
            LinkedListNode<Guid> oldestNode = _clientInvalidationLru.First!;
            _clientInvalidationLru.RemoveFirst();
            if (_clientInvalidationStates.Remove(oldestNode.Value, out ClientInvalidationState evicted)) {
                evicted.TokenSource.Cancel();
                evicted.TokenSource.Dispose();
            }
        }

        return state;
    }

    private static string BuildCacheKey(Guid clientNetId, long productId, string contextFingerprint) {
        return $"price:{EcommercePricingSchema.Version}:{clientNetId:N}:{productId}:{contextFingerprint}";
    }

    private static bool TryBuildContextFingerprint(
        ProductPricingContext pricingContext,
        string locale,
        out string fingerprint) {
        fingerprint = null;

        if (!HasCompletePricingIdentity(pricingContext)
            || !HasDurableDependencyRevisions(pricingContext)
            || !TryNormalizeSource(pricingContext.Source, out string source))
            return false;

        string normalizedLocale = string.IsNullOrWhiteSpace(locale)
            ? "unknown"
            : locale.Trim().ToLowerInvariant();

        string canonicalContext = FormattableString.Invariant(
            $"{pricingContext.ClientAgreementNetId:N}|{pricingContext.OrganizationId}|{pricingContext.CurrencyId.Value}|{pricingContext.PricingId.Value}|{(pricingContext.WithVat ? 1 : 0)}|{pricingContext.SelectionVersion}|{pricingContext.DefinitionVersion}|{source.Length}:{source}|{pricingContext.ProductPricingRevision.Length}:{pricingContext.ProductPricingRevision}|{pricingContext.PricingHierarchyRevision.Length}:{pricingContext.PricingHierarchyRevision}|{pricingContext.DiscountRevision.Length}:{pricingContext.DiscountRevision}|{pricingContext.ExchangeRateRevision.Length}:{pricingContext.ExchangeRateRevision}|{normalizedLocale.Length}:{normalizedLocale}");
        byte[] contextHash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalContext));
        fingerprint = Convert.ToHexString(contextHash);
        return true;
    }

    private static bool HasCompletePricingIdentity(ProductPricingContext pricingContext) {
        return pricingContext != null
               && pricingContext.ClientAgreementNetId != Guid.Empty
               && pricingContext.OrganizationId > 0
               && pricingContext.CurrencyId is > 0
               && pricingContext.PricingId is > 0
               && pricingContext.SelectionVersion > 0
               && pricingContext.DefinitionVersion > 0
               && TryNormalizeSource(pricingContext.Source, out _);
    }

    private static bool HasDurableDependencyRevisions(ProductPricingContext pricingContext) {
        return pricingContext?.HasDurableDependencyRevisions == true;
    }

    private static bool TryNormalizeSource(string source, out string normalizedSource) {
        return ProductSourceIdentitySql.TryNormalizeSourceWorld(source, out normalizedSource);
    }

    private sealed class ClientInvalidationState(LinkedListNode<Guid> lruNode) {
        public LinkedListNode<Guid> LruNode { get; } = lruNode;
        public CancellationTokenSource TokenSource { get; set; } = new();
    }
}
