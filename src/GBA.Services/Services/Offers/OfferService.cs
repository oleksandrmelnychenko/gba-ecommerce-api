using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Common.Configuration;
using GBA.Common.Helpers;
using GBA.Common.Models;
using GBA.Common.ResourceNames.ECommerce;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.Infrastructure;
using GBA.Services.Infrastructure.Checkout;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.Offers.Contracts;
using Microsoft.Extensions.Http;

namespace GBA.Services.Services.Offers;

public sealed class OfferService : IOfferService {
    internal const string OfferCheckoutOperation = "ecommerce-offer-checkout-v1";

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;

    private readonly IDbConnectionFactory _connectionFactory;

    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IClientResourceAccessService _clientResourceAccessService;
    private readonly ICheckoutExecutor _checkoutExecutor;

    public OfferService(
        IClientRepositoriesFactory clientRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IHttpClientFactory httpClientFactory,
        IClientResourceAccessService clientResourceAccessService,
        ICheckoutExecutor checkoutExecutor) {
        _clientRepositoriesFactory = clientRepositoriesFactory;

        _saleRepositoriesFactory = saleRepositoriesFactory;

        _productRepositoriesFactory = productRepositoriesFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        _connectionFactory = connectionFactory;
        _httpClientFactory = httpClientFactory;
        _clientResourceAccessService = clientResourceAccessService;
        _checkoutExecutor = checkoutExecutor;
    }

    public Task<ClientShoppingCart> GetOfferByNetId(Guid netId, Guid actorNetId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ClientShoppingCart offer = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection).GetByNetId(netId);

        if (offer == null) throw new Exception(OfferResourceNames.OFFER_NOT_EXISTS);
        if (!_clientResourceAccessService.CanAccessClientOrAgreement(actorNetId, offer.ClientAgreement.NetUid))
            throw new UnauthorizedAccessException();

        if (offer.ValidUntil < DateTime.Now.Date) throw new Exception(OfferResourceNames.OFFER_EXPIRED);

        if (offer.IsOfferProcessed) throw new Exception(OfferResourceNames.OFFER_PROCESSED);

        StampFirstView(connection, offer);

        foreach (OrderItem orderItem in offer.OrderItems) {
            orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
            orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

            orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

            orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
            orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
        }

        offer.TotalAmount = decimal.Round(offer.OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
        offer.TotalLocalAmount = decimal.Round(offer.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);

        return Task.FromResult(offer);
    }

    public Task<ClientShoppingCart> GetOfferForPublicLink(Guid netId) {
        // Manager-shared offer link: possession of the unguessable ClientShoppingCart.NetUID GUID
        // is the authorization (same trust model as the anonymous payment-upload links), so there
        // is no actor ownership check. Expiry/processed guards still apply — a dead link stays dead.
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ClientShoppingCart offer = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection).GetByNetId(netId);

        if (offer == null) throw new Exception(OfferResourceNames.OFFER_NOT_EXISTS);

        if (offer.ValidUntil < DateTime.Now.Date) throw new Exception(OfferResourceNames.OFFER_EXPIRED);

        if (offer.IsOfferProcessed) throw new Exception(OfferResourceNames.OFFER_PROCESSED);

        StampFirstView(connection, offer);

        foreach (OrderItem orderItem in offer.OrderItems) {
            orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
            orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

            orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

            orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
            orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
        }

        offer.TotalAmount = decimal.Round(offer.OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
        offer.TotalLocalAmount = decimal.Round(offer.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);

        return Task.FromResult(offer);
    }

    // First successful read of a live offer = the client saw it; later reads keep the original stamp.
    private void StampFirstView(IDbConnection connection, ClientShoppingCart offer) {
        if (offer.ViewedAt.HasValue) return;

        _clientRepositoriesFactory.NewClientShoppingCartRepository(connection).SetViewedByNetId(offer.NetUid);
        offer.ViewedAt = DateTime.UtcNow;
    }

