using GBA.Domain.Entities;

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
    public void Public_retail_storage_selection_is_always_constrained_to_eur() {
        Assert.Equal("EUR", EcommerceRetailDefaults.CurrencyCode);

        int constrainedSelections = 0;
        foreach (string path in RetailServicePaths) {
            string source = File.ReadAllText(RepositoryPath(path));

            Assert.DoesNotContain(".GetWithHighestPriority();", source, StringComparison.Ordinal);
            constrainedSelections += CountOccurrences(
                source,
                ".GetWithHighestPriority(EcommerceRetailDefaults.CurrencyCode)");
        }

        Assert.Equal(18, constrainedSelections);
    }

    [Fact]
    public void Search_projection_uses_the_same_eur_retail_graph() {
        string source = File.ReadAllText(RepositoryPath(
            "src/GBA.Search/Sync/ProductSyncRepository.cs"));

        Assert.Equal(3, CountOccurrences(source, "AND c.Code = 'EUR'"));
        Assert.Equal(3, CountOccurrences(source, "AND a.IsActive = 1"));
    }

    [Fact]
    public void Retail_storage_query_requires_an_active_matching_agreement() {
        string source = File.ReadAllText(RepositoryPath(
            "src/GBA.Domain/Repositories/Storages/StorageRepository.cs"));

        Assert.Contains("[RetailAgreement].IsActive = 1", source, StringComparison.Ordinal);
        Assert.Contains("[RetailCurrency].Code = @RetailCurrencyCode", source, StringComparison.Ordinal);
        Assert.Contains("[RetailClient].IsForRetail = 1", source, StringComparison.Ordinal);
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
