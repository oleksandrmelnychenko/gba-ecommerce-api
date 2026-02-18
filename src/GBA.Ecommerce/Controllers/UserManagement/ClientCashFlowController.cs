using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.UserManagement.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Ecommerce.Controllers.UserManagement;

[Authorize]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.UserManagement)]
public sealed class ClientCashFlowController(
    IResponseFactory responseFactory,
    IAccountingCashFlowService accountingCashFlowService)
    : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(UserManagementSegments.GET_CLIENT_CASH_FLOW)]
    public async Task<IActionResult> GetTokenAsync([FromQuery] Guid netId, [FromQuery] DateTime from, [FromQuery] DateTime to) {
        try {
            to = to.AddHours(23).AddMinutes(59).AddSeconds(59);
            return Ok(SuccessResponseBody(await accountingCashFlowService.GetAccountingCashFlow(netId, from, to)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}