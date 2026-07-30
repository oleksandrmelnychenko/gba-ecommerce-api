namespace GBA.Ecommerce.Api.Tests;

public sealed class RelatedProductsRetailContractTests {
    [Fact]
    public void Anonymous_related_products_use_the_canonical_retail_graph() {
        string source = File.ReadAllText(RepositoryPath(
            "src/GBA.Services/Services/Products/ProductService.cs"));

        Assert.Contains(
            "TryGetRetailContext(connection, out _, out ClientAgreement clientAgreement)",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "out ClientAgreement retailAgreement",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "storage.ForVatProducts ? retailAgreement.NetUid : null",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "component.CurrentPrice = component.CurrentWithVatPrice",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "component.CurrencyCode = EcommerceRetailDefaults.CurrencyCode",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Retail_component_stock_is_limited_to_ecommerce_storages() {
        string source = File.ReadAllText(RepositoryPath(
            "src/GBA.Domain/Repositories/Products/GetMultipleProductsRepository.cs"));

        Assert.Contains(
            "AND (@OnlyEcommerceStorages = 0 OR [Storage].ForEcommerce = 1)",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "AND [Component].Deleted = 0",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "AND [Component].IsForWeb = 1",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "if (productAvailability == null",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "|| storage == null",
            source,
            StringComparison.Ordinal);
    }

    private static string RepositoryPath(string relativePath) {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null) {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate repository file: {relativePath}");
    }
}
