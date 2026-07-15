using System.Net;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Transactions;
using Dapper;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Services.Infrastructure;
using GBA.Services.Infrastructure.SalesMutations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class SalesMutationOutboxTests {
    private const string InternalApiKey =
        "test-only-ecommerce-internal-api-key-0123456789abcdef";
    private const string AllowedInternalBaseUri = "https://crm.example";
    private const string AllowedTargetUri =
        "https://crm.example/api/v1/uk/sales/update/ecommerce";
    private const string AllowedPolishTargetUri =
        "https://crm.example/api/v1/pl/sales/update/ecommerce";
    private const string UkrainianTtnFinalizeUri =
        "https://crm.example/api/v1/uk/sales/save/ttn?phase=finalize";
    private const string ExpectedTtnUrl =
        "https://crm.example/Data/Temp/CustomersTTN-operation.pdf";

    public static TheoryData<string> OutboundSaleOperationNames => new() {
        SalesMutationOperationNames.OrderInvoiceSaleUpdate,
        SalesMutationOperationNames.RetailSaleUpdate,
        SalesMutationOperationNames.QuickSaleUpdate
    };

    public static TheoryData<string> DisallowedTargets => new() {
        "https://attacker.example/api/v1/uk/sales/update/ecommerce",
        "http://crm.example/api/v1/uk/sales/update/ecommerce",
        "https://crm.example:444/api/v1/uk/sales/update/ecommerce",
        "https://crm.example/api/v1/uk/sales/update/ecommerce/extra",
        "https://crm.example/api/v1/uk/sales/update/ecommerce?redirect=attacker",
        "https://crm.example/api/v1/uk/sales/update/ecommerce#fragment",
        "https://crm.example/api/v1/ru/sales/update/ecommerce",
        "https://crm.example@attacker.example/api/v1/uk/sales/update/ecommerce"
    };

    public static TheoryData<string> KnownLocaleTargets => new() {
        AllowedTargetUri,
        AllowedPolishTargetUri
    };

    [Theory]
    [MemberData(nameof(OutboundSaleOperationNames))]
    public async Task OutboundSalePathsAttachTheSameKeyToHeaderAndBody(string operationName) {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        SalesMutationOutboxPublisher publisher = new(store, clock);
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        SalesMutationOutboxDispatcher dispatcher = CreateDispatcher(store, handler, clock);

        Guid operationNetUid = await publisher.EnqueueAsync(
            "https://crm.example/api/v1/uk/sales/update/ecommerce",
            "{\"NetUid\":\"6cb6e33d-894c-4e99-b785-f94c4d2e2662\",\"Order\":{\"Id\":42}}",
            operationName);
        SalesMutationDeliveryResult result = await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Completed, result.Kind);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(operationNetUid.ToString("D"), request.IdempotencyKey);
        Assert.Equal(InternalApiKey, request.InternalApiKey);
        Assert.Equal(operationNetUid, ReadBodyOperationKey(request.Body));
        Assert.Equal(operationNetUid, result.OperationNetUid);
        Assert.Equal(SalesMutationOutboxStatus.Completed, store.Get(operationNetUid).Status);
    }

    [Theory]
    [MemberData(nameof(KnownLocaleTargets))]
    public async Task KnownLocaleTargetIsDelivered(string requestUrl) {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        SalesMutationOutboxPublisher publisher = new(store, clock);
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        SalesMutationOutboxDispatcher dispatcher = CreateDispatcher(store, handler, clock);
        await publisher.EnqueueAsync(
            requestUrl,
            "{\"NetUid\":\"4aceb08b-d1ee-4866-9fa6-2c5af6364a90\"}",
            SalesMutationOperationNames.RetailSaleUpdate);

        SalesMutationDeliveryResult result =
            await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Completed, result.Kind);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task PendingOrderInvoiceFinalizesCommittedTtnBeforeCrmAfterProcessRestart() {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore durableStore = new();
        SalesMutationOutboxPublisher publisher = new(durableStore, clock);
        Guid operationNetUid = await publisher.EnqueueAsync(
            AllowedTargetUri,
            $"{{\"NetUid\":\"4aceb08b-d1ee-4866-9fa6-2c5af6364a90\",\"CustomersOwnTtn\":{{\"TtnPDFPath\":{JsonSerializer.Serialize(ExpectedTtnUrl)}}}}}",
            SalesMutationOperationNames.OrderInvoiceSaleUpdate);

        RecordingHandler handler = new((request, _) =>
            Task.FromResult(
                request.RequestUri?.AbsoluteUri == UkrainianTtnFinalizeUri
                    ? new HttpResponseMessage(HttpStatusCode.OK) {
                        Content = new StringContent(JsonSerializer.Serialize(ExpectedTtnUrl))
                    }
                    : new HttpResponseMessage(HttpStatusCode.OK)));
        SalesMutationOutboxDispatcher restartedProcess =
            CreateDispatcher(durableStore, handler, clock);

        SalesMutationDeliveryResult result =
            await restartedProcess.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Completed, result.Kind);
        Assert.Equal(operationNetUid, result.OperationNetUid);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(UkrainianTtnFinalizeUri, handler.Requests[0].RequestUri);
        Assert.Equal(string.Empty, handler.Requests[0].Body);
        Assert.Equal(AllowedTargetUri, handler.Requests[1].RequestUri);
        Assert.Equal(operationNetUid, ReadBodyOperationKey(handler.Requests[1].Body));
        Assert.All(handler.Requests, request => {
            Assert.Equal(operationNetUid.ToString("D"), request.IdempotencyKey);
            Assert.Equal(InternalApiKey, request.InternalApiKey);
        });
        Assert.Equal(SalesMutationOutboxStatus.Completed, durableStore.Get(operationNetUid).Status);
    }

    [Fact]
    public async Task TtnFinalizeFailureRetriesWithoutSendingCrmUpdate() {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        SalesMutationOutboxPublisher publisher = new(store, clock);
        int finalizeAttempt = 0;
        RecordingHandler handler = new((request, _) => {
            if (request.RequestUri?.AbsoluteUri == UkrainianTtnFinalizeUri &&
                Interlocked.Increment(ref finalizeAttempt) == 1)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

            return Task.FromResult(
                request.RequestUri?.AbsoluteUri == UkrainianTtnFinalizeUri
                    ? new HttpResponseMessage(HttpStatusCode.OK) {
                        Content = new StringContent(JsonSerializer.Serialize(ExpectedTtnUrl))
                    }
                    : new HttpResponseMessage(HttpStatusCode.OK));
        });
        SalesMutationOutboxOptions options = CreateOptions();
        SalesMutationOutboxDispatcher dispatcher = CreateDispatcher(store, handler, clock, options);
        Guid operationNetUid = await publisher.EnqueueAsync(
            AllowedTargetUri,
            $"{{\"NetUid\":\"7bb5bdcb-6768-47f4-9da5-bfcf66ba30ef\",\"CustomersOwnTtn\":{{\"TtnPDFPath\":{JsonSerializer.Serialize(ExpectedTtnUrl)}}}}}",
            SalesMutationOperationNames.OrderInvoiceSaleUpdate);

        SalesMutationDeliveryResult first =
            await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Retrying, first.Kind);
        Assert.Single(handler.Requests);
        Assert.Equal(UkrainianTtnFinalizeUri, handler.Requests[0].RequestUri);

        clock.Advance(options.InitialRetryDelay);
        SalesMutationDeliveryResult retry =
            await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Completed, retry.Kind);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(UkrainianTtnFinalizeUri, handler.Requests[1].RequestUri);
        Assert.Equal(AllowedTargetUri, handler.Requests[2].RequestUri);
        Assert.All(handler.Requests, request =>
            Assert.Equal(operationNetUid.ToString("D"), request.IdempotencyKey));
    }

    [Theory]
    [MemberData(nameof(DisallowedTargets))]
    public async Task DisallowedTargetIsRejectedWithoutCreatingOrSendingAnAuthenticatedRequest(
        string requestUrl) {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        SalesMutationOutboxPublisher publisher = new(store, clock);
        RecordingHandler handler = new((_, _) =>
            throw new InvalidOperationException("A rejected target must not reach the HTTP handler."));
        StubHttpClientFactory httpClientFactory = new(handler);
        SalesMutationOutboxDispatcher dispatcher = new(
            store,
            httpClientFactory,
            CreateOptions(),
            CreateInternalAuthOptions(),
            clock);
        Guid operationNetUid = await publisher.EnqueueAsync(
            requestUrl,
            "{\"NetUid\":\"5314abf8-d993-4dbb-93b4-7670922ad0a4\"}",
            SalesMutationOperationNames.OrderInvoiceSaleUpdate);

        SalesMutationDeliveryResult result =
            await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.DeadLettered, result.Kind);
        Assert.Empty(handler.Requests);
        Assert.Equal(0, httpClientFactory.CreateClientCount);
        InMemoryOutboxStore.EntrySnapshot entry = store.Get(operationNetUid);
        Assert.Equal(SalesMutationOutboxStatus.DeadLetter, entry.Status);
        Assert.DoesNotContain(InternalApiKey, entry.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RedirectResponseIsNotFollowedAndIsDeadLettered() {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        SalesMutationOutboxPublisher publisher = new(store, clock);
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.TemporaryRedirect) {
                Headers = { Location = new Uri("https://attacker.example/collect") }
            }));
        StubHttpClientFactory httpClientFactory = new(handler);
        SalesMutationOutboxDispatcher dispatcher = new(
            store,
            httpClientFactory,
            CreateOptions(),
            CreateInternalAuthOptions(),
            clock);
        await publisher.EnqueueAsync(
            AllowedTargetUri,
            "{\"NetUid\":\"e4d913bb-a32d-45dd-a017-24a81312f593\"}",
            SalesMutationOperationNames.RetailSaleUpdate);

        SalesMutationDeliveryResult result =
            await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.DeadLettered, result.Kind);
        Assert.Equal((int)HttpStatusCode.TemporaryRedirect, result.StatusCode);
        Assert.Single(handler.Requests);
        Assert.Equal(
            [SalesMutationOutboxDispatcher.HttpClientName],
            httpClientFactory.RequestedClientNames);
    }

    [Fact]
    public void OutboxHttpClientRegistrationDisablesAutomaticRedirects() {
        string startup = ReadRepositoryFile("src", "GBA.Ecommerce", "Startup.cs");

        Assert.Contains(
            "AddHttpClient(SalesMutationOutboxDispatcher.HttpClientName)",
            startup,
            StringComparison.Ordinal);
        Assert.Contains("AllowAutoRedirect = false", startup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TimeoutIsPersistedAndRetryUsesTheSameOperationKey() {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        SalesMutationOutboxPublisher publisher = new(store, clock);
        int call = 0;
        RecordingHandler handler = new(async (_, cancellationToken) => {
            if (Interlocked.Increment(ref call) == 1) {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("Unreachable");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        SalesMutationOutboxOptions options = CreateOptions();
        options.RequestTimeout = TimeSpan.FromMilliseconds(25);
        SalesMutationOutboxDispatcher dispatcher = CreateDispatcher(store, handler, clock, options);

        Guid operationNetUid = await publisher.EnqueueAsync(
            "https://crm.example/api/v1/uk/sales/update/ecommerce",
            "{\"NetUid\":\"24ff1fd3-6728-4b64-8e4c-826066ad2577\"}",
            SalesMutationOperationNames.OrderInvoiceSaleUpdate);
        SalesMutationDeliveryResult timedOut = await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Retrying, timedOut.Kind);
        Assert.Equal(SalesMutationOutboxStatus.Pending, store.Get(operationNetUid).Status);
        clock.Advance(options.InitialRetryDelay);

        SalesMutationDeliveryResult retried = await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Completed, retried.Kind);
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => {
            Assert.Equal(operationNetUid.ToString("D"), request.IdempotencyKey);
            Assert.Equal(InternalApiKey, request.InternalApiKey);
            Assert.Equal(operationNetUid, ReadBodyOperationKey(request.Body));
        });
    }

    [Fact]
    public async Task ServerErrorIsRetriedWithTheSamePersistedRequest() {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        SalesMutationOutboxPublisher publisher = new(store, clock);
        Queue<HttpStatusCode> responses = new([
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.OK
        ]);
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(responses.Dequeue())));
        SalesMutationOutboxOptions options = CreateOptions();
        SalesMutationOutboxDispatcher dispatcher = CreateDispatcher(store, handler, clock, options);

        Guid operationNetUid = await publisher.EnqueueAsync(
            "https://crm.example/api/v1/uk/sales/update/ecommerce",
            "{\"NetUid\":\"042e1009-47c7-420c-b413-bd19ce516779\"}",
            SalesMutationOperationNames.RetailSaleUpdate);
        SalesMutationDeliveryResult unavailable = await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Retrying, unavailable.Kind);
        Assert.Equal(503, unavailable.StatusCode);
        clock.Advance(options.InitialRetryDelay);
        SalesMutationDeliveryResult retried = await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Completed, retried.Kind);
        Assert.Equal(operationNetUid, retried.OperationNetUid);
        Assert.Equal(handler.Requests[0].Body, handler.Requests[1].Body);
        Assert.Equal(handler.Requests[0].IdempotencyKey, handler.Requests[1].IdempotencyKey);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task AuthenticationFailureRetriesAreBoundedThenDeadLettered(
        HttpStatusCode statusCode) {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        SalesMutationOutboxPublisher publisher = new(store, clock);
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(statusCode)));
        SalesMutationOutboxOptions options = CreateOptions();
        SalesMutationOutboxDispatcher dispatcher = CreateDispatcher(store, handler, clock, options);
        Guid operationNetUid = await publisher.EnqueueAsync(
            AllowedTargetUri,
            "{\"NetUid\":\"5c3cf614-eb93-4414-ac6f-79acbd70001c\"}",
            SalesMutationOperationNames.QuickSaleUpdate);

        for (int attempt = 1; attempt <= options.MaxAuthenticationDeliveryAttempts; attempt++) {
            SalesMutationDeliveryResult result =
                await dispatcher.ProcessNextAsync(CancellationToken.None);

            Assert.Equal(attempt, result.AttemptCount);
            Assert.Equal((int) statusCode, result.StatusCode);
            Assert.Equal(
                attempt < options.MaxAuthenticationDeliveryAttempts
                    ? SalesMutationDeliveryKind.Retrying
                    : SalesMutationDeliveryKind.DeadLettered,
                result.Kind);
            clock.Advance(options.MaxRetryDelay);
        }

        SalesMutationDeliveryResult afterLimit =
            await dispatcher.ProcessNextAsync(CancellationToken.None);
        InMemoryOutboxStore.EntrySnapshot entry = store.Get(operationNetUid);

        Assert.Equal(SalesMutationDeliveryKind.None, afterLimit.Kind);
        Assert.Equal(options.MaxAuthenticationDeliveryAttempts, handler.Requests.Count);
        Assert.Equal(SalesMutationOutboxStatus.DeadLetter, entry.Status);
        Assert.Equal(options.MaxAuthenticationDeliveryAttempts, entry.AuthenticationFailureCount);
        Assert.DoesNotContain(InternalApiKey, entry.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FirstAuthenticationDeliveryFailureImmediatelyFailsHealth() {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        SalesMutationOutboxPublisher publisher = new(store, clock);
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        SalesMutationOutboxOptions options = CreateOptions();
        SalesMutationOutboxDispatcher dispatcher = CreateDispatcher(store, handler, clock, options);
        await publisher.EnqueueAsync(
            AllowedTargetUri,
            "{\"NetUid\":\"d52f93ac-9915-4ce6-9601-c3fb20a41f89\"}",
            SalesMutationOperationNames.OrderInvoiceSaleUpdate);
        SalesMutationDeliveryResult delivery =
            await dispatcher.ProcessNextAsync(CancellationToken.None);

        ServiceCollection services = new();
        services.AddScoped<ISalesMutationOutboxStore>(_ => store);
        using ServiceProvider provider = services.BuildServiceProvider();
        SalesMutationOutboxHealthCheck healthCheck = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            CreateInternalAuthOptions(),
            clock);
        HealthCheckResult health = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(),
            CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Retrying, delivery.Kind);
        Assert.Equal(HealthStatus.Unhealthy, health.Status);
        Assert.Equal(1, health.Data["authenticationFailures"]);
        Assert.Equal(0, health.Data["deadLetters"]);
    }

    [Fact]
    public async Task ExpiredLeaseIsReplayedAfterProcessRestart() {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore durableStore = new();
        SalesMutationOutboxPublisher publisher = new(durableStore, clock);
        SalesMutationOutboxOptions options = CreateOptions();

        Guid operationNetUid = await publisher.EnqueueAsync(
            "https://crm.example/api/v1/uk/sales/update/ecommerce",
            "{\"NetUid\":\"dc333ce9-28d3-4e47-983c-9bb2adff30d2\"}",
            SalesMutationOperationNames.QuickSaleUpdate);
        SalesMutationOutboxLease abandonedLease = await durableStore.ClaimNextAsync(
            clock.GetUtcNow().UtcDateTime,
            options.LeaseDuration,
            CancellationToken.None);
        Assert.Equal(operationNetUid, abandonedLease.OperationNetUid);

        clock.Advance(options.LeaseDuration + TimeSpan.FromMilliseconds(1));
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        SalesMutationOutboxDispatcher restartedProcess =
            CreateDispatcher(durableStore, handler, clock, options);

        SalesMutationDeliveryResult replayed =
            await restartedProcess.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.Completed, replayed.Kind);
        Assert.Equal(operationNetUid, replayed.OperationNetUid);
        Assert.Equal(2, replayed.AttemptCount);
        Assert.Equal(operationNetUid.ToString("D"), Assert.Single(handler.Requests).IdempotencyKey);
    }

    [Fact]
    public async Task NonRetryableResponseIsDeadLetteredAndFailsHealth() {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        SalesMutationOutboxPublisher publisher = new(store, clock);
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        SalesMutationOutboxOptions options = CreateOptions();
        SalesMutationOutboxDispatcher dispatcher = CreateDispatcher(store, handler, clock, options);
        Guid operationNetUid = await publisher.EnqueueAsync(
            "https://crm.example/api/v1/uk/sales/update/ecommerce",
            "{\"NetUid\":\"eed7896f-65a4-4c25-89c7-3101a4f042e9\"}",
            SalesMutationOperationNames.OrderInvoiceSaleUpdate);

        SalesMutationDeliveryResult delivery =
            await dispatcher.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(SalesMutationDeliveryKind.DeadLettered, delivery.Kind);
        Assert.Equal(SalesMutationOutboxStatus.DeadLetter, store.Get(operationNetUid).Status);

        ServiceCollection services = new();
        services.AddScoped<ISalesMutationOutboxStore>(_ => store);
        using ServiceProvider provider = services.BuildServiceProvider();
        SalesMutationOutboxHealthCheck healthCheck = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            CreateInternalAuthOptions(),
            clock);
        HealthCheckResult health = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(),
            CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, health.Status);
        Assert.Equal(1, health.Data["deadLetters"]);
    }

    [Fact]
    public async Task MissingInternalApiKeyFailsReadinessBeforeOutboxQuery() {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new();
        ServiceCollection services = new();
        services.AddScoped<ISalesMutationOutboxStore>(_ => store);
        using ServiceProvider provider = services.BuildServiceProvider();
        SalesMutationOutboxHealthCheck healthCheck = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            CreateOptions(),
            new SalesMutationInternalAuthOptions(),
            clock);

        HealthCheckResult health = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(),
            CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, health.Status);
        Assert.Empty(health.Data);
    }

    [Fact]
    public async Task DurableEnqueueFailureIsObservedByTheCaller() {
        MutableTimeProvider clock = new(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));
        InMemoryOutboxStore store = new() {
            EnqueueException = new InvalidOperationException("SQL unavailable")
        };
        SalesMutationOutboxPublisher publisher = new(store, clock);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.EnqueueAsync(
                "https://crm.example/api/v1/uk/sales/update/ecommerce",
                "{\"NetUid\":\"9788bc91-83af-4d06-abf6-f5a1217973b2\"}",
                SalesMutationOperationNames.RetailSaleUpdate));

        Assert.Equal("SQL unavailable", exception.Message);
    }

    [Fact]
    public void OrderServiceUsesDurablePublisherForExactlyThreeSaleUpdatePaths() {
        string source = ReadRepositoryFile(
            "src",
            "GBA.Services",
            "Services",
            "Orders",
            "OrderService.cs");

        Assert.DoesNotContain("QueueEcommerceSaleUpdate", source, StringComparison.Ordinal);
        Assert.Equal(3, Count(source, "await PersistEcommerceSaleUpdateAsync("));
        Assert.Contains(
            "SalesMutationOperationNames.OrderInvoiceSaleUpdate",
            ExtractMethod(source, "GenerateNewSaleWithInvoice"),
            StringComparison.Ordinal);
        Assert.Contains(
            "SalesMutationOperationNames.RetailSaleUpdate",
            ExtractMethod(source, "GenerateNewRetailSale"),
            StringComparison.Ordinal);
        Assert.Contains(
            "SalesMutationOperationNames.QuickSaleUpdate",
            ExtractMethod(source, "GenerateNewQuickSaleWithInvoice"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void OrderInvoiceOutboxPayloadCarriesTheCommittedTtnFinalizeIntent() {
        string source = ReadRepositoryFile(
            "src",
            "GBA.Services",
            "Services",
            "Orders",
            "OrderService.cs");
        string method = ExtractMethod(source, "GenerateNewSaleWithInvoice");

        int bindTtn = method.IndexOf(
            "createdSale.CustomersOwnTtn = sale.CustomersOwnTtn;",
            StringComparison.Ordinal);
        int serialize = method.IndexOf(
            "string payload = JsonSerializer.Serialize(createdSale);",
            StringComparison.Ordinal);
        int durableEnqueue = method.IndexOf(
            "await PersistEcommerceSaleUpdateAsync(",
            StringComparison.Ordinal);
        int commit = method.IndexOf("transaction.Complete();", StringComparison.Ordinal);

        Assert.True(bindTtn >= 0);
        Assert.True(serialize > bindTtn);
        Assert.True(durableEnqueue > serialize);
        Assert.True(commit > durableEnqueue);
    }

    public static TheoryData<string, bool> AggregateAtomicSalePaths => new() {
        {"GenerateNewSaleWithInvoice", false},
        {"GenerateNewRetailSale", true},
        {"GenerateNewQuickSaleWithInvoice", true}
    };

    [Theory]
    [MemberData(nameof(AggregateAtomicSalePaths))]
    public void EverySaleCreationPathWrapsTheCompleteLocalAggregateAndOutbox(
        string methodName,
        bool hasRetailPayment) {
        string source = ReadRepositoryFile(
            "src",
            "GBA.Services",
            "Services",
            "Orders",
            "OrderService.cs");
        string method = ExtractMethod(source, methodName);

        int transactionStart = method.IndexOf(
            "using (TransactionScope transaction = CreateSalesMutationTransaction(connection))",
            StringComparison.Ordinal);
        int ledgerReservation = method.IndexOf(
            "await ReserveSalesCreationAsync(connection, creationRequest)",
            StringComparison.Ordinal);
        int productLocks = method.IndexOf(
            "await AcquireProductMutationLocksAsync(connection, sale.Order.OrderItems)",
            StringComparison.Ordinal);
        int firstLocalWrite = method.IndexOf(
            "order.Id = _saleRepositoriesFactory",
            StringComparison.Ordinal);
        int outboxEnqueue = method.IndexOf(
            "await PersistEcommerceSaleUpdateAsync(",
            StringComparison.Ordinal);
        int ledgerCompletion = method.IndexOf(
            "await CompleteSalesCreationAsync(",
            StringComparison.Ordinal);
        int durableReceipt = method.IndexOf(
            "SerializeSalesCreationReceipt(createdSale",
            StringComparison.Ordinal);
        int transactionComplete = method.IndexOf("transaction.Complete();", StringComparison.Ordinal);

        Assert.True(transactionStart >= 0);
        Assert.True(ledgerReservation > transactionStart);
        Assert.True(productLocks > ledgerReservation);
        Assert.True(firstLocalWrite > productLocks);
        Assert.True(outboxEnqueue > firstLocalWrite);
        Assert.True(ledgerCompletion > outboxEnqueue);
        Assert.True(durableReceipt > ledgerCompletion);
        Assert.True(transactionComplete > ledgerCompletion);
        Assert.Equal(1, Count(method, "CreateSalesMutationTransaction(connection)"));
        Assert.Equal(1, Count(method, "transaction.Complete();"));
        Assert.DoesNotContain("BackgroundSyncRunner.Run", method, StringComparison.Ordinal);
        Assert.True(method.IndexOf("_reindexSignal.Request", StringComparison.Ordinal) > transactionComplete);
        Assert.True(method.IndexOf("QueueSaleSync", StringComparison.Ordinal) > transactionComplete);

        if (hasRetailPayment) {
            Assert.InRange(
                method.IndexOf("NewRetailPaymentStatusRepository", StringComparison.Ordinal),
                outboxEnqueue + 1,
                transactionComplete - 1);
            Assert.InRange(
                method.IndexOf("NewRetailClientPaymentImageRepository", StringComparison.Ordinal),
                outboxEnqueue + 1,
                transactionComplete - 1);
            Assert.InRange(
                method.IndexOf("paymentLink = await _paymentLinkService", StringComparison.Ordinal),
                outboxEnqueue + 1,
                transactionComplete - 1);
            Assert.Contains(
                "SerializeSalesCreationReceipt(createdSale, paymentLink)",
                method,
                StringComparison.Ordinal);
            Assert.Contains(
                "ResolveReplayPaymentLinkAsync",
                method,
                StringComparison.Ordinal);
        }
    }

    private static SalesMutationOutboxDispatcher CreateDispatcher(
        ISalesMutationOutboxStore store,
        HttpMessageHandler handler,
        TimeProvider clock,
        SalesMutationOutboxOptions? options = null) =>
        new(
            store,
            new StubHttpClientFactory(handler),
            options ?? CreateOptions(),
            CreateInternalAuthOptions(),
            clock);

    private static SalesMutationInternalAuthOptions CreateInternalAuthOptions() => new() {
        ApiKey = InternalApiKey
    };

    [Fact]
    public void CreationLedgerSchemaGuardValidatesPhysicalShapeAndConstraints() {
        string source = ReadRepositoryFile(
            "src",
            "GBA.Services",
            "Infrastructure",
            "SalesMutations",
            "SqlSalesCreationLedgerStore.cs");

        Assert.Contains("FROM sys.columns AS column_definition", source, StringComparison.Ordinal);
        Assert.Contains("INNER JOIN sys.types AS type_definition", source, StringComparison.Ordinal);
        Assert.Contains("FROM sys.key_constraints AS key_constraint", source, StringComparison.Ordinal);
        Assert.Contains("FROM sys.check_constraints AS check_constraint", source, StringComparison.Ordinal);
        Assert.Contains("check_constraint.is_not_trusted = 0", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SalesMutationTargetComesOnlyFromTheValidatedInternalOrigin() {
        string source = ReadRepositoryFile(
            "src",
            "GBA.Services",
            "Services",
            "Orders",
            "OrderService.cs");

        Assert.Contains("_salesMutationInternalBaseUri", source, StringComparison.Ordinal);
        Assert.Contains("GetValidatedAllowedInternalBaseUri", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce",
            source,
            StringComparison.Ordinal);
    }

    private static SalesMutationOutboxOptions CreateOptions() => new() {
        AllowedInternalBaseUri = AllowedInternalBaseUri,
        PollInterval = TimeSpan.FromMilliseconds(10),
        LeaseDuration = TimeSpan.FromSeconds(2),
        RequestTimeout = TimeSpan.FromSeconds(1),
        InitialRetryDelay = TimeSpan.FromSeconds(1),
        MaxRetryDelay = TimeSpan.FromMinutes(1),
        MaxAuthenticationDeliveryAttempts = 3,
        PendingUnhealthyAfter = TimeSpan.FromMinutes(1),
        DispatchCompletedRetention = TimeSpan.FromDays(1),
        CleanupInterval = TimeSpan.FromMinutes(1)
    };

    private static Guid ReadBodyOperationKey(string payload) {
        using JsonDocument document = JsonDocument.Parse(payload);
        return document.RootElement.GetProperty(SalesMutationRequestKey.BodyPropertyName).GetGuid();
    }

    private static int Count(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;

    private static string ExtractMethod(string source, string methodName) {
        int methodStart = source.IndexOf($" {methodName}(", StringComparison.Ordinal);
        Assert.True(methodStart >= 0, $"Method {methodName} was not found.");
        int methodEnd = source.IndexOf("\n    public ", methodStart + 1, StringComparison.Ordinal);
        return methodEnd < 0
            ? source[methodStart..]
            : source[methodStart..methodEnd];
    }

    private static string ReadRepositoryFile(params string[] pathParts) {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current != null) {
            string candidate = Path.Combine([current.FullName, .. pathParts]);
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate repository file {Path.Combine(pathParts)}.");
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public int CreateClientCount { get; private set; }
        public List<string> RequestedClientNames { get; } = [];

        public HttpClient CreateClient(string name) {
            CreateClientCount++;
            RequestedClientNames.Add(name);
            return new HttpClient(handler, disposeHandler: false);
        }
    }

    private sealed record RecordedRequest(
        string IdempotencyKey,
        string InternalApiKey,
        string Body,
        string RequestUri,
        HttpMethod Method);

    private sealed class RecordingHandler : HttpMessageHandler {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _response;

        public RecordingHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) =>
            _response = response;

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            string body = request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            string idempotencyKey = request.Headers
                .GetValues(SalesMutationRequestKey.HeaderName)
                .Single();
            string internalApiKey = request.Headers
                .GetValues(SalesMutationInternalAuthOptions.HeaderName)
                .Single();
            Requests.Add(new RecordedRequest(
                idempotencyKey,
                internalApiKey,
                body,
                request.RequestUri?.AbsoluteUri ?? string.Empty,
                request.Method));
            return await _response(request, cancellationToken);
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan value) => _utcNow = _utcNow.Add(value);
    }

    private sealed class InMemoryOutboxStore : ISalesMutationOutboxStore {
        private readonly object _gate = new();
        private readonly Dictionary<Guid, Entry> _entries = [];

        public Exception? EnqueueException { get; init; }

        public Task EnsureSchemaAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<SalesMutationOutboxMessage> GetAsync(
            Guid operationNetUid,
            CancellationToken cancellationToken) {
            lock (_gate) {
                return Task.FromResult(
                    _entries.TryGetValue(operationNetUid, out Entry? entry)
                        ? entry.Message
                        : null!);
            }
        }

        public Task EnqueueAsync(SalesMutationOutboxMessage message, CancellationToken cancellationToken) {
            if (EnqueueException != null) throw EnqueueException;
            lock (_gate) {
                if (_entries.TryGetValue(message.OperationNetUid, out Entry? existing)) {
                    Assert.Equal(existing.Message.OperationName, message.OperationName);
                    Assert.Equal(existing.Message.RequestUrl, message.RequestUrl);
                    Assert.Equal(existing.Message.Payload, message.Payload);
                    Assert.Equal(existing.Message.PayloadSha256, message.PayloadSha256);
                } else {
                    _entries.Add(message.OperationNetUid, new Entry(message));
                }
            }

            return Task.CompletedTask;
        }

        public Task EnqueueAsync(
            IDbConnection connection,
            SalesMutationOutboxMessage message,
            CancellationToken cancellationToken) => EnqueueAsync(message, cancellationToken);

        public Task<SalesMutationOutboxLease> ClaimNextAsync(
            DateTime utcNow,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken) {
            lock (_gate) {
                Entry? entry = _entries.Values
                    .Where(candidate =>
                        candidate.Status == SalesMutationOutboxStatus.Pending &&
                        candidate.NextAttemptUtc <= utcNow ||
                        candidate.Status == SalesMutationOutboxStatus.Leased &&
                        candidate.LeaseExpiresUtc <= utcNow)
                    .OrderBy(candidate => candidate.NextAttemptUtc)
                    .ThenBy(candidate => candidate.Message.CreatedUtc)
                    .FirstOrDefault();
                if (entry == null) return Task.FromResult<SalesMutationOutboxLease>(null!);

                entry.Status = SalesMutationOutboxStatus.Leased;
                entry.AttemptCount++;
                entry.LeaseToken = Guid.NewGuid();
                entry.LeaseExpiresUtc = utcNow.Add(leaseDuration);
                return Task.FromResult(new SalesMutationOutboxLease {
                    OperationNetUid = entry.Message.OperationNetUid,
                    OperationName = entry.Message.OperationName,
                    RequestUrl = entry.Message.RequestUrl,
                    Payload = entry.Message.Payload,
                    LeaseToken = entry.LeaseToken.Value,
                    AttemptCount = entry.AttemptCount,
                    AuthenticationFailureCount = entry.AuthenticationFailureCount
                });
            }
        }

        public Task<bool> CompleteAsync(
            Guid operationNetUid,
            Guid leaseToken,
            DateTime utcNow,
            CancellationToken cancellationToken) =>
            UpdateLeased(operationNetUid, leaseToken, entry => {
                entry.Status = SalesMutationOutboxStatus.Completed;
                entry.LeaseToken = null;
                entry.LeaseExpiresUtc = null;
                entry.LastError = string.Empty;
            });

        public Task<bool> RetryAsync(
            Guid operationNetUid,
            Guid leaseToken,
            DateTime utcNow,
            DateTime nextAttemptUtc,
            string lastError,
            SalesMutationDeliveryFailureKind failureKind,
            CancellationToken cancellationToken) =>
            UpdateLeased(operationNetUid, leaseToken, entry => {
                entry.Status = SalesMutationOutboxStatus.Pending;
                entry.NextAttemptUtc = nextAttemptUtc;
                entry.LeaseToken = null;
                entry.LeaseExpiresUtc = null;
                entry.LastError = lastError;
                if (failureKind == SalesMutationDeliveryFailureKind.Authentication)
                    entry.AuthenticationFailureCount++;
            });

        public Task<bool> DeadLetterAsync(
            Guid operationNetUid,
            Guid leaseToken,
            DateTime utcNow,
            string lastError,
            SalesMutationDeliveryFailureKind failureKind,
            CancellationToken cancellationToken) =>
            UpdateLeased(operationNetUid, leaseToken, entry => {
                entry.Status = SalesMutationOutboxStatus.DeadLetter;
                entry.LeaseToken = null;
                entry.LeaseExpiresUtc = null;
                entry.LastError = lastError;
                if (failureKind == SalesMutationDeliveryFailureKind.Authentication)
                    entry.AuthenticationFailureCount++;
            });

        public Task<int> DeleteCompletedBeforeAsync(
            DateTime cutoffUtc,
            CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<SalesMutationOutboxStats> GetStatsAsync(CancellationToken cancellationToken) {
            lock (_gate) {
                return Task.FromResult(new SalesMutationOutboxStats {
                    PendingCount = _entries.Values.Count(entry => entry.Status == SalesMutationOutboxStatus.Pending),
                    LeasedCount = _entries.Values.Count(entry => entry.Status == SalesMutationOutboxStatus.Leased),
                    DeadLetterCount = _entries.Values.Count(entry => entry.Status == SalesMutationOutboxStatus.DeadLetter),
                    AuthenticationFailureCount = _entries.Values.Count(entry =>
                        (entry.Status is SalesMutationOutboxStatus.Pending or SalesMutationOutboxStatus.Leased) &&
                        entry.AuthenticationFailureCount > 0),
                    OldestPendingUtc = _entries.Values
                        .Where(entry => entry.Status is SalesMutationOutboxStatus.Pending or SalesMutationOutboxStatus.Leased)
                        .Select(entry => (DateTime?)entry.Message.CreatedUtc)
                        .Min()
                });
            }
        }

        public EntrySnapshot Get(Guid operationNetUid) {
            lock (_gate) {
                Entry entry = _entries[operationNetUid];
                return new EntrySnapshot(
                    entry.Status,
                    entry.AttemptCount,
                    entry.AuthenticationFailureCount,
                    entry.NextAttemptUtc,
                    entry.LastError);
            }
        }

        private Task<bool> UpdateLeased(Guid operationNetUid, Guid leaseToken, Action<Entry> update) {
            lock (_gate) {
                if (!_entries.TryGetValue(operationNetUid, out Entry? entry) ||
                    entry.Status != SalesMutationOutboxStatus.Leased ||
                    entry.LeaseToken != leaseToken)
                    return Task.FromResult(false);
                update(entry);
                return Task.FromResult(true);
            }
        }

        public readonly record struct EntrySnapshot(
            SalesMutationOutboxStatus Status,
            int AttemptCount,
            int AuthenticationFailureCount,
            DateTime NextAttemptUtc,
            string LastError);

        private sealed class Entry(SalesMutationOutboxMessage message) {
            public SalesMutationOutboxMessage Message { get; } = message;
            public SalesMutationOutboxStatus Status { get; set; } = SalesMutationOutboxStatus.Pending;
            public int AttemptCount { get; set; }
            public int AuthenticationFailureCount { get; set; }
            public DateTime NextAttemptUtc { get; set; } = message.NextAttemptUtc;
            public Guid? LeaseToken { get; set; }
            public DateTime? LeaseExpiresUtc { get; set; }
            public string LastError { get; set; } = string.Empty;
        }
    }
}

[Collection("EcommerceSqlIntegration")]
public sealed class SalesMutationOutboxSqlServerTests {
    [Fact]
    public async Task LeaseSurvivesStoreRestartAndCanBeReclaimed() {
        string? connectionString = GetConnectionString();
        if (connectionString == null) return;

        TestDbConnectionFactory factory = new(connectionString);
        SqlSalesMutationOutboxStore firstProcess = new(factory);
        await firstProcess.EnsureSchemaAsync(CancellationToken.None);

        Guid operationNetUid = Guid.NewGuid();
        DateTime utcNow = new(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc);
        string payload = $"{{\"OperationNetUid\":\"{operationNetUid:D}\",\"NetUid\":\"3fbef642-49ee-4a85-a1db-64de75307bd7\"}}";
        SalesMutationOutboxMessage message = new() {
            OperationNetUid = operationNetUid,
            OperationName = SalesMutationOperationNames.OrderInvoiceSaleUpdate,
            RequestUrl = "https://crm.example/api/v1/uk/sales/update/ecommerce",
            Payload = payload,
            PayloadSha256 = SHA256.HashData(Encoding.UTF8.GetBytes(payload)),
            Status = SalesMutationOutboxStatus.Pending,
            AttemptCount = 0,
            NextAttemptUtc = utcNow,
            CreatedUtc = utcNow
        };

        await DeleteAsync(connectionString, operationNetUid);
        try {
            await firstProcess.EnqueueAsync(message, CancellationToken.None);
            SalesMutationOutboxMessage persisted = await firstProcess.GetAsync(
                operationNetUid,
                CancellationToken.None);
            Assert.Equal(message.OperationName, persisted.OperationName);
            Assert.Equal(message.Payload, persisted.Payload);

            SalesMutationOutboxLease abandoned = await firstProcess.ClaimNextAsync(
                utcNow,
                TimeSpan.FromMinutes(1),
                CancellationToken.None);
            Assert.Equal(operationNetUid, abandoned.OperationNetUid);

            SqlSalesMutationOutboxStore restartedProcess = new(factory);
            SalesMutationOutboxLease tooEarly = await restartedProcess.ClaimNextAsync(
                utcNow.AddSeconds(30),
                TimeSpan.FromMinutes(1),
                CancellationToken.None);
            Assert.Null(tooEarly);

            SalesMutationOutboxLease replayed = await restartedProcess.ClaimNextAsync(
                utcNow.AddMinutes(1).AddMilliseconds(1),
                TimeSpan.FromMinutes(1),
                CancellationToken.None);
            Assert.Equal(operationNetUid, replayed.OperationNetUid);
            Assert.Equal(2, replayed.AttemptCount);
            Assert.True(await restartedProcess.CompleteAsync(
                operationNetUid,
                replayed.LeaseToken,
                utcNow.AddMinutes(1).AddSeconds(1),
                CancellationToken.None));
        } finally {
            await DeleteAsync(connectionString, operationNetUid);
        }
    }

    [Fact]
    public async Task CompleteSaleAggregateAndOutboxCommitOrRollbackTogether() {
        string? connectionString = GetConnectionString();
        if (connectionString == null) return;

        TestDbConnectionFactory factory = new(connectionString);
        SqlSalesMutationOutboxStore store = new(factory);
        await store.EnsureSchemaAsync(CancellationToken.None);
        await using (SqlConnection setup = new(connectionString)) {
            await setup.ExecuteAsync("""
IF OBJECT_ID(N'dbo.EcommerceSalesAggregateAtomicityProbe', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EcommerceSalesAggregateAtomicityProbe (
        OperationNetUid uniqueidentifier NOT NULL,
        Component nvarchar(32) NOT NULL,
        CONSTRAINT PK_EcommerceSalesAggregateAtomicityProbe
            PRIMARY KEY (OperationNetUid, Component)
    );
END;
""");
        }

        Guid rolledBackKey = Guid.NewGuid();
        Guid committedKey = Guid.NewGuid();
        await DeleteProbeAsync(connectionString, rolledBackKey);
        await DeleteProbeAsync(connectionString, committedKey);

        try {
            using (IDbConnection connection = factory.NewSqlConnection())
            using (TransactionScope transaction = CreateTransactionScope()) {
                await WriteAggregateProbeAsync(connection, rolledBackKey);
                await store.EnqueueAsync(
                    connection,
                    CreateMessage(rolledBackKey),
                    CancellationToken.None);
            }

            Assert.Equal((0, 0), await ReadProbeCountsAsync(connectionString, rolledBackKey));

            using (IDbConnection connection = factory.NewSqlConnection())
            using (TransactionScope transaction = CreateTransactionScope()) {
                await WriteAggregateProbeAsync(connection, committedKey);
                await store.EnqueueAsync(
                    connection,
                    CreateMessage(committedKey),
                    CancellationToken.None);
                transaction.Complete();
            }

            Assert.Equal((8, 1), await ReadProbeCountsAsync(connectionString, committedKey));
        } finally {
            await DeleteProbeAsync(connectionString, rolledBackKey);
            await DeleteProbeAsync(connectionString, committedKey);
        }
    }

    private static TransactionScope CreateTransactionScope() =>
        new(
            TransactionScopeOption.Required,
            new TransactionOptions {
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromMinutes(1)
            },
            TransactionScopeAsyncFlowOption.Enabled);

    private static SalesMutationOutboxMessage CreateMessage(Guid operationNetUid) {
        DateTime utcNow = new(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc);
        string payload = $"{{\"OperationNetUid\":\"{operationNetUid:D}\",\"NetUid\":\"95483133-e651-4d4a-aac0-b0792142b37b\"}}";
        return new SalesMutationOutboxMessage {
            OperationNetUid = operationNetUid,
            OperationName = SalesMutationOperationNames.OrderInvoiceSaleUpdate,
            RequestUrl = "https://crm.example/api/v1/uk/sales/update/ecommerce",
            Payload = payload,
            PayloadSha256 = SHA256.HashData(Encoding.UTF8.GetBytes(payload)),
            Status = SalesMutationOutboxStatus.Pending,
            AttemptCount = 0,
            NextAttemptUtc = utcNow,
            CreatedUtc = utcNow
        };
    }

    private static Task<int> WriteAggregateProbeAsync(
        IDbConnection connection,
        Guid operationNetUid) =>
        connection.ExecuteAsync(
            """
INSERT INTO dbo.EcommerceSalesAggregateAtomicityProbe (OperationNetUid, Component)
VALUES
    (@OperationNetUid, N'Order'),
    (@OperationNetUid, N'OrderItem'),
    (@OperationNetUid, N'Availability'),
    (@OperationNetUid, N'Reservation'),
    (@OperationNetUid, N'LifecycleAndPayment'),
    (@OperationNetUid, N'SaleNumber'),
    (@OperationNetUid, N'Sale'),
    (@OperationNetUid, N'RetailPayment');
""",
            new { OperationNetUid = operationNetUid });

    private static async Task<(int AggregateRows, int OutboxRows)> ReadProbeCountsAsync(
        string connectionString,
        Guid operationNetUid) {
        await using SqlConnection connection = new(connectionString);
        int probeRows = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.EcommerceSalesAggregateAtomicityProbe WHERE OperationNetUid = @OperationNetUid;",
            new { OperationNetUid = operationNetUid });
        int outboxRows = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.EcommerceSalesMutationOutbox WHERE OperationNetUid = @OperationNetUid;",
            new { OperationNetUid = operationNetUid });
        return (probeRows, outboxRows);
    }

    private static async Task DeleteProbeAsync(string connectionString, Guid operationNetUid) {
        await using SqlConnection connection = new(connectionString);
        await connection.ExecuteAsync(
            """
DELETE FROM dbo.EcommerceSalesMutationOutbox WHERE OperationNetUid = @OperationNetUid;
DELETE FROM dbo.EcommerceSalesAggregateAtomicityProbe WHERE OperationNetUid = @OperationNetUid;
""",
            new { OperationNetUid = operationNetUid });
    }

    private static string? GetConnectionString() {
        string? connectionString = Environment.GetEnvironmentVariable(
            "GBA_ECOMMERCE_SQL_INTEGRATION_CONNECTION_STRING");
        bool required = string.Equals(
            Environment.GetEnvironmentVariable("GBA_ECOMMERCE_SQL_INTEGRATION_REQUIRED"),
            "1",
            StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(connectionString) && required)
            throw new InvalidOperationException(
                "GBA_ECOMMERCE_SQL_INTEGRATION_CONNECTION_STRING is required for this test run.");
        return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString;
    }

    private static async Task DeleteAsync(string connectionString, Guid operationNetUid) {
        await using SqlConnection connection = new(connectionString);
        await connection.ExecuteAsync(
            "DELETE FROM dbo.EcommerceSalesMutationOutbox WHERE OperationNetUid = @OperationNetUid;",
            new { OperationNetUid = operationNetUid });
    }

    private sealed class TestDbConnectionFactory(string connectionString) : IDbConnectionFactory {
        public System.Data.IDbConnection NewSqlConnection() => new SqlConnection(connectionString);
        public System.Data.IDbConnection NewDataAnalyticSqlConnection() => throw new NotSupportedException();
        public System.Data.IDbConnection NewIdentitySqlConnection() => throw new NotSupportedException();
        public System.Data.IDbConnection NewFenixOneCSqlConnection() => throw new NotSupportedException();
        public System.Data.IDbConnection NewAmgOneCSqlConnection() => throw new NotSupportedException();
    }
}
