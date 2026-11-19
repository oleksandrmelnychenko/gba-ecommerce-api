using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GBA.Ecommerce.Background;

/// <summary>
/// Periodically releases reservations held by expired shopping carts back into
/// ProductAvailability.Amount. Without this, stock reserved by abandoned carts is never
/// returned (the repository had the lookup but nothing called it), so products drift to 0.
/// </summary>
public sealed class ExpiredCartCleanupBackgroundService : BackgroundService {
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan TransientRetryInterval = TimeSpan.FromMinutes(1);
    private const int PersistentFailureThreshold = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredCartCleanupBackgroundService> _log;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public ExpiredCartCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredCartCleanupBackgroundService> logger)
        : this(scopeFactory, logger, Task.Delay) { }

    internal ExpiredCartCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredCartCleanupBackgroundService> logger,
        Func<TimeSpan, CancellationToken, Task> delay) {
        _scopeFactory = scopeFactory;
        _log = logger;
        _delay = delay;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        // Stagger after startup so it does not contend with the initial search rebuild.
        if (!await DelayAsync(TimeSpan.FromMinutes(2), stoppingToken)) return;

        _log.LogInformation("Expired-cart cleanup started: every {Interval}", Interval);
        int consecutiveFailures = 0;

        while (!stoppingToken.IsCancellationRequested) {
            TimeSpan nextInterval = Interval;
            try {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IClientShoppingCartService cartService = scope.ServiceProvider.GetRequiredService<IClientShoppingCartService>();

                int released = await cartService.ReleaseExpiredCartsAsync();
                if (consecutiveFailures > 0) {
                    _log.LogInformation("Expired-cart cleanup recovered after transient SQL failures");
                }
                consecutiveFailures = 0;
                if (released > 0) {
                    _log.LogInformation("Expired-cart cleanup released {Released} reservations back to stock", released);
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (SqlException ex) when (ex.Number is -2 or 258
                || ex.InnerException is SocketException { SocketErrorCode: SocketError.TryAgain or SocketError.WouldBlock }) {
                consecutiveFailures = Math.Min(consecutiveFailures + 1, PersistentFailureThreshold);
                LogLevel level = LogLevel.Error;
                if (consecutiveFailures < PersistentFailureThreshold) {
                    level = LogLevel.Warning;
                    nextInterval = TransientRetryInterval;
                }
                _log.Log(
                    level,
                    ex,
                    "Expired-cart cleanup transient SQL failure ({ConsecutiveFailure}/{ErrorThreshold}); retrying in {RetryInterval}",
                    consecutiveFailures,
                    PersistentFailureThreshold,
                    nextInterval);
            } catch (Exception ex) {
                _log.LogError(ex, "Expired-cart cleanup iteration failed; retrying next interval");
            }

            if (!await DelayAsync(nextInterval, stoppingToken)) break;
        }
    }

    private async Task<bool> DelayAsync(TimeSpan delay, CancellationToken ct) {
        try {
            await _delay(delay, ct);
            return true;
        } catch (OperationCanceledException) {
            return false;
        }
    }
}
