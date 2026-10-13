using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
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
using GBA.Services.Infrastructure.SalesMutations;
using GBA.Services.Services.Offers.Contracts;
using Microsoft.Extensions.Http;

namespace GBA.Services.Services.Offers;

public sealed class OfferService : IOfferService {
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;

    private readonly IDbConnectionFactory _connectionFactory;

    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISalesCreationLedgerStore _salesCreationLedgerStore;
    private readonly TimeProvider _timeProvider;

    public OfferService(
        IClientRepositoriesFactory clientRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IHttpClientFactory httpClientFactory,
        ISalesCreationLedgerStore salesCreationLedgerStore,
        TimeProvider timeProvider) {
        _clientRepositoriesFactory = clientRepositoriesFactory;

        _saleRepositoriesFactory = saleRepositoriesFactory;

        _productRepositoriesFactory = productRepositoriesFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        _connectionFactory = connectionFactory;
        _httpClientFactory = httpClientFactory;
        _salesCreationLedgerStore = salesCreationLedgerStore;
        _timeProvider = timeProvider;
    }

    public Task<ClientShoppingCart> GetOfferByNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ClientShoppingCart offer = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection).GetByNetId(netId);

        if (offer == null) throw new Exception(OfferResourceNames.OFFER_NOT_EXISTS);

        if (offer.ValidUntil < DateTime.Now.Date) throw new Exception(OfferResourceNames.OFFER_EXPIRED);

        if (offer.IsOfferProcessed) throw new Exception(OfferResourceNames.OFFER_PROCESSED);

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

    public async Task<Sale> GenerateNewOrderAndSaleFromOffer(ClientShoppingCart clientShoppingCart,
        Guid clientNetId, bool addCurrentCartItems, Guid operationNetUid) {
        if (clientShoppingCart == null || clientShoppingCart.NetUid == Guid.Empty || clientShoppingCart.OrderItems == null)
            throw new ArgumentException("An offer identity and requested items are required.");
        // Snapshot only inbound values here. Semantic rejection must follow ledger registration
        // so a changed retry conflicts with its committed receipt before validation runs.
        var requested = clientShoppingCart.OrderItems.Select(item => item == null ? null : new {
            ProductId = item.Product?.Id > 0 ? item.Product.Id : item.ProductId,
            DeclaredProductId = item.ProductId, NestedProductId = item.Product?.Id, item.Qty, item.Comment
        }).OrderBy(item => item?.ProductId ?? 0).ToArray();
        var request = LegacyCheckoutExecutor.Request(operationNetUid, clientNetId,
            "offer-checkout-v1", addCurrentCartItems,
            new { Offer = clientShoppingCart.NetUid, AddCartItems = addCurrentCartItems,
                Items = requested.Select(item => item == null ? null : new {
                    item.ProductId, item.DeclaredProductId, item.NestedProductId,
                    Quantity = item.Qty.ToString("R", CultureInfo.InvariantCulture), item.Comment
                }).ToArray() });
        var productsToNotify = new HashSet<Guid>();
        var executor = new LegacyCheckoutExecutor(_connectionFactory, _salesCreationLedgerStore, _timeProvider);
        (Sale sale, bool replay) = await executor.ExecuteAsync(request, connection => {
            foreach (var item in requested) {
                if (item == null) throw new ArgumentException("An offer item is required.");
                LegacyCheckoutPolicy.RequireQuantity(item.Qty);
                if (item.ProductId <= 0 || item.DeclaredProductId > 0 && item.DeclaredProductId != item.ProductId)
                    throw new ArgumentException("Offer product identities do not match.");
            }
            if (requested.Select(item => item.ProductId).Distinct().Count() != requested.Length
                || requested.Length == 0 && !addCurrentCartItems)
                throw new ArgumentException("Offer items must be distinct and nonempty.");
            IClientShoppingCartRepository carts = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);
            ClientShoppingCart offer = carts.GetByNetId(clientShoppingCart.NetUid);
            LegacyCheckoutPolicy.RequireCart(connection, offer, clientNetId, true);
            ClientAgreement agreement = _clientRepositoriesFactory.NewClientAgreementRepository(connection)
                .GetWithOrganizationById(offer.ClientAgreementId);
            LegacyCheckoutPolicy.RequireAgreement(offer, agreement);
            ClientShoppingCart cart = addCurrentCartItems
                ? carts.GetByClientAgreementNetId(agreement.NetUid, offer.IsVatCart) : null;
            if (cart != null) LegacyCheckoutPolicy.RequireCart(connection, cart, clientNetId, false);
            if (requested.Length == 0 && (cart == null || cart.OrderItems.Count == 0))
                throw new ArgumentException("You need to add products first.");
            foreach (var item in requested)
                if (offer.OrderItems.Count(source => source.ProductId == item.ProductId && !source.Deleted) != 1)
                    throw new ArgumentException("A requested product is not an unambiguous item of this offer.");

            LegacyCheckoutPolicy.LockProducts(connection, requested.Select(item => item.ProductId));
            var order = new Order {
                OrderSource = OrderSource.Offer, OrderStatus = OrderStatus.NewOrderCart,
                UserId = offer.CreatedById ?? _userRepositoriesFactory.NewUserRepository(connection).GetManagerOrGBAIdByClientNetId(clientNetId),
                ClientAgreement = agreement, ClientAgreementId = agreement.Id, ClientShoppingCartId = offer.Id
            };
            order.Id = _saleRepositoriesFactory.NewOrderRepository(connection).Add(order);
            IOrderItemRepository items = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductReservationRepository reservations = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IProductAvailabilityRepository availability = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            var processedItems = new List<OrderItem>();
            foreach (var item in requested) {
                OrderItem source = offer.OrderItems.Single(value => value.ProductId == item.ProductId && !value.Deleted);
                var available = availability.GetByProductAndOrganizationIds(item.ProductId,
                    agreement.Agreement.Organization.Id, offer.IsVatCart).OrderBy(value => value.Id).ToArray();
                if (available.Any(value => !double.IsFinite(value.Amount) || value.Amount < 0))
                    throw new InvalidOperationException("Product availability is invalid.");
                // Preserve the existing backorder case: no availability rows means an
                // ordered item without a stock reservation, rather than invented stock.
                double quantity = available.Length == 0 ? item.Qty : Math.Min(item.Qty, available.Sum(value => value.Amount));
                if (!double.IsFinite(quantity) || quantity < 0)
                    throw new InvalidOperationException("Product availability is invalid.");
                var createdItem = new OrderItem {
                    OrderId = order.Id, ProductId = item.ProductId, UserId = order.UserId,
                    Qty = quantity, OrderedQty = item.Qty, Comment = item.Comment,
                    FromOfferQty = source.Qty, IsFromOffer = true,
                    PricePerItem = source.PricePerItem, ExchangeRateAmount = source.ExchangeRateAmount,
                    Vat = source.Vat, AssignedSpecificationId = source.AssignedSpecificationId,
                    IsValidForCurrentSale = source.IsValidForCurrentSale
                };
                createdItem.Id = items.Add(createdItem);
                double remaining = quantity;
                foreach (ProductAvailability row in available.Where(value => value.Amount > 0)) {
                    if (remaining <= 0) break;
                    double reserved = Math.Min(remaining, row.Amount);
                    reservations.Add(new ProductReservation { OrderItemId = createdItem.Id,
                        ProductAvailabilityId = row.Id, Qty = reserved });
                    row.Amount -= reserved;
                    availability.Update(row);
                    remaining -= reserved;
                }
                if (available.Length != 0 && remaining > 0.000000001d)
                    throw new InvalidOperationException("Offer reservation is incomplete.");
                source.OrderedQty = quantity;
                source.OfferProcessingStatus = quantity >= source.Qty
                    ? OfferProcessingStatus.FullyProcessed : OfferProcessingStatus.PartiallyProcessed;
                processedItems.Add(source);
                if (source.Product?.NetUid is Guid productNetUid && productNetUid != Guid.Empty)
                    productsToNotify.Add(productNetUid);
            }
            items.SetOfferProcessingStatuses(processedItems);
            offer.OfferProcessingStatus = requested.Length == 0 ? OfferProcessingStatus.NotProcessed
                : processedItems.All(item => item.OfferProcessingStatus == OfferProcessingStatus.FullyProcessed)
                    && processedItems.Count == offer.OrderItems.Count
                    ? OfferProcessingStatus.FullyProcessed : OfferProcessingStatus.PartiallyProcessed;
            carts.UpdateProcessingStatus(offer);
            carts.SetProcessedByNetId(offer.NetUid);
            if (cart != null) LegacyCheckoutPolicy.MoveCartItems(connection, cart, order.Id);
            var created = new Sale {
                ClientAgreementId = agreement.Id, OrderId = order.Id, UserId = order.UserId, IsVatSale = offer.IsVatCart,
                BaseLifeCycleStatusId = _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection)
                    .Add(new BaseLifeCycleStatus { SaleLifeCycleType = SaleLifeCycleType.New }),
                BaseSalePaymentStatusId = _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection)
                    .Add(new BaseSalePaymentStatus { SalePaymentStatusType = SalePaymentStatusType.NotPaid }),
                SaleNumberId = LegacyCheckoutPolicy.AddSaleNumber(connection,
                    _saleRepositoriesFactory.NewSaleNumberRepository(connection), agreement)
            };
            ISaleRepository sales = _saleRepositoriesFactory.NewSaleRepository(connection);
            created.Id = sales.Add(created);
            return sales.GetById(created.Id);
        });
        if (!replay) {
            foreach (Guid product in productsToNotify) QueueCheckoutSync("products/sync/availability", product);
            QueueCheckoutSync("sales/sync/new", sale.NetUid);
        }
        return sale;
    }

    private void QueueCheckoutSync(string path, Guid netUid) {
        BackgroundSyncRunner.Run(async cancellationToken => {
            string baseUrl = "http://93.183.224.42";
            if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                EcommerceCrmConfig data = JsonSerializer.Deserialize<EcommerceCrmConfig>(
                    File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()), _jsonSerializerOptions);
#if DEBUG
                baseUrl = data?.CrmServerUrl;
#else
                baseUrl = data?.CrmServerUrlRelease;
#endif
            }
            using HttpClient client = _httpClientFactory.CreateClient();
            await client.GetAsync($"{baseUrl}/api/v1/{CultureInfo.CurrentCulture}/{path}?netId={netUid}", cancellationToken);
        }, "Offer checkout sync");
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
