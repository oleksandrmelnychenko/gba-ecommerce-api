using System.Reflection;
using GBA.Ecommerce.Controllers;
using Microsoft.AspNetCore.OutputCaching;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ProductStockFreshnessContractTests {
    [Fact]
    public void Product_search_with_live_sellable_stock_is_not_output_cached() {
        MethodInfo method = typeof(ProductsController).GetMethod(
            nameof(ProductsController.GetAllFromSearchAsync))!;

        Assert.Empty(method.GetCustomAttributes<OutputCacheAttribute>());
    }

    [Fact]
    public void Elasticsearch_search_with_stock_projection_is_not_output_cached() {
        MethodInfo method = typeof(ElasticsearchController).GetMethod(
            nameof(ElasticsearchController.SearchAsync))!;

        Assert.Empty(method.GetCustomAttributes<OutputCacheAttribute>());
    }
}
