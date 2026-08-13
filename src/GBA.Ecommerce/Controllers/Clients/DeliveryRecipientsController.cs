using System;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.DeliveryRecipients.Contracts;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers.Clients;

[Authorize(Roles = IdentityRoles.ClientUa + "," + IdentityRoles.Workplace)]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.DeliveryRecipients)]
public sealed class DeliveryRecipientsController(
    IDeliveryRecipientService deliveryRecipientService,
    IClientResourceAccessService clientResourceAccessService,
    IResponseFactory responseFactory) : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(DeliveryRecipientsSegments.GET_ALL_DELIVERY_RECIPIENTS_BY_CURRENT_CLIENT)]
    public async Task<IActionResult> GetAllRecipientsByCurrentClientAsync() {
        Guid userNetId = GetUserNetId();
        return Ok(SuccessResponseBody(await deliveryRecipientService.GetAllRecipientsByClientNetId(userNetId)));
    }

    [HttpPost]
    [AssignActionRoute(DeliveryRecipientsSegments.ADD_NEW)]
    [Consumes("application/json")]
    [RequestSizeLimit(16384)]
    public async Task<IActionResult> AddRecipientAsync([FromBody] CreateDeliveryRecipientRequest request) {
        string fullName = request?.FullName?.Trim() ?? string.Empty;
        string mobilePhone = request?.MobilePhone?.Trim() ?? string.Empty;
        if (fullName.Length is < 1 or > 250 || mobilePhone.Length is < 1 or > 100)
            return BadRequest(ErrorResponseBody(
                "Delivery recipient name and phone are required.",
                HttpStatusCode.BadRequest));

        try {
            return Ok(SuccessResponseBody(await deliveryRecipientService.AddRecipient(
                GetUserNetId(),
                fullName,
                mobilePhone)));
        } catch (ArgumentException) {
            return BadRequest(ErrorResponseBody(
                "Delivery recipient is invalid.",
                HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AssignActionRoute(DeliveryRecipientAddressesSegments.ECOMMERCE_ADD_NEW)]
    [Consumes("application/json")]
    [RequestSizeLimit(16384)]
    public async Task<IActionResult> AddRecipientAddressAsync(
        [FromBody] CreateDeliveryRecipientAddressRequest request) {
        try {
            return Ok(SuccessResponseBody(await deliveryRecipientService.AddAddress(
                GetUserNetId(),
                request?.DeliveryRecipientNetUid ?? Guid.Empty,
                request?.Value,
                request?.City,
                request?.Department)));
        } catch (ArgumentException) {
            return BadRequest(ErrorResponseBody(
                "Delivery address is invalid.",
                HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(DeliveryRecipientAddressesSegments.ECOMMERCE_GET_ALL_BY_RECIPIENT_NET_ID)]
    public async Task<IActionResult> GetAllRecipientAddressesByRecipientNetIdAsync([FromQuery] Guid netId) {
        if (!clientResourceAccessService.CanAccessDeliveryRecipient(GetUserNetId(), netId)) return Forbid();

        return Ok(SuccessResponseBody(await deliveryRecipientService.GetAllAddressesByRecipientNetId(netId)));
    }

    public sealed class CreateDeliveryRecipientRequest {
        public string FullName { get; set; } = string.Empty;

        public string MobilePhone { get; set; } = string.Empty;
    }

    public sealed class CreateDeliveryRecipientAddressRequest {
        public Guid DeliveryRecipientNetUid { get; set; }

        public string Value { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;
    }
}
