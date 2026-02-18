using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NLog;

namespace GBA.Ecommerce.Controllers;

[Authorize(Roles = IdentityRoles.ClientUa + "," + IdentityRoles.Workplace)]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Sales)]
public class SalesController(
    IOrderService orderService,
    IStringLocalizer<OrdersController> localizer,
    IResponseFactory responseFactory)
    : WebApiControllerBase(responseFactory) {
    private readonly IStringLocalizer<OrdersController> _localizer = localizer;

    [HttpGet]
    [AssignActionRoute(SalesSegments.GET_BY_NET_ID_ECOMMERCE)]
    public async Task<IActionResult> GetSalesByNetIdAsync([FromQuery] Guid netId) {
        try {
            return Ok(SuccessResponseBody(await orderService.GetSaleByNetId(netId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc.Message);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}