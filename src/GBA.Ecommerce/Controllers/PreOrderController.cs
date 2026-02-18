using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Sales;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.PreOrders)]
public sealed class PreOrderController(
    IPreOrderService preOrderService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpPost]
    [AssignActionRoute(PreOrderSegments.ADD_NEW)]
    public async Task<IActionResult> AddNewPreOrderAsync([FromBody] PreOrder preOrder) {
        try {
            Guid userNetId = GetUserNetId();
            return Ok(SuccessResponseBody(await preOrderService.AddNewPreOrder(preOrder, userNetId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}