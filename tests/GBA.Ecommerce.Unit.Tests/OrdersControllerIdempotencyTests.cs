using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GBA.Common.Middleware;
using GBA.Common.ResponseBuilder;
using GBA.Domain.Entities.Sales;
using GBA.Ecommerce.Controllers;
using GBA.Ecommerce.Infrastructure;
using GBA.Services.Infrastructure.SalesMutations;
using GBA.Services.Services.Offers.Contracts;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class OrdersControllerIdempotencyTests {
    [Fact]
    public async Task RetailCheckoutWithoutHeaderOrSaleNetUidReturns400BeforeServiceCall() {
        Mock<IOrderService> orderService = new();
        OrdersController controller = CreateController(orderService);

        IActionResult result = await controller.GenerateNewSaleWithInvoiceAsync(
            new Sale(),
            null,
            Guid.NewGuid(),
            string.Empty,
            0);

        Assert.IsType<BadRequestObjectResult>(result);
        orderService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RetailCheckoutUsesSaleNetUidWhenHeaderIsMissing() {
        Guid saleNetUid = Guid.NewGuid();
        Guid clientNetUid = Guid.NewGuid();
        Mock<IOrderService> orderService = new();
        orderService
            .Setup(service => service.GenerateNewRetailSale(
                It.IsAny<Sale>(),
                clientNetUid,
                false,
                saleNetUid))
            .ReturnsAsync("payment-link");
        OrdersController controller = CreateController(orderService);

        IActionResult result = await controller.GenerateNewSaleWithInvoiceAsync(
            new Sale { NetUid = saleNetUid },
            null,
            clientNetUid,
            string.Empty,
            0);

        Assert.IsType<OkObjectResult>(result);
        orderService.VerifyAll();
    }

    [Fact]
    public async Task RetailCheckoutMapsBindingMismatchTo409AndHeaderWins() {
        Guid headerKey = Guid.NewGuid();
        Guid bodyKey = Guid.NewGuid();
        Guid clientNetUid = Guid.NewGuid();
        Mock<IOrderService> orderService = new();
        orderService
            .Setup(service => service.GenerateNewRetailSale(
                It.IsAny<Sale>(),
                clientNetUid,
                true,
                headerKey))
            .ThrowsAsync(new SalesCreationIdempotencyConflictException("key conflict"));
        OrdersController controller = CreateController(orderService);

        IActionResult result = await controller.GenerateNewSaleWithInvoiceAsync(
            new Sale { NetUid = bodyKey },
            headerKey.ToString("D"),
            clientNetUid,
            string.Empty,
            1);

        Assert.IsType<ConflictObjectResult>(result);
        orderService.VerifyAll();
    }

    [Fact]
    public async Task InvoiceUploadFailureReturns502BeforeServiceCall() {
        Guid operationNetUid = Guid.NewGuid();
        Mock<IOrderService> orderService = new();
        OrdersController controller = CreateController(
            orderService,
            new StaticResponseHandler(HttpStatusCode.InternalServerError));

        IActionResult result = await controller.GenerateNewSaleWithInvoiceAsync(
            false,
            operationNetUid.ToString("D"),
            JsonSerializer.Serialize(new Sale { NetUid = operationNetUid }),
            "TTN-1",
            CreateFile("ttn.pdf", "upload-failed"));

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, objectResult.StatusCode);
        orderService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvoiceUploadConflictReturns409BeforeServiceCall() {
        Guid operationNetUid = Guid.NewGuid();
        Mock<IOrderService> orderService = new();
        OrdersController controller = CreateController(
            orderService,
            new StaticResponseHandler(HttpStatusCode.Conflict));

        IActionResult result = await controller.GenerateNewSaleWithInvoiceAsync(
            false,
            operationNetUid.ToString("D"),
            JsonSerializer.Serialize(new Sale { NetUid = operationNetUid }),
            "TTN-1",
            CreateFile("ttn.pdf", "conflicting-upload"));

        Assert.IsType<ConflictObjectResult>(result);
        orderService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SuccessfulInvoiceUploadUsesSameOperationAndUploadedBytesHashForSale() {
        Guid operationNetUid = Guid.NewGuid();
        byte[] fileBytes = Encoding.UTF8.GetBytes("stable-ttn-content");
        byte[] expectedHash = SHA256.HashData(fileBytes);
        Mock<IOrderService> orderService = new();
        orderService
            .Setup(service => service.GenerateNewSaleWithInvoice(
                It.Is<Sale>(sale =>
                    sale.CustomersOwnTtn != null &&
                    sale.CustomersOwnTtn.TtnPDFPath == "https://crm.example/Data/Temp/ttn.pdf"),
                It.IsAny<Guid>(),
                false,
                operationNetUid,
                It.Is<byte[]>(hash => hash.SequenceEqual(expectedHash))))
            .ReturnsAsync(new Sale());
        OrdersController controller = CreateController(
            orderService,
            new StaticResponseHandler(
                HttpStatusCode.OK,
                "https://crm.example/Data/Temp/ttn.pdf"));

        IActionResult result = await controller.GenerateNewSaleWithInvoiceAsync(
            false,
            operationNetUid.ToString("D"),
            JsonSerializer.Serialize(new Sale { NetUid = operationNetUid }),
            "TTN-1",
            CreateFile("ttn.pdf", fileBytes));

        Assert.IsType<OkObjectResult>(result);
        orderService.VerifyAll();
    }

    [Fact]
    public async Task SaleCreationFailureAbortsStagedFileWithoutFinalize() {
        Guid operationNetUid = Guid.NewGuid();
        Mock<IOrderService> orderService = new();
        orderService
            .Setup(service => service.GenerateNewSaleWithInvoice(
                It.IsAny<Sale>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                operationNetUid,
                It.IsAny<byte[]>()))
            .ThrowsAsync(new InvalidOperationException("sale transaction failed"));
        PhaseResponseHandler handler = new((phase, _) =>
            phase == "abort"
                ? CreateResponse(HttpStatusCode.NoContent)
                : CreateResponse(
                    HttpStatusCode.OK,
                    "https://crm.example/Data/Temp/ttn.pdf"));
        OrdersController controller = CreateController(orderService, handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.GenerateNewSaleWithInvoiceAsync(
                false,
                operationNetUid.ToString("D"),
                JsonSerializer.Serialize(new Sale { NetUid = operationNetUid }),
                "TTN-1",
                CreateFile("ttn.pdf", "create-failure")));

        Assert.Equal(["stage", "abort"], handler.Phases);
        orderService.VerifyAll();
    }

    [Fact]
    public async Task IdempotencyConflictDoesNotAbortAFileOwnedByTheCommittedOperation() {
        Guid operationNetUid = Guid.NewGuid();
        Mock<IOrderService> orderService = new();
        orderService
            .Setup(service => service.GenerateNewSaleWithInvoice(
                It.IsAny<Sale>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                operationNetUid,
                It.IsAny<byte[]>()))
            .ThrowsAsync(new SalesCreationIdempotencyConflictException(
                "The operation key is already bound to another sale payload."));
        PhaseResponseHandler handler = new((_, _) =>
            CreateResponse(
                HttpStatusCode.OK,
                "https://crm.example/Data/Temp/ttn.pdf"));
        OrdersController controller = CreateController(orderService, handler);

        IActionResult result = await controller.GenerateNewSaleWithInvoiceAsync(
            false,
            operationNetUid.ToString("D"),
            JsonSerializer.Serialize(new Sale { NetUid = operationNetUid }),
            "TTN-1",
            CreateFile("ttn.pdf", "conflicting-sale-payload"));

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(["stage"], handler.Phases);
        Assert.DoesNotContain("abort", handler.Phases);
        orderService.VerifyAll();
    }

    [Fact]
    public async Task FinalizeFailureAfterSaleCommitIsRetriableAndDoesNotAbort() {
        Guid operationNetUid = Guid.NewGuid();
        byte[] content = Encoding.UTF8.GetBytes("finalize-retry");
        byte[] expectedHash = SHA256.HashData(content);
        Mock<IOrderService> orderService = new();
        orderService
            .Setup(service => service.GenerateNewSaleWithInvoice(
                It.Is<Sale>(sale =>
                    sale.CustomersOwnTtn != null &&
                    sale.CustomersOwnTtn.TtnPDFPath ==
                        "https://crm.example/Data/Temp/ttn.pdf"),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                operationNetUid,
                It.Is<byte[]>(hash => hash.SequenceEqual(expectedHash))))
            .ReturnsAsync(new Sale { NetUid = operationNetUid });
        PhaseResponseHandler handler = new((phase, requestIndex) =>
            phase == "finalize" && requestIndex == 1
                ? CreateResponse(HttpStatusCode.InternalServerError)
                : CreateResponse(
                    HttpStatusCode.OK,
                    "https://crm.example/Data/Temp/ttn.pdf"));
        OrdersController controller = CreateController(orderService, handler);

        IActionResult first = await controller.GenerateNewSaleWithInvoiceAsync(
            false,
            operationNetUid.ToString("D"),
            JsonSerializer.Serialize(new Sale { NetUid = operationNetUid }),
            "TTN-1",
            CreateFile("ttn.pdf", content));
        IActionResult retry = await controller.GenerateNewSaleWithInvoiceAsync(
            false,
            operationNetUid.ToString("D"),
            JsonSerializer.Serialize(new Sale { NetUid = operationNetUid }),
            "TTN-1",
            CreateFile("ttn.pdf", content));

        ObjectResult failure = Assert.IsType<ObjectResult>(first);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, failure.StatusCode);
        Assert.IsType<OkObjectResult>(retry);
        Assert.Equal(
            ["stage", "finalize", "stage", "finalize"],
            handler.Phases);
        Assert.DoesNotContain("abort", handler.Phases);
        orderService.VerifyAll();
        orderService.Verify(
            service => service.GenerateNewSaleWithInvoice(
                It.IsAny<Sale>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                operationNetUid,
                It.IsAny<byte[]>()),
            Times.Exactly(2));
    }

    [Fact]
    public void InvoiceEndpointUsesTtnSpecificDiskBufferingLimits() {
        MethodInfo method = typeof(OrdersController)
            .GetMethods()
            .Single(candidate =>
                candidate.Name == nameof(OrdersController.GenerateNewSaleWithInvoiceAsync) &&
                candidate.GetParameters().FirstOrDefault()?.ParameterType == typeof(bool));
        CustomAttributeData requestLimit = method.CustomAttributes.Single(attribute =>
            attribute.AttributeType == typeof(RequestSizeLimitAttribute));
        RequestFormLimitsAttribute formLimits = method
            .GetCustomAttribute<RequestFormLimitsAttribute>()!;

        Assert.Equal(
            TtnUploadClient.MaxRequestSizeBytes,
            (long)requestLimit.ConstructorArguments.Single().Value!);
        Assert.Equal(
            TtnUploadClient.MaxRequestSizeBytes,
            formLimits.MultipartBodyLengthLimit);
        Assert.Equal(
            TtnUploadClient.MemoryBufferThresholdBytes,
            formLimits.MemoryBufferThreshold);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pl")]
    public async Task InvoiceUploadRejectsMissingOrUnsupportedRouteCultureBeforeSideEffects(
        string? routeCulture) {
        Guid operationNetUid = Guid.NewGuid();
        Mock<IOrderService> orderService = new();
        OrdersController controller = CreateController(
            orderService,
            new StaticResponseHandler(HttpStatusCode.OK),
            routeCulture);

        IActionResult result = await controller.GenerateNewSaleWithInvoiceAsync(
            false,
            operationNetUid.ToString("D"),
            JsonSerializer.Serialize(new Sale { NetUid = operationNetUid }),
            "TTN-1",
            CreateFile("ttn.pdf", "content"));

        Assert.IsType<BadRequestObjectResult>(result);
        orderService.VerifyNoOtherCalls();
    }

    private static OrdersController CreateController(
        Mock<IOrderService> orderService,
        HttpMessageHandler? handler = null,
        string? routeCulture = "uk") {
        OrdersController controller = new(
            orderService.Object,
            Mock.Of<IOfferService>(),
            handler == null
                ? Mock.Of<IHttpClientFactory>()
                : new StubHttpClientFactory(handler),
            CreateOutboxOptions(),
            new SalesMutationInternalAuthOptions {
                ApiKey = "test-only-ecommerce-internal-api-key-0123456789abcdef"
            },
            new ResponseFactory());
        DefaultHttpContext context = new();
        context.Items[UserNetIdMiddleware.NetIdKey] = Guid.NewGuid();
        controller.ControllerContext = new ControllerContext {
            HttpContext = context,
            RouteData = new RouteData()
        };
        if (routeCulture != null)
            controller.RouteData.Values["culture"] = routeCulture;
        return controller;
    }

    private static SalesMutationOutboxOptions CreateOutboxOptions() => new() {
        AllowedInternalBaseUri = "https://crm.internal.example/",
        RequestTimeout = TimeSpan.FromSeconds(5)
    };

    private static IFormFile CreateFile(string fileName, string content) =>
        CreateFile(fileName, Encoding.UTF8.GetBytes(content));

    private static IFormFile CreateFile(string fileName, byte[] content) =>
        new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName);

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false);
    }

    private sealed class StaticResponseHandler(
        HttpStatusCode statusCode,
        string responseBody = "") : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode) {
                Content = new StringContent(responseBody)
            });
    }

    private sealed class PhaseResponseHandler(
        Func<string, int, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public List<string> Phases { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            const string prefix = "?phase=";
            string query = request.RequestUri?.Query ?? string.Empty;
            Assert.StartsWith(prefix, query, StringComparison.Ordinal);
            string phase = Uri.UnescapeDataString(query[prefix.Length..]);
            int requestIndex = Phases.Count;
            Phases.Add(phase);
            return Task.FromResult(responseFactory(phase, requestIndex));
        }
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string responseBody = "") =>
        new(statusCode) {
            Content = new StringContent(responseBody)
        };
}
