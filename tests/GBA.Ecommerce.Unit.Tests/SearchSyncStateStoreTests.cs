using System.Net;
using System.Text;
using System.Text.Json;
using GBA.Search.Elasticsearch;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class SearchSyncStateStoreTests {
    [Fact]
    public async Task LegacyState_RequiresFullRebuild() {
        DateTime watermark = DateTime.UtcNow.AddMinutes(-1);
        StubHttpMessageHandler handler = new(_ => StateDocumentResponse(
            $"{{\"lastSyncTime\":\"{watermark:O}\"}}"));
        SearchSyncStateStore store = CreateStore(handler);

        SearchSyncState state = await store.GetStateAsync();

        Assert.Equal(watermark, state.WatermarkUtc, TimeSpan.FromMilliseconds(1));
        Assert.True(state.RequiresFullRebuild(EcommercePricingSchema.Version));
    }

    [Fact]
    public async Task CurrentState_IsPersistedWithPricingSchemaVersion() {
        StubHttpMessageHandler handler = new(request => request.Method == HttpMethod.Get
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : new HttpResponseMessage(HttpStatusCode.Created));
        SearchSyncStateStore store = CreateStore(handler);
        DateTime watermark = DateTime.UtcNow;

        bool persisted = await store.SetStateAsync(
            watermark,
            EcommercePricingSchema.Version,
            expectedConfigurationSignature: null,
            configurationSignature: "config-v1");

        Assert.True(persisted);
        HttpRequestMessage request = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Put);
        Assert.Contains("op_type=create", request.RequestUri!.Query, StringComparison.Ordinal);
        string body = await request.Content!.ReadAsStringAsync();
        Assert.Contains($"\"schemaVersion\":\"{EcommercePricingSchema.Version}\"", body, StringComparison.Ordinal);
        Assert.Contains("\"retailConfigurationSignature\":\"config-v1\"", body, StringComparison.Ordinal);
        Assert.Contains("\"lastSyncTime\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FullRebuildState_ResetsWatermarkToSnapshotStartAndRecordsCompletionTime() {
        DateTime newerWatermark = DateTime.UtcNow;
        DateTime requestedWatermark = newerWatermark.AddMinutes(-5);
        DateTime fullRebuildTime = newerWatermark.AddMinutes(1);
        StubHttpMessageHandler handler = new(request => request.Method == HttpMethod.Get
            ? StateDocumentResponse(
                $"{{\"lastSyncTime\":\"{newerWatermark:O}\",\"schemaVersion\":\"old\"}}",
                sequenceNumber: 8,
                primaryTerm: 3)
            : new HttpResponseMessage(HttpStatusCode.OK));
        SearchSyncStateStore store = CreateStore(handler);

        bool persisted = await store.SetFullRebuildStateAsync(
            requestedWatermark,
            EcommercePricingSchema.Version,
            fullRebuildTime,
            expectedConfigurationSignature: null,
            configurationSignature: "config-v2");

        Assert.True(persisted);
        HttpRequestMessage request = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Put);
        Assert.Contains("if_seq_no=8", request.RequestUri!.Query, StringComparison.Ordinal);
        Assert.Contains("if_primary_term=3", request.RequestUri.Query, StringComparison.Ordinal);
        string body = await request.Content!.ReadAsStringAsync();
        Assert.Contains(requestedWatermark.ToString("yyyy-MM-ddTHH:mm:ss"), body, StringComparison.Ordinal);
        Assert.Contains(fullRebuildTime.ToString("yyyy-MM-ddTHH:mm:ss"), body, StringComparison.Ordinal);
        Assert.Contains("\"lastFullRebuildStartedTime\"", body, StringComparison.Ordinal);
        Assert.Contains("\"lastIncrementalCatchUpTime\":null", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IncrementalStartedBeforeCutover_DoesNotOverwriteFullRebuildCheckpoint() {
        DateTime rebuildWatermark = DateTime.UtcNow.AddMinutes(-5);
        DateTime rebuildCompleted = DateTime.UtcNow;
        DateTime staleIncrementalStart = rebuildCompleted.AddSeconds(-10);
        StubHttpMessageHandler handler = new(request => request.Method == HttpMethod.Get
            ? StateDocumentResponse(
                $"{{\"lastSyncTime\":\"{rebuildWatermark:O}\",\"schemaVersion\":\"current\",\"lastFullRebuildTime\":\"{rebuildCompleted:O}\"}}",
                sequenceNumber: 9,
                primaryTerm: 3)
            : new HttpResponseMessage(HttpStatusCode.OK));
        SearchSyncStateStore store = CreateStore(handler);

        bool persisted = await store.SetStateAsync(
            staleIncrementalStart,
            "stale-schema",
            expectedConfigurationSignature: null,
            configurationSignature: "config-v1");

        Assert.True(persisted);
        HttpRequestMessage request = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Put);
        string body = await request.Content!.ReadAsStringAsync();
        Assert.Contains(rebuildWatermark.ToString("yyyy-MM-ddTHH:mm:ss"), body, StringComparison.Ordinal);
        Assert.Contains("\"schemaVersion\":\"current\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("stale-schema", body, StringComparison.Ordinal);
    }

    [Fact]
    public void CurrentFullRebuildProof_DoesNotRequireAnotherFullRebuild() {
        DateTime now = DateTime.UtcNow;
        SearchSyncState state = new(
            now,
            EcommercePricingSchema.Version,
            LastFullRebuildUtc: now.AddMinutes(-1),
            LastFullRebuildStartedUtc: now.AddMinutes(-2));

        Assert.False(state.RequiresFullRebuild(EcommercePricingSchema.Version));
    }

    [Fact]
    public void LegacyStateWithoutRebuildStartProof_RequiresFullRebuild() {
        DateTime now = DateTime.UtcNow;
        SearchSyncState state = new(
            now,
            EcommercePricingSchema.Version,
            LastFullRebuildUtc: now.AddMinutes(-1));

        Assert.True(state.RequiresFullRebuild(EcommercePricingSchema.Version));
        Assert.False(state.HasCompletedRequiredIncrementalCatchUp);
    }

    [Fact]
    public void IncrementalCatchUpMustCompleteAfterFullRebuild() {
        DateTime rebuildStarted = DateTime.UtcNow.AddMinutes(-3);
        DateTime rebuildCompleted = rebuildStarted.AddMinutes(1);
        SearchSyncState pending = new(
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            rebuildCompleted,
            LastFullRebuildStartedUtc: rebuildStarted);
        SearchSyncState caughtUp = pending with {
            LastIncrementalCatchUpUtc = rebuildCompleted.AddMinutes(1)
        };

        Assert.False(pending.HasCompletedRequiredIncrementalCatchUp);
        Assert.True(caughtUp.HasCompletedRequiredIncrementalCatchUp);
    }

    [Fact]
    public void FullRebuildDate_IsSharedThroughState() {
        DateTime fullRebuild = new(2026, 7, 14, 3, 10, 0, DateTimeKind.Utc);
        SearchSyncState state = new(DateTime.UtcNow, EcommercePricingSchema.Version, fullRebuild);

        Assert.True(state.WasFullyRebuiltOn(new DateOnly(2026, 7, 14)));
        Assert.False(state.WasFullyRebuiltOn(new DateOnly(2026, 7, 15)));
    }

    [Fact]
    public async Task ActiveRebuildLease_PreventsAnotherOwnerFromAcquiringIt() {
        DateTime leaseExpiration = DateTime.UtcNow.AddMinutes(2);
        StubHttpMessageHandler handler = new(request => {
            if (request.Method == HttpMethod.Put) {
                return new HttpResponseMessage(HttpStatusCode.Conflict);
            }

            return StateDocumentResponse(
                $"{{\"ownerId\":\"other-owner\",\"leaseExpiresAtUtc\":\"{leaseExpiration:O}\",\"ownedIndex\":\"products_20260714030000000\"}}",
                sequenceNumber: 4,
                primaryTerm: 2);
        });
        SearchSyncStateStore store = CreateStore(handler);

        SearchRebuildLease? lease = await store.TryAcquireRebuildLeaseAsync(TimeSpan.FromMinutes(5));

        Assert.Null(lease);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("op_type=create", handler.Requests[0].RequestUri!.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExpiredRebuildLease_IsReplacedWithConditionalOwnership() {
        DateTime leaseExpiration = DateTime.UtcNow.AddMinutes(-1);
        int putCount = 0;
        StubHttpMessageHandler handler = new(request => {
            if (request.Method == HttpMethod.Put && ++putCount == 1) {
                return new HttpResponseMessage(HttpStatusCode.Conflict);
            }

            if (request.Method == HttpMethod.Get) {
                return StateDocumentResponse(
                    $"{{\"ownerId\":\"expired-owner\",\"leaseExpiresAtUtc\":\"{leaseExpiration:O}\",\"ownedIndex\":\"products_20260714020000000\"}}",
                    sequenceNumber: 11,
                    primaryTerm: 5);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        SearchSyncStateStore store = CreateStore(handler);

        SearchRebuildLease? lease = await store.TryAcquireRebuildLeaseAsync(TimeSpan.FromMinutes(5));

        Assert.NotNull(lease);
        HttpRequestMessage replacement = handler.Requests.Last();
        Assert.Equal(HttpMethod.Put, replacement.Method);
        Assert.Contains("if_seq_no=11", replacement.RequestUri!.Query, StringComparison.Ordinal);
        Assert.Contains("if_primary_term=5", replacement.RequestUri.Query, StringComparison.Ordinal);
        string body = await replacement.Content!.ReadAsStringAsync();
        Assert.Contains(lease.OwnerId, body, StringComparison.Ordinal);
        Assert.DoesNotContain("expired-owner", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LeaseRenewal_DoesNotOverwriteAnotherOwner() {
        DateTime leaseExpiration = DateTime.UtcNow.AddMinutes(2);
        StubHttpMessageHandler handler = new(_ => StateDocumentResponse(
            $"{{\"ownerId\":\"other-owner\",\"leaseExpiresAtUtc\":\"{leaseExpiration:O}\"}}"));
        SearchSyncStateStore store = CreateStore(handler);

        bool renewed = await store.RenewRebuildLeaseAsync(
            new SearchRebuildLease("this-owner"),
            "products_20260714030000000",
            TimeSpan.FromMinutes(5));

        Assert.False(renewed);
        Assert.DoesNotContain(handler.Requests, request => request.Method == HttpMethod.Put);
    }

    [Fact]
    public async Task LeaseRelease_DeletesWithCurrentSequenceAndPrimaryTerm() {
        DateTime leaseExpiration = DateTime.UtcNow.AddMinutes(2);
        StubHttpMessageHandler handler = new(request => request.Method == HttpMethod.Get
            ? StateDocumentResponse(
                $"{{\"ownerId\":\"this-owner\",\"leaseExpiresAtUtc\":\"{leaseExpiration:O}\"}}",
                sequenceNumber: 6,
                primaryTerm: 4)
            : new HttpResponseMessage(HttpStatusCode.OK));
        SearchSyncStateStore store = CreateStore(handler);

        await store.ReleaseRebuildLeaseAsync(new SearchRebuildLease("this-owner"));

        HttpRequestMessage delete = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Delete);
        Assert.Contains("if_seq_no=6", delete.RequestUri!.Query, StringComparison.Ordinal);
        Assert.Contains("if_primary_term=4", delete.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfigurationAcknowledgement_IsDurableAcrossReplicas() {
        DurableStateHandler handler = new();
        SearchSyncStateStore firstReplica = CreateStore(handler);
        SearchSyncStateStore secondReplica = CreateStore(handler);

        bool persisted = await firstReplica.SetStateAsync(
            DateTime.UtcNow,
            EcommercePricingSchema.Version,
            expectedConfigurationSignature: null,
            configurationSignature: "config-shared-v1");
        SearchSyncState observed = await secondReplica.GetStateAsync();

        Assert.True(persisted);
        Assert.Equal("config-shared-v1", observed.RetailConfigurationSignature);
        Assert.Equal(EcommercePricingSchema.Version, observed.SchemaVersion);
    }

    [Fact]
    public async Task StaleReplica_CannotOverwriteNewConfigurationAcknowledgement() {
        DateTime watermark = DateTime.UtcNow;
        StubHttpMessageHandler handler = new(_ => StateDocumentResponse(
            $"{{\"lastSyncTime\":\"{watermark:O}\","
            + $"\"schemaVersion\":\"{EcommercePricingSchema.Version}\","
            + "\"retailConfigurationSignature\":\"config-v2\"}}",
            sequenceNumber: 12,
            primaryTerm: 4));
        SearchSyncStateStore store = CreateStore(handler);

        bool persisted = await store.SetStateAsync(
            watermark.AddMinutes(1),
            EcommercePricingSchema.Version,
            expectedConfigurationSignature: "config-v1",
            configurationSignature: "config-v1");

        Assert.False(persisted);
        Assert.DoesNotContain(handler.Requests, request => request.Method == HttpMethod.Put);
    }

    [Fact]
    public async Task AliasCutoverPhase_IsAtomicallyFencedAgainstNewLeaseOwner() {
        LeaseCutoverHandler handler = new("owner-a", "products_20260714030000000");
        SearchSyncStateStore store = CreateStore(handler);
        SearchRebuildLease owner = new("owner-a");

        bool fenced = await store.BeginAliasCutoverAsync(
            owner,
            "products_20260714030000000");
        SearchRebuildLease? contender = await store.TryAcquireRebuildLeaseAsync(TimeSpan.FromMinutes(5));

        Assert.True(fenced);
        Assert.Null(contender);
        Assert.Equal("cutover", handler.Phase);
        Assert.True(handler.LeaseExpiresAtUtc > DateTime.UtcNow.AddYears(100));
    }

    private static SearchSyncStateStore CreateStore(HttpMessageHandler handler) {
        HttpClient client = new(handler) { BaseAddress = new Uri("http://elasticsearch/") };
        return new SearchSyncStateStore(
            client,
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            NullLogger<SearchSyncStateStore>.Instance);
    }

    private static HttpResponseMessage StateDocumentResponse(
        string source,
        long sequenceNumber = 1,
        long primaryTerm = 1) {
        return JsonResponse(
            $"{{\"_seq_no\":{sequenceNumber},\"_primary_term\":{primaryTerm},\"_source\":{source}}}");
    }

    private static HttpResponseMessage JsonResponse(string body) {
        return new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            Requests.Add(request);
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class DurableStateHandler : HttpMessageHandler {
        private readonly object sync = new();
        private string? source;
        private long sequenceNumber;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            if (request.Method == HttpMethod.Get) {
                lock (sync) {
                    return source == null
                        ? new HttpResponseMessage(HttpStatusCode.NotFound)
                        : StateDocumentResponse(source, sequenceNumber, 1);
                }
            }

            if (request.Method != HttpMethod.Put) {
                throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
            }

            string body = await request.Content!.ReadAsStringAsync(cancellationToken);
            lock (sync) {
                source = body;
                sequenceNumber++;
            }
            return new HttpResponseMessage(HttpStatusCode.Created);
        }
    }

    private sealed class LeaseCutoverHandler : HttpMessageHandler {
        private readonly object sync = new();
        private string ownerId;
        private string ownedIndex;
        private long sequenceNumber = 7;

        public LeaseCutoverHandler(string ownerId, string ownedIndex) {
            this.ownerId = ownerId;
            this.ownedIndex = ownedIndex;
            LeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        }

        public DateTime LeaseExpiresAtUtc { get; private set; }
        public string Phase { get; private set; } = "build";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            if (request.Method == HttpMethod.Get) {
                lock (sync) {
                    return StateDocumentResponse(
                        JsonSerializer.Serialize(new {
                            ownerId,
                            leaseExpiresAtUtc = LeaseExpiresAtUtc,
                            ownedIndex,
                            phase = Phase
                        }),
                        sequenceNumber,
                        1);
                }
            }

            if (request.Method != HttpMethod.Put) {
                throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
            }

            if (request.RequestUri!.Query.Contains("op_type=create", StringComparison.Ordinal)) {
                return new HttpResponseMessage(HttpStatusCode.Conflict);
            }

            string body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            lock (sync) {
                ownerId = root.GetProperty("ownerId").GetString()!;
                ownedIndex = root.GetProperty("ownedIndex").GetString()!;
                LeaseExpiresAtUtc = root.GetProperty("leaseExpiresAtUtc").GetDateTime();
                Phase = root.GetProperty("phase").GetString()!;
                sequenceNumber++;
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
