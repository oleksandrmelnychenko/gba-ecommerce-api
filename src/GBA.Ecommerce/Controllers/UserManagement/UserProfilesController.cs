using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Ecommerce.Controllers.UserManagement;

[Authorize]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.UserProfilesManagement)]
public sealed class UserProfilesController(
    IClientService clientService,
    IClientAgreementService clientAgreementService,
    IResponseFactory responseFactory)
    : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(UserProfilesSegments.GET_BY_NET_ID)]
    public async Task<IActionResult> GetClientByNetId([FromQuery] Guid netId) {
        try {
            Guid userNetId = GetUserNetId();
            Guid clientNetId = netId.Equals(Guid.Empty) ? userNetId : netId;
            return Ok(SuccessResponseBody(await clientService.GetByNetId(clientNetId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(UserProfilesSegments.GET_ROOT_BY_SUBCLIENT)]
    public async Task<IActionResult> GetRootClientBySubClientNetId([FromQuery] Guid netId) {
        try {
            return Ok(SuccessResponseBody(await clientService.GetRootClientBySubClientNerId(netId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(UserProfilesSegments.UPDATE_SELECTED_AGREEMENT)]
    public async Task<IActionResult> UpdateSelectedClientAgreement(Guid agreementNetId) {
        try {
            Guid userNetId = GetUserNetId();
            return Ok(SuccessResponseBody(await clientAgreementService.UpdateSelectedClientAgreement(userNetId, agreementNetId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}