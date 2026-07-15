using GBA.Search.Configuration;
using GBA.Search.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class SearchSyncConfigurationTests {
    [Fact]
    public void StartupOptions_RejectAliasSwapMode() {
        IConfiguration configuration = Configuration(useAliasSwap: true);
        ServiceCollection services = new();
        services.AddElasticsearchSearch(
            configuration,
            () => throw new InvalidOperationException("Database access is not expected."));
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<SyncSettings>>().Value);

        Assert.Contains(
            SyncSettingsValidator.AliasSwapUnsupportedMessage,
            exception.Failures);
    }

    [Fact]
    public void StartupOptions_AcceptConcreteGenerationMode() {
        IConfiguration configuration = Configuration(useAliasSwap: false);
        ServiceCollection services = new();
        services.AddElasticsearchSearch(
            configuration,
            () => throw new InvalidOperationException("Database access is not expected."));
        using ServiceProvider provider = services.BuildServiceProvider();

        SyncSettings settings = provider.GetRequiredService<IOptions<SyncSettings>>().Value;

        Assert.False(settings.UseAliasSwap);
    }

    private static IConfiguration Configuration(bool useAliasSwap) {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                [$"{SyncSettings.SectionName}:{nameof(SyncSettings.UseAliasSwap)}"] =
                    useAliasSwap.ToString()
            })
            .Build();
    }
}
