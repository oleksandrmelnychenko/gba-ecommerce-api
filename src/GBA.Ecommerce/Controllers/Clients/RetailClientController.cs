using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Common.WebApi.RoutingConfiguration.Maps.Clients;
using GBA.Domain.Entities.Clients;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers.Clients;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.RetailClients)]
public sealed class RetailClientController(
    IClientService clientService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpPost]
    [AssignActionRoute(RetailClientSegments.ADD_NEW)]
    public async Task<IActionResult> AddTemporaryClient([FromBody] RetailClient retailClient) {
        if (retailClient.Name == null || retailClient.Name.Equals(string.Empty)) throw new Exception("Username is required");
        if (retailClient.PhoneNumber == null || retailClient.PhoneNumber.Equals(string.Empty)) throw new Exception("Phone number is required");

        RetailClient result = await clientService.AddRetailClient(retailClient);
        if (result == null) throw new Exception("Unable to add retail client.");

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