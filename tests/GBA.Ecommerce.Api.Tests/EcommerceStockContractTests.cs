using System;
using System.IO;

namespace GBA.Ecommerce.Api.Tests;

public sealed class EcommerceStockContractTests {
    [Fact]
    public void Every_search_projection_selects_one_retail_storage_and_uses_it_for_stock() {
        string source = ReadSource("src/GBA.Search/Sync/ProductSyncRepository.cs");

        Assert.Equal(4, Count(source, "s.ID AS StorageId"));
        Assert.Equal(12, Count(source, "pa.StorageID = rc.StorageId"));
        Assert.Equal(4, Count(source, "ORDER BY s.RetailPriority, s.ID, a.IsSelected DESC, ca.ID"));
        Assert.DoesNotContain(
            "SELECT SUM(pa.Amount) FROM ProductAvailability pa INNER JOIN Storage s ON s.ID = pa.StorageID WHERE pa.ProductID = p.ID AND pa.Deleted = 0 AND s.ForDefective = 0 AND s.ForEcommerce = 1",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Anonymous_search_and_checkout_use_live_stock_from_one_highest_priority_storage() {
        string controller = ReadSource("src/GBA.Ecommerce/Controllers/ProductsController.cs");
        string productService = ReadSource("src/GBA.Services/Services/Products/ProductService.cs");
        string cartService = ReadSource("src/GBA.Services/Services/Clients/ClientShoppingCartService.cs");
        string orderService = ReadSource("src/GBA.Services/Services/Orders/OrderService.cs");
        string availabilityRepository = ReadSource("src/GBA.Domain/Repositories/Products/ProductAvailabilityRepository.cs");

        Assert.Contains("productService.GetSellableQuantities(productIds, userNetId)", controller);
        Assert.Contains("storage.Id,", productService);
        Assert.Contains("GetByProductAndStorageIds(product.Id, storage.Id)", cartService);
        Assert.Contains("EcommercePurchasability.HasSellablePrice(product)", cartService);
        Assert.Contains("productAvailability.Amount <= 0", orderService);
        Assert.Contains("CASE WHEN @RetailStorageId IS NOT NULL", availabilityRepository);
        Assert.Contains("SELECT TOP (1)", availabilityRepository);
        Assert.Contains("AND pa.StorageID = @RetailStorageId", availabilityRepository);
    }

    [Fact]
    public void Authenticated_stock_uses_only_the_agreement_organizations_sale_storage() {
        string availabilityRepository = ReadSource("src/GBA.Domain/Repositories/Products/ProductAvailabilityRepository.cs");
        string productRepository = ReadSource("src/GBA.Domain/Repositories/Products/GetSingleProductRepository.cs");
        string multipleProductsRepository = ReadSource("src/GBA.Domain/Repositories/Products/GetMultipleProductsRepository.cs");
        string orderItemRepository = ReadSource("src/GBA.Domain/Repositories/Sales/OrderItemRepository.cs");
        string cartRepository = ReadSource("src/GBA.Domain/Repositories/Clients/ClientShoppingCartRepository.cs");
        string storageScope = ReadSource("src/GBA.Domain/Repositories/Products/EcommerceStorageScope.cs");
        string cartService = ReadSource("src/GBA.Services/Services/Clients/ClientShoppingCartService.cs");
        string orderService = ReadSource("src/GBA.Services/Services/Orders/OrderService.cs");
        string offerService = ReadSource("src/GBA.Services/Services/Offers/OfferService.cs");
        int ecommerceAvailabilityStart = availabilityRepository.IndexOf(
            "public IEnumerable<ProductAvailability> GetForEcommercePurchase(",
            StringComparison.Ordinal);
        int ecommerceAvailabilityEnd = availabilityRepository.IndexOf(
            "public IEnumerable<ProductAvailability> GetAllByProductAndStorageIds(",
            ecommerceAvailabilityStart,
            StringComparison.Ordinal);

        Assert.True(ecommerceAvailabilityStart >= 0 && ecommerceAvailabilityEnd > ecommerceAvailabilityStart);
        string ecommerceAvailability = availabilityRepository[ecommerceAvailabilityStart..ecommerceAvailabilityEnd];

        Assert.Contains("GetForEcommercePurchase", cartService);
        Assert.Contains("GetForEcommercePurchase", orderService);
        Assert.Contains("GetForEcommercePurchase", offerService);
        Assert.DoesNotContain("GetByProductAndOrganizationIds", offerService, StringComparison.Ordinal);
        Assert.Equal(1, Count(availabilityRepository, "EcommerceStorageScope.NamedStorageSql"));
        Assert.Equal(1, Count(availabilityRepository, "EcommerceStorageScope.AliasedStorageSql"));
        Assert.Equal(2, Count(productRepository, "EcommerceStorageScope.NamedStorageSql"));
        Assert.Contains("EcommerceStorageScope.NamedStorageSql", multipleProductsRepository);
        Assert.Equal(2, Count(orderItemRepository, "EcommerceStorageScope.ProductSellableQuantitySql"));
        Assert.Equal(2, Count(cartRepository, "EcommerceStorageScope.ProductSellableQuantitySql"));
        Assert.Contains("[AgreementOrganization].StorageID = [Storage].ID", storageScope);
        Assert.Contains("[AgreementOrganization].StorageID = s.ID", storageScope);
        Assert.Contains("[ScopedOrganization].StorageID = [ScopedStorage].ID", storageScope);
        Assert.DoesNotContain("OrganizationID = @OrganizationId", storageScope, StringComparison.Ordinal);
        Assert.DoesNotContain("ForVatProducts", ecommerceAvailability, StringComparison.Ordinal);
        Assert.DoesNotContain("AvailableForReSale", ecommerceAvailability, StringComparison.Ordinal);
        Assert.DoesNotContain("Locale", ecommerceAvailability, StringComparison.Ordinal);
        Assert.Contains("s.ForDefective = 0", ecommerceAvailability);
    }

    [Fact]
    public void Authenticated_stock_resolves_sub_clients_to_the_root_agreement_owner() {
        string agreementRepository = ReadSource("src/GBA.Domain/Repositories/Clients/ClientAgreementRepository.cs");
        string clientRepository = ReadSource("src/GBA.Domain/Repositories/Clients/ClientRepository.cs");
        string agreementService = ReadSource("src/GBA.Services/Services/Clients/ClientAgreementService.cs");
        string productService = ReadSource("src/GBA.Services/Services/Products/ProductService.cs");

        Assert.Equal(2, Count(agreementRepository, "GetByClientContextNetId(clientNetId"));
        Assert.Contains(
            "COALESCE([ClientOwnerLink].RootClientID, [ActorClient].ID)",
            agreementRepository,
            StringComparison.Ordinal);
        Assert.Contains("[ClientOwnerLink].Deleted = 0", agreementRepository, StringComparison.Ordinal);
        Assert.Contains("[Agreement].ForReSale = 0", agreementRepository, StringComparison.Ordinal);
        Assert.Contains("WHERE [ClientSubClient].Deleted = 0", clientRepository, StringComparison.Ordinal);
        Assert.Contains("GetRootNetIdBySubClientNetId(clientNetId)", agreementService, StringComparison.Ordinal);
        Assert.Contains("ShopClientAgreements.Resolve(clientAgreementRepository, clientNetId)", productService, StringComparison.Ordinal);
    }

    [Fact]
    public void Retail_configuration_gaps_fail_closed_without_exposing_internal_stock() {
        string productService = ReadSource("src/GBA.Services/Services/Products/ProductService.cs");
        string cartService = ReadSource("src/GBA.Services/Services/Clients/ClientShoppingCartService.cs");
        string productRepository = ReadSource("src/GBA.Domain/Repositories/Products/GetSingleProductRepository.cs");

        Assert.Contains("product.AvailableQtyUk = 0", productService);
        Assert.Contains("product.CurrentPrice = 0", productService);
        Assert.Contains("Retail client is not configured", cartService);
        Assert.Contains("Retail agreement is not configured", cartService);
        Assert.Contains("GetNetIdBySlug(slug)", productService);
        Assert.Contains("productRepository.GetByNetIdForRetail(", productService);
        Assert.DoesNotContain("productRepository.GetBySlug(slug)", productService);
        Assert.Contains("public Guid? GetNetIdBySlug(string slug)", productRepository);
    }

    [Fact]
    public void Retail_product_detail_queries_bind_the_selected_storage() {
        string source = ReadSource("src/GBA.Domain/Repositories/Products/GetSingleProductRepository.cs");
        int standardMethodStart = source.IndexOf("public Product GetProductByNetId(", StringComparison.Ordinal);
        int standardMethodEnd = source.IndexOf(
            "internal static void IncludeAvailabilityFromJoinedStorage(",
            standardMethodStart,
            StringComparison.Ordinal);
        int retailMethodStart = source.IndexOf("public Product GetByNetIdForRetail(", StringComparison.Ordinal);
        int retailMethodEnd = source.IndexOf(
            "internal static void ExposeRetailAvailabilityInBothStorefrontBuckets",
            retailMethodStart,
            StringComparison.Ordinal);

        Assert.True(standardMethodStart >= 0 && standardMethodEnd > standardMethodStart);
        Assert.True(retailMethodStart > standardMethodEnd);
        Assert.True(retailMethodEnd > retailMethodStart);

        string standardMethod = source[standardMethodStart..standardMethodEnd];
        string retailMethod = source[retailMethodStart..retailMethodEnd];

        Assert.DoesNotContain("@StorageId", standardMethod, StringComparison.Ordinal);
        Assert.Contains(
            "IncludeAvailabilityFromJoinedStorage(\n                    productToReturn,\n                    productAvailability,\n                    storage,\n                    withVat);",
            standardMethod,
            StringComparison.Ordinal);
        Assert.Contains(
            "IncludeAvailabilityFromJoinedStorage(\n                    product,\n                    productAvailability,\n                    storage,\n                    withVat);",
            standardMethod,
            StringComparison.Ordinal);
        Assert.Contains("EcommerceStorageScope.NamedStorageSql", standardMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("ForVatProducts", standardMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("AvailableForReSale", standardMethod, StringComparison.Ordinal);
        Assert.Equal(2, Count(retailMethod, "AND [ProductAvailability].StorageID = @StorageId"));
        Assert.Equal(2, Count(retailMethod, "StorageId = storageId"));
    }

    [Fact]
    public void Cart_reindex_is_requested_after_the_stock_mutation_commits() {
        string source = ReadSource("src/GBA.Services/Services/Clients/ClientShoppingCartService.cs");

        int write = source.IndexOf("private T RunCartWrite<T>(", StringComparison.Ordinal);
        int commit = source.IndexOf("scope.Complete();", write, StringComparison.Ordinal);
        int signal = source.IndexOf("_reindexSignal.Request(productId);", commit, StringComparison.Ordinal);

        Assert.True(write >= 0 && commit > write && signal > commit);
        Assert.Equal(1, Count(source, "_reindexSignal.Request("));
    }

    private static int Count(string source, string value) {
        int count = 0;
        int index = source.IndexOf(value, StringComparison.Ordinal);
        while (index >= 0) {
            count++;
            index = source.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string ReadSource(string relativePath) {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null) {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate repository file: {relativePath}");
    }
}
