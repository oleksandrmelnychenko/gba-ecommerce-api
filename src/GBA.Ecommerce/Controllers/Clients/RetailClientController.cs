using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Common.WebApi.RoutingConfiguration.Maps.Clients;
using GBA.Domain.Entities.Clients;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Ecommerce.Controllers.Clients;

[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.RetailClients)]
public sealed class RetailClientController(
    IClientService clientService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpPost]
    [AssignActionRoute(RetailClientSegments.ADD_NEW)]
    public async Task<IActionResult> AddTemporaryClient([FromBody] RetailClient retailClient) {
        try {
            if (retailClient.Name == null || retailClient.Name.Equals(string.Empty)) throw new Exception("Username is required");
            if (retailClient.PhoneNumber == null || retailClient.PhoneNumber.Equals(string.Empty)) throw new Exception("Phone number is required");

            RetailClient result = await clientService.AddRetailClient(retailClient);

            return Ok(SuccessResponseBody(result));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(RetailClientSegments.GET)]
    public async Task<IActionResult> GetRetailClientByNetId([FromQuery] Guid netId) {
        try {
            if (netId.Equals(Guid.Empty)) throw new Exception("NetId cannot be empty");

            return Ok(SuccessResponseBody(await clientService.GetRetailClientByNetId(netId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(RetailClientSegments.GET_CHECK_ORDER_ITEM)]
    public async Task<IActionResult> GetRetailClientByNetIdCheckOrderItems([FromQuery] Guid netId) {
        try {
            if (netId.Equals(Guid.Empty)) throw new Exception("NetId cannot be empty");

            return Ok(SuccessResponseBody(await clientService.GetRetailClientByNetIdCheckOrderItems(netId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}