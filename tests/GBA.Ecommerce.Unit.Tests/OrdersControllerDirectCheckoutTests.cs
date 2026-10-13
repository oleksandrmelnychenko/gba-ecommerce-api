using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.Middleware;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Ecommerce.Controllers;
using GBA.Services.Infrastructure.SalesMutations;
using GBA.Services.Services.Offers.Contracts;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class OrdersControllerDirectCheckoutTests {
    private static readonly Guid ClientNetUid = Guid.Parse("8d3dc1aa-2ced-44ee-88a1-65149b44efb2");
    private static readonly Guid OperationNetUid = Guid.Parse("ebca23e6-fc7b-4f3a-b08f-5156e718cc19");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-uuid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task BothRoutesRequireHeaderEvenWhenOfferHasNetUid(string? key) {
        Fixture fixture = new();

        IActionResult cart = await fixture.Controller.GenerateOrderFromClientShoppingCartAsync(0, key);
        IActionResult offer = await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(fixture.Offer, 0, key);

        foreach (IActionResult result in new[] { cart, offer }) {
            IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(Assert.IsType<BadRequestObjectResult>(result).Value);
            Assert.Contains("Idempotency-Key header", response.Message);
            Assert.DoesNotContain("Sale.NetUid", response.Message);
            Assert.Null(response.Body);
        }
        fixture.VerifyNoServiceCalls();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public async Task BothRoutesRejectUnknownFlags(int flag) {
        Fixture fixture = new();

        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.GenerateOrderFromClientShoppingCartAsync(flag, OperationNetUid.ToString()));
        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(fixture.Offer, flag, OperationNetUid.ToString()));

        fixture.VerifyNoServiceCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ModelBindingFailureDoesNotBecomeZeroFlag(bool offerRoute) {
        Fixture fixture = new();
        fixture.Controller.ModelState.AddModelError(offerRoute ? "addCartItems" : "withVat", "The value 'true' is not valid.");

        IActionResult result = offerRoute
            ? await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(fixture.Offer, 0, OperationNetUid.ToString())
            : await fixture.Controller.GenerateOrderFromClientShoppingCartAsync(0, OperationNetUid.ToString());

        IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(Assert.IsType<BadRequestObjectResult>(result).Value);
        Assert.Null(response.Body);
        fixture.VerifyNoServiceCalls();
    }

    [Theory]
    [InlineData("type")]
    [InlineData(ClaimTypes.Role)]
    public async Task BothRoutesRejectWorkplaceBeforeServiceCall(string claimType) {
        Fixture fixture = new(new Claim(claimType, IdentityRoles.Workplace));

        IActionResult cart = await fixture.Controller.GenerateOrderFromClientShoppingCartAsync(1, OperationNetUid.ToString());
        IActionResult offer = await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(fixture.Offer, 1, OperationNetUid.ToString());

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(cart).StatusCode);
        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(offer).StatusCode);
        fixture.VerifyNoServiceCalls();
    }

    [Fact]
    public async Task BothRoutesRejectMissingAuthenticatedIdentity() {
        Fixture fixture = new();
        fixture.Controller.HttpContext.Items.Remove(UserNetIdMiddleware.NetIdKey);

        Assert.IsType<UnauthorizedResult>(await fixture.Controller.GenerateOrderFromClientShoppingCartAsync(0, OperationNetUid.ToString()));
        Assert.IsType<UnauthorizedResult>(await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(fixture.Offer, 0, OperationNetUid.ToString()));

        fixture.VerifyNoServiceCalls();
    }

    [Fact]
    public async Task OfferRejectsMissingBodyOrOfferIdentity() {
        Fixture fixture = new();

        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(null!, 0, OperationNetUid.ToString()));
        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(new ClientShoppingCart(), 0, OperationNetUid.ToString()));

        fixture.VerifyNoServiceCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task BothRoutesForwardHeaderPrincipalAndFlagAndPreserveResponseEnvelope(int flag) {
        Fixture fixture = new();
        Sale sale = new() { NetUid = Guid.NewGuid() };
        fixture.Orders.Setup(service => service.GenerateNewOrderAndSaleFromClientShoppingCart(ClientNetUid, flag == 1, OperationNetUid)).ReturnsAsync(sale);
        fixture.Offers.Setup(service => service.GenerateNewOrderAndSaleFromOffer(fixture.Offer, ClientNetUid, flag == 1, OperationNetUid)).ReturnsAsync(sale);

        IActionResult cart = await fixture.Controller.GenerateOrderFromClientShoppingCartAsync(flag, $" {OperationNetUid:D} ");
        IActionResult offer = await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(fixture.Offer, flag, OperationNetUid.ToString());

        foreach (IActionResult result in new[] { cart, offer }) {
            IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.Same(sale, response.Body);
            Assert.False(JsonSerializer.SerializeToElement(response.Body).TryGetProperty("CheckoutRejected", out _));
        }
        fixture.Orders.VerifyAll();
        fixture.Offers.VerifyAll();
    }

    [Fact]
    public async Task BothRoutesReturn409WhenOperationBindingConflicts() {
        Fixture fixture = new();
        fixture.Orders.Setup(service => service.GenerateNewOrderAndSaleFromClientShoppingCart(ClientNetUid, false, OperationNetUid))
            .ThrowsAsync(new SalesCreationIdempotencyConflictException("cart key conflict"));
        fixture.Offers.Setup(service => service.GenerateNewOrderAndSaleFromOffer(fixture.Offer, ClientNetUid, false, OperationNetUid))
            .ThrowsAsync(new SalesCreationIdempotencyConflictException("offer key conflict"));

        IActionResult cart = await fixture.Controller.GenerateOrderFromClientShoppingCartAsync(0, OperationNetUid.ToString());
        IActionResult offer = await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(fixture.Offer, 0, OperationNetUid.ToString());
        foreach (IActionResult result in new[] { cart, offer }) {
            IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(Assert.IsType<ConflictObjectResult>(result).Value);
            Assert.Null(response.Body);
        }

        fixture.Orders.VerifyAll();
        fixture.Offers.VerifyAll();
    }

    [Fact]
    public async Task GenericValidationErrorsRemain400WithoutPermissionToResetKey() {
        Fixture fixture = new();
        fixture.Orders.Setup(service => service.GenerateNewOrderAndSaleFromClientShoppingCart(ClientNetUid, false, OperationNetUid))
            .ThrowsAsync(new ArgumentException("The shopping cart is unavailable."));
        fixture.Offers.Setup(service => service.GenerateNewOrderAndSaleFromOffer(fixture.Offer, ClientNetUid, false, OperationNetUid))
            .ThrowsAsync(new ArgumentException("The offer is unavailable."));

        IActionResult cart = await fixture.Controller.GenerateOrderFromClientShoppingCartAsync(0, OperationNetUid.ToString());
        IActionResult offer = await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(fixture.Offer, 0, OperationNetUid.ToString());
        foreach (IActionResult result in new[] { cart, offer }) {
            IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(Assert.IsType<BadRequestObjectResult>(result).Value);
            Assert.Null(response.Body);
        }

        fixture.Orders.VerifyAll();
        fixture.Offers.VerifyAll();
    }

    [Fact]
    public async Task DefiniteRolledBackValidationErrorsReturnMatchingKeyMarkerOnBothRoutes() {
        Fixture fixture = new();
        fixture.Orders.Setup(service => service.GenerateNewOrderAndSaleFromClientShoppingCart(ClientNetUid, false, OperationNetUid))
            .ThrowsAsync(new LegacyCheckoutRejectedException("The shopping cart is unavailable."));
        fixture.Offers.Setup(service => service.GenerateNewOrderAndSaleFromOffer(fixture.Offer, ClientNetUid, false, OperationNetUid))
            .ThrowsAsync(new LegacyCheckoutRejectedException("The offer is unavailable."));

        IActionResult cart = await fixture.Controller.GenerateOrderFromClientShoppingCartAsync(0, OperationNetUid.ToString());
        IActionResult offer = await fixture.Controller.GenerateOrderAndSaleFromOfferAsync(fixture.Offer, 0, OperationNetUid.ToString());
        foreach (IActionResult result in new[] { cart, offer }) {
            IWebResponse response = Assert.IsAssignableFrom<IWebResponse>(Assert.IsType<BadRequestObjectResult>(result).Value);
            JsonElement body = JsonSerializer.SerializeToElement(response.Body);
            Assert.True(body.GetProperty("CheckoutRejected").GetBoolean());
            Assert.Equal(OperationNetUid, body.GetProperty("OperationNetUid").GetGuid());
            Assert.NotEqual(fixture.Offer.NetUid, body.GetProperty("OperationNetUid").GetGuid());
        }
        fixture.Orders.VerifyAll();
        fixture.Offers.VerifyAll();
    }

    [Fact]
    public async Task NetworkFailureDoesNotBecomeDefiniteRejection() {
        Fixture fixture = new();
        fixture.Orders.Setup(service => service.GenerateNewOrderAndSaleFromClientShoppingCart(ClientNetUid, false, OperationNetUid))
            .ThrowsAsync(new HttpRequestException("Response was lost."));
        fixture.Offers.Setup(service => service.GenerateNewOrderAndSaleFromOffer(fixture.Offer, ClientNetUid, false, OperationNetUid))
            .ThrowsAsync(new HttpRequestException("Response was lost."));

        await Assert.ThrowsAsync<HttpRequestException>(() => fixture.Controller.GenerateOrderFromClientShoppingCartAsync(0, OperationNetUid.ToString()));
        await Assert.ThrowsAsync<HttpRequestException>(() => fixture.Controller.GenerateOrderAndSaleFromOfferAsync(fixture.Offer, 0, OperationNetUid.ToString()));

        fixture.Orders.VerifyAll();
        fixture.Offers.VerifyAll();
    }

    private sealed class Fixture {
        internal Mock<IOrderService> Orders { get; } = new(MockBehavior.Strict);
        internal Mock<IOfferService> Offers { get; } = new(MockBehavior.Strict);
        internal ClientShoppingCart Offer { get; } = new() { NetUid = Guid.NewGuid() };
        internal OrdersController Controller { get; }

        internal Fixture(params Claim[] claims) {
            Controller = new OrdersController(
                Orders.Object,
                Offers.Object,
                Mock.Of<IHttpClientFactory>(),
                new SalesMutationOutboxOptions { AllowedInternalBaseUri = "https://crm.internal.example/" },
                new SalesMutationInternalAuthOptions {
                    ApiKey = "test-only-ecommerce-internal-api-key-0123456789abcdef"
                },
                new ResponseFactory());
            DefaultHttpContext context = new() {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            };
            context.Items[UserNetIdMiddleware.NetIdKey] = ClientNetUid;
            Controller.ControllerContext = new ControllerContext { HttpContext = context };
        }

        internal void VerifyNoServiceCalls() {
            Orders.VerifyNoOtherCalls();
            Offers.VerifyNoOtherCalls();
        }
    }
}
