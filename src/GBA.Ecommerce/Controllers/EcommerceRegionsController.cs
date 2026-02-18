using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Ecommerce;
using GBA.Services.Services.EcommerceRegions.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Regions)]
public sealed class EcommerceRegionsController(
    IResponseFactory responseFactory,
    IEcommerceRegionService ecommerceRegionService) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(EcommerceRegionsSegments.GET_ALL)]
    public async Task<IActionResult> GetAllRegions() {
        return Ok(SuccessResponseBody(await ecommerceRegionService.GetAllLocale()));
    }

    [HttpPost]
    [AssignActionRoute(EcommerceRegionsSegments.UPDATE)]
    public async Task<IActionResult> Update([FromBody] EcommerceRegion ecommerceRegion) {
        return Ok(SuccessResponseBody(await ecommerceRegionService.Update(ecommerceRegion)));
    }
}