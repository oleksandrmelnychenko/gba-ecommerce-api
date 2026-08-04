using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Domain.EntityHelpers;
using GBA.Ecommerce.Controllers;
using GBA.Search.Elasticsearch;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.Products;
using GBA.Services.Services.Products.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ProductSearchShortQueryTests {
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_search_returns_an_empty_success_without_calling_the_search_backend(
        string query) {
        Mock<IElasticsearchProductSearchService> searchService = new();
        ProductsController controller = new(
            Mock.Of<IProductService>(),
            searchService.Object,
            Mock.Of<IPriceCacheService>(),
            Mock.Of<IClientResourceAccessService>(),
            new ResponseFactory());

        IActionResult result = await controller.GetAllFromSearchAsync(
            query,
            20,
            0);

        OkObjectResult response = Assert.IsType<OkObjectResult>(result);
        IWebResponse body = Assert.IsAssignableFrom<IWebResponse>(response.Value);
        Assert.IsType<List<ProtectedSearchProduct>>(body.Body);
        Assert.Empty((List<ProtectedSearchProduct>)body.Body);
        searchService.VerifyNoOtherCalls();
    }
}
