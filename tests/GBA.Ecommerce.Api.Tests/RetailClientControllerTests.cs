using System.Net;
using System.Text.Json;
using GBA.Common.Exceptions.GlobalHandler;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Ecommerce.Controllers.Clients;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class RetailClientControllerTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Empty_net_id_is_handled_as_a_bad_request_in_retail_client_reads(
        bool checkOrderItems) {
        Mock<IClientService> clientService = new(MockBehavior.Strict);
        RetailClientController controller = CreateController(clientService.Object);

        ArgumentException exception = checkOrderItems
            ? await Assert.ThrowsAsync<ArgumentException>(() =>
                controller.GetRetailClientByNetIdCheckOrderItems(Guid.Empty))
            : await Assert.ThrowsAsync<ArgumentException>(() =>
                controller.GetRetailClientByNetId(Guid.Empty));

        Assert.Equal("netId", exception.ParamName);

        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        GlobalExceptionHandler handler = new();
        await handler.HandleException(context, new ExceptionHandlerFeature { Error = exception });

        context.Response.Body.Position = 0;
        using JsonDocument response = await JsonDocument.ParseAsync(context.Response.Body);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal(
            "The request is invalid.",
            response.RootElement.GetProperty(nameof(ErrorResponse.Message)).GetString());
        clientService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Valid_net_id_keeps_both_retail_client_reads_successful() {
        Guid netId = Guid.NewGuid();
        RetailClient retailClient = new() { NetUid = netId };
        Mock<IClientService> clientService = new(MockBehavior.Strict);
        clientService
            .Setup(service => service.GetRetailClientByNetId(netId))
            .ReturnsAsync(retailClient);
        clientService
            .Setup(service => service.GetRetailClientByNetIdCheckOrderItems(netId))
            .ReturnsAsync((retailClient, string.Empty));
        RetailClientController controller = CreateController(clientService.Object);

        OkObjectResult getResult = Assert.IsType<OkObjectResult>(
            await controller.GetRetailClientByNetId(netId));
        OkObjectResult checkResult = Assert.IsType<OkObjectResult>(
            await controller.GetRetailClientByNetIdCheckOrderItems(netId));

        IWebResponse getResponse = Assert.IsAssignableFrom<IWebResponse>(getResult.Value);
        IWebResponse checkResponse = Assert.IsAssignableFrom<IWebResponse>(checkResult.Value);
        Assert.Same(retailClient, getResponse.Body);
        Assert.Equal((retailClient, string.Empty), checkResponse.Body);
        clientService.Verify(service => service.GetRetailClientByNetId(netId), Times.Once);
        clientService.Verify(
            service => service.GetRetailClientByNetIdCheckOrderItems(netId),
            Times.Once);
        clientService.VerifyNoOtherCalls();
    }

    private static RetailClientController CreateController(IClientService clientService) {
        return new RetailClientController(clientService, new ResponseFactory());
    }
}
