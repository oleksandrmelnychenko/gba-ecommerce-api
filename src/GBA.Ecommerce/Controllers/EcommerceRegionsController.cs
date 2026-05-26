using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Ecommerce;
using GBA.Services.Services.EcommerceRegions.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Regions)]
public sealed class EcommerceRegionsController(
    IResponseFactory responseFactory,
    IOutputCacheStore outputCacheStore,
    IEcommerceRegionService ecommerceRegionService) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(EcommerceRegionsSegments.GET_ALL)]
    [OutputCache(PolicyName = "Regions")]
    public async Task<IActionResult> GetAllRegions() {
        return Ok(SuccessResponseBody(await ecommerceRegionService.GetAllLocale()));
    }

    [HttpPost]
    [AssignActionRoute(EcommerceRegionsSegments.UPDATE)]
    public async Task<IActionResult> Update([FromBody] EcommerceRegion ecommerceRegion) {
        var result = await ecommerceRegionService.Update(ecommerceRegion);
        await outputCacheStore.EvictByTagAsync("regions", HttpContext.RequestAborted);
        return Ok(SuccessResponseBody(result));
    }
}
