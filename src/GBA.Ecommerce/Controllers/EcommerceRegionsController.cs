using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Ecommerce;
using GBA.Services.Services.EcommerceRegions.Contracts;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Regions)]
public sealed class EcommerceRegionsController(
    IResponseFactory responseFactory,
    IEcommerceRegionService ecommerceRegionService) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(EcommerceRegionsSegments.GET_ALL)]
    public async Task<IActionResult> GetAllRegions() {
        try {
            return Ok(SuccessResponseBody(await ecommerceRegionService.GetAllLocale()));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AssignActionRoute(EcommerceRegionsSegments.UPDATE)]
    public async Task<IActionResult> Update([FromBody] EcommerceRegion ecommerceRegion) {
        try {
            return Ok(SuccessResponseBody(await ecommerceRegionService.Update(ecommerceRegion)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}