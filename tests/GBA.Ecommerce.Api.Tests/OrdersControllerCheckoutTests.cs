using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GBA.Common.Middleware;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Ecommerce.Controllers;
using GBA.Services.Infrastructure.Checkout;
using GBA.Services.Services.Offers.Contracts;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class OrdersControllerCheckoutTests {
    private static readonly Guid UserNetId = Guid.NewGuid();
    private const string InvoiceJson = "{\"Comment\":\"Leave at the door\",\"Order\":{\"OrderItems\":[{\"NetUid\":\"8f0a7f0e-7a52-4b56-9d1f-6c8b8f4f2c10\"}]}}";

    [Fact]
    public async Task Cart_checkout_without_an_idempotency_key_is_rejected_before_any_write() {
        Mock<IOrderService> orderService = new(MockBehavior.Strict);
        OrdersController controller = NewController(orderService.Object, Mock.Of<IOfferService>());

        IActionResult result = await controller.GenerateOrderFromClientShoppingCartAsync(0, null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Invoice_checkout_without_an_idempotency_key_is_rejected_before_any_write() {
        Mock<IOrderService> orderService = new(MockBehavior.Strict);
        OrdersController controller = NewController(orderService.Object, Mock.Of<IOfferService>());

        IActionResult result = await controller.GenerateNewSaleWithInvoiceAsync(false, InvoiceJson, null!, null!, "not-a-key");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Offer_checkout_without_an_idempotency_key_is_rejected_before_any_write() {
        Mock<IOfferService> offerService = new(MockBehavior.Strict);
        OrdersController controller = NewController(Mock.Of<IOrderService>(), offerService.Object);

        IActionResult result = await controller.GenerateOrderAndSaleFromOfferAsync(
            new ClientShoppingCart { NetUid = Guid.NewGuid() },
            0,
            string.Empty);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Cart_checkout_passes_the_key_and_marks_a_replayed_response() {
        Guid key = Guid.NewGuid();
        Sale sale = CheckoutExecutorTests.NewSale();
        CheckoutAttempt? received = null;
        Mock<IOrderService> orderService = new();
        orderService
            .Setup(service => service.GenerateNewOrderAndSaleFromClientShoppingCart(UserNetId, true, It.IsAny<CheckoutAttempt>()))
            .Callback<Guid, bool, CheckoutAttempt>((_, _, attempt) => received = attempt)
            .ReturnsAsync(new CheckoutResult(sale, true));
        OrdersController controller = NewController(orderService.Object, Mock.Of<IOfferService>());

        IActionResult result = await controller.GenerateOrderFromClientShoppingCartAsync(1, key.ToString());

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(sale, Assert.IsAssignableFrom<IWebResponse>(ok.Value).Body);
        Assert.Equal("true", controller.Response.Headers[CheckoutIdempotencyKey.ReplayedHeaderName].ToString());
        Assert.Equal(key, received!.IdempotencyKey);
        Assert.Equal(CheckoutFingerprint.ForCart(true), received.RequestFingerprint);
    }

    [Fact]
    public async Task A_first_checkout_response_is_not_marked_as_replayed() {
        Mock<IOrderService> orderService = new();
        orderService
            .Setup(service => service.GenerateNewOrderAndSaleFromClientShoppingCart(UserNetId, false, It.IsAny<CheckoutAttempt>()))
            .ReturnsAsync(new CheckoutResult(CheckoutExecutorTests.NewSale(), false));
        OrdersController controller = NewController(orderService.Object, Mock.Of<IOfferService>());

        IActionResult result = await controller.GenerateOrderFromClientShoppingCartAsync(0, Guid.NewGuid().ToString());

        Assert.IsType<OkObjectResult>(result);
        Assert.False(controller.Response.Headers.ContainsKey(CheckoutIdempotencyKey.ReplayedHeaderName));
    }

    [Fact]
    public async Task A_reused_key_is_reported_as_unprocessable() {
        Mock<IOrderService> orderService = new();
        orderService
            .Setup(service => service.GenerateNewOrderAndSaleFromClientShoppingCart(UserNetId, false, It.IsAny<CheckoutAttempt>()))
            .ThrowsAsync(new CheckoutKeyReuseException());
        OrdersController controller = NewController(orderService.Object, Mock.Of<IOfferService>());

        IActionResult result = await controller.GenerateOrderFromClientShoppingCartAsync(0, Guid.NewGuid().ToString());

        ObjectResult error = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, error.StatusCode);
    }

    [Fact]
    public async Task A_checkout_still_in_progress_is_reported_as_a_conflict() {
        Mock<IOfferService> offerService = new();
        offerService
            .Setup(service => service.GenerateNewOrderAndSaleFromOffer(
                It.IsAny<ClientShoppingCart>(), UserNetId, true, It.IsAny<CheckoutAttempt>()))
            .ThrowsAsync(new CheckoutConflictException(CheckoutConflictException.InProgressMessage));
        OrdersController controller = NewController(Mock.Of<IOrderService>(), offerService.Object);

        IActionResult result = await controller.GenerateOrderAndSaleFromOfferAsync(
            new ClientShoppingCart { NetUid = Guid.NewGuid() },
            1,
            Guid.NewGuid().ToString());

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Offer_checkout_binds_the_key_to_the_canonical_selection() {
        Guid key = Guid.NewGuid();
        ClientShoppingCart offer = new() { NetUid = Guid.NewGuid() };
        offer.OrderItems.Add(new OrderItem { NetUid = Guid.NewGuid(), Qty = 2 });
        CheckoutAttempt? received = null;
        Mock<IOfferService> offerService = new();
        offerService
            .Setup(service => service.GenerateNewOrderAndSaleFromOffer(offer, UserNetId, false, It.IsAny<CheckoutAttempt>()))
            .Callback<ClientShoppingCart, Guid, bool, CheckoutAttempt>((_, _, _, attempt) => received = attempt)
            .ReturnsAsync(new CheckoutResult(CheckoutExecutorTests.NewSale(), false));
        OrdersController controller = NewController(Mock.Of<IOrderService>(), offerService.Object);

        IActionResult result = await controller.GenerateOrderAndSaleFromOfferAsync(offer, 0, key.ToString());

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(key, received!.IdempotencyKey);
        Assert.Equal(CheckoutFingerprint.ForOffer(offer, false), received.RequestFingerprint);
    }

    [Fact]
    public async Task Identical_invoice_submissions_produce_the_same_attempt_and_a_changed_one_does_not() {
        Guid key = Guid.NewGuid();
        List<CheckoutAttempt> attempts = new();
        Mock<IOrderService> orderService = new();
        orderService
            .Setup(service => service.GenerateNewSaleWithInvoice(
                It.IsAny<Sale>(), UserNetId, false, false, It.IsAny<CheckoutAttempt>()))
            .Callback<Sale, Guid, bool, bool, CheckoutAttempt>((_, _, _, _, attempt) => attempts.Add(attempt))
            .ReturnsAsync(() => new CheckoutResult(CheckoutExecutorTests.NewSale(), false));

        await NewController(orderService.Object, Mock.Of<IOfferService>(), "0")
            .GenerateNewSaleWithInvoiceAsync(false, InvoiceJson, "TTN-1", null!, key.ToString());
        await NewController(orderService.Object, Mock.Of<IOfferService>(), "0")
            .GenerateNewSaleWithInvoiceAsync(false, InvoiceJson, "TTN-1", null!, key.ToString());
        await NewController(orderService.Object, Mock.Of<IOfferService>(), "0")
            .GenerateNewSaleWithInvoiceAsync(false, InvoiceJson.Replace("door", "gate"), "TTN-1", null!, key.ToString());

        Assert.Equal(3, attempts.Count);
        Assert.All(attempts, attempt => Assert.Equal(key, attempt.IdempotencyKey));
        Assert.Equal(attempts[0].RequestFingerprint, attempts[1].RequestFingerprint);
        Assert.NotEqual(attempts[0].RequestFingerprint, attempts[2].RequestFingerprint);
    }

    [Fact]
    public async Task A_replayed_invoice_with_a_ttn_file_does_not_upload_the_file_again() {
        Guid key = Guid.NewGuid();
        Sale stored = CheckoutExecutorTests.NewSale();
        Mock<IHttpClientFactory> httpClientFactory = new(MockBehavior.Strict);
        Mock<IOrderService> orderService = new(MockBehavior.Strict);
        orderService
            .Setup(service => service.FindInvoiceCheckoutReplay(UserNetId, true, It.IsAny<CheckoutAttempt>()))
            .Returns(new CheckoutResult(stored, true));
        OrdersController controller = NewController(
            orderService.Object,
            Mock.Of<IOfferService>(),
            "1",
            httpClientFactory.Object);

        IActionResult result = await controller.GenerateNewSaleWithInvoiceAsync(
            false,
            InvoiceJson,
            "TTN-1",
            PdfFile(),
            key.ToString());

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(stored, Assert.IsAssignableFrom<IWebResponse>(ok.Value).Body);
        Assert.Equal("true", controller.Response.Headers[CheckoutIdempotencyKey.ReplayedHeaderName].ToString());
        orderService.Verify(
            service => service.GenerateNewSaleWithInvoice(
                It.IsAny<Sale>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CheckoutAttempt>()),
            Times.Never);
    }

    private static IFormFile PdfFile() {
        byte[] content = Encoding.ASCII.GetBytes("%PDF-1.4 test ttn");
        return new FormFile(new MemoryStream(content), 0, content.Length, "invoiceFile", "ttn.pdf") {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private static OrdersController NewController(
        IOrderService orderService,
        IOfferService offerService,
        string withVat = "0",
        IHttpClientFactory? httpClientFactory = null) {
        DefaultHttpContext httpContext = new();
        httpContext.Items[UserNetIdMiddleware.NetIdKey] = UserNetId;
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("type", "client") }, "test"));
        httpContext.Request.QueryString = new QueryString($"?withVat={withVat}");

        return new OrdersController(
            orderService,
            offerService,
            httpClientFactory ?? Mock.Of<IHttpClientFactory>(),
            new ResponseFactory()) {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
