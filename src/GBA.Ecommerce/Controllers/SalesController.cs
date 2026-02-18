using System;
using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers;

[Authorize(Roles = IdentityRoles.ClientUa + "," + IdentityRoles.Workplace)]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Sales)]
public class SalesController(
    IOrderService orderService,
    IResponseFactory responseFactory)
    : WebApiControllerBase(responseFactory) {

    [HttpGet]
    [AssignActionRoute(SalesSegments.GET_BY_NET_ID_ECOMMERCE)]
    public async Task<IActionResult> GetSalesByNetIdAsync([FromQuery] Guid netId) {
        return Ok(SuccessResponseBody(await orderService.GetSaleByNetId(netId)));
    }
}