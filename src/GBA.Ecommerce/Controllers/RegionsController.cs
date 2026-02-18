using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Regions.Contracts;
using Microsoft.AspNetCore.Mvc;
using NLog;

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
        try {
            return Ok(SuccessResponseBody(await regionService.GetAllRegions()));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(RegionsSegments.GET_AVAILABLE_CODE_BY_REGION)]
    public async Task<IActionResult> GetAllRegions([FromQuery] Guid netId) {
        try {
            return Ok(SuccessResponseBody(await regionCodeService.GetAvailableRegionCode(netId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}