using System;
using System.IO;

namespace GBA.Ecommerce.Api.Tests;

public sealed class EcommerceStockContractTests {
    [Fact]
    public void Every_search_projection_selects_one_retail_storage_and_uses_it_for_stock() {
        string source = ReadSource("src/GBA.Search/Sync/ProductSyncRepository.cs");

        Assert.Equal(3, Count(source, "s.ID AS StorageId"));
        Assert.Equal(12, Count(source, "pa.StorageID = rc.StorageId"));
        Assert.Equal(3, Count(source, "ORDER BY s.RetailPriority, s.ID, a.IsSelected DESC, ca.ID"));
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

        Assert.Contains("productService.GetSellableQuantities(productIds, userNetId, locale)", controller);
        Assert.Contains("storage.Id,", productService);
        Assert.Contains("GetByProductAndStorageIds(product.Id, storage.Id)", cartService);
        Assert.Contains("EcommercePurchasability.HasSellablePrice(product)", cartService);
        Assert.Contains("productAvailability.Amount <= 0", orderService);
        Assert.Contains("CASE WHEN @RetailStorageId IS NOT NULL", availabilityRepository);
        Assert.Contains("SELECT TOP (1)", availabilityRepository);
        Assert.Contains("AND pa.StorageID = @RetailStorageId", availabilityRepository);
    }

    [Fact]
    public void Authenticated_cart_and_search_share_the_multi_storage_agreement_scope() {
        string availabilityRepository = ReadSource("src/GBA.Domain/Repositories/Products/ProductAvailabilityRepository.cs");
        string productRepository = ReadSource("src/GBA.Domain/Repositories/Products/GetSingleProductRepository.cs");
        string storageScope = ReadSource("src/GBA.Domain/Repositories/Products/EcommerceStorageScope.cs");
        string cartService = ReadSource("src/GBA.Services/Services/Clients/ClientShoppingCartService.cs");
        string orderService = ReadSource("src/GBA.Services/Services/Orders/OrderService.cs");

        Assert.Contains("GetForEcommercePurchase", cartService);
        Assert.Contains("GetForEcommercePurchase", orderService);
        Assert.Equal(2, Count(availabilityRepository, "EcommerceStorageScope.NamedStorageSql"));
        Assert.Equal(2, Count(availabilityRepository, "EcommerceStorageScope.AliasedStorageSql"));
        Assert.Equal(2, Count(productRepository, "EcommerceStorageScope.NamedStorageSql"));
        Assert.Contains("[Storage].OrganizationID = @OrganizationId", storageScope);
        Assert.Contains("[AgreementOrganization].StorageID = [Storage].ID", storageScope);
        Assert.Contains("[AgreementOrganization].StorageID = s.ID", storageScope);
        Assert.Contains("s.ForVatProducts = 1", availabilityRepository);
        Assert.Contains("s.AvailableForReSale = 1", availabilityRepository);
        Assert.Contains("s.ForDefective = 0", availabilityRepository);
        Assert.Contains("s.Locale = @Culture", availabilityRepository);
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
        int retailMethodStart = source.IndexOf("public Product GetByNetIdForRetail(", StringComparison.Ordinal);
        int retailMethodEnd = source.IndexOf(
            "internal static void ExposeRetailAvailabilityInBothStorefrontBuckets",
            retailMethodStart,
            StringComparison.Ordinal);

        Assert.True(standardMethodStart >= 0 && retailMethodStart > standardMethodStart);
        Assert.True(retailMethodEnd > retailMethodStart);

        string standardMethod = source[standardMethodStart..retailMethodStart];
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
        Assert.Contains("AND [Storage].ForVatProducts = 1", standardMethod, StringComparison.Ordinal);
        Assert.Equal(2, Count(retailMethod, "AND [ProductAvailability].StorageID = @StorageId"));
        Assert.Equal(2, Count(retailMethod, "StorageId = storageId"));
    }

    [Fact]
    public void Cart_reindex_is_requested_after_stock_mutation() {
        string source = ReadSource("src/GBA.Services/Services/Clients/ClientShoppingCartService.cs");

        int addMutation = source.IndexOf("OrderItem addedItem = AddNewItemToShoppingCart", StringComparison.Ordinal);
        int addSignal = source.IndexOf("_reindexSignal.Request(orderItem.ProductId);", addMutation, StringComparison.Ordinal);
        Assert.True(addMutation >= 0 && addSignal > addMutation);

        int updateMutation = source.IndexOf("orderItemRepository.UpdateQty(orderItem);", StringComparison.Ordinal);
        int updateSignal = source.IndexOf("_reindexSignal.Request(orderItem.ProductId);", updateMutation, StringComparison.Ordinal);
        Assert.True(updateMutation >= 0 && updateSignal > updateMutation);
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
