using System;
using GBA.Services.Infrastructure.SalesMutations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GBA.Ecommerce.DependencyInjection;

/// <summary>Registers fail-closed authentication for durable CRM sales mutations.</summary>
public static class SalesMutationInternalAuthServiceCollectionExtensions {
    /// <summary>Loads and validates the shared internal-service key during startup.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddSalesMutationInternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        SalesMutationInternalAuthOptions options =
            configuration.GetSection(SalesMutationInternalAuthOptions.SectionName)
                .Get<SalesMutationInternalAuthOptions>() ??
            new SalesMutationInternalAuthOptions();
        options.GetValidatedApiKey();
        services.AddSingleton(options);
        return services;
    }
}
