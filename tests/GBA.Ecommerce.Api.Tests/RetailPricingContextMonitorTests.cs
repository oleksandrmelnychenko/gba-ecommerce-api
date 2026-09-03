using System.Data.Common;
using GBA.Ecommerce.Background;
using Microsoft.Extensions.Logging;

namespace GBA.Ecommerce.Api.Tests;

public sealed class RetailPricingContextMonitorTests {
    [Fact]
    public void First_database_failure_is_recoverable_warning() {
        CollectingLogger<RetailPricingContextMonitor> logger = new();

        RetailPricingContextMonitor.LogDatabaseFailure(
            logger,
            new TestDbException(),
            consecutiveFailure: 1);

        Assert.Equal([LogLevel.Warning], logger.Levels);
    }

    [Fact]
    public void Persistent_database_failure_escalates_to_error() {
        CollectingLogger<RetailPricingContextMonitor> logger = new();

        RetailPricingContextMonitor.LogDatabaseFailure(
            logger,
            new TestDbException(),
            RetailPricingContextMonitor.PersistentDatabaseFailureThreshold);

        Assert.Equal([LogLevel.Error], logger.Levels);
    }

    private sealed class TestDbException : DbException;

    private sealed class CollectingLogger<T> : ILogger<T> {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            Levels.Add(logLevel);
        }
    }

    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();

        public void Dispose() {
        }
    }
}
