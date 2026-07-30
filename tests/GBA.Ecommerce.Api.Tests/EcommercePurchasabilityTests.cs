using System;
using System.IO;
using GBA.Domain.Entities.Products;

namespace GBA.Ecommerce.Api.Tests;

public sealed class EcommercePurchasabilityTests {
    private static readonly string[] _gatedCallSites = [
        "src/GBA.Services/Services/Orders/OrderService.cs",
        "src/GBA.Services/Services/Clients/ClientShoppingCartService.cs"
    ];

    [Fact]
    public void A_normal_non_promotional_web_product_is_purchasable() {
        Product product = new() {
            Deleted = false,
            IsForWeb = true,
            IsForSale = false,
            IsForZeroSale = false,
            CurrentPrice = 12.34m
        };

        Assert.True(EcommercePurchasability.IsPurchasable(product));
        Assert.True(EcommercePurchasability.HasSellablePrice(product));
    }

    [Fact]
    public void Promotional_flags_do_not_decide_purchasability() {
        Product promotional = new() { IsForWeb = true, IsForSale = true };
        Product zeroSale = new() { IsForWeb = true, IsForZeroSale = true };
        Product plain = new() { IsForWeb = true };

        Assert.True(EcommercePurchasability.IsPurchasable(promotional));
        Assert.True(EcommercePurchasability.IsPurchasable(zeroSale));
        Assert.True(EcommercePurchasability.IsPurchasable(plain));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Deleted_or_unpublished_products_are_not_purchasable(bool deleted, bool isForWeb) {
        Assert.False(EcommercePurchasability.IsPurchasable(new Product {
            Deleted = deleted,
            IsForWeb = isForWeb,
            IsForSale = true
        }));
    }

    [Fact]
    public void A_missing_product_is_not_purchasable() {
        Assert.False(EcommercePurchasability.IsPurchasable(null));
        Assert.False(EcommercePurchasability.HasSellablePrice(null));
    }

    [Fact]
    public void An_unpriced_product_is_never_sellable_at_zero() {
        Product unpriced = new() { IsForWeb = true, CurrentPrice = 0m };

        Assert.True(EcommercePurchasability.IsPurchasable(unpriced));
        Assert.False(EcommercePurchasability.HasSellablePrice(unpriced));
    }

    [Fact]
    public void No_checkout_path_gates_a_purchase_on_the_promotional_flag() {
        foreach (string path in _gatedCallSites) {
            string source = File.ReadAllText(RepositoryPath(path));

            Assert.DoesNotContain("!product.IsForSale", source, StringComparison.Ordinal);
            Assert.DoesNotContain("!product.IsForZeroSale", source, StringComparison.Ordinal);
            Assert.Contains("EcommercePurchasability.IsPurchasable(product)", source, StringComparison.Ordinal);
            Assert.Contains("EcommercePurchasability.HasSellablePrice(product)", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void All_three_authoritative_product_resolvers_are_gated() {
        string orderService = File.ReadAllText(
            RepositoryPath("src/GBA.Services/Services/Orders/OrderService.cs"));
        string cartService = File.ReadAllText(
            RepositoryPath("src/GBA.Services/Services/Clients/ClientShoppingCartService.cs"));

        Assert.Equal(2, CountOccurrences(orderService, "EcommercePurchasability.IsPurchasable(product)"));
        Assert.Equal(1, CountOccurrences(cartService, "EcommercePurchasability.IsPurchasable(product)"));
    }

    private static int CountOccurrences(string source, string marker) {
        int count = 0;
        int index = source.IndexOf(marker, StringComparison.Ordinal);
        while (index >= 0) {
            count++;
            index = source.IndexOf(marker, index + marker.Length, StringComparison.Ordinal);
        }

        return count;
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
}
