using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Sales;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.PreOrders)]
[Authorize(Roles = IdentityRoles.ClientUa + "," + IdentityRoles.Workplace)]
public sealed class PreOrderController(
    IPreOrderService preOrderService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpPost]
    [AssignActionRoute(PreOrderSegments.ADD_NEW)]
    [EnableRateLimiting("api")]
    [Consumes("application/json")]
    [RequestSizeLimit(65536)]
    public async Task<IActionResult> AddNewPreOrderAsync([FromBody] PreOrder preOrder) {
        Guid userNetId = GetUserNetId();
        return Ok(SuccessResponseBody(await preOrderService.AddNewPreOrder(preOrder, userNetId)));
    }
}
