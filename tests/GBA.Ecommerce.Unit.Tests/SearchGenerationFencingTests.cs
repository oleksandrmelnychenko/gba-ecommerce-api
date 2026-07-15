using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GBA.Search.Elasticsearch;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class SearchGenerationFencingTests {
    private static readonly PricingDependencyRevisions PricingRevisions = new(
        "product-pricing:db:1",
        "pricing-hierarchy:db:1",
        "discounts:db:1",
        "exchange-rates:db:1");

    [Fact]
    public async Task FullPromotion_RemainsPendingUntilLaterIncrementalPromotion() {
        GenerationDocumentHandler handler = new();
        SearchSyncStateStore store = CreateStore(handler);
        DateTime rebuildStarted = DateTime.UtcNow.AddMinutes(-5);
        DateTime rebuildCompleted = rebuildStarted.AddMinutes(1);

        SearchRebuildLease fullLease = (await store.TryAcquireWriteLeaseAsync(
            TimeSpan.FromMinutes(5)))!;
        fullLease = (await store.BindWriteLeaseConfigurationAsync(
            fullLease,
            "config-v1",
            TimeSpan.FromMinutes(5)))!;
        Assert.True(await store.RenewWriteLeaseAsync(
            fullLease,
            "products_20260714010000000",
            TimeSpan.FromMinutes(5)));
        Assert.NotNull(await store.PromoteGenerationAsync(
            fullLease,
            "products_20260714010000000",
            rebuildStarted,
            EcommercePricingSchema.Version,
            rebuildCompleted,
            expectedConfigurationSignature: null,
            configurationSignature: "config-v1",
            indexedPricingRevisions: PricingRevisions));

        SearchActiveGeneration pending = (await store.GetActiveGenerationAsync())!;
        Assert.Equal(rebuildStarted, pending.State.LastFullRebuildStartedUtc);
        Assert.Null(pending.State.LastIncrementalCatchUpUtc);
        Assert.False(pending.State.HasCompletedRequiredIncrementalCatchUp);

        await store.ReleaseWriteLeaseAsync(fullLease);
        SearchRebuildLease incrementalLease = (await store.TryAcquireWriteLeaseAsync(
            TimeSpan.FromMinutes(5)))!;
        incrementalLease = (await store.BindWriteLeaseConfigurationAsync(
            incrementalLease,
            "config-v1",
            TimeSpan.FromMinutes(5)))!;
        Assert.True(await store.RenewWriteLeaseAsync(
            incrementalLease,
            "products_20260714020000000",
            TimeSpan.FromMinutes(5)));
        DateTime catchUpStarted = DateTime.UtcNow;
        Assert.NotNull(await store.PromoteGenerationAsync(
            incrementalLease,
            "products_20260714020000000",
            catchUpStarted,
            EcommercePricingSchema.Version,
            fullRebuildUtc: null,
            expectedConfigurationSignature: "config-v1",
            configurationSignature: "config-v1",
            indexedPricingRevisions: PricingRevisions));

        SearchActiveGeneration caughtUp = (await store.GetActiveGenerationAsync())!;
        Assert.Equal(rebuildStarted, caughtUp.State.LastFullRebuildStartedUtc);
        Assert.Equal(catchUpStarted, caughtUp.State.LastIncrementalCatchUpUtc);
        Assert.True(caughtUp.State.HasCompletedRequiredIncrementalCatchUp);
    }

    [Fact]
    public async Task EmptyGeneration_TwoReplicas_OnlyOneAcquiresAndPromotesFirstCutover() {
        GenerationDocumentHandler handler = new();
        SearchSyncStateStore first = CreateStore(handler);
        SearchSyncStateStore second = CreateStore(handler);

        SearchRebuildLease?[] leases = await Task.WhenAll(
            first.TryAcquireWriteLeaseAsync(TimeSpan.FromMinutes(5)),
            second.TryAcquireWriteLeaseAsync(TimeSpan.FromMinutes(5)));

        SearchRebuildLease acquiredWinner = Assert.Single(leases, lease => lease != null)!;
        SearchSyncStateStore ownerStore = leases[0] != null ? first : second;
        SearchRebuildLease winner = (await ownerStore.BindWriteLeaseConfigurationAsync(
            acquiredWinner,
            "config-v1",
            TimeSpan.FromMinutes(5)))!;
        Assert.Equal(1, winner.FencingToken);
        Assert.Null(winner.ExpectedActiveIndex);
        Assert.Equal(0, winner.ExpectedGeneration);

        Assert.True(await ownerStore.RenewWriteLeaseAsync(
            winner,
            "products_20260714010000000",
            TimeSpan.FromMinutes(5)));
        SearchGenerationAcknowledgement? acknowledgement = await ownerStore.PromoteGenerationAsync(
            winner,
            "products_20260714010000000",
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            DateTime.UtcNow,
            expectedConfigurationSignature: null,
            configurationSignature: "config-v1",
            indexedPricingRevisions: PricingRevisions);

        Assert.NotNull(acknowledgement);
        Assert.Equal(winner.OwnerId, acknowledgement.OwnerId);
        Assert.Equal(winner.FencingToken, acknowledgement.FencingToken);
        Assert.Equal(1, acknowledgement.Generation);
        SearchActiveGeneration? observed = await second.GetActiveGenerationAsync();
        Assert.Equal("products_20260714010000000", observed!.IndexName);
        Assert.Equal(1, observed.Generation);
        Assert.Equal("config-v1", observed.State.RetailConfigurationSignature);
        Assert.Equal(winner.ConfigurationEpoch, observed.State.RetailConfigurationEpoch);
        Assert.True(PricingRevisions.MatchesExactly(observed.State.IndexedPricingRevisions));
        Assert.True(observed.HasConsistentConfiguration);
    }

    [Fact]
    public async Task PromotedOwner_CanRenewFenceThroughPostPromotionCleanup() {
        GenerationDocumentHandler handler = new();
        SearchSyncStateStore store = CreateStore(handler);
        SearchRebuildLease lease = (await store.TryAcquireWriteLeaseAsync(TimeSpan.FromMinutes(5)))!;
        lease = (await store.BindWriteLeaseConfigurationAsync(
            lease,
            "config-v1",
            TimeSpan.FromMinutes(5)))!;
        const string stagingIndex = "products_20260714010000000";
        Assert.True(await store.RenewWriteLeaseAsync(
            lease,
            stagingIndex,
            TimeSpan.FromMinutes(5)));
        Assert.NotNull(await store.PromoteGenerationAsync(
            lease,
            stagingIndex,
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            DateTime.UtcNow,
            expectedConfigurationSignature: null,
            configurationSignature: "config-v1",
            indexedPricingRevisions: PricingRevisions));

        bool renewed = await store.RenewWriteLeaseAsync(
            lease,
            stagingIndex,
            TimeSpan.FromMinutes(5));

        Assert.True(renewed);
        Assert.True(await store.ValidateWriteLeaseAsync(lease, stagingIndex));
        Assert.Equal(lease.OwnerId, handler.CurrentLeaseOwner);
    }

    [Fact]
    public async Task ExpiredOwner_CannotPromoteOrReleaseReplacementLease() {
        GenerationDocumentHandler handler = new();
        SearchSyncStateStore first = CreateStore(handler);
        SearchSyncStateStore second = CreateStore(handler);
        SearchRebuildLease initial = (await first.TryAcquireWriteLeaseAsync(TimeSpan.FromMinutes(5)))!;
        initial = (await first.BindWriteLeaseConfigurationAsync(
            initial,
            "config-v1",
            TimeSpan.FromMinutes(5)))!;
        Assert.True(await first.RenewWriteLeaseAsync(
            initial,
            "products_20260714010000000",
            TimeSpan.FromMinutes(5)));
        Assert.NotNull(await first.PromoteGenerationAsync(
            initial,
            "products_20260714010000000",
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            DateTime.UtcNow,
            null,
            "config-v1",
            PricingRevisions));
        await first.ReleaseWriteLeaseAsync(initial);

        SearchRebuildLease stale = (await first.TryAcquireWriteLeaseAsync(TimeSpan.FromMinutes(5)))!;
        stale = (await first.BindWriteLeaseConfigurationAsync(
            stale,
            "config-v1",
            TimeSpan.FromMinutes(5)))!;
        Assert.True(await first.RenewWriteLeaseAsync(
            stale,
            "products_20260714020000000",
            TimeSpan.FromMinutes(5)));
        handler.ExpireCurrentLease();
        SearchRebuildLease replacement = (await second.TryAcquireWriteLeaseAsync(TimeSpan.FromMinutes(5)))!;
        replacement = (await second.BindWriteLeaseConfigurationAsync(
            replacement,
            "config-v1",
            TimeSpan.FromMinutes(5)))!;
        Assert.True(replacement.FencingToken > stale.FencingToken);
        Assert.True(await second.RenewWriteLeaseAsync(
            replacement,
            "products_20260714030000000",
            TimeSpan.FromMinutes(5)));
        Assert.False(await first.RenewWriteLeaseAsync(
            stale,
            "products_20260714020000000",
            TimeSpan.FromMinutes(5)));

        SearchGenerationAcknowledgement? staleAck = await first.PromoteGenerationAsync(
            stale,
            "products_20260714020000000",
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            null,
            "config-v1",
            "config-v1",
            PricingRevisions);
        await first.ReleaseWriteLeaseAsync(stale);

        Assert.Null(staleAck);
        Assert.Equal(replacement.OwnerId, handler.CurrentLeaseOwner);
        SearchGenerationAcknowledgement? replacementAck = await second.PromoteGenerationAsync(
            replacement,
            "products_20260714030000000",
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            null,
            "config-v1",
            "config-v1",
            PricingRevisions);
        Assert.Equal(2, replacementAck!.Generation);
    }

    [Fact]
    public async Task NewConfigurationEpoch_FencesExpiredCoordinatorAndPromotesOnlyCurrentVersion() {
        GenerationDocumentHandler handler = new();
        SearchSyncStateStore first = CreateStore(handler);
        SearchSyncStateStore second = CreateStore(handler);

        SearchRebuildLease initial = (await first.TryAcquireWriteLeaseAsync(TimeSpan.FromMinutes(5)))!;
        initial = (await first.BindWriteLeaseConfigurationAsync(
            initial,
            "config-v1",
            TimeSpan.FromMinutes(5)))!;
        Assert.True(await first.RenewWriteLeaseAsync(
            initial,
            "products_20260714010000000",
            TimeSpan.FromMinutes(5)));
        Assert.NotNull(await first.PromoteGenerationAsync(
            initial,
            "products_20260714010000000",
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            DateTime.UtcNow,
            null,
            "config-v1",
            PricingRevisions));
        await first.ReleaseWriteLeaseAsync(initial);

        SearchRebuildLease stale = (await first.TryAcquireWriteLeaseAsync(TimeSpan.FromMinutes(5)))!;
        stale = (await first.BindWriteLeaseConfigurationAsync(
            stale,
            "config-v1",
            TimeSpan.FromMinutes(5)))!;
        Assert.True(await first.RenewWriteLeaseAsync(
            stale,
            "products_20260714020000000",
            TimeSpan.FromMinutes(5)));

        handler.ExpireCurrentLease();
        SearchRebuildLease current = (await second.TryAcquireWriteLeaseAsync(TimeSpan.FromMinutes(5)))!;
        current = (await second.BindWriteLeaseConfigurationAsync(
            current,
            "config-v2",
            TimeSpan.FromMinutes(5)))!;
        Assert.True(current.ConfigurationEpoch > stale.ConfigurationEpoch);
        Assert.True(await second.RenewWriteLeaseAsync(
            current,
            "products_20260714030000000",
            TimeSpan.FromMinutes(5)));

        Assert.False(await first.ValidateWriteLeaseAsync(stale, "products_20260714020000000"));
        Assert.Null(await first.PromoteGenerationAsync(
            stale,
            "products_20260714020000000",
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            null,
            "config-v1",
            "config-v1",
            PricingRevisions));

        SearchGenerationAcknowledgement? acknowledgement = await second.PromoteGenerationAsync(
            current,
            "products_20260714030000000",
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            DateTime.UtcNow,
            "config-v1",
            "config-v2",
            PricingRevisions);
        SearchActiveGeneration active = (await first.GetActiveGenerationAsync())!;

        Assert.NotNull(acknowledgement);
        Assert.Equal(current.ConfigurationEpoch, active.State.RetailConfigurationEpoch);
        Assert.Equal("config-v2", active.State.RetailConfigurationSignature);
        Assert.True(PricingRevisions.MatchesExactly(active.State.IndexedPricingRevisions));
        Assert.True(active.HasConsistentConfiguration);
    }

    [Fact]
    public async Task RealSequenceConflict_IsRetriedWithoutLosingFencingOwnership() {
        GenerationDocumentHandler handler = new();
        SearchSyncStateStore store = CreateStore(handler);
        SearchRebuildLease lease = (await store.TryAcquireWriteLeaseAsync(TimeSpan.FromMinutes(5)))!;
        lease = (await store.BindWriteLeaseConfigurationAsync(
            lease,
            "config-v1",
            TimeSpan.FromMinutes(5)))!;
        handler.ConditionalConflictsRemaining = 1;

        bool renewed = await store.RenewWriteLeaseAsync(
            lease,
            "products_20260714010000000",
            TimeSpan.FromMinutes(5));

        Assert.True(renewed);
        Assert.Equal(lease.OwnerId, handler.CurrentLeaseOwner);
        Assert.True(handler.ConflictResponses >= 1);
    }

    private static SearchSyncStateStore CreateStore(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch/") },
        Options.Create(new ElasticsearchSettings { IndexName = "products" }),
        NullLogger<SearchSyncStateStore>.Instance);

    private sealed class GenerationDocumentHandler : HttpMessageHandler {
        private readonly object sync = new();
        private string? source;
        private long sequenceNumber;
        private const long PrimaryTerm = 1;

        public int ConditionalConflictsRemaining { get; set; }
        public int ConflictResponses { get; private set; }

        public string? CurrentLeaseOwner {
            get {
                lock (sync) {
                    if (source == null) return null;
                    using JsonDocument document = JsonDocument.Parse(source);
                    return document.RootElement.TryGetProperty("leaseOwnerId", out JsonElement owner)
                           && owner.ValueKind == JsonValueKind.String
                        ? owner.GetString()
                        : null;
                }
            }
        }

        public void ExpireCurrentLease() {
            lock (sync) {
                JsonObject body = JsonNode.Parse(source!)!.AsObject();
                body["leaseExpiresAtUtc"] = DateTime.UtcNow.AddMinutes(-1);
                source = body.ToJsonString();
                sequenceNumber++;
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            if (!request.RequestUri!.AbsolutePath.EndsWith(
                    "/generation-control",
                    StringComparison.Ordinal)) {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (request.Method == HttpMethod.Get) {
                lock (sync) {
                    return source == null
                        ? new HttpResponseMessage(HttpStatusCode.NotFound)
                        : JsonResponse(
                            $"{{\"_seq_no\":{sequenceNumber},\"_primary_term\":{PrimaryTerm},\"_source\":{source}}}");
                }
            }

            if (request.Method != HttpMethod.Put) {
                throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
            }

            string nextSource = await request.Content!.ReadAsStringAsync(cancellationToken);
            lock (sync) {
                bool createOnly = request.RequestUri.Query.Contains("op_type=create", StringComparison.Ordinal);
                if (createOnly && source != null) return Conflict();

                if (!createOnly) {
                    long expectedSequence = ReadQueryInt64(request.RequestUri.Query, "if_seq_no");
                    long expectedPrimaryTerm = ReadQueryInt64(request.RequestUri.Query, "if_primary_term");
                    if (expectedSequence != sequenceNumber || expectedPrimaryTerm != PrimaryTerm) {
                        return Conflict();
                    }

                    if (ConditionalConflictsRemaining > 0) {
                        ConditionalConflictsRemaining--;
                        sequenceNumber++;
                        return Conflict();
                    }
                }

                source = nextSource;
                sequenceNumber++;
                return new HttpResponseMessage(createOnly ? HttpStatusCode.Created : HttpStatusCode.OK);
            }
        }

        private HttpResponseMessage Conflict() {
            ConflictResponses++;
            return new HttpResponseMessage(HttpStatusCode.Conflict);
        }

        private static long ReadQueryInt64(string query, string key) {
            foreach (string part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) {
                string[] pair = part.Split('=', 2);
                if (pair.Length == 2
                    && string.Equals(pair[0], key, StringComparison.Ordinal)
                    && long.TryParse(pair[1], out long value)) {
                    return value;
                }
            }

            return long.MinValue;
        }

        private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK) {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}
