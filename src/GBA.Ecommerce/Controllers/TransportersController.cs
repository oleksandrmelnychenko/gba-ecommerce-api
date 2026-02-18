using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Transporters.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers;

//[Authorize(Roles = IdentityRoles.ClientUa)]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Transporters)]
public sealed class TransportersController(
    ITransporterService transporterService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(TransporterTypesSegments.ECOMMERCE_GET_ALL)]
    public async Task<IActionResult> GetAllTransporterTypesAsync() {
        return Ok(SuccessResponseBody(await transporterService.GetAllTransporterTypes()));
    }

    [HttpGet]
    [AssignActionRoute(TransportersSegments.GET_ALL_BY_TRANSPORTER_TYPE_NET_ID)]
    public async Task<IActionResult> GetAllTransportersByTransporterTypeNetIdAsync([FromQuery] Guid netId) {
        return Ok(SuccessResponseBody(await transporterService.GetAllTransportersByTransporterTypeNetId(netId)));
    }
}