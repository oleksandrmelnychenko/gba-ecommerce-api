using System;
using System.Data;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using GBA.Search.Services;
using GBA.Search.Sync;
using GBA.Search.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GBA.Search.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddElasticsearchSearch(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<IDbConnection> connectionFactory) {

        services.Configure<ElasticsearchSettings>(
            configuration.GetSection("Elasticsearch"));
        services.Configure<SyncSettings>(
            configuration.GetSection(SyncSettings.SectionName));

        services.AddSingleton<SearchTextProcessor>();

        services.AddSingleton(connectionFactory);
        services.AddSingleton<ProductSyncRepository>();

        // Elasticsearch HTTP client
        services.AddHttpClient<IElasticsearchIndexService, ElasticsearchIndexService>()
            .ConfigureHttpClient((sp, client) => {
                ElasticsearchSettings? settings = configuration
                    .GetSection("Elasticsearch")
                    .Get<ElasticsearchSettings>() ?? new ElasticsearchSettings();

                client.BaseAddress = new Uri(settings.Url.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            });

        services.AddHttpClient<ElasticsearchProductSearchService>()
            .ConfigureHttpClient((sp, client) => {
                ElasticsearchSettings? settings = configuration
                    .GetSection("Elasticsearch")
                    .Get<ElasticsearchSettings>() ?? new ElasticsearchSettings();

                client.BaseAddress = new Uri(settings.Url.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            });

        services.AddSingleton<IElasticsearchProductSearchService>(sp =>
            sp.GetRequiredService<ElasticsearchProductSearchService>());
        services.AddSingleton<IProductSearchService>(sp =>
            sp.GetRequiredService<ElasticsearchProductSearchService>());

        services.AddHttpClient<IElasticsearchSyncService, ElasticsearchSyncService>()
            .ConfigureHttpClient((sp, client) => {
                ElasticsearchSettings? settings = configuration
                    .GetSection("Elasticsearch")
                    .Get<ElasticsearchSettings>() ?? new ElasticsearchSettings();

                client.BaseAddress = new Uri(settings.Url.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds * 10); // Longer for sync
            });

        return services;
    }
}
