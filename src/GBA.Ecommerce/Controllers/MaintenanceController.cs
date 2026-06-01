using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, "maintenance")]
[Authorize(Roles = IdentityRoles.Administrator)]
public sealed class MaintenanceController(
    IClientShoppingCartService clientShoppingCartService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {

    [HttpPost]
    [Route("carts/release-expired")]
    public async Task<IActionResult> ReleaseExpiredCartsAsync() {
        int released = await clientShoppingCartService.ReleaseExpiredCartsAsync();
        return Ok(SuccessResponseBody(new { released }));
    }
}
