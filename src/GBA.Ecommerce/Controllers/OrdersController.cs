using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using GBA.Common.Helpers;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Common.WebApi;
using GBA.Common.WebApi.RoutingConfiguration.Maps;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Services.Services.Offers.Contracts;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using NLog;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GBA.Ecommerce.Controllers;

[Authorize(Roles = IdentityRoles.ClientUa + "," + IdentityRoles.Workplace)]
[AssignControllerRoute(WebApiEnvironmnet.Current, WebApiVersion.ApiVersion1, ApplicationSegments.Orders)]
public sealed class OrdersController(
    IOrderService orderService,
    IOfferService offerService,
    IHttpClientFactory httpClientFactory,
    IStringLocalizer<OrdersController> localizer,
    IResponseFactory responseFactory)
    : WebApiControllerBase(responseFactory) {
    [HttpGet]
    [AssignActionRoute(OrdersSegments.ADD_NEW)]
    public async Task<IActionResult> GenerateOrderFromClientShoppingCartAsync([FromQuery] int withVat) {
        try {
            Guid userNetId = GetUserNetId();
            return Ok(SuccessResponseBody(await orderService.GenerateNewOrderAndSaleFromClientShoppingCart(userNetId, withVat.Equals(1))));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AssignActionRoute(OrdersSegments.ADD_NEW_AS_INVOICE)]
    public async Task<IActionResult> GenerateNewSaleWithInvoiceAsync(
        [FromQuery] bool withVat,
        string invoice,
        string number,
        IFormFile invoiceFile) {
        try {
            Guid userNetId = GetUserNetId();
            Claim? type = User.Claims.FirstOrDefault(e => e.Type.Equals("type"));
            bool isWorkplace = type != null && type.Value.Equals(IdentityRoles.Workplace);

            // TODO Localize messages

            if (string.IsNullOrEmpty(invoice)) return BadRequest(ErrorResponseBody("ShoppingCart entity can not be empty", HttpStatusCode.BadRequest));

            invoice = invoice.Replace(" 02:00\"", "+02:00\"").Replace(" 03:00\"", "+03:00\"");

            Sale parsedSale = JsonSerializer.Deserialize<Sale>(invoice);

            if (parsedSale == null) return BadRequest(ErrorResponseBody("Invalid sale data", HttpStatusCode.BadRequest));

            parsedSale.CustomersOwnTtn = new CustomersOwnTtn {
                Number = number
            };

            if (invoiceFile == null)
                return Ok(SuccessResponseBody(await orderService.GenerateNewSaleWithInvoice(parsedSale, userNetId, isWorkplace)));

            try {
                string crmUrl;

                if (System.IO.File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                    dynamic data = JsonConvert.DeserializeObject<dynamic>(await System.IO.File.ReadAllTextAsync(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));
#if DEBUG
                    crmUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/save/ttn";
#else
                    crmUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/save/ttn";
#endif
                } else {
                    crmUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/save/ttn";
                }

                using HttpClient httpClient = httpClientFactory.CreateClient();

                MultipartFormDataContent formData = new();

                using StreamContent streamContent = new(invoiceFile.OpenReadStream());
                formData.Add(streamContent, "file", invoiceFile.FileName);

                HttpResponseMessage response = await httpClient.PostAsync(crmUrl, formData);

                response.EnsureSuccessStatusCode();

                parsedSale.CustomersOwnTtn.TtnPDFPath = await response.Content.ReadAsStringAsync();
            } catch (Exception) {
                // ignored
            }

            return Ok(SuccessResponseBody(await orderService.GenerateNewSaleWithInvoice(parsedSale, userNetId, isWorkplace)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(localizer[exc.Message], HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.ADD_NEW_AS_QUICK_INVOICE)]
    public async Task<IActionResult> GenerateNewSaleWithInvoiceAsync(
        [FromBody] Sale sale,
        [FromQuery] Guid clientNetId,
        [FromQuery] string card,
        [FromQuery] int fullPayment) {
        try {
            return Ok(SuccessResponseBody(
                await orderService.GenerateNewRetailSale(
                    sale,
                    clientNetId,
                    fullPayment.Equals(1))));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(localizer[exc.Message], HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.CALCULATE_TOTAL_PRICES)]
    public async Task<IActionResult> CalculateTotalsForOrderAsync([FromBody] Order order) {
        try {
            return Ok(SuccessResponseBody(await orderService.DynamicallyCalculateTotalPrices(order)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.GET_ECOMMERCE_OFFER_BY_NET_ID)]
    public async Task<IActionResult> GetOfferByNetIdAsync([FromQuery] Guid netId) {
        try {
            return Ok(SuccessResponseBody(await offerService.GetOfferByNetId(netId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(localizer[exc.Message], HttpStatusCode.BadRequest));
        }
    }

    [HttpGet]
    [AssignActionRoute(OrdersSegments.GET_ALL_AVAILABLE_FOR_CLIENT_ECOMMERCE_OFFERS)]
    public async Task<IActionResult> GetAllAvailableOffersForClientAsync() {
        try {
            Guid userNetId = GetUserNetId();
            return Ok(SuccessResponseBody(await offerService.GetAllAvailableOffersByClientNetId(userNetId)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AssignActionRoute(OrdersSegments.ADD_NEW_FROM_OFFER)]
    public async Task<IActionResult> GenerateOrderAndSaleFromOfferAsync([FromBody] ClientShoppingCart clientShoppingCart, [FromQuery] int addCartItems) {
        try {
            Guid userNetId = GetUserNetId();
            return Ok(
                SuccessResponseBody(
                    await offerService.GenerateNewOrderAndSaleFromOffer(clientShoppingCart, userNetId, addCartItems == 1)
                )
            );
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(localizer[exc.Message], HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.CALCULATE_TOTAL_PRICES_FOR_CHANGED_OFFER)]
    public async Task<IActionResult> CalculateTotalsForOrderAsOfferAsync([FromBody] Order order) {
        try {
            return Ok(SuccessResponseBody(await offerService.DynamicallyCalculateTotalPrices(order)));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [AssignActionRoute(OrdersSegments.UPLOAD_CLIENT_PAYMENT_CONFIRMATION)]
    public async Task<IActionResult> UploadPaymentImageAsync([FromQuery] Guid clientNetId, [FromQuery] Guid saleNetId, IFormFile image) {
        try {
            if (image == null) throw new Exception("Image cannot be null");

            byte[] buffer = new byte[image.Length];

            await using (Stream stream = image.OpenReadStream()) {
                stream.ReadExactly(buffer);
            }

            string base64Image = Convert.ToBase64String(buffer);

            await orderService.SendPaymentImageToCrm(saleNetId, clientNetId, new PaymentConfirmationImageModel(base64Image, image.FileName));

            return Ok(SuccessResponseBody("success"));
        } catch (Exception exc) {
            Logger.Log(LogLevel.Error, exc.Message);
            return BadRequest(ErrorResponseBody(exc.Message, HttpStatusCode.BadRequest));
        }
    }
}
