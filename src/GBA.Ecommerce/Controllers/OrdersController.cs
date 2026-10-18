using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Common.Configuration;
using GBA.Common.Helpers;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.Models;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Services.Infrastructure.Checkout;
using GBA.Services.Services.Offers.Contracts;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GBA.Ecommerce.Controllers;

[Authorize(Roles = IdentityRoles.ClientUa + "," + IdentityRoles.Workplace)]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Orders)]
public sealed class OrdersController(
    IOrderService orderService,
    IOfferService offerService,
    IHttpClientFactory httpClientFactory,
    IResponseFactory responseFactory)
    : WebApiControllerBase(responseFactory) {
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    [HttpGet]
    [AssignActionRoute(OrdersSegments.ADD_NEW)]
    public async Task<IActionResult> GenerateOrderFromClientShoppingCartAsync(
        [FromQuery] int withVat,
        [FromHeader(Name = CheckoutIdempotencyKey.HeaderName)] string? idempotencyKey) {
        if (!CheckoutIdempotencyKey.TryParse(idempotencyKey, out Guid operationNetUid))
            return MissingIdempotencyKey();

        Guid userNetId = GetUserNetId();
        bool isVat = withVat.Equals(1);
        CheckoutAttempt attempt = new(operationNetUid, CheckoutFingerprint.ForCart(isVat));

        return await CheckoutResponseAsync(() =>
            orderService.GenerateNewOrderAndSaleFromClientShoppingCart(userNetId, isVat, attempt));
    }

    [HttpPost]
    [AssignActionRoute(OrdersSegments.ADD_NEW_AS_INVOICE)]
    [RequestSizeLimit(2097152)]
    public async Task<IActionResult> GenerateNewSaleWithInvoiceAsync(
        [FromQuery] bool withVat,
        string invoice,
        string number,
        IFormFile invoiceFile,
        [FromHeader(Name = CheckoutIdempotencyKey.HeaderName)] string? idempotencyKey) {
        Guid userNetId = GetUserNetId();
        Claim? type = User.Claims.FirstOrDefault(e => e.Type.Equals("type"));
        bool isWorkplace = type != null && type.Value.Equals(IdentityRoles.Workplace);

        // TODO Localize messages

        if (string.IsNullOrEmpty(invoice) || invoice.Length > 524288)
            return BadRequest(ErrorResponseBody("ShoppingCart entity is invalid", HttpStatusCode.BadRequest));
        if (number?.Length > 100)
            return BadRequest(ErrorResponseBody("TTN number is invalid", HttpStatusCode.BadRequest));
        if (invoiceFile != null) {
            if (invoiceFile.Length is <= 0 or > 1500000)
                return BadRequest(ErrorResponseBody("TTN file is invalid", HttpStatusCode.BadRequest));

            byte[] header = new byte[Math.Min(16, (int)invoiceFile.Length)];
            await using Stream validationStream = invoiceFile.OpenReadStream();
            int bytesRead = await validationStream.ReadAsync(header);
            if (!HasValidCheckoutDocumentSignature(header.AsSpan(0, bytesRead), invoiceFile.ContentType))
                return BadRequest(ErrorResponseBody("TTN file is invalid", HttpStatusCode.BadRequest));
        }

        if (!CheckoutIdempotencyKey.TryParse(idempotencyKey, out Guid operationNetUid))
            return MissingIdempotencyKey();

        string requestedVatMode = Request.Query["withVat"].ToString();
        bool isVatRequested = withVat || requestedVatMode.Equals("1", StringComparison.Ordinal);
        byte[]? invoiceFileHash = null;

        if (invoiceFile != null) {
            await using Stream hashStream = invoiceFile.OpenReadStream();
            invoiceFileHash = await SHA256.HashDataAsync(hashStream);
        }

        CheckoutAttempt attempt = new(
            operationNetUid,
            CheckoutFingerprint.ForInvoice(requestedVatMode, invoice, number, invoiceFileHash));

        invoice = invoice.Replace(" 02:00\"", "+02:00\"").Replace(" 03:00\"", "+03:00\"");

        Sale? parsedSale = JsonSerializer.Deserialize<Sale>(invoice);

        if (parsedSale == null) return BadRequest(ErrorResponseBody("Invalid sale data", HttpStatusCode.BadRequest));

        parsedSale.CustomersOwnTtn = new CustomersOwnTtn {
            Number = number
        };

        return await CheckoutResponseAsync(async () => {
            if (invoiceFile != null) {
                CheckoutResult? replay = orderService.FindInvoiceCheckoutReplay(userNetId, isVatRequested, attempt);
                if (replay != null) return replay;

                parsedSale.CustomersOwnTtn.TtnPDFPath = await UploadTtnFileAsync(invoiceFile);
            }

            return await orderService.GenerateNewSaleWithInvoice(
                parsedSale,
                userNetId,
                isWorkplace,
                isVatRequested,
                attempt);
        });
    }

    private async Task<string?> UploadTtnFileAsync(IFormFile invoiceFile) {
        string crmUrl;

        if (System.IO.File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
            EcommerceCrmConfig data = JsonSerializer.Deserialize<EcommerceCrmConfig>(
                await System.IO.File.ReadAllTextAsync(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()),
                _jsonSerializerOptions) ?? new EcommerceCrmConfig();
#if DEBUG
            crmUrl = $"{data?.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/save/ttn";
#else
            crmUrl = $"{data?.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/save/ttn";
#endif
        } else {
            throw new InvalidOperationException("CRM endpoint is not configured.");
        }

        using HttpClient httpClient = httpClientFactory.CreateClient(
            EcommerceInternalHttpClientDefaults.ClientName);

        MultipartFormDataContent formData = new();

        using StreamContent streamContent = new(invoiceFile.OpenReadStream());
        formData.Add(streamContent, "file", Path.GetFileName(invoiceFile.FileName));

        using HttpResponseMessage response = await httpClient.PostAsync(crmUrl, formData);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync()
            : null;
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.ADD_NEW_AS_QUICK_INVOICE)]
    [EnableRateLimiting("checkout")]
    [Consumes("application/json")]
    [RequestSizeLimit(524288)]
    public async Task<IActionResult> GenerateNewSaleWithInvoiceAsync(
        [FromBody] Sale sale,
        [FromQuery] Guid clientNetId,
        [FromQuery] string card,
        [FromQuery] int fullPayment) {
        return Ok(SuccessResponseBody(
            await orderService.GenerateNewRetailSale(
                sale,
                clientNetId,
                fullPayment.Equals(1))));
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.CALCULATE_TOTAL_PRICES)]
    [EnableRateLimiting("checkout")]
    [Consumes("application/json")]
    [RequestSizeLimit(524288)]
    public async Task<IActionResult> CalculateTotalsForOrderAsync([FromBody] Order order) {
        return Ok(SuccessResponseBody(await orderService.DynamicallyCalculateTotalPrices(order, GetUserNetId())));
    }

    [HttpGet]
    [AssignActionRoute(OrdersSegments.GET_ECOMMERCE_OFFER_BY_NET_ID)]
    public async Task<IActionResult> GetOfferByNetIdAsync([FromQuery] Guid netId) {
        return Ok(SuccessResponseBody(await offerService.GetOfferByNetId(netId, GetUserNetId())));
    }

    [HttpGet]
    [AssignActionRoute(OrdersSegments.GET_ALL_AVAILABLE_FOR_CLIENT_ECOMMERCE_OFFERS)]
    public async Task<IActionResult> GetAllAvailableOffersForClientAsync() {
        Guid userNetId = GetUserNetId();
        return Ok(SuccessResponseBody(await offerService.GetAllAvailableOffersByClientNetId(userNetId)));
    }

    [HttpPost]
    [AssignActionRoute(OrdersSegments.ADD_NEW_FROM_OFFER)]
    [EnableRateLimiting("api")]
    [Consumes("application/json")]
    [RequestSizeLimit(524288)]
    public async Task<IActionResult> GenerateOrderAndSaleFromOfferAsync(
        [FromBody] ClientShoppingCart clientShoppingCart,
        [FromQuery] int addCartItems,
        [FromHeader(Name = CheckoutIdempotencyKey.HeaderName)] string? idempotencyKey) {
        if (clientShoppingCart == null || clientShoppingCart.NetUid == Guid.Empty)
            return BadRequest(ErrorResponseBody("A valid offer selection is required.", HttpStatusCode.BadRequest));
        if (!CheckoutIdempotencyKey.TryParse(idempotencyKey, out Guid operationNetUid))
            return MissingIdempotencyKey();

        Guid userNetId = GetUserNetId();
        bool addCurrentCartItems = addCartItems == 1;
        CheckoutAttempt attempt = new(
            operationNetUid,
            CheckoutFingerprint.ForOffer(clientShoppingCart, addCurrentCartItems));

        return await CheckoutResponseAsync(() =>
            offerService.GenerateNewOrderAndSaleFromOffer(clientShoppingCart, userNetId, addCurrentCartItems, attempt));
    }

    [HttpPost]
    [AssignActionRoute(OrdersSegments.CALCULATE_TOTAL_PRICES_FOR_CHANGED_OFFER)]
    [EnableRateLimiting("checkout")]
    [Consumes("application/json")]
    [RequestSizeLimit(524288)]
    public async Task<IActionResult> CalculateTotalsForOrderAsOfferAsync([FromBody] Order order) {
        return Ok(SuccessResponseBody(await offerService.DynamicallyCalculateTotalPrices(order)));
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.UPLOAD_CLIENT_PAYMENT_CONFIRMATION)]
    [EnableRateLimiting("checkout")]
    [RequestSizeLimit(2359296)]
    public async Task<IActionResult> UploadPaymentImageAsync([FromQuery] Guid clientNetId, [FromQuery] Guid saleNetId, IFormFile image) {
        if (clientNetId == Guid.Empty || saleNetId == Guid.Empty)
            throw new ArgumentException("A valid checkout is required.");
        if (image == null || image.Length is <= 0 or > 2097152)
            throw new ArgumentException("A valid image is required.");

        byte[] buffer = new byte[image.Length];

        await using (Stream stream = image.OpenReadStream()) {
            await stream.ReadExactlyAsync(buffer);
        }

        if (!HasValidPaymentImageSignature(buffer, image.ContentType))
            throw new ArgumentException("A valid image is required.");

        string base64Image = Convert.ToBase64String(buffer);
        string safeFileName = Path.GetFileName(image.FileName);
        if (safeFileName.Length > 255) safeFileName = safeFileName[..255];

        await orderService.SendPaymentImageToCrm(saleNetId, clientNetId, new PaymentConfirmationImageModel(base64Image, safeFileName));

        return Ok(SuccessResponseBody("success"));
    }

    private IActionResult MissingIdempotencyKey() {
        return BadRequest(ErrorResponseBody(
            $"A non-empty UUID {CheckoutIdempotencyKey.HeaderName} header is required.",
            HttpStatusCode.BadRequest));
    }

    private async Task<IActionResult> CheckoutResponseAsync(Func<Task<CheckoutResult>> checkout) {
        CheckoutResult result;

        try {
            result = await checkout();
        } catch (CheckoutKeyReuseException exception) {
            return UnprocessableEntity(ErrorResponseBody(exception.Message, HttpStatusCode.UnprocessableEntity));
        } catch (CheckoutConflictException exception) {
            return Conflict(ErrorResponseBody(exception.Message, HttpStatusCode.Conflict));
        }

        if (result.IsReplay) Response.Headers[CheckoutIdempotencyKey.ReplayedHeaderName] = "true";

        return Ok(SuccessResponseBody(result.Sale));
    }

    private static bool HasValidPaymentImageSignature(byte[] bytes, string? contentType) {
        if (bytes.Length < 12) return false;

        return contentType?.ToLowerInvariant() switch {
            "image/jpeg" or "image/jpg" =>
                bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
            "image/png" =>
                bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            "image/gif" =>
                bytes.AsSpan(0, 6).SequenceEqual("GIF87a"u8) ||
                bytes.AsSpan(0, 6).SequenceEqual("GIF89a"u8),
            "image/webp" =>
                bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "image/heic" or "image/heif" =>
                bytes.AsSpan(4, 4).SequenceEqual("ftyp"u8) &&
                IsAllowedIsoImageBrand(bytes.AsSpan(8, 4)),
            _ => false
        };
    }

    private static bool HasValidCheckoutDocumentSignature(ReadOnlySpan<byte> bytes, string? contentType) {
        if (contentType?.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) == true)
            return bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8);

        return HasValidPaymentImageSignature(bytes.ToArray(), contentType);
    }

    private static bool IsAllowedIsoImageBrand(ReadOnlySpan<byte> brand) {
        return brand.SequenceEqual("heic"u8) ||
               brand.SequenceEqual("heix"u8) ||
               brand.SequenceEqual("hevc"u8) ||
               brand.SequenceEqual("hevx"u8) ||
               brand.SequenceEqual("mif1"u8) ||
               brand.SequenceEqual("msf1"u8);
    }
}
