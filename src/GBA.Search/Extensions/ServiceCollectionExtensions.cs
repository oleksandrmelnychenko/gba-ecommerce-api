using System;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using GBA.Search.Configuration;
using GBA.Search.Elasticsearch;
using GBA.Search.Services;
using GBA.Search.Sync;
using GBA.Search.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GBA.Search.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddElasticsearchSearch(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<IDbConnection> connectionFactory) {

        services.Configure<ElasticsearchSettings>(
            configuration.GetSection("Elasticsearch"));
        services.AddSingleton<IValidateOptions<SyncSettings>, SyncSettingsValidator>();
        services.AddOptions<SyncSettings>()
            .Bind(configuration.GetSection(SyncSettings.SectionName))
            .ValidateOnStart();

        services.AddSingleton<SearchTextProcessor>();

        services.AddSingleton(connectionFactory);
        services.AddSingleton<ProductSyncRepository>();
        services.AddSingleton<IProductSyncRepository>(sp =>
            sp.GetRequiredService<ProductSyncRepository>());

        services.AddHttpClient<IElasticsearchIndexService, ElasticsearchIndexService>()
            .ConfigureHttpClient((sp, client) => ConfigureElasticClient(client, configuration, 1));

        services.AddHttpClient<ElasticsearchProductSearchService>()
            .ConfigureHttpClient((sp, client) => ConfigureElasticClient(client, configuration, 1));

        services.AddSingleton<IElasticsearchProductSearchService>(sp =>
            sp.GetRequiredService<ElasticsearchProductSearchService>());
        services.AddSingleton<IProductSearchService>(sp =>
            sp.GetRequiredService<ElasticsearchProductSearchService>());

        services.AddHttpClient<ISearchSyncStateStore, SearchSyncStateStore>()
            .ConfigureHttpClient((sp, client) => ConfigureElasticClient(client, configuration, 1));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ISearchServingGenerationResolver, SearchServingGenerationResolver>();

        services.AddHttpClient<IElasticsearchSyncService, ElasticsearchSyncService>()
            .ConfigureHttpClient((sp, client) => ConfigureElasticClient(client, configuration, 10));

        services.AddHostedService<ProductSearchSyncBackgroundService>();

        return services;
    }

    private static void ConfigureElasticClient(System.Net.Http.HttpClient client, IConfiguration configuration, int timeoutMultiplier) {
        ElasticsearchSettings settings = configuration
            .GetSection("Elasticsearch")
            .Get<ElasticsearchSettings>() ?? new ElasticsearchSettings();

        client.BaseAddress = new Uri(settings.Url.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds * timeoutMultiplier);

        if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password)) {
            string token = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }
    }
}
