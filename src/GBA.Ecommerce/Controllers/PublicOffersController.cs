using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Services.Services.Offers.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace GBA.Ecommerce.Controllers;

// Anonymous, token-scoped view of a manager-shared offer: the unguessable ClientShoppingCart.NetUID
// in the link IS the authorization. The payload is deliberately trimmed — offer lines and totals
// only, no client contact/agreement internals beyond the display name.
[AllowAnonymous]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.PublicOffers)]
public sealed class PublicOffersController(
    IOfferService offerService,
    ILogger<PublicOffersController> logger,
    IResponseFactory responseFactory)
    : WebApiControllerBase(responseFactory) {

    [HttpGet]
    [EnableRateLimiting("search")]
    [AssignActionRoute("get")]
    public async Task<IActionResult> GetByLinkAsync([FromQuery] Guid netId) {
        if (netId == Guid.Empty) {
            return BadRequest(ErrorResponseBody("offer id is required", HttpStatusCode.BadRequest));
        }

        try {
            var offer = await offerService.GetOfferForPublicLink(netId);

            return Ok(SuccessResponseBody(new {
                offer.NetUid,
                offer.Number,
                offer.Created,
                offer.ValidUntil,
                ClientName = offer.ClientAgreement?.Client?.FullName,
                CurrencyCode = offer.ClientAgreement?.Agreement?.Currency?.Code,
                Items = (offer.OrderItems ?? []).Select(item => new {
                    Name = item.Product?.NameUA ?? item.Product?.Name,
                    item.Product?.VendorCode,
                    ProductNetUid = item.Product?.NetUid,
                    item.Qty,
                    Price = item.Product?.CurrentPrice ?? 0,
                    PriceLocal = item.Product?.CurrentLocalPrice ?? 0,
                    Total = item.TotalAmount,
                    TotalLocal = item.TotalAmountLocal,
                    item.Comment,
                }).ToList(),
                offer.TotalAmount,
                offer.TotalLocalAmount,
            }));
        } catch (Exception exc) {
            // not-exists / expired / processed all collapse to 404 for an anonymous caller —
            // a dead link must not reveal WHY it is dead; the real cause goes to the log.
            logger.LogWarning(exc, "Public offer link {NetId} rejected", netId);
            return NotFound(ErrorResponseBody("offer_unavailable", HttpStatusCode.NotFound));
        }
    }
}