    public Task<List<ClientShoppingCart>> GetAllAvailableOffersByClientNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientShoppingCartRepository shoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

        List<ClientShoppingCart> offers = shoppingCartRepository.GetAllAvailableOffersByClientNetId(netId);

        for (int i = 0; i < offers.Count; i++) {
            offers[i] = shoppingCartRepository.GetByNetId(offers[i].NetUid);

            foreach (OrderItem orderItem in offers[i].OrderItems) {
                orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
            }

            offers[i].TotalAmount = decimal.Round(offers[i].OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
            offers[i].TotalLocalAmount = decimal.Round(offers[i].OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);
        }

        return Task.FromResult(offers);
    }

    public Task<CheckoutResult> GenerateNewOrderAndSaleFromOffer(
        ClientShoppingCart clientShoppingCart,
        Guid clientNetId,
        bool addCurrentCartItems,
        CheckoutAttempt attempt) {
        if (clientShoppingCart == null || clientShoppingCart.NetUid == Guid.Empty)
            throw new ArgumentException("A valid offer selection is required.");

        CheckoutRequest request = new(OfferCheckoutOperation, attempt, clientNetId, clientNetId, addCurrentCartItems);

        return Task.FromResult(_checkoutExecutor.Execute(request, connection => {
            IClientShoppingCartRepository shoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

            ClientShoppingCart offer = shoppingCartRepository.GetByNetId(clientShoppingCart.NetUid);

            if (offer == null)
                throw new Exception(OfferResourceNames.OFFER_NOT_EXISTS);
            if (!offer.IsOffer || offer.Deleted)
                throw new Exception(OfferResourceNames.OFFER_NOT_EXISTS);
            if (offer.IsOfferProcessed)
                throw new Exception(OfferResourceNames.OFFER_PROCESSED);
            if (offer.ValidUntil < DateTime.Now.Date)
                throw new Exception(OfferResourceNames.OFFER_EXPIRED);
            if (!CheckoutTransaction.Suppressed(() =>
                    _clientResourceAccessService.CanAccessClientOrAgreement(clientNetId, offer.ClientAgreement.NetUid)))
                throw new UnauthorizedAccessException();

            List<OrderItem> selectedOrderItems = BuildCanonicalOfferItems(offer, clientShoppingCart);
            if (!shoppingCartRepository.TrySetProcessedByNetId(clientShoppingCart.NetUid))
                throw new Exception(OfferResourceNames.OFFER_PROCESSED);

            ClientShoppingCart currentCart = addCurrentCartItems
                ? CheckoutLocks.LockShoppingCart(
                    connection,
                    shoppingCartRepository.GetByClientNetId(clientNetId, offer.IsVatCart),
                    () => shoppingCartRepository.GetByClientNetId(clientNetId, offer.IsVatCart))
                : null;

            Order order = new() {
                OrderSource = OrderSource.Offer,
                OrderStatus = OrderStatus.NewOrderCart,
                UserId = offer.CreatedById ?? _userRepositoriesFactory.NewUserRepository(connection).GetManagerOrGBAIdByClientNetId(clientNetId),
                ClientAgreement = _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetActiveByRootClientNetId(clientNetId, offer.IsVatCart),
                ClientShoppingCartId = offer.Id
            };

            if (order.ClientAgreement?.Agreement?.Organization == null)
                throw new ArgumentException("A valid client agreement is required.");

            order.ClientAgreementId = order.ClientAgreement.Id;

            order.Id = _saleRepositoriesFactory
                .NewOrderRepository(connection)
                .Add(order);

            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            CheckoutLocks.LockProductStock(
                connection,
                selectedOrderItems.Select(item => item.Product?.Id ?? item.ProductId));

            if (selectedOrderItems.Any()) {
                List<OrderItem> offeredOrderItems = new();

                foreach (OrderItem orderItem in selectedOrderItems) {
                    orderItem.ClientShoppingCartId = null;
                    orderItem.OrderId = order.Id;
                    orderItem.ProductId = orderItem.Product.Id;
                    orderItem.UserId = order.UserId;

                    IEnumerable<ProductAvailability> productAvailabilities =
                        productAvailabilityRepository
                            .GetForEcommercePurchase(
                                orderItem.ProductId,
                                order.ClientAgreement.Agreement.Organization.Id
                            ).ToList();

                    if (productAvailabilities.Any()) {
                        orderItem.OrderedQty = orderItem.Qty;

                        if (productAvailabilities.Sum(a => a.Amount) < orderItem.Qty) orderItem.Qty = productAvailabilities.Sum(a => a.Amount);

                        if (offer.OrderItems.Any(i => i.ProductId.Equals(orderItem.ProductId))) {
                            OrderItem fromOfferItem = offer.OrderItems.First(i => i.ProductId.Equals(orderItem.ProductId));

                            orderItem.FromOfferQty = fromOfferItem.Qty;

                            orderItem.IsFromOffer = true;

                            fromOfferItem.OfferProcessingStatus =
                                orderItem.Qty >= fromOfferItem.Qty ? OfferProcessingStatus.FullyProcessed : OfferProcessingStatus.PartiallyProcessed;

                            fromOfferItem.OrderedQty = orderItem.Qty;

                            offeredOrderItems.Add(fromOfferItem);
                        }

                        orderItem.Id = orderItemRepository.Add(orderItem);

                        double toDecreaseQty = orderItem.Qty;

                        foreach (ProductAvailability productAvailability in productAvailabilities.Where(a => a.Amount > 0)) {
                            if (toDecreaseQty.Equals(0d)) break;

                            if (productAvailability.Amount >= toDecreaseQty) {
                                productReservationRepository
                                    .Add(new ProductReservation {
                                        OrderItemId = orderItem.Id,
                                        ProductAvailabilityId = productAvailability.Id,
                                        Qty = toDecreaseQty
                                    });

                                productAvailability.Amount -= toDecreaseQty;

                                toDecreaseQty = 0d;
                            } else {
                                productReservationRepository
                                    .Add(new ProductReservation {
                                        OrderItemId = orderItem.Id,
                                        ProductAvailabilityId = productAvailability.Id,
                                        Qty = productAvailability.Amount
                                    });

                                toDecreaseQty -= productAvailability.Amount;

                                productAvailability.Amount = 0d;
                            }

                            productAvailabilityRepository.Update(productAvailability);
                        }

                        BackgroundSyncRunner.Run(async cancellationToken => {
                            string saleSyncCrmUrl;

                            if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                                EcommerceCrmConfig data = JsonSerializer.Deserialize<EcommerceCrmConfig>(
                                    File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()),
                                    _jsonSerializerOptions);

                                saleSyncCrmUrl =
                                    $"{data?.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
                            } else {
                                throw new InvalidOperationException("CRM endpoint is not configured.");
                            }

                            using HttpClient httpClient = _httpClientFactory.CreateClient(
                                EcommerceInternalHttpClientDefaults.ClientName);
                            await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
                        }, "Offer product availability sync");
                    } else {
                        orderItem.OrderedQty = orderItem.Qty;
                        orderItem.IsFromOffer = true;

                        orderItem.Id = orderItemRepository.Add(orderItem);
                    }
                }

                orderItemRepository.SetOfferProcessingStatuses(offeredOrderItems);
            } else {
                offer.OfferProcessingStatus = OfferProcessingStatus.NotProcessed;
            }

            shoppingCartRepository.UpdateProcessingStatus(offer);

            if (currentCart != null && currentCart.OrderItems.Any())
                CheckoutLocks.ClaimCartItems(connection, currentCart, order.Id);

            Sale sale = new() {
                ClientAgreementId = order.ClientAgreementId,
                OrderId = order.Id,
                UserId = order.UserId,
                IsVatSale = offer.IsVatCart,
                BaseLifeCycleStatusId =
                    _saleRepositoriesFactory
                        .NewBaseLifeCycleStatusRepository(connection)
                        .Add(
                            new BaseLifeCycleStatus {
                                SaleLifeCycleType = SaleLifeCycleType.New
                            }
                        ),
                BaseSalePaymentStatusId =
                    _saleRepositoriesFactory
                        .NewBaseSalePaymentStatusRepository(connection)
                        .Add(
                            new BaseSalePaymentStatus {
                                SalePaymentStatusType = SalePaymentStatusType.NotPaid
                            }
                        )
            };

            sale.SaleNumberId = CheckoutSaleNumbers.Add(
                connection,
                _saleRepositoriesFactory.NewSaleNumberRepository(connection),
                order.ClientAgreement.Agreement.Organization);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            sale.Id = saleRepository.Add(sale);

            Sale createdSale = saleRepository.GetById(sale.Id);

            BackgroundSyncRunner.Run(async cancellationToken => {
                string saleSyncCrmUrl;

                if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                    EcommerceCrmConfig data = JsonSerializer.Deserialize<EcommerceCrmConfig>(
                        File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()),
                        _jsonSerializerOptions);

                    saleSyncCrmUrl = $"{data?.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
                } else {
                    throw new InvalidOperationException("CRM endpoint is not configured.");
                }

                using HttpClient httpClient = _httpClientFactory.CreateClient(
                    EcommerceInternalHttpClientDefaults.ClientName);
                await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
            }, "Offer sale sync");

