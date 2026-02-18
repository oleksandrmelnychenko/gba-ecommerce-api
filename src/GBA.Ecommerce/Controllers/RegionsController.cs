using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Regions.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Regions)]
public sealed class RegionsController(
    IRegionService regionService,
    IRegionCodeService regionCodeService,
    IResponseFactory responseFactory)
    : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(RegionsSegments.GET_ALL)]
    public async Task<IActionResult> GetAllRegions() {
        return Ok(SuccessResponseBody(await regionService.GetAllRegions()));
    }

    [HttpGet]
    [AssignActionRoute(RegionsSegments.GET_AVAILABLE_CODE_BY_REGION)]
    public async Task<IActionResult> GetAllRegions([FromQuery] Guid netId) {
        return Ok(SuccessResponseBody(await regionCodeService.GetAvailableRegionCode(netId)));
    }
}