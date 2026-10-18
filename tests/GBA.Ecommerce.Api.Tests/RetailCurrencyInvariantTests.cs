namespace GBA.Ecommerce.Api.Tests;

public sealed class RetailCurrencyInvariantTests {
    private static readonly string[] RetailServicePaths = [
        "src/GBA.Services/Services/Clients/ClientAgreementService.cs",
        "src/GBA.Services/Services/Clients/ClientService.cs",
        "src/GBA.Services/Services/Clients/ClientShoppingCartService.cs",
        "src/GBA.Services/Services/Orders/OrderService.cs",
        "src/GBA.Services/Services/Products/ProductService.cs"
    ];

    [Fact]
    public void Public_retail_storage_selection_uses_the_configured_shop_agreement() {
        foreach (string path in RetailServicePaths) {
            string source = File.ReadAllText(RepositoryPath(path));

            Assert.DoesNotContain("EcommerceRetailDefaults", source, StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".GetWithHighestPriority(\"EUR\")",
                source,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Search_projection_uses_the_selected_dynamic_retail_graph() {
        string source = File.ReadAllText(RepositoryPath(
            "src/GBA.Search/Sync/ProductSyncRepository.cs"));

        Assert.DoesNotContain("c.Code = 'EUR'", source, StringComparison.Ordinal);
        Assert.Equal(
            3,
            CountOccurrences(
                source,
                "ORDER BY s.RetailPriority, s.ID, a.IsSelected DESC, ca.ID"));
        Assert.Equal(3, CountOccurrences(source, "AND a.IsActive = 1"));
        Assert.Equal(
            3,
            CountOccurrences(source, "'EUR' AS RetailCurrencyCode"));
        Assert.Contains("HasRetailConfigurationChangesSql", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Retail_storage_query_requires_an_active_matching_agreement() {
        string source = File.ReadAllText(RepositoryPath(
            "src/GBA.Domain/Repositories/Storages/StorageRepository.cs"));

        Assert.Contains("[RetailAgreement].IsActive = 1", source, StringComparison.Ordinal);
        Assert.Contains(
            "AND (@RetailCurrencyCode IS NULL OR [RetailCurrency].Code = @RetailCurrencyCode)",
            source,
            StringComparison.Ordinal);
        Assert.Contains("[RetailClient].IsForRetail = 1", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "AND (@RetailCurrencyCode IS NULL OR EXISTS",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Retail_agreement_lookup_excludes_deleted_and_inactive_rows() {
        string source = File.ReadAllText(RepositoryPath(
            "src/GBA.Domain/Repositories/Clients/ClientAgreementRepository.cs"));
        int methodStart = source.IndexOf(
            "GetByClientNetIdWithOrWithoutVat",
            StringComparison.Ordinal);
        Assert.True(methodStart >= 0);

        string method = source[methodStart..];
        Assert.Contains("AND [ClientAgreement].Deleted = 0", method, StringComparison.Ordinal);
        Assert.Contains("AND [Agreement].Deleted = 0", method, StringComparison.Ordinal);
        Assert.Contains("AND [Agreement].IsActive = 1", method, StringComparison.Ordinal);
        Assert.Contains(
            "ORDER BY [Agreement].IsSelected DESC, [ClientAgreement].ID",
            method,
            StringComparison.Ordinal);
        Assert.Contains("agreement.Currency = currency", method, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_preview_and_checkout_use_the_product_aware_filtered_fx_rate() {
        string[] pricingPaths = [
            "src/GBA.Services/Services/Clients/ClientShoppingCartService.cs",
            "src/GBA.Services/Services/Orders/OrderService.cs"
        ];

        foreach (string path in pricingPaths) {
            string source = File.ReadAllText(RepositoryPath(path));

            Assert.Contains(
                ".GetEuroExchangeRateByCurrentCultureFiltered(",
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".GetByCurrencyCodeAndCurrentCulture(",
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "CurrentLocalPrice / product.CurrentPrice",
                source,
                StringComparison.Ordinal);
        }

        string orderService = File.ReadAllText(RepositoryPath(pricingPaths[1]));
        Assert.Contains("GetRetailAgreement(connection, storage, withVat)", orderService, StringComparison.Ordinal);
        Assert.Contains(
            "ApplyAuthoritativeRetailProduct(connection, storage, clientAgreement, orderItem)",
            orderService,
            StringComparison.Ordinal);
    }

    private static string RepositoryPath(string relativePath) {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null) {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate repository file: {relativePath}");
    }

    private static int CountOccurrences(string source, string marker) {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(marker, index, StringComparison.Ordinal)) >= 0) {
            count++;
            index += marker.Length;
        }

        return count;
    }

}
