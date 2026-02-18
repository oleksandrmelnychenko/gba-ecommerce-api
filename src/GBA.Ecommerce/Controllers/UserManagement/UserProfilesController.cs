using System;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        Guid userNetId = GetUserNetId();
        Guid clientNetId = netId.Equals(Guid.Empty) ? userNetId : netId;
        return Ok(SuccessResponseBody(await clientService.GetByNetId(clientNetId)));
    }

    [HttpGet]
    [AssignActionRoute(UserProfilesSegments.GET_ROOT_BY_SUBCLIENT)]
    public async Task<IActionResult> GetRootClientBySubClientNetId([FromQuery] Guid netId) {
        return Ok(SuccessResponseBody(await clientService.GetRootClientBySubClientNerId(netId)));
    }

    [HttpGet]
    [AssignActionRoute(UserProfilesSegments.UPDATE_SELECTED_AGREEMENT)]
    public async Task<IActionResult> UpdateSelectedClientAgreement(Guid agreementNetId) {
        Guid userNetId = GetUserNetId();
        return Ok(SuccessResponseBody(await clientAgreementService.UpdateSelectedClientAgreement(userNetId, agreementNetId)));
    }
}