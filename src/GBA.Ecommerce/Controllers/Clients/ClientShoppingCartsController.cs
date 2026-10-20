using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ClientShoppingCartModels;
using GBA.Services.Infrastructure.Checkout;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace GBA.Ecommerce.Controllers.Clients;

[Authorize(Roles = IdentityRoles.ClientUa + "," + IdentityRoles.Workplace)]
[EnableRateLimiting("api")]
[RequestSizeLimit(524288)]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.ClientShoppingCartItems)]
[CartConflictFilter]
public sealed class ClientShoppingCartsController(
    IResponseFactory responseFactory,
    IClientShoppingCartService clientShoppingCartService,
    IStringLocalizer<ClientShoppingCartsController> localizer)
    : WebApiControllerBase(responseFactory) {
    [HttpPost]
    [AssignActionRoute(ClientShoppingCartItemsSegments.ADD_NEW)]
    public async Task<IActionResult> AddNewOrderItemAsync(
        [FromBody] OrderItem orderItem,
        [FromQuery] int withVat,
        [FromHeader(Name = CheckoutIdempotencyKey.HeaderName)] string? idempotencyKey = null) {
        Guid userNetId = GetUserNetId();
        if (!TryResolveOperation(idempotencyKey, out Guid? operationNetUid)) return InvalidIdempotencyKey();
        try {
            return Ok(SuccessResponseBody(await clientShoppingCartService.Add(orderItem, userNetId, withVat.Equals(1), operationNetUid)));
        } catch (LocalizedException locExc) {
            return BadRequest(ErrorResponseBody(Localize(locExc), System.Net.HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AssignActionRoute(ClientShoppingCartItemsSegments.ADD_NEW_WITH_VERIFY)]
    public async Task<IActionResult> AddNewOrderItemWithVerifyAsync(
        [FromBody] OrderItem orderItem,
        [FromQuery] int withVat,
        [FromHeader(Name = CheckoutIdempotencyKey.HeaderName)] string? idempotencyKey = null) {
        if (!TryResolveOperation(idempotencyKey, out Guid? operationNetUid)) return InvalidIdempotencyKey();
        try {
            Guid userNetId = GetUserNetId();

            return Ok(SuccessResponseBody(new {
                orderItem = await clientShoppingCartService.Add(orderItem, userNetId, withVat.Equals(1), operationNetUid),
                qtyRemainderProduct = (object)0,
                message = "",
                statusCode = TypeOfStatusCode.Success
            }));
        } catch (LocalizedException locExc) {
            return Ok(SuccessResponseBody(
                new {
                    orderItem = (OrderItem?)null,
                    qtyRemainderProduct = locExc.UnlocalizeElementMessage,
                    message = string.Format(localizer[locExc.LocalizedMessageKey].Value, locExc.UnlocalizeElementMessage),
                    statusCode = TypeOfStatusCode.Error
                }));
        }
    }

    [HttpPost]
    [AssignActionRoute(ClientShoppingCartItemsSegments.VERIFY)]
    public async Task<IActionResult> VerifyOrderItem([FromBody] OrderItem orderItem) {
        (bool success, string message) = await clientShoppingCartService.VerifyProductAvailability(orderItem);

        if (!success) return BadRequest(ErrorResponseBody(message, System.Net.HttpStatusCode.BadRequest));

        return Ok(SuccessResponseBody(orderItem));
    }

    [HttpPost]
    [AssignActionRoute(ClientShoppingCartItemsSegments.ADD_NEW_MANY)]
    public async Task<IActionResult> AddNewOrderItemAsync(
        [FromBody] List<OrderItem> orderItems,
        [FromQuery] int withVat,
        [FromHeader(Name = CheckoutIdempotencyKey.HeaderName)] string? idempotencyKey = null) {
        Guid userNetId = GetUserNetId();
        if (!TryResolveOperation(idempotencyKey, out Guid? operationNetUid)) return InvalidIdempotencyKey();

        try {
            return Ok(SuccessResponseBody(await clientShoppingCartService.Add(orderItems, userNetId, withVat.Equals(1), operationNetUid)));
        } catch (LocalizedException locExc) {
            return BadRequest(ErrorResponseBody(Localize(locExc), System.Net.HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AssignActionRoute(ClientShoppingCartItemsSegments.UPDATE)]
    public async Task<IActionResult> UpdateOrderItemAsync([FromBody] OrderItem orderItem, [FromQuery] int withVat) {
        Guid userNetId = GetUserNetId();

        return Ok(SuccessResponseBody(await clientShoppingCartService.Update(orderItem, userNetId, withVat.Equals(1))));
    }

    [HttpPost]
    [AssignActionRoute(ClientShoppingCartItemsSegments.UPDATE_MANY)]
    public async Task<IActionResult> UpdateOrderItemAsync([FromBody] List<OrderItem> orderItems, [FromQuery] int withVat) {
        Guid userNetId = GetUserNetId();

        return Ok(SuccessResponseBody(await clientShoppingCartService.Update(orderItems, userNetId, withVat.Equals(1))));
    }

    [HttpGet]
    [AssignActionRoute(ClientShoppingCartItemsSegments.GET_ALL)]
    public async Task<IActionResult> GetAllItemsFromCurrentShoppingCartAsync([FromQuery] int withVat) {
        Guid userNetId = GetUserNetId();

        return Ok(SuccessResponseBody(await clientShoppingCartService.GetAllItemsFromCurrentShoppingCartByClientNetId(userNetId, withVat.Equals(1))));
    }

    [HttpDelete]
    [AssignActionRoute(ClientShoppingCartItemsSegments.DELETE)]
    public async Task<IActionResult> DeleteItemFromShoppingCartByNetId([FromQuery] Guid netId, [FromQuery] int withVat) {
        Guid userNetId = GetUserNetId();

        await clientShoppingCartService.DeleteItemFromShoppingCartByNetId(netId, userNetId, withVat.Equals(1));

        return Ok(SuccessResponseBody(netId));
    }

    [HttpDelete]
    [AssignActionRoute(ClientShoppingCartItemsSegments.DELETE_ALL)]
    public async Task<IActionResult> DeleteAllItemsFromShoppingCartByNetId([FromQuery] int withVat) {
        Guid userNetId = GetUserNetId();

        await clientShoppingCartService.DeleteAllItemsFromShoppingCartByClientNetId(userNetId, withVat.Equals(1));

        return Ok(SuccessResponseBody(null));
    }

    private static bool TryResolveOperation(string? idempotencyKey, out Guid? operationNetUid) {
        operationNetUid = null;
        if (idempotencyKey == null) return true;
        if (!CheckoutIdempotencyKey.TryParse(idempotencyKey, out Guid parsed)) return false;

        operationNetUid = parsed;
        return true;
    }

    private IActionResult InvalidIdempotencyKey() {
        return BadRequest(ErrorResponseBody(
            $"{CheckoutIdempotencyKey.HeaderName} must be a non-empty UUID.",
            System.Net.HttpStatusCode.BadRequest));
    }

    private string Localize(LocalizedException locExc) {
        return locExc.LocalizedMessageKey != null
            ? string.Format(localizer[locExc.LocalizedMessageKey].Value, locExc.UnlocalizeElementMessage)
            : locExc.Message;
    }
}
