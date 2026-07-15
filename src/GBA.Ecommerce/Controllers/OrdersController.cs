using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Ecommerce.Infrastructure;
using GBA.Services.Infrastructure;
using GBA.Services.Infrastructure.SalesMutations;
using GBA.Services.Services.Offers.Contracts;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce.Controllers;

[Authorize(Roles = IdentityRoles.ClientUa + "," + IdentityRoles.Workplace)]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Orders)]
public sealed class OrdersController(
    IOrderService orderService,
    IOfferService offerService,
    IHttpClientFactory httpClientFactory,
    SalesMutationOutboxOptions salesMutationOutboxOptions,
    SalesMutationInternalAuthOptions salesMutationInternalAuthOptions,
    IResponseFactory responseFactory)
    : WebApiControllerBase(responseFactory) {
    private readonly TtnUploadClient _ttnUploadClient = new(
        httpClientFactory,
        salesMutationOutboxOptions,
        salesMutationInternalAuthOptions);

    [HttpGet]
    [AssignActionRoute(OrdersSegments.ADD_NEW)]
    public async Task<IActionResult> GenerateOrderFromClientShoppingCartAsync([FromQuery] int withVat) {
        Guid userNetId = GetUserNetId();
        return Ok(SuccessResponseBody(await orderService.GenerateNewOrderAndSaleFromClientShoppingCart(userNetId, withVat.Equals(1))));
    }

    [HttpPost]
    [RequestSizeLimit(TtnUploadClient.MaxRequestSizeBytes)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = TtnUploadClient.MaxRequestSizeBytes,
        MemoryBufferThreshold = TtnUploadClient.MemoryBufferThresholdBytes)]
    [AssignActionRoute(OrdersSegments.ADD_NEW_AS_INVOICE)]
    public async Task<IActionResult> GenerateNewSaleWithInvoiceAsync(
        [FromQuery] bool withVat,
        [FromHeader(Name = SalesMutationRequestKey.HeaderName)] string? idempotencyKey,
        string invoice,
        string number,
        IFormFile invoiceFile) {
        Guid userNetId = GetUserNetId();
        Claim? type = User.Claims.FirstOrDefault(e => e.Type.Equals("type"));
        bool isWorkplace = type != null && type.Value.Equals(IdentityRoles.Workplace);

        // TODO Localize messages

        if (string.IsNullOrEmpty(invoice)) return BadRequest(ErrorResponseBody("ShoppingCart entity can not be empty", HttpStatusCode.BadRequest));

        invoice = invoice.Replace(" 02:00\"", "+02:00\"").Replace(" 03:00\"", "+03:00\"");

        Sale? parsedSale = JsonSerializer.Deserialize<Sale>(invoice);

        if (parsedSale == null) return BadRequest(ErrorResponseBody("Invalid sale data", HttpStatusCode.BadRequest));
        if (!SalesCreationRequestKey.TryResolveInboundKey(
                idempotencyKey,
                parsedSale.NetUid,
                out Guid operationNetUid))
            return InvalidIdempotencyKey();

        parsedSale.CustomersOwnTtn = new CustomersOwnTtn {
            Number = number
        };

        if (invoiceFile == null)
            return await ExecuteSalesCreationAsync(() =>
                orderService.GenerateNewSaleWithInvoice(
                    parsedSale,
                    userNetId,
                    isWorkplace,
                    operationNetUid,
                    null));

        if (!TryResolveTtnUploadCulture(out CultureInfo uploadCulture))
            return BadRequest(ErrorResponseBody(
                "The route culture is not supported for TTN upload.",
                HttpStatusCode.BadRequest));

        TtnUploadResult upload;
        try {
            upload = await _ttnUploadClient.StageAsync(
                invoiceFile,
                operationNetUid,
                uploadCulture,
                HttpContext.RequestAborted);
        } catch (TtnUploadHttpException exception) {
            return TtnUploadFailure(exception);
        } catch (OperationCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested) {
            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                ErrorResponseBody(
                    "The internal TTN upload timed out.",
                    HttpStatusCode.GatewayTimeout));
        } catch (HttpRequestException) {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                ErrorResponseBody(
                    "The internal TTN upload could not be completed.",
                    HttpStatusCode.BadGateway));
        }

        parsedSale.CustomersOwnTtn.TtnPDFPath = upload.Url;
        Sale createdSale;
        try {
            createdSale = await orderService.GenerateNewSaleWithInvoice(
                parsedSale,
                userNetId,
                isWorkplace,
                operationNetUid,
                upload.Sha256);
        } catch (SalesCreationIdempotencyConflictException exception) {
            return Conflict(ErrorResponseBody(
                exception.Message,
                HttpStatusCode.Conflict));
        } catch {
            await BestEffortAbortTtnAsync(operationNetUid, uploadCulture);
            throw;
        }

        try {
            string finalizedUrl = await _ttnUploadClient.FinalizeAsync(
                operationNetUid,
                uploadCulture,
                HttpContext.RequestAborted);
            if (!string.Equals(finalizedUrl, upload.Url, StringComparison.Ordinal))
                return TtnFinalizeFailure(
                    "The internal TTN finalize response did not match the staged file URL.");
        } catch (OperationCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested) {
            return TtnFinalizeFailure("The internal TTN finalize operation timed out.");
        } catch (HttpRequestException) {
            return TtnFinalizeFailure(
                "The internal TTN finalize operation could not be completed.");
        } catch (TtnUploadHttpException exception) {
            return TtnFinalizeFailure(exception.Message);
        }

        return Ok(SuccessResponseBody(createdSale));
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.ADD_NEW_AS_QUICK_INVOICE)]
    public async Task<IActionResult> GenerateNewSaleWithInvoiceAsync(
        [FromBody] Sale sale,
        [FromHeader(Name = SalesMutationRequestKey.HeaderName)] string? idempotencyKey,
        [FromQuery] Guid clientNetId,
        [FromQuery] string card,
        [FromQuery] int fullPayment) {
        if (!SalesCreationRequestKey.TryResolveInboundKey(
                idempotencyKey,
                sale.NetUid,
                out Guid operationNetUid))
            return InvalidIdempotencyKey();

        return await ExecuteSalesCreationAsync(() =>
            orderService.GenerateNewRetailSale(
                sale,
                clientNetId,
                fullPayment.Equals(1),
                operationNetUid));
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.CALCULATE_TOTAL_PRICES)]
    public async Task<IActionResult> CalculateTotalsForOrderAsync([FromBody] Order order) {
        return Ok(SuccessResponseBody(await orderService.DynamicallyCalculateTotalPrices(order)));
    }

    [HttpGet]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.GET_ECOMMERCE_OFFER_BY_NET_ID)]
    public async Task<IActionResult> GetOfferByNetIdAsync([FromQuery] Guid netId) {
        return Ok(SuccessResponseBody(await offerService.GetOfferByNetId(netId)));
    }

    [HttpGet]
    [AssignActionRoute(OrdersSegments.GET_ALL_AVAILABLE_FOR_CLIENT_ECOMMERCE_OFFERS)]
    public async Task<IActionResult> GetAllAvailableOffersForClientAsync() {
        Guid userNetId = GetUserNetId();
        return Ok(SuccessResponseBody(await offerService.GetAllAvailableOffersByClientNetId(userNetId)));
    }

    [HttpPost]
    [AssignActionRoute(OrdersSegments.ADD_NEW_FROM_OFFER)]
    public async Task<IActionResult> GenerateOrderAndSaleFromOfferAsync([FromBody] ClientShoppingCart clientShoppingCart, [FromQuery] int addCartItems) {
        Guid userNetId = GetUserNetId();
        return Ok(
            SuccessResponseBody(
                await offerService.GenerateNewOrderAndSaleFromOffer(clientShoppingCart, userNetId, addCartItems == 1)
            )
        );
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.CALCULATE_TOTAL_PRICES_FOR_CHANGED_OFFER)]
    public async Task<IActionResult> CalculateTotalsForOrderAsOfferAsync([FromBody] Order order) {
        return Ok(SuccessResponseBody(await offerService.DynamicallyCalculateTotalPrices(order)));
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.UPLOAD_CLIENT_PAYMENT_CONFIRMATION)]
    public async Task<IActionResult> UploadPaymentImageAsync([FromQuery] Guid clientNetId, [FromQuery] Guid saleNetId, IFormFile image) {
        if (image == null) throw new Exception("Image cannot be null");

        byte[] buffer = new byte[image.Length];

        await using (Stream stream = image.OpenReadStream()) {
            stream.ReadExactly(buffer);
        }

        string base64Image = Convert.ToBase64String(buffer);

        await orderService.SendPaymentImageToCrm(saleNetId, clientNetId, new PaymentConfirmationImageModel(base64Image, image.FileName));

        return Ok(SuccessResponseBody("success"));
    }

    private IActionResult InvalidIdempotencyKey() =>
        BadRequest(ErrorResponseBody(
            $"Provide a non-empty UUID in the {SalesMutationRequestKey.HeaderName} header or Sale.NetUid.",
            HttpStatusCode.BadRequest));

    private IActionResult TtnUploadFailure(TtnUploadHttpException exception) {
        if (exception.StatusCode == HttpStatusCode.Conflict)
            return Conflict(ErrorResponseBody(
                exception.Message,
                HttpStatusCode.Conflict));

        HttpStatusCode responseStatus = exception.StatusCode switch {
            HttpStatusCode.BadRequest => HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge => HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.UnsupportedMediaType => HttpStatusCode.UnsupportedMediaType,
            _ => HttpStatusCode.BadGateway
        };
        return StatusCode(
            (int) responseStatus,
            ErrorResponseBody(exception.Message, responseStatus));
    }

    private bool TryResolveTtnUploadCulture(out CultureInfo culture) {
        string? routeCulture = null;
        if (ControllerContext.RouteData?.Values.TryGetValue(
                "culture",
                out object? routeValue) == true)
            routeCulture = routeValue?.ToString()?.Trim();
        if (string.Equals(routeCulture, "uk", StringComparison.OrdinalIgnoreCase)) {
            culture = CultureInfo.GetCultureInfo("uk");
            return true;
        }

        culture = CultureInfo.InvariantCulture;
        return false;
    }

    private async Task BestEffortAbortTtnAsync(
        Guid operationNetUid,
        CultureInfo culture) {
        try {
            await _ttnUploadClient.AbortAsync(
                operationNetUid,
                culture,
                CancellationToken.None);
        } catch {
            // The durable digest binding remains retryable even if cleanup is unavailable.
        }
    }

    private IActionResult TtnFinalizeFailure(string message) {
        Response.Headers.RetryAfter = "1";
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            ErrorResponseBody(message, HttpStatusCode.ServiceUnavailable));
    }

    private async Task<IActionResult> ExecuteSalesCreationAsync<T>(Func<Task<T>> createSale) {
        try {
            return Ok(SuccessResponseBody(await createSale()));
        } catch (SalesCreationIdempotencyConflictException exception) {
            return Conflict(ErrorResponseBody(exception.Message, HttpStatusCode.Conflict));
        }
    }
}
