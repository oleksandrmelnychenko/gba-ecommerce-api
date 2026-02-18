using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Products.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.CarBrands)]
public sealed class CarBrandsController(
    ICarBrandService carBrandService,
    IResponseFactory responseFactory)
    : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(CarBrandsSegments.GET_ALL_PRODUCTS_FILTERED)]
    public async Task<IActionResult> GetAllProductsFilteredAsync(
        [FromQuery] Guid carBrandNetId,
        [FromQuery] string carBrandAlias,
        [FromQuery] long limit = 20,
        [FromQuery] long offset = 0) {
        Guid userNetId = GetUserNetId();

        return Ok(
            SuccessResponseBody(
                string.IsNullOrEmpty(carBrandAlias)
                    ? await carBrandService.GetAllProductsFilteredByCarBrand(
                        carBrandNetId,
                        userNetId,
                        limit,
                        offset
                    )
                    : await carBrandService.GetAllProductsFilteredByCarBrand(
                        carBrandAlias,
                        userNetId,
                        limit,
                        offset
                    )
            )
        );
    }

    [HttpGet]
    [AssignActionRoute(CarBrandsSegments.GET_ALL_CAR_BRANDS)]
    public async Task<IActionResult> GetAllCarBrandsAsync() {
        return Ok(
            SuccessResponseBody(
                await carBrandService.GetAllCarBrands()
            )
        );
    }
}