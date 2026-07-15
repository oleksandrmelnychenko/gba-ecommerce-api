using System.Reflection;
using GBA.Domain.Repositories.Products;
using GBA.Domain.Repositories.Sales;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class DomainPricingRepositoryTests {
    [Fact]
    public void OptimizedProductBatch_UsesAgreementWorldAndCannotMultiplyProductsByPricingRows() {
        string sql = GetPrivateConstant(typeof(OptimizedProductRepository), "ProductBatchSql");

        Assert.Contains("GetCalculatedProductPriceWithSharesAndVat", sql, StringComparison.Ordinal);
        Assert.Contains("@ClientAgreementNetId", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductPricing", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BasePricingHierarchy", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ProductProductGroup", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GROUP BY pa.ProductID", sql, StringComparison.Ordinal);
        Assert.Contains("SELECT TOP (1)", sql, StringComparison.Ordinal);
        Assert.Contains("FROM ProductSlug ps", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void HistoricalSalePrice_UsesFrozenValueBeforeSourceAwareFallback() {
        string priceSql = GetPrivateConstant(typeof(SaleRepository), "FrozenOrderItemPriceWithFallbackSql");
        string localPriceSql = GetPrivateConstant(typeof(SaleRepository), "FrozenOrderItemLocalPriceWithFallbackSql");

        Assert.Contains("[BaseLifeCycleStatus].[SaleLifeCycleType] = 0", priceSql, StringComparison.Ordinal);
        Assert.Contains("ELSE COALESCE([OrderItem].[PricePerItem]", priceSql, StringComparison.Ordinal);
        Assert.Contains("GetCalculatedProductPriceWithSharesAndVat", priceSql, StringComparison.Ordinal);
        Assert.Contains("[OrderItem].[ExchangeRateAmount]", localPriceSql, StringComparison.Ordinal);
        Assert.Contains("GetCalculatedProductLocalPriceWithSharesAndVat", localPriceSql, StringComparison.Ordinal);
    }

    [Fact]
    public void ActiveSaleLoaders_DoNotEnumerateProductPricingRows() {
        string source = ReadRepositorySource("Sales", "SaleRepository.cs");
        string dynamicSaleMethod = ExtractMethod(source, "public Sale GetByIdWithCalculatedDynamicPrices(");
        string saleHistoryMethod = ExtractMethod(source, "public Sale GetByNetId(");

        Assert.DoesNotContain("ProductPricing", dynamicSaleMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductPricing", saleHistoryMethod, StringComparison.Ordinal);
        Assert.Contains("GetCalculatedProductPriceWithSharesAndVat", dynamicSaleMethod, StringComparison.Ordinal);
        Assert.Contains("FrozenOrderItemPriceWithFallbackSql", saleHistoryMethod, StringComparison.Ordinal);
        Assert.Contains("OrderItems.Any", dynamicSaleMethod, StringComparison.Ordinal);
        Assert.Contains("OrderItems.Any", saleHistoryMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductQueries_DoNotReferenceMisspelledPricingFunction() {
        string source = ReadRepositorySource("Products", "GetMultipleProductsRepository.cs");

        Assert.DoesNotContain("GetCalculatedProductPriceWithSharesAndVatWith", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CarBrandGuidQuery_BindsEachAgreementToItsReferencedPriceParameter() {
        string source = ReadRepositorySource("Products", "GetMultipleProductsRepository.cs");
        string method = ExtractMethod(
            source,
            "public List<Product> GetAllProductsByCarBrandNetId(\n        Guid carBrandNetId");

        Assert.Contains(
            "GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, 0, NULL)",
            method,
            StringComparison.Ordinal);
        Assert.Contains(
            "GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, 0, NULL)",
            method,
            StringComparison.Ordinal);
        Assert.Contains(
            "GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL)",
            method,
            StringComparison.Ordinal);
        Assert.Contains(
            "GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL)",
            method,
            StringComparison.Ordinal);
        Assert.Contains("ClientAgreementNetId = nonVatAgreementNetId", method, StringComparison.Ordinal);
        Assert.Contains("VatAgreementNetId = vatAgreementNetId ?? Guid.Empty", method, StringComparison.Ordinal);
        Assert.DoesNotContain("NonVatAgreementNetId = vatAgreementNetId", method, StringComparison.Ordinal);
        Assert.Contains("ON [Storage].ID = [ProductAvailability].StorageID", method, StringComparison.Ordinal);
        Assert.Contains("AND [Storage].OrganizationID = @OrganizationId", method, StringComparison.Ordinal);
        Assert.Contains(
            "ProductSourceIdentitySql.CanonicalSourceWorldPredicate(\"[Product]\")",
            method,
            StringComparison.Ordinal);
        Assert.Contains("pricingScope.NonVatSource", method, StringComparison.Ordinal);
        Assert.Contains("pricingScope.VatSource", method, StringComparison.Ordinal);
        Assert.DoesNotContain("FromFenix", method, StringComparison.Ordinal);
        Assert.DoesNotContain("ON [Storage].ID = 0", method, StringComparison.Ordinal);
    }

    [Fact]
    public void RetailContextQueries_RequireActiveNonDeletedFenixScopeAndDeterministicOrdering() {
        string storageSource = ReadRepositorySource("Storages", "StorageRepository.cs");
        string storageMethod = ExtractMethod(
            storageSource,
            "public Storage GetFenixRetailWithHighestPriority(");
        string agreementSource = ReadRepositorySource("Clients", "ClientAgreementRepository.cs");
        string agreementMethod = ExtractMethod(
            agreementSource,
            "public ClientAgreement GetActiveRetailFenixByOrganizationId(");
        string selectedMethod = ExtractMethod(
            agreementSource,
            "public ClientAgreement GetSelectedForPricing(");

        Assert.Contains("[Storage].Deleted = 0", storageMethod, StringComparison.Ordinal);
        Assert.Contains("[Organization].Deleted = 0", storageMethod, StringComparison.Ordinal);
        Assert.Contains("[Agreement].IsActive = 1", storageMethod, StringComparison.Ordinal);
        Assert.Contains("DATALENGTH([Agreement].SourceFenixID)", storageMethod, StringComparison.Ordinal);
        Assert.Contains("DATALENGTH([Agreement].SourceAmgID)", storageMethod, StringComparison.Ordinal);
        Assert.Contains("[Agreement].SourceAmgCode IS NULL", storageMethod, StringComparison.Ordinal);
        Assert.Contains("ORDER BY [Storage].RetailPriority, [Storage].ID", storageMethod, StringComparison.Ordinal);

        Assert.Contains("[ClientAgreement].Deleted = 0", agreementMethod, StringComparison.Ordinal);
        Assert.Contains("[Agreement].Deleted = 0", agreementMethod, StringComparison.Ordinal);
        Assert.Contains("[Agreement].IsActive = 1", agreementMethod, StringComparison.Ordinal);
        Assert.Contains("[Organization].Deleted = 0", agreementMethod, StringComparison.Ordinal);
        Assert.Contains("DATALENGTH([Agreement].SourceFenixID)", agreementMethod, StringComparison.Ordinal);
        Assert.Contains("DATALENGTH([Agreement].SourceAmgID)", agreementMethod, StringComparison.Ordinal);
        Assert.Contains("[Agreement].SourceAmgCode IS NULL", agreementMethod, StringComparison.Ordinal);
        Assert.Contains("[Agreement].Updated DESC, [Agreement].ID", agreementMethod, StringComparison.Ordinal);

        Assert.Contains("[Agreement].IsActive = 1", selectedMethod, StringComparison.Ordinal);
        Assert.Contains("[Organization].Deleted = 0", selectedMethod, StringComparison.Ordinal);
        Assert.Contains("[WorkplaceClientAgreement].IsSelected = 1", selectedMethod, StringComparison.Ordinal);
        Assert.Contains("[Agreement].Updated DESC, [Agreement].ID", selectedMethod, StringComparison.Ordinal);
    }

    private static string GetPrivateConstant(Type type, string name) {
        FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{type.Name}.{name} was not found.");

        return (string)((field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null))
            ?? throw new InvalidOperationException($"{type.Name}.{name} is null."));
    }

    private static string ReadRepositorySource(string area, string fileName) {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory != null) {
            string path = Path.Combine(
                directory.FullName,
                "src",
                "GBA.Domain",
                "Repositories",
                area,
                fileName);

            if (File.Exists(path)) return File.ReadAllText(path);
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {fileName} from {AppContext.BaseDirectory}.");
    }

    private static string ExtractMethod(string source, string signature) {
        int methodStart = source.IndexOf(signature, StringComparison.Ordinal);
        if (methodStart < 0) throw new InvalidOperationException($"Method '{signature}' was not found.");

        int bodyStart = source.IndexOf('{', methodStart);
        if (bodyStart < 0) throw new InvalidOperationException($"Method '{signature}' has no body.");

        int depth = 0;
        for (int index = bodyStart; index < source.Length; index++) {
            switch (source[index]) {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0) return source[methodStart..(index + 1)];
                    break;
            }
        }

        throw new InvalidOperationException($"Method '{signature}' has an unterminated body.");
    }
}
