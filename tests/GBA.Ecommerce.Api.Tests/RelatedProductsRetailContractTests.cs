namespace GBA.Ecommerce.Api.Tests;

public sealed class RelatedProductsRetailContractTests {
    [Fact]
    public void Anonymous_related_products_use_the_canonical_retail_graph() {
        string source = File.ReadAllText(RepositoryPath(
            "src/GBA.Services/Services/Products/ProductService.cs"));

        Assert.Contains(
            "TryGetRetailContext(connection, out Storage storage, out ClientAgreement clientAgreement)",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "storage.Id",
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
            "component.CurrencyCode = retailAgreement.Agreement.Currency?.Code",
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

    [Fact]
    public void Authenticated_analogue_relation_survives_without_stock_in_the_agreement_scope() {
        string method = AuthenticatedAnalogueRepositoryMethod();
        int relationFilterStart = method.IndexOf(
            "\"WHERE [ProductAnalogue].BaseProductID = @Id \"",
            StringComparison.Ordinal);

        Assert.True(relationFilterStart >= 0);
        string relationFilters = method[relationFilterStart..];

        Assert.Contains(
            "\"AND [ProductAnalogue].Deleted = 0 \"",
            relationFilters,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "[Storage].Locale",
            relationFilters,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "EcommerceStorageScope.NamedStorageSql",
            relationFilters,
            StringComparison.Ordinal);

        int addAnalogue = method.IndexOf("analogues.Add(analogue);", StringComparison.Ordinal);
        int missingStockGuard = method.IndexOf(
            "if (productAvailability == null || storage == null) return analogue;",
            StringComparison.Ordinal);
        Assert.True(addAnalogue >= 0 && missingStockGuard > addAnalogue);
    }

    [Fact]
    public void Authenticated_analogue_stock_remains_limited_to_the_agreement_scope() {
        string method = AuthenticatedAnalogueRepositoryMethod();
        int availabilityJoinStart = method.IndexOf(
            "\"LEFT JOIN [ProductAvailability] \"",
            StringComparison.Ordinal);
        int relationFilterStart = method.IndexOf(
            "\"WHERE [ProductAnalogue].BaseProductID = @Id \"",
            StringComparison.Ordinal);

        Assert.True(availabilityJoinStart >= 0 && relationFilterStart > availabilityJoinStart);
        string availabilityJoin = method[availabilityJoinStart..relationFilterStart];

        Assert.Contains("AND EXISTS (", availabilityJoin, StringComparison.Ordinal);
        Assert.Contains("[Storage].Locale = @Culture", availabilityJoin, StringComparison.Ordinal);
        Assert.Contains("[Storage].ForDefective = 0", availabilityJoin, StringComparison.Ordinal);
        Assert.Contains("[Storage].Deleted = 0", availabilityJoin, StringComparison.Ordinal);
        Assert.Contains(
            "EcommerceStorageScope.NamedStorageSql",
            availabilityJoin,
            StringComparison.Ordinal);
    }

    private static string AuthenticatedAnalogueRepositoryMethod() {
        string source = File.ReadAllText(RepositoryPath(
            "src/GBA.Domain/Repositories/Products/GetMultipleProductsRepository.cs"));
        int methodStart = source.IndexOf(
            "public List<FromSearchProduct> GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPrices(",
            StringComparison.Ordinal);
        int methodEnd = source.IndexOf(
            "internal static void AddAvailabilityToRequestedBucket(",
            methodStart,
            StringComparison.Ordinal);

        Assert.True(methodStart >= 0 && methodEnd > methodStart);
        return source[methodStart..methodEnd];
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