            return CheckoutSaleResponse.From(createdSale);
        }));
    }

    private static List<OrderItem> BuildCanonicalOfferItems(
        ClientShoppingCart offer,
        ClientShoppingCart requestedCart) {
        if (requestedCart?.OrderItems == null ||
            requestedCart.OrderItems.Count is < 1 or > 100)
            throw new ArgumentException("A valid offer selection is required.");

        Dictionary<Guid, OrderItem> offerItems = offer.OrderItems
            .Where(item => item.NetUid != Guid.Empty)
            .ToDictionary(item => item.NetUid);
        HashSet<Guid> selectedNetIds = new();
        List<OrderItem> selected = new(requestedCart.OrderItems.Count);

        foreach (OrderItem requested in requestedCart.OrderItems) {
            if (requested == null ||
                requested.NetUid == Guid.Empty ||
                !selectedNetIds.Add(requested.NetUid) ||
                !offerItems.TryGetValue(requested.NetUid, out OrderItem offered) ||
                double.IsNaN(requested.Qty) ||
                double.IsInfinity(requested.Qty) ||
                requested.Qty <= 0 ||
                requested.Qty > offered.Qty)
                throw new ArgumentException("A valid offer selection is required.");

            selected.Add(new OrderItem {
                ProductId = offered.ProductId,
                Product = offered.Product,
                Qty = requested.Qty,
                OverLordQty = requested.Qty,
                Comment = offered.Comment,
                IsValidForCurrentSale = offered.IsValidForCurrentSale,
                PricePerItem = offered.PricePerItem,
                PricePerItemWithoutVat = offered.PricePerItemWithoutVat,
                ExchangeRateAmount = offered.ExchangeRateAmount,
                Vat = offered.Vat,
                AssignedSpecificationId = offered.AssignedSpecificationId,
                IsFromReSale = offered.IsFromReSale,
                MisplacedSaleId = offered.MisplacedSaleId,
                IsFromShiftedItem = offered.IsFromShiftedItem
            });
        }

        return selected;
    }

    public Task<Order> DynamicallyCalculateTotalPrices(Order order) {
        foreach (OrderItem orderItem in order.OrderItems) {
            orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.ChangedQty), 2, MidpointRounding.AwayFromZero);
            orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.ChangedQty);

            orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

            orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
            orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
        }

        order.TotalAmount = decimal.Round(order.OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
        order.TotalAmountLocal = decimal.Round(order.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);

        return Task.FromResult(order);
    }
}
