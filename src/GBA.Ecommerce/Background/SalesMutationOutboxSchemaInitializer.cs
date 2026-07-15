using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Services.Infrastructure.SalesMutations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GBA.Ecommerce.Background;

public sealed class SalesMutationOutboxSchemaInitializer : IHostedService {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SalesMutationOutboxOptions _options;
    private readonly ILogger<SalesMutationOutboxSchemaInitializer> _logger;

    public SalesMutationOutboxSchemaInitializer(
        IServiceScopeFactory scopeFactory,
        SalesMutationOutboxOptions options,
        ILogger<SalesMutationOutboxSchemaInitializer> logger) {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _options.Validate();
        using IServiceScope scope = _scopeFactory.CreateScope();
        ISalesCreationLedgerStore creationLedgerStore =
            scope.ServiceProvider.GetRequiredService<ISalesCreationLedgerStore>();
        ISalesMutationOutboxStore store =
            scope.ServiceProvider.GetRequiredService<ISalesMutationOutboxStore>();
        await creationLedgerStore.EnsureSchemaAsync(cancellationToken);
        await store.EnsureSchemaAsync(cancellationToken);
        _logger.LogInformation(
            "Durable ecommerce sales creation ledger and mutation outbox schemas are ready");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
