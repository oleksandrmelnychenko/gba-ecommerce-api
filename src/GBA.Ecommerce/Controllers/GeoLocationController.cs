using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.GeoLocations.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.GeoLocations)]
public sealed class GeoLocationController(
    IGeoLocationService geoLocationService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(GeoLocationSegments.GET_CURRENT)]
    public async Task<IActionResult> GetProductByNetId() {
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        return Ok(SuccessResponseBody(await geoLocationService.GetGeoLocationDataByIpAddress(ipAddress)));
    }
}