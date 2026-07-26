using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Common.WebApi.RoutingConfiguration.Maps.Clients;
using GBA.Domain.Entities.Clients;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GBA.Ecommerce.Controllers.Clients;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.RetailClients)]
[EnableRateLimiting("checkout")]
public sealed class RetailClientController(
    IClientService clientService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpPost]
    [AssignActionRoute(RetailClientSegments.ADD_NEW)]
    [Consumes("application/json")]
    [RequestSizeLimit(262144)]
    public async Task<IActionResult> AddTemporaryClient([FromBody] RetailClient retailClient) {
        if (string.IsNullOrWhiteSpace(retailClient?.Name) || retailClient.Name.Trim().Length > 120)
            throw new ArgumentException("A valid name is required.");
        if (string.IsNullOrWhiteSpace(retailClient.PhoneNumber) ||
            retailClient.PhoneNumber.Trim().Length is < 7 or > 32)
            throw new ArgumentException("A valid phone number is required.");

        RetailClient result = await clientService.AddRetailClient(retailClient);
        if (result == null) throw new InvalidOperationException("Unable to add retail client.");

        return Ok(SuccessResponseBody(result));
    }

    [HttpGet]
    [AssignActionRoute(RetailClientSegments.GET)]
    public async Task<IActionResult> GetRetailClientByNetId([FromQuery] Guid netId) {
        if (netId.Equals(Guid.Empty)) throw new Exception("NetId cannot be empty");

        return Ok(SuccessResponseBody(await clientService.GetRetailClientByNetId(netId)));
    }

    [HttpGet]
    [AssignActionRoute(RetailClientSegments.GET_CHECK_ORDER_ITEM)]
    public async Task<IActionResult> GetRetailClientByNetIdCheckOrderItems([FromQuery] Guid netId) {
        if (netId.Equals(Guid.Empty)) throw new Exception("NetId cannot be empty");

        return Ok(SuccessResponseBody(await clientService.GetRetailClientByNetIdCheckOrderItems(netId)));
    }
}
