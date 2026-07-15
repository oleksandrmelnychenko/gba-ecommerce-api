using System;
using System.Collections.Generic;
using GBA.Ecommerce.DependencyInjection;
using GBA.Services.Infrastructure.SalesMutations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class SalesMutationInternalAuthTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    public void OptionsRejectMissingOrWeakApiKey(string? apiKey) {
        SalesMutationInternalAuthOptions options = new() { ApiKey = apiKey! };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            options.GetValidatedApiKey());

        Assert.Contains("ApiKey", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistrationFailsClosedWhenApiKeyIsAbsent() {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddSalesMutationInternalAuthentication(configuration));

        Assert.Contains("ApiKey", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistrationLoadsApiKeyWithoutPersistingItInTrackedSettings() {
        const string apiKey = "test-only-ecommerce-internal-api-key-0123456789abcdef";
        Dictionary<string, string?> values = new() {
            [$"{SalesMutationInternalAuthOptions.SectionName}:ApiKey"] = apiKey
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        ServiceCollection services = new();

        services.AddSalesMutationInternalAuthentication(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal(
            apiKey,
            provider.GetRequiredService<SalesMutationInternalAuthOptions>()
                .GetValidatedApiKey());
    }
}
