using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.GeoLocations.Contracts;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.GeoLocations)]
public sealed class GeoLocationController(
    IGeoLocationService geoLocationService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(GeoLocationSegments.GET_CURRENT)]
    public async Task<IActionResult> GetProductByNetId() {
        try {
            return Ok(SuccessResponseBody(await geoLocationService.GetGeoLocationDataByIpAddress(HttpContext.Connection.RemoteIpAddress.ToString())));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}