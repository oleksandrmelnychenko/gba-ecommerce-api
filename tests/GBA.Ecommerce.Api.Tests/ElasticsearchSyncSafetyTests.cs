using System.Net;
using System.Net.Sockets;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ElasticsearchSyncSafetyTests {
    public ElasticsearchSyncSafetyTests() {
        ElasticsearchSyncService.ResetForcedRebuildBackoff();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public Task ScheduledSync_WatermarkReadTimeout_RetriesBeforeEscalationAndRecovers(
        int timedOutReads, int expectedErrors) =>
        AssertScheduledWatermarkRecoveryAsync(timedOutReads, expectedErrors);

    [Theory]
    [InlineData(SocketError.TryAgain, true, 1, 0)]
    [InlineData(SocketError.WouldBlock, true, 2, 1)]
    [InlineData(SocketError.TryAgain, false, 1, 0)]
    [InlineData(SocketError.ConnectionRefused, false, 1, 0)]
    [InlineData(SocketError.ConnectionRefused, false, 2, 1)]
    [InlineData(SocketError.AccessDenied, true, 1, 1)]
    public Task ScheduledSync_WatermarkSocketFailure_RetriesOnlyTemporaryErrors(
        SocketError socketError, bool wrapped, int failedReads, int expectedErrors) {
        var socket = new SocketException((int)socketError);
        var connectionFailure = new HttpRequestException(socket.Message, socket);
        Exception failure = wrapped
            ? new TaskCanceledException("The operation was canceled.", connectionFailure)
            : connectionFailure;
        return AssertScheduledWatermarkRecoveryAsync(failedReads, expectedErrors, failure);
    }

    private static async Task AssertScheduledWatermarkRecoveryAsync(
        int failedReads, int expectedErrors, Exception? failure = null) {
        DateTime previous = DateTime.UtcNow.AddMinutes(-10);
        WatermarkHandler handler = new(previous, failedReads, failure);
        using HttpClient http = new(handler) {
            BaseAddress = new Uri("http://elasticsearch/"),
            Timeout = TimeSpan.FromMilliseconds(250)
        };
        var elastic = Options.Create(new ElasticsearchSettings { MaxRetries = 1, RetryBaseDelayMilliseconds = 1 });
        var schedule = Options.Create(new SyncSettings { IncrementalIntervalSeconds = 5, FullRebuildHour = -1 });
        SearchSyncStateStore state = new(http, elastic, new CollectingLogger<SearchSyncStateStore>());
        TestProductSyncRepository repository = new();
        TestElasticsearchIndexService index = new();
        ElasticsearchSyncService sync = CreateSyncService(repository, state, new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(), maxRetries: 1, index: index);
        TaskCompletionSource<SyncResult> completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IElasticsearchSyncService> scheduled = new(MockBehavior.Strict);
        scheduled.Setup(service => service.IncrementalSyncAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) => {
                SyncResult result = await sync.IncrementalSyncAsync(ct);
                completed.TrySetResult(result);
                return result;
            });
        CollectingLogger<ProductSearchSyncBackgroundService> logger = new();
        int iterations = 0;
        using ServiceProvider provider = new ServiceCollection()
            .AddScoped<ISearchSyncStateStore>(_ => {
                iterations++;
                return state;
            })
            .AddSingleton(scheduled.Object)
            .AddSingleton(schedule)
            .AddSingleton(elastic)
            .AddSingleton<ILogger<ProductSearchSyncBackgroundService>>(logger)
            .AddHostedService<ProductSearchSyncBackgroundService>()
            .BuildServiceProvider();
        IHostedService worker = provider.GetRequiredService<IHostedService>();
        await worker.StartAsync(CancellationToken.None);
        try {
            SyncResult result = await completed.Task.WaitAsync(TimeSpan.FromSeconds(40));
            Assert.True(result.Success, result.Error);
        } finally {
            await worker.StopAsync(CancellationToken.None);
        }

        Assert.Equal(expectedErrors + 1, iterations);
        Assert.Equal(expectedErrors, logger.Count(LogLevel.Error));
        var failures = logger.Entries.Where(entry => entry.Exception != null).ToList();
        Assert.Equal(failedReads, failures.Count);
        Assert.Equal(failedReads - expectedErrors, failures.Count(entry => entry.Level == LogLevel.Warning));
        Assert.All(failures, entry => {
            if (failure != null) {
                Assert.Same(failure, entry.Exception);
            } else {
                Assert.IsType<TimeoutException>(Assert.IsType<TaskCanceledException>(entry.Exception).InnerException);
            }
        });
        Assert.Equal(failedReads + 2, handler.Reads);
        Assert.Equal(1, handler.Writes);
        Assert.Equal(1, repository.ChangedCalls);
        Assert.Equal(0, repository.AllProductsCalls);
        SearchSyncHealthProbe probe = new(index, state, schedule, new CollectingLogger<SearchSyncHealthProbe>());
        SearchSyncHealthSnapshot snapshot = await probe.GetSnapshotAsync();
        Assert.True(snapshot.Healthy && snapshot.IndexExists && snapshot.Ready);
        Assert.False(snapshot.Stale);
        Assert.Null(snapshot.Error);
        Assert.True(snapshot.LastSyncUtc > previous);
        Assert.Equal(handler.Watermark, snapshot.LastSyncUtc);
        Assert.InRange(snapshot.LagSeconds!.Value, 0, schedule.Value.LagWarningSeconds);
    }

    [Fact]
    public async Task WatermarkRetry_HostStoppingDuringBackoff_CancelsWithoutAnotherRead() {
        using CancellationTokenSource stopping = new();
        CollectingLogger<ProductSearchSyncBackgroundService> logger = new();
        ElasticsearchTimeoutRetry retry = new(new ElasticsearchSettings { RetryBaseDelayMilliseconds = 30000 }, logger);
        int reads = 0;
        Task<DateTime> reading = retry.ExecuteAsync<DateTime>("sync watermark read", ct => {
            reads++;
            throw new TaskCanceledException("Watermark read timed out", new TimeoutException(), ct);
        }, stopping.Token);
        stopping.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reading);
        Assert.Equal(1, reads);
        Assert.Equal(1, logger.Count(LogLevel.Warning));
        Assert.Equal(0, logger.Count(LogLevel.Error));
    }

    [Fact]
    public async Task IncrementalSync_OutdatedIndexSchema_RebuildsWithTheCurrentAnalyzers() {
        TestProductSyncRepository repository = new();
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1),
            SourceFingerprint = repository.Fingerprint
        };
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0,
            index: new TestElasticsearchIndexService { SchemaVersion = null });

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.True(result.Success);
        Assert.Equal(1, repository.AllProductsCalls);
        Assert.Equal(0, repository.ChangedCalls);
    }

    [Fact]
    public async Task IncrementalSync_FailedForcedRebuild_BacksOffAndKeepsSyncingIncrementally() {
        TestProductSyncRepository repository = new();
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1),
            SourceFingerprint = repository.Fingerprint
        };
        TestElasticsearchIndexService index = new() {
            SchemaVersion = null,
            CreateSucceeds = false
        };
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0,
            index: index);

        SyncResult first = await service.IncrementalSyncAsync();
        SyncResult second = await service.IncrementalSyncAsync();

        Assert.False(first.Success);
        Assert.True(second.Success);
        Assert.Equal(1, index.CreateCalls);
        Assert.Equal(1, repository.ChangedCalls);
    }

    [Fact]
    public async Task FullRebuild_FailedSwap_DeletesTheAbandonedIndex() {
        TestProductSyncRepository repository = new();
        SuccessfulElasticsearchHandler handler = new();
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            new TestSearchSyncStateStore(),
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0,
            handler,
            new TestElasticsearchIndexService { SwapSucceeds = false });

        SyncResult result = await service.FullRebuildAsync();

        Assert.False(result.Success);
        Assert.Contains("DELETE /products_test", handler.Requests);
    }

    [Fact]
    public void HealthProbe_LongRunningSyncThatStartedFresh_IsNotStale() {
        DateTime now = DateTime.UtcNow;

        Assert.True(SearchSyncHealthProbe.IsCoveredByRunningSync(
            now.AddMinutes(-9),
            now.AddMinutes(-8),
            now,
            300));
        Assert.False(SearchSyncHealthProbe.IsCoveredByRunningSync(
            now.AddMinutes(-60),
            now.AddMinutes(-8),
            now,
            300));
        Assert.False(SearchSyncHealthProbe.IsCoveredByRunningSync(
            now.AddMinutes(-41),
            now.AddMinutes(-40),
            now,
            300));
        Assert.False(SearchSyncHealthProbe.IsCoveredByRunningSync(
            now.AddMinutes(-9),
            null,
            now,
            300));
    }
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

    [Theory]
    [InlineData(-2)]
    [InlineData(258)]
    public async Task IncrementalSync_SourceFingerprintTimeout_RetriesOnlyTheReadAndRestoresReadiness(int number) {
        DateTime previous = DateTime.UtcNow.AddMinutes(-10);
        TestProductSyncRepository repository = new();
        repository.FingerprintErrors.Enqueue(TestSqlExceptions.Create(number));
        TestSearchSyncStateStore state = new() { Watermark = previous };
        TestElasticsearchIndexService index = new();
        CollectingLogger<ElasticsearchSyncService> logger = new();
        ElasticsearchSyncService service = CreateSyncService(
            repository, state, new TestSearchCacheInvalidator(), logger, maxRetries: 1, index: index);

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, repository.FingerprintReads);
        Assert.Equal(1, state.WatermarkReads);
        Assert.Equal(1, repository.ChangedCalls);
        Assert.Equal(0, repository.AllProductsCalls);
        Assert.Equal(repository.Fingerprint, state.SourceFingerprint);
        Assert.Equal(1, state.SetCalls);
        Assert.True(state.Watermark > previous);
        Assert.Equal(1, logger.Count(LogLevel.Warning));
        Assert.Equal(0, logger.Count(LogLevel.Error));
        var settings = Options.Create(new SyncSettings());
        SearchSyncHealthProbe probe = new(index, state, settings, new CollectingLogger<SearchSyncHealthProbe>());
        SearchSyncHealthSnapshot snapshot = await probe.GetSnapshotAsync();
        Assert.True(snapshot.Healthy && snapshot.IndexExists && snapshot.Ready);
        Assert.False(snapshot.Stale);
        Assert.Null(snapshot.Error);
        Assert.Equal(state.Watermark, snapshot.LastSyncUtc);
        Assert.InRange(snapshot.LagSeconds!.Value, 0, settings.Value.LagWarningSeconds);
    }

    [Theory]
    [InlineData(-2, 2)]
    [InlineData(208, 1)]
    public async Task IncrementalSync_SourceFingerprintFailure_PreservesStateUntilNextSuccess(int number, int expectedReads) {
        DateTime previous = DateTime.UtcNow.AddMinutes(-10);
        TestProductSyncRepository repository = new();
        var failure = TestSqlExceptions.Create(number);
        for (int i = 0; i < expectedReads; i++) {
            repository.FingerprintErrors.Enqueue(failure);
        }
        TestSearchSyncStateStore state = new() { Watermark = previous };
        CollectingLogger<ElasticsearchSyncService> logger = new();
        ElasticsearchSyncService service = CreateSyncService(
            repository, state, new TestSearchCacheInvalidator(), logger, maxRetries: 1);

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.False(result.Success);
        Assert.Equal(failure.Message, result.Error);
        Assert.Equal(expectedReads, repository.FingerprintReads);
        Assert.Equal(expectedReads - 1, logger.Count(LogLevel.Warning));
        Assert.Same(failure, Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Error).Exception);
        Assert.Equal(previous, state.Watermark);
        Assert.Equal(0, state.SetCalls);
        Assert.Empty(state.Markers);
        Assert.Equal(0, repository.ChangedCalls);

        SyncResult recovery = await service.IncrementalSyncAsync();

        Assert.True(recovery.Success, recovery.Error);
        Assert.Equal(1, state.SetCalls);
        Assert.True(state.Watermark > previous);
    }

    [Fact]
    public async Task IncrementalSync_SourceFingerprintRetry_StopsDuringBackoff() {
        using CancellationTokenSource stopping = new();
        TestProductSyncRepository repository = new();
        repository.FingerprintErrors.Enqueue(TestSqlExceptions.Create(-2));
        TestSearchSyncStateStore state = new() { Watermark = DateTime.UtcNow.AddMinutes(-1) };
        CollectingLogger<ElasticsearchSyncService> logger = new();
        ElasticsearchSyncService service = CreateSyncService(
            repository, state, new TestSearchCacheInvalidator(), logger, maxRetries: 1, retryBaseDelayMilliseconds: 30000);

        Task<SyncResult> syncing = service.IncrementalSyncAsync(stopping.Token);
        stopping.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => syncing);
        Assert.Equal(1, repository.FingerprintReads);
        Assert.Equal(1, logger.Count(LogLevel.Warning));
        Assert.Equal(0, logger.Count(LogLevel.Error));
        Assert.Equal(0, state.SetCalls);
        Assert.Empty(state.Markers);
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
    public async Task ElasticsearchHealth_NoSuccessfulSync_RemainsNotReady() {
        TestElasticsearchIndexService indexService = new() {
            Healthy = true,
            IndexExists = true
        };
        SearchSyncHealthProbe probe = new(
            indexService,
            new TestSearchSyncStateStore { Watermark = DateTime.MinValue },
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
        Assert.False(body.RootElement.GetProperty("ready").GetBoolean());
        Assert.True(body.RootElement.GetProperty("stale").GetBoolean());
        Assert.Contains(
            "no successful watermark",
            body.RootElement.GetProperty("error").GetString());
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
    public async Task SearchIndexHealthCheck_NoSuccessfulSync_WithServingIndex_IsDegraded() {
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

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("no successful watermark", result.Description);
        Assert.Equal(true, result.Data["healthy"]);
        Assert.Equal(true, result.Data["indexExists"]);
        Assert.Equal(false, result.Data["ready"]);
        Assert.Equal(true, result.Data["stale"]);
    }

    [Fact]
    public void TimeoutRetryDelay_IsExponentialAndBounded() {
        Assert.Equal(
            TimeSpan.FromMilliseconds(100),
            ElasticsearchTimeoutRetry.GetTimeoutRetryDelay(1, 100));
        Assert.Equal(
            TimeSpan.FromMilliseconds(400),
            ElasticsearchTimeoutRetry.GetTimeoutRetryDelay(3, 100));
        Assert.Equal(
            TimeSpan.FromSeconds(30),
            ElasticsearchTimeoutRetry.GetTimeoutRetryDelay(10, 1000));
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

    [Fact]
    public async Task IncrementalSync_RemovesDocumentsWhoseProductsVanishedFromTheSourceDatabase() {
        TestProductSyncRepository repository = new() {
            LiveIds = [10, 11, 12],
            GetProductsByIds = ids => Task.FromResult(
                ids.Select(CreateProduct).ToList())
        };
        TestElasticsearchIndexService index = new() {
            IndexedIds = [1, 2, 3, 10, 11]
        };
        SuccessfulElasticsearchHandler handler = new();
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1),
            SourceFingerprint = repository.Fingerprint
        };
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0,
            handler,
            index);

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.True(result.Success);
        Assert.Equal(3, result.DocumentsDeleted);
        Assert.Equal(1, result.DocumentsIndexed);
        string bulk = string.Join('\n', handler.BulkBodies);
        Assert.Contains("\"delete\":{\"_index\":\"products\",\"_id\":1}", bulk);
        Assert.Contains("\"delete\":{\"_index\":\"products\",\"_id\":3}", bulk);
        Assert.Contains("\"_id\":12,\"require_alias\":true", bulk);
        Assert.DoesNotContain("\"_id\":10", bulk);
        Assert.Equal(0, repository.AllProductsCalls);
        Assert.Equal(1, state.SetCalls);
    }

    [Fact]
    public async Task IncrementalSync_MatchingCounts_SkipsTheMembershipScan() {
        TestProductSyncRepository repository = new() { LiveIds = [10, 11] };
        TestElasticsearchIndexService index = new() { IndexedIds = [10, 11] };
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1),
            SourceFingerprint = repository.Fingerprint
        };
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0,
            index: index);

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.True(result.Success);
        Assert.Equal(0, index.IdScanCalls);
        Assert.Equal(0, repository.LiveIdCalls);
    }

    [Fact]
    public async Task IncrementalSync_SourceDatabaseReplaced_RebuildsInsteadOfTrustingTheWatermark() {
        TestProductSyncRepository repository = new() { Fingerprint = "2026-10-18|fork-b|38041649" };
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1),
            SourceFingerprint = "2026-05-19|fork-a|37669239"
        };
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0);

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.True(result.Success);
        Assert.Equal(1, repository.AllProductsCalls);
        Assert.Equal(0, repository.ChangedCalls);
        Assert.Equal("2026-10-18|fork-b|38041649", state.SourceFingerprint);
        Assert.Equal(1, state.SetCalls);
    }

    [Fact]
    public async Task IncrementalSync_FirstRunWithoutFingerprint_AdoptsItWithoutRebuilding() {
        TestProductSyncRepository repository = new();
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1)
        };
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0);

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.True(result.Success);
        Assert.Equal(0, repository.AllProductsCalls);
        Assert.Equal(1, repository.ChangedCalls);
        Assert.Equal(repository.Fingerprint, state.SourceFingerprint);
    }

    [Fact]
    public async Task IncrementalSync_MissingAlias_RebuildsInsteadOfWritingIntoAnAutoCreatedIndex() {
        TestProductSyncRepository repository = new();
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1),
            SourceFingerprint = repository.Fingerprint
        };
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0,
            index: new TestElasticsearchIndexService { AliasExists = false });

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.True(result.Success);
        Assert.Equal(1, repository.AllProductsCalls);
        Assert.Equal(0, repository.ChangedCalls);
    }

    [Fact]
    public async Task IncrementalSync_TouchedButUnchangedRetailConfiguration_DoesNotReindexTheCatalog() {
        TestProductSyncRepository repository = new() {
            LiveIds = [10, 11, 12],
            GetChangedProductIds = _ => Task.FromResult(new List<long> { 11 }),
            GetProductsByIds = ids => Task.FromResult(ids.Select(CreateProduct).ToList())
        };
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1),
            SourceFingerprint = repository.Fingerprint,
            RetailSignature = repository.RetailSignature
        };
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0);

        SyncResult result = await service.IncrementalSyncAsync();

        Assert.True(result.Success);
        Assert.Equal(1, result.DocumentsIndexed);
        Assert.Equal(0, repository.LiveIdCalls);
    }

    [Fact]
    public async Task IncrementalSync_ChangedRetailConfiguration_ReindexesEveryLiveProductOnce() {
        TestProductSyncRepository repository = new() {
            LiveIds = [10, 11, 12],
            RetailSignature = "retail-b",
            GetProductsByIds = ids => Task.FromResult(ids.Select(CreateProduct).ToList())
        };
        TestSearchSyncStateStore state = new() {
            Watermark = DateTime.UtcNow.AddMinutes(-1),
            SourceFingerprint = repository.Fingerprint,
            RetailSignature = "retail-a"
        };
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            state,
            new TestSearchCacheInvalidator(),
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0);

        SyncResult first = await service.IncrementalSyncAsync();
        SyncResult second = await service.IncrementalSyncAsync();

        Assert.Equal(3, first.DocumentsIndexed);
        Assert.Equal(0, second.DocumentsIndexed);
        Assert.Equal("retail-b", state.RetailSignature);
        Assert.Equal(0, repository.AllProductsCalls);
    }

    [Fact]
    public void RetailConfigurationSignature_HashesTheValuesTheProjectionUses() {
        string sql = ProductSyncRepository.RetailConfigurationSignatureSql;

        Assert.Contains("HASHBYTES('SHA2_256'", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY s.RetailPriority, s.ID, a.IsSelected DESC, ca.ID", sql, StringComparison.Ordinal);
        Assert.Contains("CalculatedExtraCharge", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("Updated", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TargetedReindex_AfterCartStockChange_KeepsPriceAndSeoCaches() {
        TestProductSyncRepository repository = new() {
            GetProductsByIds = ids => Task.FromResult(ids.Select(CreateProduct).ToList())
        };
        TestSearchCacheInvalidator cache = new();
        ElasticsearchSyncService service = CreateSyncService(
            repository,
            new TestSearchSyncStateStore(),
            cache,
            new CollectingLogger<ElasticsearchSyncService>(),
            maxRetries: 0);

        SyncResult result = await service.ReindexProductsAsync([42]);

        Assert.True(result.Success);
        Assert.Equal(1, result.DocumentsIndexed);
        Assert.Equal(0, cache.Calls);
    }

    [Fact]
    public void DiffProductIds_SeparatesOrphansFromMissingProducts() {
        (List<long> orphans, List<long> missing) = ElasticsearchSyncService.DiffProductIds(
            [37854037, 38215752, 37952324],
            [38215752, 38336866]);

        Assert.Equal([37854037, 37952324], orphans);
        Assert.Equal([38336866], missing);
    }

    [Fact]
    public void IndexActionLine_RequiresTheAliasOnlyForLiveWrites() {
        string liveWrite = ElasticsearchSyncService.BuildIndexActionLine(7, "products", requireAlias: true);
        string rebuildWrite = ElasticsearchSyncService.BuildIndexActionLine(7, "products_20261018030033714", requireAlias: false);

        Assert.Contains("\"require_alias\":true", liveWrite);
        Assert.DoesNotContain("require_alias", rebuildWrite);
    }

    [Fact]
    public void IndexCleanup_KeepsStateIndexAliasTargetAndNewestVersions() {
        string[] indices = [
            "products_sync_state",
            "products_20261018030033714",
            "products_20261017030023940",
            "products_20261016030011111",
            "products_20260726030021316_2cbda34a83a34d25a82b2925dc17da8b",
            "products_extra"
        ];

        IReadOnlyList<string> deleted = ElasticsearchIndexService.SelectIndicesToDelete(
            "products",
            indices,
            new HashSet<string> { "products_20261016030011111" },
            keep: 2);

        Assert.Equal(["products_20260726030021316_2cbda34a83a34d25a82b2925dc17da8b"], deleted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void IndexCleanup_NeverDeletesTheNewestIndexEvenWithALowKeepSetting(int keep) {
        IReadOnlyList<string> deleted = ElasticsearchIndexService.SelectIndicesToDelete(
            "products",
            ["products_sync_state", "products_20261018030033714", "products_20261017030023940"],
            new HashSet<string>(),
            keep);

        Assert.Equal(["products_20261017030023940"], deleted);
    }

    [Fact]
    public void OriginalNumbers_AreDeduplicatedPerProduct() {
        Dictionary<long, List<string>> numbers = new();

        ProductSyncRepository.AppendOriginalNumber(numbers, 38215752, "5001842840");
        ProductSyncRepository.AppendOriginalNumber(numbers, 38215752, "9700519532");
        ProductSyncRepository.AppendOriginalNumber(numbers, 38215752, "9700519532");
        ProductSyncRepository.AppendOriginalNumber(numbers, 38215752, " ");

        Assert.Equal(["5001842840", "9700519532"], numbers[38215752]);
    }

    [Fact]
    public void SourceFingerprint_DetectsRestoresAndRemintedCatalogs() {
        Assert.Contains("recovery_fork_guid", ProductSyncRepository.SourceFingerprintSql, StringComparison.Ordinal);
        Assert.Contains("create_date", ProductSyncRepository.SourceFingerprintSql, StringComparison.Ordinal);
        Assert.Contains("MIN(ID) FROM Product", ProductSyncRepository.SourceFingerprintSql, StringComparison.Ordinal);
        Assert.Contains("Deleted = 0", ProductSyncRepository.LiveProductCountSql, StringComparison.Ordinal);
        Assert.Contains("Deleted = 0", ProductSyncRepository.LiveProductIdsSql, StringComparison.Ordinal);
    }

    private static ElasticsearchSyncService CreateSyncService(
        TestProductSyncRepository repository,
        ISearchSyncStateStore state,
        TestSearchCacheInvalidator cache,
        CollectingLogger<ElasticsearchSyncService> logger,
        int maxRetries,
        SuccessfulElasticsearchHandler? handler = null,
        TestElasticsearchIndexService? index = null,
        int retryBaseDelayMilliseconds = 1) {
        HttpClient http = new(handler ?? new SuccessfulElasticsearchHandler()) {
            BaseAddress = new Uri("http://elasticsearch/")
        };

        return new ElasticsearchSyncService(
            http,
            Options.Create(new ElasticsearchSettings {
                IndexName = "products",
                MaxRetries = maxRetries,
                RetryBaseDelayMilliseconds = retryBaseDelayMilliseconds
            }),
            Options.Create(new SyncSettings {
                BatchSize = 1000,
                LagWarningSeconds = 60
            }),
            repository,
            index ?? new TestElasticsearchIndexService { IndexedIds = repository.LiveIds },
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
        public int AllProductsCalls { get; private set; }
        public int ChangedCalls { get; private set; }
        public int LiveIdCalls { get; private set; }
        public List<long> LiveIds { get; set; } = [];
        public string Fingerprint { get; set; } = "2026-05-19|fork-a|38041649";
        public Queue<Exception> FingerprintErrors { get; } = new();
        public int FingerprintReads { get; private set; }

        public Task<List<ProductSyncData>> GetAllProductsAsync() {
            AllProductsCalls++;
            return Task.FromResult(new List<ProductSyncData>());
        }

        public Task<List<long>> GetChangedProductIdsAsync(DateTime since) {
            ChangedCalls++;
            return GetChangedProductIds(since);
        }

        public Task<long> GetLiveProductCountAsync() =>
            Task.FromResult((long)LiveIds.Count);

        public Task<List<long>> GetLiveProductIdsAsync() {
            LiveIdCalls++;
            return Task.FromResult(LiveIds.ToList());
        }

        public string RetailSignature { get; set; } = "retail-a";

        public Task<string> GetSourceFingerprintAsync(CancellationToken ct = default) {
            ct.ThrowIfCancellationRequested();
            FingerprintReads++;
            return FingerprintErrors.TryDequeue(out var error)
                ? Task.FromException<string>(error)
                : Task.FromResult(Fingerprint);
        }

        public Task<string> GetRetailConfigurationSignatureAsync() =>
            Task.FromResult(RetailSignature);

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
        public Dictionary<string, string> Markers { get; } = new();
        public int SetCalls { get; private set; }
        public int WatermarkReads { get; private set; }

        public string? SourceFingerprint {
            get => Markers.GetValueOrDefault(SearchSyncMarkers.SourceDatabase);
            set {
                if (value is null) Markers.Remove(SearchSyncMarkers.SourceDatabase);
                else Markers[SearchSyncMarkers.SourceDatabase] = value;
            }
        }

        public string? RetailSignature {
            get => Markers.GetValueOrDefault(SearchSyncMarkers.RetailConfiguration);
            set {
                if (value is null) Markers.Remove(SearchSyncMarkers.RetailConfiguration);
                else Markers[SearchSyncMarkers.RetailConfiguration] = value;
            }
        }

        public Task<string?> GetMarkerAsync(string name, CancellationToken ct = default) =>
            Task.FromResult(Markers.GetValueOrDefault(name));

        public Task SetMarkerAsync(
            string name,
            string value,
            CancellationToken ct = default) {
            ct.ThrowIfCancellationRequested();
            Markers[name] = value;
            return Task.CompletedTask;
        }

        public Task<DateTime> GetWatermarkAsync(CancellationToken ct = default) {
            WatermarkReads++;
            return Task.FromResult(Watermark);
        }

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
        public bool AliasExists { get; set; } = true;
        public string? SchemaVersion { get; set; } = ElasticsearchIndexService.IndexSchemaVersion;
        public bool CreateSucceeds { get; set; } = true;
        public bool SwapSucceeds { get; set; } = true;
        public int CreateCalls { get; private set; }

        public Task<string?> GetIndexSchemaVersionAsync(CancellationToken ct = default) =>
            Task.FromResult(SchemaVersion);
        public List<long> IndexedIds { get; set; } = [];
        public int IdScanCalls { get; private set; }

        Task<bool> IElasticsearchIndexService.AliasExistsAsync(CancellationToken ct) =>
            Task.FromResult(AliasExists);

        public Task<long> CountDocumentsAsync(CancellationToken ct = default) =>
            Task.FromResult((long)IndexedIds.Count);

        public Task<List<long>> GetIndexedProductIdsAsync(CancellationToken ct = default) {
            IdScanCalls++;
            return Task.FromResult(IndexedIds.ToList());
        }

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
            CancellationToken ct = default) {
            CreateCalls++;
            return Task.FromResult<string?>(CreateSucceeds ? "products_test" : null);
        }

        public Task<bool> SwapAliasAsync(
            string targetIndex,
            CancellationToken ct = default) =>
            Task.FromResult(SwapSucceeds);

        public Task<int> CleanupOldVersionedIndicesAsync(
            int keep,
            CancellationToken ct = default) =>
            Task.FromResult(0);
    }

    private sealed class SuccessfulElasticsearchHandler : HttpMessageHandler {
        public int TimeoutResponsesRemaining { get; set; }
        public int BulkCalls { get; private set; }
        public List<string> BulkBodies { get; } = [];
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add($"{request.Method} {request.RequestUri?.AbsolutePath}");
            bool isBulk = request.RequestUri?.AbsolutePath.EndsWith(
                "/_bulk",
                StringComparison.Ordinal) == true;
            if (isBulk) {
                BulkCalls++;
                BulkBodies.Add(request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult());
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

    private sealed class WatermarkHandler(DateTime previous, int failedReads, Exception? failure) : HttpMessageHandler {
        public DateTime Watermark { get; private set; } = previous;
        public int Reads { get; private set; }
        public int Writes { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
            if (request.RequestUri!.AbsolutePath == "/products_sync_state/_doc/watermark") {
                if (request.Method == HttpMethod.Get) {
                    if (++Reads <= failedReads) {
                        if (failure != null) throw failure;
                        await Task.Delay(Timeout.Infinite, ct);
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK) {
                        Content = new StringContent(JsonSerializer.Serialize(new { _source = new { lastSyncTime = Watermark } }))
                    };
                }
                Assert.Equal(HttpMethod.Put, request.Method);
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
                Watermark = body.RootElement.GetProperty("lastSyncTime").GetDateTime();
                Writes++;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            Assert.StartsWith("/products_sync_state/_doc/marker-", request.RequestUri.AbsolutePath);
            return new HttpResponseMessage(request.Method == HttpMethod.Get ? HttpStatusCode.NotFound : HttpStatusCode.OK);
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
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            Entries.Add((logLevel, exception));
        }

        public int Count(LogLevel level) =>
            Entries.Count(entry => entry.Level == level);
    }

    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();

        public void Dispose() {
        }
    }
}
