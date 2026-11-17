using GBA.Ecommerce.Background;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ExpiredCartCleanupBackgroundServiceTests {
    [Theory]
    [InlineData(-2)]
    [InlineData(258)]
    public async Task First_sql_timeout_warns_and_retries_then_reports_released_reservations(int number) {
        SqlException timeout = TestSqlExceptions.Create(number);

        RunResult result = await RunAsync(_ => throw timeout, _ => 2);

        LogEntry failure = Assert.Single(result.Logs, entry => entry.Exception != null);
        Assert.Equal(LogLevel.Warning, failure.Level);
        Assert.Same(timeout, failure.Exception);
        Assert.Contains("(1/5)", failure.Message);
        Assert.Contains("00:01:00", failure.Message);
        Assert.Equal(
            [TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1), TimeSpan.FromHours(1)],
            result.Delays);
        Assert.Contains(result.Logs, entry => entry.Level == LogLevel.Information
            && entry.Message == "Expired-cart cleanup released 2 reservations back to stock");
        Assert.Contains(result.Logs, entry => entry.Level == LogLevel.Information
            && entry.Message == "Expired-cart cleanup recovered after SQL timeouts");
        Assert.Equal(2, result.Calls);
        Assert.Equal(2, result.ScopesCreated);
        Assert.Equal(2, result.ScopesDisposed);
    }

    [Fact]
    public async Task Persistent_timeouts_escalate_and_back_off_until_a_success_resets_the_streak() {
        SqlException timeout = TestSqlExceptions.Create(-2);
        RunResult result = await RunAsync(
            _ => throw timeout,
            _ => throw timeout,
            _ => throw timeout,
            _ => throw timeout,
            _ => throw timeout,
            _ => throw timeout,
            _ => 0,
            _ => throw timeout,
            _ => 1);

        LogEntry[] failures = result.Logs.Where(entry => entry.Exception != null).ToArray();
        Assert.Equal(
            [LogLevel.Warning, LogLevel.Warning, LogLevel.Warning, LogLevel.Warning,
                LogLevel.Error, LogLevel.Error, LogLevel.Warning],
            failures.Select(entry => entry.Level));
        Assert.Contains("(5/5)", failures[4].Message);
        Assert.Contains("(5/5)", failures[5].Message);
        Assert.Contains("(1/5)", failures[6].Message);
        Assert.All(failures, entry => Assert.Same(timeout, entry.Exception));
        Assert.Equal(
            [TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromHours(1),
                TimeSpan.FromHours(1), TimeSpan.FromHours(1), TimeSpan.FromMinutes(1),
                TimeSpan.FromHours(1)],
            result.Delays);
        Assert.Equal(9, result.Calls);
        Assert.Equal(result.Calls, result.ScopesCreated);
        Assert.Equal(result.Calls, result.ScopesDisposed);
    }

    [Theory]
    [InlineData(18456)]
    [InlineData(208)]
    public async Task Non_timeout_sql_failures_remain_errors(int number) {
        SqlException exception = TestSqlExceptions.Create(number);

        RunResult result = await RunAsync(_ => throw exception, _ => 0);

        AssertErrorWithHourlyRetry(result, exception);
    }

    [Fact]
    public async Task Application_failures_remain_errors() {
        InvalidOperationException exception = new("Invalid cleanup state");

        RunResult result = await RunAsync(_ => throw exception, _ => 0);

        AssertErrorWithHourlyRetry(result, exception);
    }

    [Fact]
    public async Task Unexpected_cancellation_remains_an_error() {
        OperationCanceledException exception = new();

        RunResult result = await RunAsync(_ => throw exception, _ => 0);

        AssertErrorWithHourlyRetry(result, exception);
    }

    [Fact]
    public async Task Successful_empty_cleanup_keeps_the_hourly_schedule_without_failure_logs() {
        RunResult result = await RunAsync(_ => 0, _ => 0);

        Assert.Equal([TimeSpan.FromMinutes(2), TimeSpan.FromHours(1), TimeSpan.FromHours(1)], result.Delays);
        Assert.DoesNotContain(result.Logs, entry => entry.Exception != null);
        Assert.DoesNotContain(result.Logs, entry => entry.Message.Contains("released"));
        Assert.Equal(2, result.Calls);
    }

    [Fact]
    public async Task Stopping_during_startup_does_not_run_cleanup() {
        RunResult result = await RunAsync();

        Assert.Equal(0, result.Calls);
        Assert.Empty(result.Logs);
        Assert.Equal([TimeSpan.FromMinutes(2)], result.Delays);
    }

    [Fact]
    public async Task Stopping_during_cleanup_disposes_the_scope_without_logging_a_failure() {
        RunResult result = await RunAsync(stopping => {
            stopping.Cancel();
            throw new OperationCanceledException(stopping.Token);
        });

        Assert.Equal(1, result.Calls);
        Assert.Equal(1, result.ScopesDisposed);
        Assert.Equal([TimeSpan.FromMinutes(2)], result.Delays);
        Assert.DoesNotContain(result.Logs, entry => entry.Exception != null);
    }

    [Fact]
    public async Task Stopping_during_the_retry_delay_does_not_run_another_iteration() {
        RunResult result = await RunAsync(_ => throw TestSqlExceptions.Create(-2));

        Assert.Equal(1, result.Calls);
        Assert.Equal(1, result.ScopesDisposed);
        Assert.Equal([TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1)], result.Delays);
        Assert.Equal(LogLevel.Warning, Assert.Single(result.Logs, entry => entry.Exception != null).Level);
    }

    private static void AssertErrorWithHourlyRetry(RunResult result, Exception exception) {
        LogEntry failure = Assert.Single(result.Logs, entry => entry.Exception != null);
        Assert.Equal(LogLevel.Error, failure.Level);
        Assert.Same(exception, failure.Exception);
        Assert.Equal("Expired-cart cleanup iteration failed; retrying next interval", failure.Message);
        Assert.Equal([TimeSpan.FromMinutes(2), TimeSpan.FromHours(1), TimeSpan.FromHours(1)], result.Delays);
        Assert.Equal(2, result.Calls);
    }

    private static async Task<RunResult> RunAsync(params Func<CancellationTokenSource, int>[] iterations) {
        using CancellationTokenSource stopping = new();
        RunResult result = new();
        CollectingLogger logger = new(result.Logs);
        Mock<IClientShoppingCartService> cartService = new(MockBehavior.Strict);
        cartService.Setup(service => service.ReleaseExpiredCartsAsync())
            .Returns(() => Task.FromResult(iterations[result.Calls++](stopping)));
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(service => service.GetService(typeof(IClientShoppingCartService))).Returns(cartService.Object);
        Mock<IServiceScopeFactory> scopeFactory = new(MockBehavior.Strict);
        scopeFactory.Setup(factory => factory.CreateScope()).Returns(() => {
            result.ScopesCreated++;
            Mock<IServiceScope> scope = new(MockBehavior.Strict);
            scope.SetupGet(value => value.ServiceProvider).Returns(provider.Object);
            scope.Setup(value => value.Dispose()).Callback(() => result.ScopesDisposed++);
            return scope.Object;
        });

        using ExpiredCartCleanupBackgroundService service = new(scopeFactory.Object, logger, (delay, ct) => {
            result.Delays.Add(delay);
            if (result.Calls >= iterations.Length) {
                stopping.Cancel();
                return Task.FromCanceled(ct);
            }
            return Task.CompletedTask;
        });

        await service.StartAsync(stopping.Token);
        await service.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));
        await service.StopAsync(CancellationToken.None);
        cartService.Verify(value => value.ReleaseExpiredCartsAsync(), Times.Exactly(iterations.Length));
        Assert.Equal(result.ScopesCreated, result.ScopesDisposed);
        return result;
    }

    private sealed class RunResult {
        public int Calls { get; set; }
        public int ScopesCreated { get; set; }
        public int ScopesDisposed { get; set; }
        public List<TimeSpan> Delays { get; } = [];
        public List<LogEntry> Logs { get; } = [];
    }

    private sealed record LogEntry(LogLevel Level, Exception? Exception, string Message);

    private sealed class CollectingLogger(List<LogEntry> entries) : ILogger<ExpiredCartCleanupBackgroundService> {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) {
            entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
        }
    }
}
