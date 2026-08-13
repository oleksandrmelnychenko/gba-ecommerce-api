using System.Net;
using System.Text.Json;
using GBA.Common.Search;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Ecommerce.Controllers;
using GBA.Ecommerce.HealthChecks;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using GBA.Search.Models;
using GBA.Search.Sync;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ElasticsearchSyncSafetyTests {
    [Fact]
    public async Task IncrementalSync_TransientTimeout_RetriesAndReleasesGate() {
        TestProductSyncRepository repository = new() {
            GetChangedProductIds = _ => Task.FromResult(new List<long> { 42 }),
            GetProductsByIds = _ => Task.FromResult(new List<ProductSyncData> {
                CreateProduct(42)
            })
        };
        SuccessfulElasticsearchHandler handler = new() {
            TimeoutResponsesRemaining = 2
        };
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1)
        };
        CollectingLogger<ElasticsearchSyncService> logger = new();
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            logger,
            maxRetries: 2,
            handler);

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.True(result.Success);
        Assert.Equal(3, handler.BulkCalls);
        Assert.Equal(1, state.SetCalls);
        Assert.Equal(2, logger.Count(LogLevel.Warning));
        Assert.Equal(0, logger.Count(LogLevel.Error));
    }

    [Fact]
    public async Task IncrementalSync_ExhaustedTimeout_EscalatesToError() {
        TestProductSyncRepository repository = new() {
            GetChangedProductIds = _ => Task.FromResult(new List<long> { 42 }),
            GetProductsByIds = _ => Task.FromResult(new List<ProductSyncData> {
                CreateProduct(42)
            })
        };
        SuccessfulElasticsearchHandler handler = new() {
            TimeoutResponsesRemaining = 2
        };
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1)
        };
        CollectingLogger<ElasticsearchSyncService> logger = new();
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            logger,
            maxRetries: 1,
            handler);

        SyncResult result = await service.IncrementalSyncAsync();
        SyncResult recovery = await service.IncrementalSyncAsync();

        Assert.False(result.Success);
        Assert.True(recovery.Success);
        Assert.Equal(3, handler.BulkCalls);
        Assert.Equal(1, state.SetCalls);
        Assert.Equal(1, logger.Count(LogLevel.Warning));
        Assert.Equal(1, logger.Count(LogLevel.Error));
    }

    [Fact]
    public async Task IncrementalSync_NonHttpTaskCancellation_IsNotDowngradedOrRetried() {
        int calls = 0;
        TestProductSyncRepository repository = new() {
            GetChangedProductIds = _ => {
                calls++;
                return Task.FromException<List<long>>(
                    new TaskCanceledException("foreign cancellation"));
            }
        };
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1)
        };
        CollectingLogger<ElasticsearchSyncService> logger = new();
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            logger,
            maxRetries: 3);

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.False(result.Success);
        Assert.Equal(1, calls);
        Assert.Equal(0, logger.Count(LogLevel.Warning));
        Assert.Equal(1, logger.Count(LogLevel.Error));
    }

    [Fact]
    public async Task IncrementalSync_FullRebuildFallback_PropagatesCallerCancellation() {
        TestSearchSyncStateStore state = new() { Watermark = DateTime.MinValue };
        ElasticsearchSyncService service = CreateSyncService(
            new TestProductSyncRepository(),
            state,
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 3);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.IncrementalSyncAsync(cancellation.Token));

        Assert.Equal(0, state.SetCalls);
    }

    [Fact]
    public async Task IncrementalSync_CacheFailure_DoesNotAdvanceWatermarkAndNextRunRecovers() {
        TestProductSyncRepository repository = new() {
            GetDeletedProductIds = _ => Task.FromResult(new List<long> { 42 })
        };
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1)
        };
        TestSearchCacheInvalidator cache = new() { FailuresRemaining = 1 };
        CollectingLogger<ElasticsearchSyncService> logger = new();
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            cache,
            logger,
            maxRetries: 0);

        SyncResult first = await service.IncrementalSyncAsync();
        SyncResult second = await service.IncrementalSyncAsync();

        Assert.False(first.Success);
        Assert.True(second.Success);
        Assert.Equal(2, cache.Calls);
        Assert.Equal(1, state.SetCalls);
        Assert.Equal(2, repository.DeletedCalls);
    }

    [Fact]
    public async Task SearchHealthProbe_StaleWatermark_IsNotReady() {
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-10)
        };
        SearchSyncHealthProbe probe = new(
            new TestElasticsearchIndexService { Healthy = true },
            state,
            Options.Create(new SyncSettings { LagWarningSeconds = 60 }),
            new CollectingLogger<SearchSyncHealthProbe>());

        SearchSyncHealthSnapshot snapshot = await probe.GetSnapshotAsync();

        Assert.True(snapshot.Healthy);
        Assert.False(snapshot.Ready);
        Assert.True(snapshot.Stale);
    }

    [Fact]
    public async Task SearchHealthProbe_MissingProductIndex_IsNotReady() {
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow
        };
        SearchSyncHealthProbe probe = new(
            new TestElasticsearchIndexService {
                Healthy = true,
                IndexExists = false
            },
            state,
            Options.Create(new SyncSettings { LagWarningSeconds = 60 }),
            new CollectingLogger<SearchSyncHealthProbe>());

        SearchSyncHealthSnapshot snapshot = await probe.GetSnapshotAsync();

        Assert.True(snapshot.Healthy);
        Assert.False(snapshot.IndexExists);
        Assert.False(snapshot.Ready);
        Assert.False(snapshot.Stale);
    }

    [Fact]
    public async Task ElasticsearchIndexHealth_RedCluster_IsUnhealthy() {
        HttpClient http = new(new StaticJsonHandler(
            "{\"status\":\"red\",\"timed_out\":false}")) {
            BaseAddress = new Uri("http://elasticsearch/")
        };
        ElasticsearchIndexService service = new(
            http,
            Options.Create(new ElasticsearchSettings()),
            new CollectingLogger<ElasticsearchIndexService>());

        bool healthy = await service.IsHealthyAsync();

        Assert.False(healthy);
    }

    [Fact]
    public async Task ElasticsearchHealth_UnreadyReturnsConsistent503AndLowerCamelBody() {
        TestElasticsearchIndexService indexService = new() {
            Healthy = true,
            IndexExists = false
        };
        SearchSyncHealthProbe probe = new(
            indexService,
            new TestSearchSyncStateStore { Watermark = DateTime.UtcNow },
            Options.Create(new SyncSettings { LagWarningSeconds = 60 }),
            new CollectingLogger<SearchSyncHealthProbe>());
        ElasticsearchController controller = CreateController(
            indexService,
            Mock.Of<IElasticsearchSyncService>(),
            probe);

        ObjectResult result = Assert.IsType<ObjectResult>(
            await controller.HealthAsync(CancellationToken.None));
        IWebResponse envelope = Assert.IsAssignableFrom<IWebResponse>(result.Value);
        using JsonDocument body = JsonDocument.Parse(JsonSerializer.Serialize(
            envelope.Body,
            new JsonSerializerOptions { PropertyNamingPolicy = null }));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, envelope.StatusCode);
        Assert.True(body.RootElement.TryGetProperty("healthy", out _));
        Assert.True(body.RootElement.TryGetProperty("indexExists", out _));
        Assert.False(body.RootElement.TryGetProperty("Healthy", out _));
    }

    [Fact]
    public async Task IncrementalSyncFailure_ReturnsConsistent503Envelope() {
        Mock<IElasticsearchSyncService> syncService = new();
        syncService
            .Setup(service => service.IncrementalSyncAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SyncResult.Failed("timeout"));
        TestElasticsearchIndexService indexService = new();
        SearchSyncHealthProbe probe = new(
            indexService,
            new TestSearchSyncStateStore { Watermark = DateTime.UtcNow },
            Options.Create(new SyncSettings { LagWarningSeconds = 60 }),
            new CollectingLogger<SearchSyncHealthProbe>());
        ElasticsearchController controller = CreateController(
            indexService,
            syncService.Object,
            probe);

        ObjectResult result = Assert.IsType<ObjectResult>(
            await controller.IncrementalSyncAsync(CancellationToken.None));
        IWebResponse envelope = Assert.IsAssignableFrom<IWebResponse>(result.Value);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, envelope.StatusCode);
        Assert.IsType<SyncResult>(envelope.Body);
    }

    [Fact]
    public async Task SearchIndexHealthCheck_MissingIndex_IsUnhealthy() {
        SearchSyncHealthProbe probe = new(
            new TestElasticsearchIndexService {
                Healthy = true,
                IndexExists = false
            },
            new TestSearchSyncStateStore { Watermark = DateTime.UtcNow },
            Options.Create(new SyncSettings { LagWarningSeconds = 60 }),
            new CollectingLogger<SearchSyncHealthProbe>());
        SearchIndexHealthCheck healthCheck = new(probe);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task SearchIndexHealthCheck_StaleSuccessfulSync_IsDegraded() {
        SearchSyncHealthProbe probe = new(
            new TestElasticsearchIndexService {
                Healthy = true,
                IndexExists = true
            },
            new TestSearchSyncStateStore {
                Watermark = DateTime.UtcNow.AddMinutes(-10)
            },
            Options.Create(new SyncSettings { LagWarningSeconds = 60 }),
            new CollectingLogger<SearchSyncHealthProbe>());
        SearchIndexHealthCheck healthCheck = new(probe);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("sync is stale", result.Description);
        Assert.Equal(true, result.Data["stale"]);
    }

    [Fact]
    public async Task SearchIndexHealthCheck_FreshSuccessfulSync_IsHealthy() {
        SearchSyncHealthProbe probe = new(
            new TestElasticsearchIndexService {
                Healthy = true,
                IndexExists = true
            },
            new TestSearchSyncStateStore { Watermark = DateTime.UtcNow },
            Options.Create(new SyncSettings { LagWarningSeconds = 60 }),
            new CollectingLogger<SearchSyncHealthProbe>());
        SearchIndexHealthCheck healthCheck = new(probe);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Null(result.Description);
        Assert.Equal(false, result.Data["stale"]);
    }

    [Fact]
    public async Task SearchIndexHealthCheck_NoSuccessfulSync_IsUnhealthy() {
        SearchSyncHealthProbe probe = new(
            new TestElasticsearchIndexService {
                Healthy = true,
                IndexExists = true
            },
            new TestSearchSyncStateStore { Watermark = DateTime.MinValue },
            Options.Create(new SyncSettings { LagWarningSeconds = 60 }),
            new CollectingLogger<SearchSyncHealthProbe>());
        SearchIndexHealthCheck healthCheck = new(probe);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("no successful watermark", result.Description);
    }

    [Fact]
    public void TimeoutRetryDelay_IsExponentialAndBounded() {
        Assert.Equal(
            TimeSpan.FromMilliseconds(100),
            ElasticsearchSyncService.GetTimeoutRetryDelay(1, 100));
        Assert.Equal(
            TimeSpan.FromMilliseconds(400),
            ElasticsearchSyncService.GetTimeoutRetryDelay(3, 100));
        Assert.Equal(
            TimeSpan.FromSeconds(30),
            ElasticsearchSyncService.GetTimeoutRetryDelay(10, 1000));
    }

    [Fact]
    public void PartitionBulkItems_UsesConfiguredBatchSize() {
        int[] items = Enumerable.Range(1, 2501).ToArray();

        int[][] batches = ElasticsearchSyncService
            .PartitionBulkItems(items, 1000)
            .ToArray();

        Assert.Equal([1000, 1000, 501], batches.Select(batch => batch.Length));
        Assert.Equal(items, batches.SelectMany(batch => batch));
    }

    [Fact]
    public void EnsureSuccessfulHttpResponse_ThrowsForEmptyErrorResponse() {
        using HttpResponseMessage response = new(HttpStatusCode.RequestEntityTooLarge);

        HttpRequestException exception = Assert.Throws<HttpRequestException>(() =>
            ElasticsearchSyncService.EnsureSuccessfulHttpResponse(
                response,
                "bulk index",
                string.Empty));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.StatusCode);
        Assert.Contains("HTTP 413", exception.Message);
    }

    [Fact]
    public void ValidateBulkResponse_ThrowsWhenAnyIndexItemFails() {
        const string response = """
            {
              "errors": true,
              "items": [
                { "index": { "status": 201 } },
                {
                  "index": {
                    "status": 400,
                    "error": { "type": "mapper_parsing_exception", "reason": "bad document" }
                  }
                }
              ]
            }
            """;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ElasticsearchSyncService.ValidateBulkResponse(response, "index", 2));

        Assert.Contains("1/2", exception.Message);
        Assert.Contains("mapper_parsing_exception", exception.Message);
    }

    [Fact]
    public void ValidateBulkResponse_AllowsIdempotentMissingDelete() {
        const string response = """
            {
              "errors": true,
              "items": [
                { "delete": { "status": 200 } },
                { "delete": { "status": 404, "result": "not_found" } }
              ]
            }
            """;

        int processed = ElasticsearchSyncService.ValidateBulkResponse(
            response,
            "delete",
            2,
            allowNotFound: true);

        Assert.Equal(2, processed);
    }

    private static ElasticsearchSyncService CreateSyncService(
        TestProductSyncRepository repository,
        TestSearchSyncStateStore state,
        TestSearchCacheInvalidator cache,
        CollectingLogger<ElasticsearchSyncService> logger,
        int maxRetries,
        SuccessfulElasticsearchHandler? handler = null) {
        HttpClient http = new(handler ?? new SuccessfulElasticsearchHandler()) {
            BaseAddress = new Uri("http://elasticsearch/")
        };

        return new ElasticsearchSyncService(
            http,
            Options.Create(new ElasticsearchSettings {
                IndexName = "products",
                MaxRetries = maxRetries,
                RetryBaseDelayMilliseconds = 1
            }),
            Options.Create(new SyncSettings {
                BatchSize = 1000,
                LagWarningSeconds = 60
            }),
            repository,
            new TestElasticsearchIndexService(),
            state,
            cache,
            logger);
    }

    private static ProductSyncData CreateProduct(long id) => new() {
        Id = id,
        NetUid = Guid.NewGuid(),
        VendorCode = $"SKU-{id}",
        Name = $"Product {id}",
        IsForWeb = true,
        IsForSale = true,
        Updated = DateTime.UtcNow
    };

    private static ElasticsearchController CreateController(
        IElasticsearchIndexService indexService,
        IElasticsearchSyncService syncService,
        SearchSyncHealthProbe probe) => new(
        indexService,
        syncService,
        Mock.Of<IElasticsearchProductSearchService>(),
        probe,
        Mock.Of<IOutputCacheStore>(),
        new ResponseFactory());

    private sealed class TestProductSyncRepository : IProductSyncRepository {
        public Func<DateTime, Task<List<long>>> GetChangedProductIds { get; set; } =
            _ => Task.FromResult(new List<long>());

        public Func<DateTime, Task<List<long>>> GetDeletedProductIds { get; set; } =
            _ => Task.FromResult(new List<long>());

        public Func<IReadOnlyCollection<long>, Task<List<ProductSyncData>>>
            GetProductsByIds { get; set; } =
            _ => Task.FromResult(new List<ProductSyncData>());

        public int DeletedCalls { get; private set; }

        public Task<List<ProductSyncData>> GetAllProductsAsync() =>
            Task.FromResult(new List<ProductSyncData>());

        public Task<List<long>> GetChangedProductIdsAsync(DateTime since) =>
            GetChangedProductIds(since);

        public Task<List<ProductSyncData>> GetProductsByIdsAsync(
            IReadOnlyCollection<long> ids) =>
            GetProductsByIds(ids);

        public Task<List<long>> GetDeletedProductIdsAsync(DateTime since) {
            DeletedCalls++;
            return GetDeletedProductIds(since);
        }

        public Task<Dictionary<long, List<string>>> GetOriginalNumbersForProductsAsync(
            IEnumerable<long> productIds) =>
            Task.FromResult(new Dictionary<long, List<string>>());
    }

    private sealed class TestSearchSyncStateStore : ISearchSyncStateStore {
        public DateTime Watermark { get; set; }
        public int SetCalls { get; private set; }

        public Task<DateTime> GetWatermarkAsync(CancellationToken ct = default) =>
            Task.FromResult(Watermark);

        public Task SetWatermarkAsync(
            DateTime watermarkUtc,
            CancellationToken ct = default) {
            ct.ThrowIfCancellationRequested();
            SetCalls++;
            Watermark = watermarkUtc;
            return Task.CompletedTask;
        }
    }

    private sealed class TestSearchCacheInvalidator : ISearchCacheInvalidator {
        public int Calls { get; private set; }
        public int FailuresRemaining { get; set; }

        public ValueTask InvalidateProductsAsync(
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            if (FailuresRemaining > 0) {
                FailuresRemaining--;
                return ValueTask.FromException(
                    new InvalidOperationException("cache invalidation failed"));
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestElasticsearchIndexService
        : IElasticsearchIndexService {
        public bool Healthy { get; set; } = true;
        public bool IndexExists { get; set; } = true;

        public Task<bool> CreateIndexAsync(CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<bool> DeleteIndexAsync(CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<bool> IndexExistsAsync(CancellationToken ct = default) =>
            Task.FromResult(IndexExists);

        public Task<bool> IsHealthyAsync(CancellationToken ct = default) {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(Healthy);
        }

        public Task<string?> CreateVersionedIndexAsync(
            CancellationToken ct = default) =>
            Task.FromResult<string?>("products_test");

        public Task<bool> SwapAliasAsync(
            string targetIndex,
            CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<int> CleanupOldVersionedIndicesAsync(
            int keep,
            CancellationToken ct = default) =>
            Task.FromResult(0);
    }

    private sealed class SuccessfulElasticsearchHandler : HttpMessageHandler {
        public int TimeoutResponsesRemaining { get; set; }
        public int BulkCalls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            bool isBulk = request.RequestUri?.AbsolutePath.EndsWith(
                "/_bulk",
                StringComparison.Ordinal) == true;
            if (isBulk) {
                BulkCalls++;
                if (TimeoutResponsesRemaining > 0) {
                    TimeoutResponsesRemaining--;
                    throw new TaskCanceledException(
                        "Elasticsearch request timed out",
                        new TimeoutException());
                }
            }

            string responseBody = isBulk
                    ? "{\"errors\":false}"
                    : "{}";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(responseBody)
            });
        }
    }

    private sealed class StaticJsonHandler(string responseBody)
        : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(responseBody)
            });
        }
    }

    private sealed class CollectingLogger<T> : ILogger<T> {
        private readonly List<LogLevel> _levels = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            _levels.Add(logLevel);
        }

        public int Count(LogLevel level) =>
            _levels.Count(candidate => candidate == level);
    }

    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();

        public void Dispose() {
        }
    }
}
