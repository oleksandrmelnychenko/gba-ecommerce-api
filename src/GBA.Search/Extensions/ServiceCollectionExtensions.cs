using System;
using System.Data;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using GBA.Search.Jobs;
using GBA.Search.Resilience;
using GBA.Search.Services;
using GBA.Search.Services.Synonyms;
using GBA.Search.Sync;
using GBA.Search.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GBA.Search.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddProductSearch(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<IDbConnection> connectionFactory) {

        services.Configure<TypesenseSettings>(
            configuration.GetSection(TypesenseSettings.SectionName));
        services.Configure<SyncSettings>(
            configuration.GetSection(SyncSettings.SectionName));
        services.Configure<ResilienceSettings>(
            configuration.GetSection(ResilienceSettings.SectionName));
        services.Configure<SearchTuningSettings>(
            configuration.GetSection(SearchTuningSettings.SectionName));
        services.Configure<SearchSynonymsSettings>(
            configuration.GetSection(SearchSynonymsSettings.SectionName));

        services.AddSingleton<SearchTextProcessor>();
        services.AddSingleton<ISynonymProvider, FileSynonymProvider>();

        services.AddSingleton(connectionFactory);
        services.AddSingleton<ProductSyncRepository>();

        services.AddHttpClient<TypesenseSearchService>()
            .ConfigureHttpClient((sp, client) => {
                TypesenseSettings settings = configuration
                    .GetSection(TypesenseSettings.SectionName)
                    .Get<TypesenseSettings>() ?? new TypesenseSettings();

                client.BaseAddress = new Uri(settings.Url.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", settings.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            });

        services.AddSingleton<SqlFallbackSearchService>();

        services.AddSingleton<ResilientSearchService>();
        services.AddSingleton<IProductSearchService>(sp => sp.GetRequiredService<ResilientSearchService>());
        services.AddSingleton<IProductSearchDebugService>(sp => sp.GetRequiredService<ResilientSearchService>());

        services.AddHttpClient<ProductSyncService>()
            .ConfigureHttpClient((sp, client) => {
                TypesenseSettings settings = configuration
                    .GetSection(TypesenseSettings.SectionName)
                    .Get<TypesenseSettings>() ?? new TypesenseSettings();

                client.BaseAddress = new Uri(settings.Url.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", settings.ApiKey);
            });

        services.AddSingleton<IProductSyncService>(sp => sp.GetRequiredService<ProductSyncService>());

        services.AddHostedService<SearchSyncBackgroundService>();

        services.AddHealthChecks()
            .AddCheck<TypesenseHealthCheck>("typesense");

        return services;
    }

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
                var settings = configuration
                    .GetSection("Elasticsearch")
                    .Get<ElasticsearchSettings>() ?? new ElasticsearchSettings();

                client.BaseAddress = new Uri(settings.Url.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            });

        services.AddHttpClient<ElasticsearchProductSearchService>()
            .ConfigureHttpClient((sp, client) => {
                var settings = configuration
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
                var settings = configuration
                    .GetSection("Elasticsearch")
                    .Get<ElasticsearchSettings>() ?? new ElasticsearchSettings();

                client.BaseAddress = new Uri(settings.Url.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds * 10); // Longer for sync
            });

        return services;
    }
}
