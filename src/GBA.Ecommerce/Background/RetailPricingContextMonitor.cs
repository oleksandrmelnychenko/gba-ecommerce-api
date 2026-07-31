using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Ecommerce.Hubs;
using GBA.Services.Services.Products;
using GBA.Services.Services.Products.Contracts;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GBA.Ecommerce.Background;

/// <summary>
/// Detects changes to the configured ShopClient agreement and invalidates every storefront
/// pricing layer before notifying connected browsers.
/// </summary>
public sealed class RetailPricingContextMonitor(
    IServiceScopeFactory scopeFactory,
    IPriceCacheService priceCacheService,
    IOutputCacheStore outputCacheStore,
    IHubContext<StorefrontHub> hubContext,
    ILogger<RetailPricingContextMonitor> logger) : BackgroundService {
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        string? previousCacheKey = null;

        while (!stoppingToken.IsCancellationRequested) {
            try {
                using IServiceScope scope = scopeFactory.CreateScope();
                IProductService productService =
                    scope.ServiceProvider.GetRequiredService<IProductService>();
                ProductPricingContext context =
                    productService.GetPricingContext(Guid.Empty, false);

                if (previousCacheKey != null
                    && !string.Equals(
                        previousCacheKey,
                        context.CacheKey,
                        StringComparison.Ordinal)) {
                    priceCacheService.InvalidateForClient(Guid.Empty);
                    await outputCacheStore.EvictByTagAsync("products", stoppingToken);
                    await hubContext.Clients.All.SendAsync(
                        "StorePricingContextChanged",
                        new {
                            context.CurrencyCode,
                            context.WithVat,
                            ChangedAtUtc = DateTime.UtcNow
                        },
                        stoppingToken);

                    logger.LogInformation(
                        "Store pricing context changed; storefront caches invalidated. Currency={CurrencyCode}, WithVat={WithVat}",
                        context.CurrencyCode,
                        context.WithVat);
                }

                previousCacheKey = context.CacheKey;
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception exception) {
                logger.LogError(
                    exception,
                    "Failed to monitor the storefront pricing context.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
