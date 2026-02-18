using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Transporters.Contracts;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Ecommerce.Controllers;

//[Authorize(Roles = IdentityRoles.ClientUa)]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Transporters)]
public sealed class TransportersController(
    ITransporterService transporterService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(TransporterTypesSegments.ECOMMERCE_GET_ALL)]
    public async Task<IActionResult> GetAllTransporterTypesAsync() {
        try {
            return Ok(SuccessResponseBody(await transporterService.GetAllTransporterTypes()));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(TransportersSegments.GET_ALL_BY_TRANSPORTER_TYPE_NET_ID)]
    public async Task<IActionResult> GetAllTransportersByTransporterTypeNetIdAsync([FromQuery] Guid netId) {
        try {
            return Ok(SuccessResponseBody(await transporterService.GetAllTransportersByTransporterTypeNetId(netId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}