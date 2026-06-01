using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Services.Services.Clients.Contracts;
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

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredCartCleanupBackgroundService> _log;

    public ExpiredCartCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredCartCleanupBackgroundService> logger) {
        _scopeFactory = scopeFactory;
        _log = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        // Stagger after startup so it does not contend with the initial search rebuild.
        if (!await DelayAsync(TimeSpan.FromMinutes(2), stoppingToken)) return;

        _log.LogInformation("Expired-cart cleanup started: every {Interval}", Interval);

        while (!stoppingToken.IsCancellationRequested) {
            try {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IClientShoppingCartService cartService = scope.ServiceProvider.GetRequiredService<IClientShoppingCartService>();

                int released = await cartService.ReleaseExpiredCartsAsync();
                if (released > 0) {
                    _log.LogInformation("Expired-cart cleanup released {Released} reservations back to stock", released);
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                _log.LogError(ex, "Expired-cart cleanup iteration failed; retrying next interval");
            }

            if (!await DelayAsync(Interval, stoppingToken)) break;
        }
    }

    private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken ct) {
        try {
            await Task.Delay(delay, ct);
            return true;
        } catch (OperationCanceledException) {
            return false;
        }
    }
}
