using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.DeliveryRecipients.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GBA.Ecommerce.Controllers.Clients;

[Authorize(Roles = IdentityRoles.ClientUa + "," + IdentityRoles.Workplace)]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.DeliveryRecipients)]
public sealed class DeliveryRecipientsController(
    IDeliveryRecipientService deliveryRecipientService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(DeliveryRecipientsSegments.GET_ALL_DELIVERY_RECIPIENTS_BY_CURRENT_CLIENT)]
    public async Task<IActionResult> GetAllRecipientsByCurrentClientAsync() {
        try {
            Guid userNetId = GetUserNetId();
            return Ok(SuccessResponseBody(await deliveryRecipientService.GetAllRecipientsByClientNetId(userNetId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(DeliveryRecipientAddressesSegments.ECOMMERCE_GET_ALL_BY_RECIPIENT_NET_ID)]
    public async Task<IActionResult> GetAllRecipientAddressesByRecipientNetIdAsync([FromQuery] Guid netId) {
        try {
            return Ok(SuccessResponseBody(await deliveryRecipientService.GetAllAddressesByRecipientNetId(netId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}