using GBA.Domain.Repositories.Products;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class PriceCacheServiceTests {
    [Fact]
    public void ClientInvalidationState_IsBoundedUnderHighClientChurn() {
        using MemoryCache cache = new(new MemoryCacheOptions());
        PriceCacheService service = new(cache, NullLogger<PriceCacheService>.Instance);

        for (int i = 0; i < 10_000; i++) {
            service.InvalidateForClient(Guid.NewGuid());
        }

        FieldInfo field = typeof(PriceCacheService).GetField(
            "_clientInvalidationStates",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Client invalidation state field was not found.");
        object states = field.GetValue(service)
            ?? throw new InvalidOperationException("Client invalidation state was null.");
        int count = (int)(states.GetType().GetProperty("Count")?.GetValue(states)
            ?? throw new InvalidOperationException("Client invalidation state count was unavailable."));

        Assert.InRange(count, 0, 2_048);
    }

    [Fact]
    public void InvalidateForClient_ForcesFreshPricesWithoutAffectingOtherClients() {
        using MemoryCache cache = new(new MemoryCacheOptions());
        PriceCacheService service = new(cache, NullLogger<PriceCacheService>.Instance);
        Guid firstClient = Guid.NewGuid();
        Guid secondClient = Guid.NewGuid();
        ProductPricingContext firstContext = CreateContext(false);
        ProductPricingContext secondContext = CreateContext(false);
        int firstClientFetches = 0;
        int secondClientFetches = 0;

        Dictionary<long, ProductPriceInfo> FirstFetch(List<long> ids) {
            firstClientFetches++;
            return CreatePrices(ids, firstClientFetches * 100m);
        }

        Dictionary<long, ProductPriceInfo> SecondFetch(List<long> ids) {
            secondClientFetches++;
            return CreatePrices(ids, 500m);
        }

        service.GetPrices([42], firstClient, firstContext, "uk", FirstFetch);
        service.GetPrices([42], secondClient, secondContext, "uk", SecondFetch);
        service.InvalidateForClient(firstClient);

        Dictionary<long, ProductPriceInfo> refreshed = service.GetPrices([42], firstClient, firstContext, "uk", FirstFetch);
        service.GetPrices([42], secondClient, secondContext, "uk", SecondFetch);

        Assert.Equal(2, firstClientFetches);
        Assert.Equal(1, secondClientFetches);
        Assert.Equal(200m, refreshed[42].Price);
    }

    [Fact]
    public void SchemaVersion_PreventsLegacyCacheKeysFromSurvivingCutover() {
        using MemoryCache cache = new(new MemoryCacheOptions());
        PriceCacheService service = new(cache, NullLogger<PriceCacheService>.Instance);
        Guid clientNetId = Guid.NewGuid();
        ProductPricingContext pricingContext = CreateContext(false);
        cache.Set($"price:{clientNetId}:42:0:uk", new ProductPriceInfo { Price = 1m });
        int fetches = 0;

        Dictionary<long, ProductPriceInfo> result = service.GetPrices(
            [42],
            clientNetId,
            pricingContext,
            "uk",
            ids => {
                fetches++;
                return CreatePrices(ids, 853m);
            });

        Assert.Equal("source-world-pricing-v4-ct-fenced", EcommercePricingSchema.Version);
        Assert.Equal(1, fetches);
        Assert.Equal(853m, result[42].Price);
    }

    [Theory]
    [InlineData("pricing")]
    [InlineData("currency")]
    [InlineData("source")]
    [InlineData("organization")]
    [InlineData("vat")]
    [InlineData("agreement")]
    [InlineData("definition-version")]
    [InlineData("product-pricing-revision")]
    [InlineData("pricing-hierarchy-revision")]
    [InlineData("discount-revision")]
    [InlineData("exchange-rate-revision")]
    public void MonetaryContextSwitchAndSwitchBack_DoNotReusePrices(string changedDimension) {
        using MemoryCache cache = new(new MemoryCacheOptions());
        PriceCacheService service = new(cache, NullLogger<PriceCacheService>.Instance);
        Guid clientNetId = Guid.NewGuid();
        ProductPricingContext initialContext = CreateContext(false);
        ProductPricingContext switchedContext = ChangeContext(initialContext, changedDimension);
        ProductPricingContext switchedBackContext = initialContext with {
            SelectionVersion = initialContext.SelectionVersion + 1
        };
        int fetches = 0;

        Dictionary<long, ProductPriceInfo> Fetch(List<long> ids) {
            fetches++;
            return CreatePrices(ids, fetches * 100m);
        }

        Dictionary<long, ProductPriceInfo> initial = service.GetPrices([42], clientNetId, initialContext, "uk", Fetch);
        Dictionary<long, ProductPriceInfo> initialCached = service.GetPrices([42], clientNetId, initialContext, "uk", Fetch);
        Dictionary<long, ProductPriceInfo> switched = service.GetPrices([42], clientNetId, switchedContext, "uk", Fetch);
        Dictionary<long, ProductPriceInfo> switchedCached = service.GetPrices([42], clientNetId, switchedContext, "uk", Fetch);
        Dictionary<long, ProductPriceInfo> switchedBack = service.GetPrices([42], clientNetId, switchedBackContext, "uk", Fetch);
        Dictionary<long, ProductPriceInfo> switchedBackCached = service.GetPrices([42], clientNetId, switchedBackContext, "uk", Fetch);

        Assert.Equal(3, fetches);
        Assert.Equal(100m, initial[42].Price);
        Assert.Equal(100m, initialCached[42].Price);
        Assert.Equal(200m, switched[42].Price);
        Assert.Equal(200m, switchedCached[42].Price);
        Assert.Equal(300m, switchedBack[42].Price);
        Assert.Equal(300m, switchedBackCached[42].Price);
    }

    [Fact]
    public void SwitchBackOnAnotherReplica_DoesNotReviveItsOriginalEntry() {
        using MemoryCache firstReplicaCache = new(new MemoryCacheOptions());
        using MemoryCache secondReplicaCache = new(new MemoryCacheOptions());
        PriceCacheService firstReplica = new(firstReplicaCache, NullLogger<PriceCacheService>.Instance);
        PriceCacheService secondReplica = new(secondReplicaCache, NullLogger<PriceCacheService>.Instance);
        Guid clientNetId = Guid.NewGuid();
        ProductPricingContext initialContext = CreateContext(false);
        ProductPricingContext switchedContext = initialContext with {
            ClientAgreementNetId = Guid.NewGuid(),
            SelectionVersion = initialContext.SelectionVersion + 1
        };
        ProductPricingContext switchedBackContext = initialContext with {
            SelectionVersion = initialContext.SelectionVersion + 2
        };
        int firstReplicaFetches = 0;
        int secondReplicaFetches = 0;

        Dictionary<long, ProductPriceInfo> FetchOnFirstReplica(List<long> ids) {
            firstReplicaFetches++;
            return CreatePrices(ids, firstReplicaFetches * 100m);
        }

        Dictionary<long, ProductPriceInfo> FetchOnSecondReplica(List<long> ids) {
            secondReplicaFetches++;
            return CreatePrices(ids, 500m);
        }

        Dictionary<long, ProductPriceInfo> initial = firstReplica.GetPrices(
            [42], clientNetId, initialContext, "uk", FetchOnFirstReplica);
        secondReplica.GetPrices([42], clientNetId, switchedContext, "uk", FetchOnSecondReplica);
        Dictionary<long, ProductPriceInfo> switchedBack = firstReplica.GetPrices(
            [42], clientNetId, switchedBackContext, "uk", FetchOnFirstReplica);

        Assert.Equal(2, firstReplicaFetches);
        Assert.Equal(1, secondReplicaFetches);
        Assert.Equal(100m, initial[42].Price);
        Assert.Equal(200m, switchedBack[42].Price);
    }

    [Theory]
    [InlineData("agreement")]
    [InlineData("organization")]
    [InlineData("source")]
    [InlineData("currency")]
    [InlineData("pricing")]
    [InlineData("selection-version")]
    [InlineData("definition-version")]
    public void IncompletePricingIdentity_FailsClosedWithoutFetching(string missingDimension) {
        using MemoryCache cache = new(new MemoryCacheOptions());
        PriceCacheService service = new(cache, NullLogger<PriceCacheService>.Instance);
        ProductPricingContext invalidContext = InvalidateContext(CreateContext(false), missingDimension);
        bool fetched = false;

        Dictionary<long, ProductPriceInfo> result = service.GetPrices(
            [42],
            Guid.NewGuid(),
            invalidContext,
            "uk",
            ids => {
                fetched = true;
                return CreatePrices(ids, 100m);
            });

        Assert.False(fetched);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("product-pricing-revision")]
    [InlineData("pricing-hierarchy-revision")]
    [InlineData("discount-revision")]
    [InlineData("exchange-rate-revision")]
    public void UnavailableDurableRevision_BypassesCacheAndReadsDatabase(
        string missingRevision) {
        using MemoryCache cache = new(new MemoryCacheOptions());
        PriceCacheService service = new(cache, NullLogger<PriceCacheService>.Instance);
        ProductPricingContext context = InvalidateContext(CreateContext(false), missingRevision);
        int fetches = 0;

        Dictionary<long, ProductPriceInfo> first = service.GetPrices(
            [42],
            Guid.NewGuid(),
            context,
            "uk",
            ids => {
                fetches++;
                return CreatePrices(ids, fetches * 100m);
            });
        Dictionary<long, ProductPriceInfo> second = service.GetPrices(
            [42],
            Guid.NewGuid(),
            context,
            "uk",
            ids => {
                fetches++;
                return CreatePrices(ids, fetches * 100m);
            });

        Assert.Equal(2, fetches);
        Assert.Equal(100m, first[42].Price);
        Assert.Equal(200m, second[42].Price);
    }

    private static ProductPricingContext CreateContext(bool withVat, string source = "fenix") {
        return new ProductPricingContext(
            Guid.NewGuid(),
            17,
            withVat,
            source,
            1,
            1,
            100,
            200,
            ProductPricingRevision: "product-pricing:1",
            PricingHierarchyRevision: "pricing-hierarchy:1",
            DiscountRevision: "discount:1",
            ExchangeRateRevision: "exchange-rate:1");
    }

    private static ProductPricingContext ChangeContext(
        ProductPricingContext context,
        string changedDimension) {
        return changedDimension switch {
            "pricing" => context with { PricingId = context.PricingId.GetValueOrDefault() + 1 },
            "currency" => context with { CurrencyId = context.CurrencyId.GetValueOrDefault() + 1 },
            "source" => context with { Source = "amg" },
            "organization" => context with { OrganizationId = context.OrganizationId + 1 },
            "vat" => context with { WithVat = !context.WithVat },
            "agreement" => context with { ClientAgreementNetId = Guid.NewGuid() },
            "definition-version" => context with { DefinitionVersion = context.DefinitionVersion + 1 },
            "product-pricing-revision" => context with { ProductPricingRevision = "product-pricing:2" },
            "pricing-hierarchy-revision" => context with { PricingHierarchyRevision = "pricing-hierarchy:2" },
            "discount-revision" => context with { DiscountRevision = "discount:2" },
            "exchange-rate-revision" => context with { ExchangeRateRevision = "exchange-rate:2" },
            _ => throw new ArgumentOutOfRangeException(nameof(changedDimension), changedDimension, null)
        };
    }

    private static ProductPricingContext InvalidateContext(
        ProductPricingContext context,
        string missingDimension) {
        return missingDimension switch {
            "agreement" => context with { ClientAgreementNetId = Guid.Empty },
            "organization" => context with { OrganizationId = 0 },
            "source" => context with { Source = "fenix:" },
            "currency" => context with { CurrencyId = null },
            "pricing" => context with { PricingId = null },
            "selection-version" => context with { SelectionVersion = 0 },
            "definition-version" => context with { DefinitionVersion = 0 },
            "product-pricing-revision" => context with { ProductPricingRevision = string.Empty },
            "pricing-hierarchy-revision" => context with { PricingHierarchyRevision = string.Empty },
            "discount-revision" => context with { DiscountRevision = string.Empty },
            "exchange-rate-revision" => context with { ExchangeRateRevision = string.Empty },
            _ => throw new ArgumentOutOfRangeException(nameof(missingDimension), missingDimension, null)
        };
    }

    private static Dictionary<long, ProductPriceInfo> CreatePrices(IEnumerable<long> ids, decimal price) {
        return ids.ToDictionary(
            id => id,
            _ => new ProductPriceInfo { Price = price, CurrencyCode = "UAH" });
    }
}
