using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.UserManagement.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers.UserManagement;

[Authorize]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Agreements)]
public sealed class AgreementsController(
    IResponseFactory responseFactory,
    IAgreementService agreementService) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(AgreementsSegments.GET_ALL_TOTAL_BY_CLIENT)]
    public async Task<IActionResult> GetAllAgreementsByClientNetIdAsync([FromQuery] Guid netId) {
        return Ok(SuccessResponseBody(await agreementService.GetAllAgreementsByClientNetId(netId)));
    }

    [HttpGet]
    [AssignActionRoute(AgreementsSegments.GET_TOTAL_AGREEMENT_DEBT_AFTER_DAYS)]
    public async Task<IActionResult> GetDebtAfterDaysByClientAgreementNetIdAsync([FromQuery] Guid netId, [FromQuery] int days) {
        return Ok(SuccessResponseBody(await agreementService.GetDebtAfterDaysByClientAgreementNetId(netId, days)));
    }
}