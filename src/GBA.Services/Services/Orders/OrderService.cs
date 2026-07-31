using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Common.Configuration;
using GBA.Common.Helpers;
using GBA.Common.Models;
using GBA.Common.ResourceNames;
using GBA.Common.Search;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.Delivery.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.Infrastructure;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.Messengers.Contracts;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.Extensions.Http;

namespace GBA.Services.Services.Orders;

public sealed class OrderService : IOrderService {
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IDeliveryRepositoriesFactory _deliveryRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPaymentLinkService _paymentLinkService;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IRetailClientRepositoriesFactory _retailClientRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISearchReindexSignal _reindexSignal;
    private readonly IClientResourceAccessService _clientResourceAccessService;

    public OrderService(
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IDeliveryRepositoriesFactory deliveryRepositoriesFactory,
        IRetailClientRepositoriesFactory retailClientRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IPaymentLinkService paymentLinkService,
        IHttpClientFactory httpClientFactory,
        IClientResourceAccessService clientResourceAccessService,
        ISearchReindexSignal reindexSignal) {
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _reindexSignal = reindexSignal;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _deliveryRepositoriesFactory = deliveryRepositoriesFactory;
        _retailClientRepositoriesFactory = retailClientRepositoriesFactory;
        _connectionFactory = connectionFactory;
        _paymentLinkService = paymentLinkService;
        _httpClientFactory = httpClientFactory;
        _clientResourceAccessService = clientResourceAccessService;
    }

    /// <summary>
    /// Resolves the booked FX rate from the ExchangeRate table for the given agreement currency in
    /// the current culture. The rate is server-authoritative and is NOT derived from
    /// CurrentLocalPrice / CurrentPrice: dbo.GetCalculatedProductLocalPriceWithSharesAndVat
    /// currently returns the agreement-currency price unchanged, so that ratio booked every EUR
    /// sale at 1 EUR = 1 UAH. Returns 0 when no rate row exists (the agreement is already in the
    /// local currency), in which case the caller keeps the repository's local price.
    /// </summary>
    private decimal ResolveLocalExchangeRate(IDbConnection connection, string currencyCode) {
        if (string.IsNullOrWhiteSpace(currencyCode)) return 0m;

        ExchangeRate exchangeRate = _exchangeRateRepositoriesFactory
            .NewExchangeRateRepository(connection)
            .GetByCurrencyCodeAndCurrentCulture(currencyCode);

        return exchangeRate != null && exchangeRate.Amount > 0m ? exchangeRate.Amount : 0m;
    }

    private OrderItem ApplyAuthoritativeRetailProduct(
        IDbConnection connection,
        Storage storage,
        bool withVat,
        OrderItem orderItem) {
        if (orderItem?.Product == null || orderItem.Product.NetUid == Guid.Empty)
            throw new ArgumentException("Every order item must reference a valid product.");

        if (!double.IsFinite(orderItem.Qty) || orderItem.Qty <= 0 || orderItem.Qty > 100000)
            throw new ArgumentException("Order item quantity is invalid.");

        Product product = _productRepositoriesFactory
            .NewGetSingleProductRepository(connection)
            .GetByNetIdForRetail(orderItem.Product.NetUid, storage.OrganizationId.Value, withVat);
        if (!EcommercePurchasability.IsPurchasable(product))
            throw new ArgumentException(EcommercePurchasability.NotAvailableMessage);
        if (!EcommercePurchasability.HasSellablePrice(product))
            throw new ArgumentException(EcommercePurchasability.NotPricedMessage);

        decimal exchangeRateAmount = ResolveLocalExchangeRate(connection, product.CurrencyCode);
        if (exchangeRateAmount > 0m)
            product.CurrentLocalPrice = decimal.Round(
                product.CurrentPrice * exchangeRateAmount, 2, MidpointRounding.AwayFromZero);

        // All commercial fields are server-authoritative. The browser only selects
        // the product capability and quantity.
        orderItem.Product = product;
        orderItem.ProductId = product.Id;
        orderItem.PricePerItem = product.CurrentPrice;
        orderItem.ExchangeRateAmount = exchangeRateAmount > 0m
            ? exchangeRateAmount
            : decimal.Round(product.CurrentLocalPrice / product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
        orderItem.TotalAmount = decimal.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
        orderItem.TotalAmountLocal = decimal.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
        orderItem.OverLordQty = orderItem.Qty;
        orderItem.OrderedQty = orderItem.Qty;
        orderItem.OneTimeDiscount = 0;
        orderItem.Discount = 0;
        orderItem.DiscountAmount = 0;
        orderItem.PricePerItemWithoutVat = 0;
        orderItem.FromOfferQty = 0;
        orderItem.IsFromOffer = false;
        orderItem.IsFromReSale = false;
        orderItem.AssignedSpecificationId = null;
        orderItem.MisplacedSaleId = null;
        orderItem.IsMisplacedItem = false;
        orderItem.IsValidForCurrentSale = true;
        orderItem.Vat = withVat
            ? Convert.ToDecimal(storage.Organization?.VatRate?.Value ?? 0)
            : 0;

        return orderItem;
    }

    private OrderItem ApplyAuthoritativeClientProduct(
        IDbConnection connection,
        ClientAgreement clientAgreement,
        OrderItem orderItem) {
        if (orderItem?.Product == null || orderItem.Product.NetUid == Guid.Empty ||
            !double.IsFinite(orderItem.Qty) || orderItem.Qty <= 0 || orderItem.Qty > 100000)
            throw new ArgumentException("Order item is invalid.");

        Product product = _productRepositoriesFactory
            .NewGetSingleProductRepository(connection)
            .GetProductByNetId(
                orderItem.Product.NetUid,
                clientAgreement.NetUid,
                clientAgreement.Agreement.WithVATAccounting,
                clientAgreement.Agreement.CurrencyId,
                clientAgreement.Agreement.OrganizationId);
        if (!EcommercePurchasability.IsPurchasable(product))
            throw new ArgumentException(EcommercePurchasability.NotAvailableMessage);
        if (!EcommercePurchasability.HasSellablePrice(product))
            throw new ArgumentException(EcommercePurchasability.NotPricedMessage);

        decimal exchangeRateAmount = ResolveLocalExchangeRate(
            connection, clientAgreement.Agreement.Currency?.Code ?? product.CurrencyCode);
        if (exchangeRateAmount > 0m)
            product.CurrentLocalPrice = decimal.Round(
                product.CurrentPrice * exchangeRateAmount, 2, MidpointRounding.AwayFromZero);

        orderItem.Product = product;
        orderItem.ProductId = product.Id;
        orderItem.PricePerItem = product.CurrentPrice;
        orderItem.ExchangeRateAmount = exchangeRateAmount > 0m
            ? exchangeRateAmount
            : decimal.Round(product.CurrentLocalPrice / product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
        orderItem.TotalAmount = decimal.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
        orderItem.TotalAmountLocal = decimal.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
        orderItem.OneTimeDiscount = 0;
        orderItem.Discount = 0;
        orderItem.DiscountAmount = 0;
        orderItem.PricePerItemWithoutVat = 0;
        orderItem.FromOfferQty = 0;
        orderItem.IsFromOffer = false;
        orderItem.IsFromReSale = false;
        orderItem.AssignedSpecificationId = null;
        orderItem.MisplacedSaleId = null;
        orderItem.IsValidForCurrentSale = true;
        orderItem.Vat = clientAgreement.Agreement.WithVATAccounting
            ? Convert.ToDecimal(clientAgreement.Agreement.Organization?.VatRate?.Value ?? 0)
            : 0;

        return orderItem;
    }

    private static Sale BuildCreatedSaleResponse(Sale sale) {
        return new Sale {
            Id = sale.Id,
            NetUid = sale.NetUid,
            Created = sale.Created,
            Updated = sale.Updated,
            Deleted = sale.Deleted,
            OrderId = sale.OrderId,
            ClientAgreementId = sale.ClientAgreementId,
            BaseLifeCycleStatusId = sale.BaseLifeCycleStatusId,
            BaseSalePaymentStatusId = sale.BaseSalePaymentStatusId,
            DeliveryRecipientId = sale.DeliveryRecipientId,
            DeliveryRecipientAddressId = sale.DeliveryRecipientAddressId,
            TransporterId = sale.TransporterId,
            SaleNumberId = sale.SaleNumberId,
            ShiftStatusId = sale.ShiftStatusId,
            WorkplaceId = sale.WorkplaceId,
            CustomersOwnTtnId = sale.CustomersOwnTtnId,
            Comment = sale.Comment,
            IsInvoice = sale.IsInvoice,
            IsVatSale = sale.IsVatSale,
            IsCashOnDelivery = sale.IsCashOnDelivery,
            HasDocuments = sale.HasDocuments,
            CashOnDeliveryAmount = sale.CashOnDeliveryAmount,
            ShipmentDate = sale.ShipmentDate,
            SaleNumber = sale.SaleNumber == null
                ? null
                : new SaleNumber {
                    Id = sale.SaleNumber.Id,
                    NetUid = sale.SaleNumber.NetUid,
                    Created = sale.SaleNumber.Created,
                    Updated = sale.SaleNumber.Updated,
                    Deleted = sale.SaleNumber.Deleted,
                    Value = sale.SaleNumber.Value,
                    OrganizationId = sale.SaleNumber.OrganizationId
                },
            Order = sale.Order == null
                ? null
                : new Order {
                    Id = sale.Order.Id,
                    NetUid = sale.Order.NetUid,
                    Created = sale.Order.Created,
                    Updated = sale.Order.Updated,
                    Deleted = sale.Order.Deleted,
                    OrderSource = sale.Order.OrderSource,
                    OrderStatus = sale.Order.OrderStatus
                }
        };
    }

    private void QueueEcommerceSaleUpdate(string crmApiUrl, string payload, string operationName) {
        BackgroundSyncRunner.Run(async cancellationToken => {
            using HttpClient httpClient = _httpClientFactory.CreateClient(
                EcommerceInternalHttpClientDefaults.ClientName);
            using HttpRequestMessage requestMessage = new(HttpMethod.Post, crmApiUrl) {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            HttpResponseMessage responseMessage =
                await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (responseMessage.IsSuccessStatusCode) return;

            using (responseMessage) {
                string responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
                string responseErrorMessage = ExtractErrorMessage(responseContent);
                throw new Exception(responseErrorMessage);
            }
        }, operationName);
    }

    public Task<Sale> GenerateNewOrderAndSaleFromClientShoppingCart(Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ClientShoppingCart clientShoppingCart =
                _clientRepositoriesFactory.NewClientShoppingCartRepository(connection).GetByClientNetId(clientNetId, withVat);

            if (clientShoppingCart == null || !clientShoppingCart.OrderItems.Any()) throw new Exception("You need to add products first.");

            Order order = new() {
                OrderSource = OrderSource.Shop,
                OrderStatus = OrderStatus.NewOrderCart,
                UserId = _userRepositoriesFactory.NewUserRepository(connection).GetManagerOrGBAIdByClientNetId(clientNetId),
                ClientAgreement = _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetActiveByRootClientNetId(clientNetId, withVat)
            };

            order.ClientAgreementId = order.ClientAgreement.Id;

            order.Id = _saleRepositoriesFactory
                .NewOrderRepository(connection)
                .Add(order);

            _saleRepositoriesFactory
                .NewOrderItemRepository(connection)
                .Update(
                    clientShoppingCart
                        .OrderItems
                        .Select(item => {
                            item.ClientShoppingCartId = null;
                            item.OrderId = order.Id;

                            return item;
                        })
                );

            Sale sale = new() {
                ClientAgreementId = order.ClientAgreementId,
                OrderId = order.Id,
                UserId = order.UserId,
                IsVatSale = clientShoppingCart.IsVatCart,
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

            ISaleNumberRepository saleNumberRepository = _saleRepositoriesFactory.NewSaleNumberRepository(connection);

            SaleNumber lastSaleNumber = saleNumberRepository.GetLastRecordByOrganizationNetId(order.ClientAgreement.Agreement.Organization.NetUid);
            SaleNumber saleNumber;

            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            try {
                if (lastSaleNumber != null && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year)) {
                    saleNumber = new SaleNumber {
                        OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                        Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}"
                    };

                    saleNumber.Value +=
                        string.Format("{0:D8}",
                            Convert.ToInt32(
                                lastSaleNumber.Value.Substring(
                                    lastSaleNumber.Organization.Code.Length + currentMonth.Length,
                                    lastSaleNumber.Value.Length - (lastSaleNumber.Organization.Code.Length + currentMonth.Length)))
                            + 1);
                } else {
                    saleNumber = new SaleNumber {
                        OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                        Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                    };
                }
            } catch (FormatException) {
                saleNumber = new SaleNumber {
                    OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                    Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                };
            }

            sale.SaleNumberId = saleNumberRepository.Add(saleNumber);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            sale.Id = saleRepository.Add(sale);

            sale = saleRepository.GetById(sale.Id);

            BackgroundSyncRunner.Run(async cancellationToken => {
                string saleSyncCrmUrl;

                if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                    EcommerceCrmConfig data = ReadEcommerceCrmConfig();

#if DEBUG
                    saleSyncCrmUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={sale.NetUid.ToString()}";
#else
                            saleSyncCrmUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={sale.NetUid.ToString()}";
#endif
                } else {
                    throw new InvalidOperationException("CRM endpoint is not configured.");
                }

                using HttpClient httpClient = _httpClientFactory.CreateClient(
                    EcommerceInternalHttpClientDefaults.ClientName);
                await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
            }, "Order sale sync");

            return Task.FromResult(sale);
    }

    /// <summary>
    /// Totals for the cart / checkout preview. The endpoint that calls this is anonymous, so every
    /// price in the request body is untrusted input and is discarded: each line is re-resolved from
    /// the database through the same authoritative path checkout uses - the retail
    /// storage/agreement for anonymous callers, the caller's own agreement when signed in. The
    /// browser only chooses the product and the quantity, never the price, discount, VAT or FX rate.
    /// The running totals also start from zero so a caller cannot seed them.
    /// </summary>
    public Task<Order> DynamicallyCalculateTotalPrices(Order order, Guid clientNetId) {
        if (order?.OrderItems == null || order.OrderItems.Count == 0)
            throw new ArgumentException("Order must contain between 1 and 100 items.");
        if (order.OrderItems.Count > 100)
            throw new ArgumentException("Order must contain between 1 and 100 items.");
        if (order.OrderItems.Any(orderItem =>
                orderItem?.Product == null ||
                orderItem.Product.NetUid == Guid.Empty))
            throw new ArgumentException("Every order item must reference a valid product.");

        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IClientAgreementRepository clientAgreementRepository =
            _clientRepositoriesFactory.NewClientAgreementRepository(connection);

        ClientAgreement clientAgreement = clientNetId == Guid.Empty
            ? null
            : clientAgreementRepository.GetSelectedByClientNetId(clientNetId)
              ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(clientNetId);

        Storage storage = null;
        bool withVat = false;

        if (clientAgreement == null) {
            storage = _storageRepositoryFactory
                .NewStorageRepository(connection)
                .GetWithHighestPriority();
            if (storage == null || !storage.OrganizationId.HasValue)
                throw new InvalidOperationException("Retail storage is not configured.");

            withVat = storage.ForVatProducts;
        }

        order.TotalAmount = 0m;
        order.TotalAmountLocal = 0m;
        order.OverLordTotalAmount = 0m;
        order.OverLordTotalAmountLocal = 0m;

        foreach (OrderItem orderItem in order.OrderItems) {
            if (clientAgreement == null)
                ApplyAuthoritativeRetailProduct(connection, storage, withVat, orderItem);
            else
                ApplyAuthoritativeClientProduct(connection, clientAgreement, orderItem);

            if (orderItem.OverLordQty <= 0) orderItem.OverLordQty = orderItem.Qty;

            orderItem.OverLordTotalAmount = decimal.Round(
                orderItem.PricePerItem * Convert.ToDecimal(orderItem.OverLordQty), 2, MidpointRounding.AwayFromZero);
            orderItem.OverLordTotalAmountLocal = decimal.Round(
                orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.OverLordQty), 2, MidpointRounding.AwayFromZero);

            order.TotalAmount = decimal.Round(order.TotalAmount + orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
            order.TotalAmountLocal = decimal.Round(order.TotalAmountLocal + orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
            order.OverLordTotalAmount = decimal.Round(order.OverLordTotalAmount + orderItem.OverLordTotalAmount, 2, MidpointRounding.AwayFromZero);
            order.OverLordTotalAmountLocal = decimal.Round(order.OverLordTotalAmountLocal + orderItem.OverLordTotalAmountLocal, 2, MidpointRounding.AwayFromZero);
        }

        return Task.FromResult(order);
    }

    public async Task<Sale> GenerateNewSaleWithInvoice(Sale sale, Guid clientNetId, bool isWorkplace) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

            ClientAgreement selectedClientAgreement = isWorkplace
                ? _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetSelectedByWorkplaceNetId(clientNetId)
                : _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetSelectedByClientNetId(clientNetId);
            if (selectedClientAgreement == null)
                throw new ArgumentException("A valid client agreement is required.");

            bool withVat = selectedClientAgreement.Agreement.WithVATAccounting;

            if (sale?.Order?.OrderItems == null ||
                sale.Order.OrderItems.Count == 0 ||
                sale.Order.OrderItems.Count > 100)
                throw new ArgumentException("Order must contain between 1 and 100 items.");

            if (sale.DeliveryRecipient != null && sale.DeliveryRecipient.NetUid != Guid.Empty) {
                if (!_clientResourceAccessService.CanAccessDeliveryRecipient(clientNetId, sale.DeliveryRecipient.NetUid))
                    throw new ArgumentException("Delivery recipient is invalid.");

                DeliveryRecipient recipient = _deliveryRepositoriesFactory
                    .NewDeliveryRecipientRepository(connection)
                    .GetByNetId(sale.DeliveryRecipient.NetUid);
                sale.DeliveryRecipient = recipient;
                sale.DeliveryRecipientId = recipient.Id;
            } else if (sale.DeliveryRecipientId.HasValue) {
                throw new ArgumentException("Delivery recipient is invalid.");
            }

            if (sale.DeliveryRecipientAddress != null && sale.DeliveryRecipientAddress.NetUid != Guid.Empty) {
                if (!_clientResourceAccessService.CanAccessDeliveryRecipientAddress(clientNetId, sale.DeliveryRecipientAddress.NetUid))
                    throw new ArgumentException("Delivery recipient address is invalid.");

                DeliveryRecipientAddress address = _deliveryRepositoriesFactory
                    .NewDeliveryRecipientAddressRepository(connection)
                    .GetByNetId(sale.DeliveryRecipientAddress.NetUid);
                if (sale.DeliveryRecipient == null ||
                    address.DeliveryRecipientId != sale.DeliveryRecipient.Id)
                    throw new ArgumentException("Delivery recipient address is invalid.");

                sale.DeliveryRecipientAddress = address;
                sale.DeliveryRecipientAddressId = address.Id;
            } else if (sale.DeliveryRecipientAddressId.HasValue) {
                throw new ArgumentException("Delivery recipient address is invalid.");
            }

            Order order = new() {
                OrderSource = OrderSource.Shop,
                OrderStatus = OrderStatus.NewOrderCart,
                ClientAgreement = selectedClientAgreement
            };

            order.ClientAgreementId = order.ClientAgreement.Id;

            Workplace workplace = null;

            if (isWorkplace) workplace = _clientRepositoriesFactory.NewWorkplaceRepository(connection).GetByNetIdWithClient(clientNetId);

            Client client = isWorkplace
                ? workplace.MainClient
                : _clientRepositoriesFactory.NewClientRepository(connection).GetByNetIdWithRegionCode(clientNetId);


            ClientShoppingCart currentCart =
                _clientRepositoriesFactory
                    .NewClientShoppingCartRepository(connection)
                    .GetByClientAgreementNetId(
                        selectedClientAgreement.NetUid,
                        withVat,
                        workplace?.Id
                    );

            if (currentCart == null || currentCart.OrderItems.Count == 0)
                throw new ArgumentException("The current shopping cart is empty.");

            HashSet<Guid> requestedItemNetIds = new();
            List<OrderItem> authorizedOrderItems = new();
            foreach (OrderItem requestedItem in sale.Order.OrderItems) {
                if (requestedItem.NetUid == Guid.Empty ||
                    !requestedItemNetIds.Add(requestedItem.NetUid))
                    throw new ArgumentException("Order item is invalid.");

                OrderItem cartItem = currentCart.OrderItems
                    .SingleOrDefault(item => item.NetUid == requestedItem.NetUid);
                if (cartItem == null)
                    throw new ArgumentException("Order item is not part of the current shopping cart.");

                ApplyAuthoritativeClientProduct(connection, selectedClientAgreement, cartItem);
                authorizedOrderItems.Add(cartItem);
            }

            sale.Order.OrderItems = authorizedOrderItems.ToHashSet();

            order.Id = _saleRepositoriesFactory
                .NewOrderRepository(connection)
                .Add(order);

            foreach (OrderItem orderItem in sale.Order.OrderItems)
                if (currentCart.OrderItems.Any(i => i.ProductId.Equals(orderItem.Product.Id)))
                    orderItem.Id = currentCart.OrderItems.First(i => i.ProductId.Equals(orderItem.Product.Id)).Id;

            foreach (OrderItem orderItem in sale.Order.OrderItems.Where(i => !i.IsNew() && i.Qty > 0))
                if (currentCart.OrderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                    OrderItem fromCartItem = currentCart.OrderItems.First(i => i.Id.Equals(orderItem.Id));

                    orderItem.ClientShoppingCartId = null;
                    orderItem.OrderId = order.Id;

                    orderItemRepository.UpdateItemAssignment(orderItem);

                    if (fromCartItem.Qty.Equals(orderItem.Qty)) {
                        orderItemRepository.Update(orderItem);
                    } else {
                        if (fromCartItem.Qty > orderItem.Qty) {
                            double toDecreaseAmount = fromCartItem.Qty - orderItem.Qty;

                            fromCartItem.Qty -= orderItem.Qty;

                            orderItemRepository.Update(fromCartItem);

                            IEnumerable<ProductReservation> reservations =
                                productReservationRepository
                                    .GetAllByOrderItemIdWithAvailability(
                                        fromCartItem.Id
                                    );

                            List<Tuple<ProductReservation, double>> usedReservations = new();

                            foreach (ProductReservation reservation in reservations) {
                                if (toDecreaseAmount.Equals(0d)) break;

                                if (reservation.Qty >= toDecreaseAmount) {
                                    usedReservations.Add(new Tuple<ProductReservation, double>(reservation, toDecreaseAmount));

                                    reservation.Qty -= toDecreaseAmount;

                                    toDecreaseAmount = 0d;

                                    if (reservation.Qty > 0)
                                        productReservationRepository.Update(reservation);
                                    else
                                        productReservationRepository.Delete(reservation.NetUid);
                                } else {
                                    usedReservations.Add(new Tuple<ProductReservation, double>(reservation, reservation.Qty));

                                    toDecreaseAmount -= reservation.Qty;

                                    productReservationRepository.Delete(reservation.NetUid);
                                }
                            }

                            orderItem.Id = orderItemRepository.Add(orderItem);

                            foreach ((ProductReservation productReservation, double usedQty) in usedReservations) {
                                ProductReservation reservation =
                                    productReservationRepository
                                        .GetByOrderItemAndProductAvailabilityIds(
                                            orderItem.Id,
                                            productReservation.ProductAvailabilityId
                                        );

                                if (reservation != null) {
                                    reservation.Qty += usedQty;

                                    productReservationRepository.Update(reservation);
                                } else {
                                    productReservationRepository.Add(new ProductReservation {
                                        ProductAvailabilityId = productReservation.ProductAvailabilityId,
                                        OrderItemId = orderItem.Id,
                                        Qty = usedQty
                                    });
                                }
                            }
                        } else {
                            IEnumerable<ProductAvailability> productAvailabilities =
                                productAvailabilityRepository
                                    .GetByProductAndOrganizationIds(
                                        orderItem.ProductId,
                                        order.ClientAgreement.Agreement.Organization.Id,
                                        withVat
                                    );

                            double toDecreaseAmount = orderItem.Qty - fromCartItem.Qty;

                            fromCartItem.Qty = 0d;

                            orderItemRepository.Update(fromCartItem);

                            if (productAvailabilities.Sum(a => a.Amount) < toDecreaseAmount) {
                                orderItem.Qty -= toDecreaseAmount - productAvailabilities.Sum(a => a.Amount);

                                toDecreaseAmount = productAvailabilities.Sum(a => a.Amount);
                            }

                            orderItem.Id = orderItemRepository.Add(orderItem);

                            IEnumerable<ProductReservation> reservations =
                                productReservationRepository
                                    .GetAllByOrderItemIdWithAvailability(
                                        fromCartItem.Id
                                    );

                            foreach (ProductReservation reservation in reservations) {
                                reservation.OrderItemId = orderItem.Id;

                                productReservationRepository.Update(reservation);
                            }

                            foreach (ProductAvailability productAvailability in productAvailabilities.Where(a => a.Amount > 0)) {
                                if (toDecreaseAmount.Equals(0d)) break;

                                ProductReservation reservation =
                                    productReservationRepository
                                        .GetByOrderItemAndProductAvailabilityIds(
                                            orderItem.Id,
                                            productAvailability.Id
                                        );

                                if (productAvailability.Amount >= toDecreaseAmount) {
                                    productAvailability.Amount -= toDecreaseAmount;

                                    if (reservation != null) {
                                        reservation.Qty += toDecreaseAmount;

                                        productReservationRepository.Update(reservation);
                                    } else {
                                        productReservationRepository.Add(new ProductReservation {
                                            ProductAvailabilityId = productAvailability.Id,
                                            OrderItemId = orderItem.Id,
                                            Qty = toDecreaseAmount
                                        });
                                    }

                                    toDecreaseAmount = 0d;
                                } else {
                                    if (reservation != null) {
                                        reservation.Qty += productAvailability.Amount;

                                        productReservationRepository.Update(reservation);
                                    } else {
                                        productReservationRepository.Add(new ProductReservation {
                                            ProductAvailabilityId = productAvailability.Id,
                                            OrderItemId = orderItem.Id,
                                            Qty = productAvailability.Amount,
                                            RegionCode = client.RegionCode != null ? client.RegionCode.Value : string.Empty
                                        });
                                    }

                                    toDecreaseAmount -= productAvailability.Amount;

                                    productAvailability.Amount = 0d;
                                }

                                productAvailabilityRepository.Update(productAvailability);
                            }
                        }
                    }
                } else {
                    orderItem.ClientShoppingCartId = null;
                    orderItem.OrderId = order.Id;

                    orderItemRepository.Update(orderItem);
                }

            foreach (OrderItem orderItem in sale.Order.OrderItems.Where(i => i.IsNew())) {
                orderItem.ClientShoppingCartId = null;
                orderItem.OrderId = order.Id;
                orderItem.ProductId = orderItem.Product.Id;

                IEnumerable<ProductAvailability> productAvailabilities =
                    productAvailabilityRepository
                        .GetByProductAndOrganizationIds(
                            orderItem.ProductId,
                            order.ClientAgreement.Agreement.Organization.Id,
                            withVat
                        );

                if (!productAvailabilities.Any()) continue;

                if (productAvailabilities.Sum(a => a.Amount) < orderItem.Qty) orderItem.Qty = productAvailabilities.Sum(a => a.Amount);

                double toDecreaseQty = orderItem.Qty;

                orderItem.Id = orderItemRepository.Add(orderItem);

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

                _reindexSignal.Request(orderItem.ProductId);

                BackgroundSyncRunner.Run(async cancellationToken => {
                    string saleSyncCrmUrl;

                    if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                        EcommerceCrmConfig data = ReadEcommerceCrmConfig();

#if DEBUG
                        saleSyncCrmUrl =
                            $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#else
                                saleSyncCrmUrl =
 $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#endif
                    } else {
                        throw new InvalidOperationException("CRM endpoint is not configured.");
                    }

                    using HttpClient httpClient = _httpClientFactory.CreateClient(
                        EcommerceInternalHttpClientDefaults.ClientName);
                    await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
                }, "Order invoice product availability sync");
            }

            sale.ClientAgreementId = order.ClientAgreementId;
            sale.OrderId = order.Id;
            sale.IsVatSale = currentCart.IsVatCart;
            sale.ChangedToInvoice = null;

            sale.BaseLifeCycleStatusId =
                _saleRepositoriesFactory
                    .NewBaseLifeCycleStatusRepository(connection)
                    .Add(
                        new BaseLifeCycleStatus {
                            SaleLifeCycleType = SaleLifeCycleType.New
                        }
                    );

            sale.BaseSalePaymentStatusId =
                _saleRepositoriesFactory
                    .NewBaseSalePaymentStatusRepository(connection)
                    .Add(
                        new BaseSalePaymentStatus {
                            SalePaymentStatusType = SalePaymentStatusType.NotPaid
                        }
                    );

            ISaleNumberRepository saleNumberRepository = _saleRepositoriesFactory.NewSaleNumberRepository(connection);

            SaleNumber lastSaleNumber = saleNumberRepository.GetLastRecordByOrganizationNetId(order.ClientAgreement.Agreement.Organization.NetUid);
            SaleNumber saleNumber;

            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            try {
                if (lastSaleNumber != null && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year)) {
                    saleNumber = new SaleNumber {
                        OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                        Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}"
                    };

                    saleNumber.Value +=
                        string.Format("{0:D8}",
                            Convert.ToInt32(
                                lastSaleNumber.Value.Substring(
                                    lastSaleNumber.Organization.Code.Length + currentMonth.Length,
                                    lastSaleNumber.Value.Length - (lastSaleNumber.Organization.Code.Length + currentMonth.Length)))
                            + 1);
                } else {
                    saleNumber = new SaleNumber {
                        OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                        Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                    };
                }
            } catch (FormatException) {
                saleNumber = new SaleNumber {
                    OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                    Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                };
            }

            sale.SaleNumberId = saleNumberRepository.Add(saleNumber);

            if (isWorkplace) sale.WorkplaceId = workplace.Id;

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            if (sale.CustomersOwnTtn != null && !(sale.CustomersOwnTtn.IsNew() && sale.CustomersOwnTtn.IsEmpty()))
                sale.CustomersOwnTtnId = saleRepository.AddCustomersOwnTtn(sale.CustomersOwnTtn);

            sale.Id = saleRepository.Add(sale);

            Sale createdSale = saleRepository.GetByIdWithCalculatedDynamicPrices(sale.Id);

            BackgroundSyncRunner.Run(async cancellationToken => {
                string saleSyncCrmUrl;

                if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                    EcommerceCrmConfig data = ReadEcommerceCrmConfig();
#if DEBUG
                    saleSyncCrmUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#else
                            saleSyncCrmUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#endif
                } else {
                    throw new InvalidOperationException("CRM endpoint is not configured.");
                }

                using HttpClient httpClient = _httpClientFactory.CreateClient(
                    EcommerceInternalHttpClientDefaults.ClientName);
                await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
            }, "Order invoice sale sync");

            createdSale.DeliveryRecipient = sale.DeliveryRecipient;
            createdSale.DeliveryRecipientAddress = sale.DeliveryRecipientAddress;
            createdSale.Transporter = sale.Transporter;

            string crmApiUrl;

            if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                EcommerceCrmConfig data = ReadEcommerceCrmConfig();
#if DEBUG
                crmApiUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#else
                        crmApiUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#endif
            } else {
                throw new InvalidOperationException("CRM endpoint is not configured.");
            }

            string payload = JsonSerializer.Serialize(createdSale);

            QueueEcommerceSaleUpdate(crmApiUrl, payload, "Order invoice sale update");

            return BuildCreatedSaleResponse(createdSale);
    }

    public async Task<string> GenerateNewRetailSale(Sale sale, Guid retailClientNetId, bool fullPayment) {
        if (retailClientNetId == Guid.Empty)
            throw new ArgumentException("A valid retail client is required.");
        if (sale?.Order?.OrderItems == null || sale.Order.OrderItems.Count == 0 || sale.Order.OrderItems.Count > 100)
            throw new ArgumentException("Order must contain between 1 and 100 items.");

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
            IMisplacedSaleRepository misplacedSaleRepository = _saleRepositoriesFactory.NewMisplacedSaleRepository(connection);
            IRetailClientRepository retailClientRepository = _retailClientRepositoriesFactory.NewRetailClientRepository(connection);

            Client clientForRetail = clientRepository.GetRetailClient();
            RetailClient retailClient = retailClientRepository.GetByNetId(retailClientNetId);
            if (retailClient == null)
                throw new ArgumentException("A valid retail client is required.");

            Storage storage = storageRepository
                .GetWithHighestPriority();
            if (storage == null || !storage.OrganizationId.HasValue)
                throw new InvalidOperationException("Retail storage is not configured.");

            List<OrderItem> misplacedOrderItems = new();

            ClientAgreement clientAgreement =
                clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(clientForRetail.NetUid, storage.OrganizationId.Value, storage.ForVatProducts);

            bool withVat = clientAgreement.Agreement.WithVATAccounting;

            Order order = new() {
                OrderSource = OrderSource.Shop,
                OrderStatus = OrderStatus.NewOrderCart,
                ClientAgreement = clientAgreement,
                ClientAgreementId = clientAgreement.Id
            };

            order.Id = _saleRepositoriesFactory
                .NewOrderRepository(connection)
                .Add(order);

            foreach (OrderItem orderItem in sale.Order.OrderItems.Where(i => i.Qty > 0)) {
                ApplyAuthoritativeRetailProduct(connection, storage, withVat, orderItem);

                ProductAvailability productAvailability =
                    productAvailabilityRepository.GetByProductAndStorageIds(orderItem.ProductId, storage.Id);

                if (productAvailability == null || productAvailability.Amount.Equals(0)) {
                    orderItem.IsMisplacedItem = true;
                    misplacedOrderItems.Add(orderItem);
                    continue;
                }

                if (productAvailability.Amount < orderItem.Qty) {
                    misplacedOrderItems.Add(new OrderItem {
                        IsValidForCurrentSale = true,
                        Product = orderItem.Product,
                        ProductId = orderItem.ProductId,
                        IsMisplacedItem = true,
                        Qty = orderItem.Qty - productAvailability.Amount,
                        OverLordQty = orderItem.Qty - productAvailability.Amount,
                        OrderedQty = orderItem.Qty - productAvailability.Amount,
                        PricePerItem = orderItem.PricePerItem,
                        ExchangeRateAmount = orderItem.ExchangeRateAmount,
                        TotalAmount = orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty - productAvailability.Amount),
                        TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty - productAvailability.Amount),
                        Vat = orderItem.Vat
                    });

                    orderItem.Qty = productAvailability.Amount;
                }

                orderItem.ClientShoppingCartId = null;
                orderItem.OrderId = order.Id;
                orderItem.ProductId = orderItem.Product.Id;

                double toDecreaseQty = orderItem.Qty;

                orderItem.Id = orderItemRepository.Add(orderItem);

                if (productAvailability.Amount > 0 && !toDecreaseQty.Equals(0d)) {
                    if (productAvailability.Amount >= toDecreaseQty) {
                        productReservationRepository
                            .Add(new ProductReservation {
                                OrderItemId = orderItem.Id,
                                ProductAvailabilityId = productAvailability.Id,
                                Qty = toDecreaseQty
                            });

                        productAvailability.Amount -= toDecreaseQty;
                    } else {
                        productReservationRepository
                            .Add(new ProductReservation {
                                OrderItemId = orderItem.Id,
                                ProductAvailabilityId = productAvailability.Id,
                                Qty = productAvailability.Amount
                            });

                        productAvailability.Amount = 0d;
                    }

                    productAvailabilityRepository.Update(productAvailability);
                }

                _reindexSignal.Request(orderItem.ProductId);

                BackgroundSyncRunner.Run(async cancellationToken => {
                    string saleSyncCrmUrl;

                    if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                        EcommerceCrmConfig data = ReadEcommerceCrmConfig();

#if DEBUG
                        saleSyncCrmUrl =
                            $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#else
                                saleSyncCrmUrl =
 $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#endif
                    } else {
                        throw new InvalidOperationException("CRM endpoint is not configured.");
                    }

                    using HttpClient httpClient = _httpClientFactory.CreateClient(
                        EcommerceInternalHttpClientDefaults.ClientName);
                    await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
                }, "Retail sale product availability sync");
            }

            sale.ClientAgreementId = order.ClientAgreementId;
            sale.OrderId = order.Id;
            sale.IsVatSale = withVat;
            sale.IsFullPayment = fullPayment;
            sale.ChangedToInvoice = null;
            sale.RetailClientId = retailClient.Id;

            sale.BaseLifeCycleStatusId =
                _saleRepositoriesFactory
                    .NewBaseLifeCycleStatusRepository(connection)
                    .Add(
                        new BaseLifeCycleStatus {
                            SaleLifeCycleType = SaleLifeCycleType.New
                        }
                    );

            sale.BaseSalePaymentStatusId =
                _saleRepositoriesFactory
                    .NewBaseSalePaymentStatusRepository(connection)
                    .Add(
                        new BaseSalePaymentStatus {
                            SalePaymentStatusType = SalePaymentStatusType.NotPaid
                        }
                    );

            ISaleNumberRepository saleNumberRepository = _saleRepositoriesFactory.NewSaleNumberRepository(connection);

            SaleNumber lastSaleNumber = saleNumberRepository.GetLastRecordByOrganizationNetId(order.ClientAgreement.Agreement.Organization.NetUid);
            SaleNumber saleNumber;

            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            try {
                if (lastSaleNumber != null && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year)) {
                    saleNumber = new SaleNumber {
                        OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                        Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}"
                    };

                    saleNumber.Value +=
                        string.Format("{0:D8}",
                            Convert.ToInt32(
                                lastSaleNumber.Value.Substring(
                                    lastSaleNumber.Organization.Code.Length + currentMonth.Length,
                                    lastSaleNumber.Value.Length - (lastSaleNumber.Organization.Code.Length + currentMonth.Length)))
                            + 1);
                } else {
                    saleNumber = new SaleNumber {
                        OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                        Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                    };
                }
            } catch (FormatException) {
                saleNumber = new SaleNumber {
                    OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                    Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                };
            }

            sale.SaleNumberId = saleNumberRepository.Add(saleNumber);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            sale.Id = saleRepository.Add(sale);

            Sale createdSale = saleRepository.GetByIdWithCalculatedDynamicPrices(sale.Id);

            if (misplacedOrderItems.Any()) {
                long misplacedSaleId = misplacedSaleRepository.Add(new MisplacedSale { RetailClientId = retailClient.Id });

                misplacedOrderItems.ForEach(o => {
                    o.MisplacedSaleId = misplacedSaleId;
                    o.ProductId = o.Product.Id;
                });

                orderItemRepository.Add(misplacedOrderItems);

                MisplacedSale misplacedSale = misplacedSaleRepository.GetById(misplacedSaleId);

                misplacedSale.SaleId = sale.Id;
                createdSale.MisplacedSaleId = misplacedSaleId;

                misplacedSaleRepository.Update(misplacedSale);
                saleRepository.Update(createdSale);
            }

            BackgroundSyncRunner.Run(async cancellationToken => {
                string saleSyncCrmUrl;

                if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                    EcommerceCrmConfig data = ReadEcommerceCrmConfig();

#if DEBUG
                    saleSyncCrmUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#else
                            saleSyncCrmUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#endif
                } else {
                    throw new InvalidOperationException("CRM endpoint is not configured.");
                }

                using HttpClient httpClient = _httpClientFactory.CreateClient(
                    EcommerceInternalHttpClientDefaults.ClientName);
                await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
            }, "Retail sale sync");

            createdSale.DeliveryRecipient = sale.DeliveryRecipient;
            createdSale.DeliveryRecipientAddress = sale.DeliveryRecipientAddress;
            createdSale.Transporter = sale.Transporter;

            string crmApiUrl;

            if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                EcommerceCrmConfig data = ReadEcommerceCrmConfig();

#if DEBUG
                //crmApiUrl = $"http://localhost:35981/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
                crmApiUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#else
                        crmApiUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#endif
            } else {
                throw new InvalidOperationException("CRM endpoint is not configured.");
            }

            string payload = JsonSerializer.Serialize(createdSale);


            QueueEcommerceSaleUpdate(crmApiUrl, payload, "Retail sale update");

            // sale = saleRepository.GetByNetId(createdSale.NetUid);

            long statusId = _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection).Add(new RetailPaymentStatus {
                RetailPaymentStatusType = RetailPaymentStatusType.New
            });

            _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection)
                .Add(new RetailClientPaymentImage {
                    RetailClientId = retailClient.Id,
                    SaleId = sale.Id,
                    RetailPaymentStatusId = statusId
                });

            return await _paymentLinkService.GenerateSalePaymentInfoMessage(retailClientNetId, createdSale.NetUid);
    }

    // Old 
    public async Task<string> GenerateNewQuickSaleWithInvoice(Sale sale, Guid retailClientNetId, bool fullPayment) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
            IMisplacedSaleRepository misplacedSaleRepository = _saleRepositoriesFactory.NewMisplacedSaleRepository(connection);
            IRetailClientRepository retailClientRepository = _retailClientRepositoriesFactory.NewRetailClientRepository(connection);

            Client clientForRetail = clientRepository.GetRetailClient();
            RetailClient retailClient = retailClientRepository.GetByNetId(retailClientNetId);

            Storage storage = storageRepository
                .GetWithHighestPriority();

            ClientAgreement clientAgreement =
                clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(clientForRetail.NetUid, storage.OrganizationId.Value, retailClient.EcommerceRegion.IsLocalPayment);

            bool withVat = clientAgreement.Agreement.WithVATAccounting;

            Order order = new() {
                OrderSource = OrderSource.Shop,
                OrderStatus = OrderStatus.NewOrderCart,
                ClientAgreement = clientAgreement
            };

            order.ClientAgreementId = order.ClientAgreement.Id;

            order.Id = _saleRepositoriesFactory
                .NewOrderRepository(connection)
                .Add(order);

            List<Storage> allStorages = storageRepository.GetAllNonDefectiveByCurrentLocale().ToList();

            List<Storage> storages = new(allStorages.Where(s => s.AvailableForReSale));
            storages.AddRange(allStorages.Where(e => e.OrganizationId.Equals(clientAgreement.Agreement.OrganizationId)));

            List<OrderItem> misplacedOrderItems = new();

            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                if (!orderItem.IsMisplacedItem) continue;

                misplacedOrderItems.Add(orderItem);
            }

            sale.Order.OrderItems = sale.Order.OrderItems.Where(o => !o.IsMisplacedItem).ToImmutableHashSet();

            foreach (OrderItem orderItem in sale.Order.OrderItems.Where(i => i.IsNew() && i.Qty > 0)) {
                orderItem.ClientShoppingCartId = null;
                orderItem.OrderId = order.Id;
                orderItem.ProductId = orderItem.Product.Id;

                IEnumerable<ProductAvailability> productAvailabilities;

                if (withVat)
                    productAvailabilities =
                        productAvailabilityRepository
                            .GetByProductAndOrganizationIds(
                                orderItem.ProductId,
                                order.ClientAgreement.Agreement.Organization.Id,
                                true
                            );
                else
                    productAvailabilities = productAvailabilityRepository
                        .GetAllByProductAndStorageIds(
                            orderItem.ProductId,
                            storages.Select(e => e.Id).ToList());

                if (!productAvailabilities.Any()) continue;

                if (productAvailabilities.Sum(a => a.Amount) < orderItem.Qty) orderItem.Qty = productAvailabilities.Sum(a => a.Amount);

                double toDecreaseQty = orderItem.Qty;

                orderItem.Id = orderItemRepository.Add(orderItem);

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

                _reindexSignal.Request(orderItem.ProductId);

                BackgroundSyncRunner.Run(async cancellationToken => {
                    string saleSyncCrmUrl;

                    if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                        EcommerceCrmConfig data = ReadEcommerceCrmConfig();

#if DEBUG
                        saleSyncCrmUrl =
                            $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#else
                                saleSyncCrmUrl =
 $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#endif
                    } else {
                        throw new InvalidOperationException("CRM endpoint is not configured.");
                    }

                    using HttpClient httpClient = _httpClientFactory.CreateClient(
                        EcommerceInternalHttpClientDefaults.ClientName);
                    await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
                }, "Quick sale product availability sync");
            }

            sale.ClientAgreementId = order.ClientAgreementId;
            sale.OrderId = order.Id;
            sale.IsVatSale = withVat;
            sale.IsFullPayment = fullPayment;
            sale.ChangedToInvoice = null;
            sale.RetailClientId = retailClient.Id;

            sale.BaseLifeCycleStatusId =
                _saleRepositoriesFactory
                    .NewBaseLifeCycleStatusRepository(connection)
                    .Add(
                        new BaseLifeCycleStatus {
                            SaleLifeCycleType = SaleLifeCycleType.New
                        }
                    );

            sale.BaseSalePaymentStatusId =
                _saleRepositoriesFactory
                    .NewBaseSalePaymentStatusRepository(connection)
                    .Add(
                        new BaseSalePaymentStatus {
                            SalePaymentStatusType = SalePaymentStatusType.NotPaid
                        }
                    );

            ISaleNumberRepository saleNumberRepository = _saleRepositoriesFactory.NewSaleNumberRepository(connection);

            SaleNumber lastSaleNumber = saleNumberRepository.GetLastRecordByOrganizationNetId(order.ClientAgreement.Agreement.Organization.NetUid);
            SaleNumber saleNumber;

            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            try {
                if (lastSaleNumber != null && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year)) {
                    saleNumber = new SaleNumber {
                        OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                        Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}"
                    };

                    saleNumber.Value +=
                        string.Format("{0:D8}",
                            Convert.ToInt32(
                                lastSaleNumber.Value.Substring(
                                    lastSaleNumber.Organization.Code.Length + currentMonth.Length,
                                    lastSaleNumber.Value.Length - (lastSaleNumber.Organization.Code.Length + currentMonth.Length)))
                            + 1);
                } else {
                    saleNumber = new SaleNumber {
                        OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                        Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                    };
                }
            } catch (FormatException) {
                saleNumber = new SaleNumber {
                    OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                    Value = $"{order.ClientAgreement.Agreement.Organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                };
            }

            sale.SaleNumberId = saleNumberRepository.Add(saleNumber);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            sale.Id = saleRepository.Add(sale);

            Sale createdSale = saleRepository.GetByIdWithCalculatedDynamicPrices(sale.Id);

            if (misplacedOrderItems.Any()) {
                long misplacedSaleId = misplacedSaleRepository.Add(new MisplacedSale { RetailClientId = retailClient.Id });

                misplacedOrderItems.ForEach(o => {
                    o.MisplacedSaleId = misplacedSaleId;
                    o.ProductId = o.Product.Id;
                });

                orderItemRepository.Add(misplacedOrderItems);

                MisplacedSale misplacedSale = misplacedSaleRepository.GetById(misplacedSaleId);

                misplacedSale.SaleId = sale.Id;
                createdSale.MisplacedSaleId = misplacedSaleId;

                misplacedSaleRepository.Update(misplacedSale);
                saleRepository.Update(createdSale);
            }

            BackgroundSyncRunner.Run(async cancellationToken => {
                string saleSyncCrmUrl;

                if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                    EcommerceCrmConfig data = ReadEcommerceCrmConfig();

#if DEBUG
                    saleSyncCrmUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#else
                            saleSyncCrmUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#endif
                } else {
                    throw new InvalidOperationException("CRM endpoint is not configured.");
                }

                using HttpClient httpClient = _httpClientFactory.CreateClient(
                    EcommerceInternalHttpClientDefaults.ClientName);
                await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
            }, "Quick sale sync");

            createdSale.DeliveryRecipient = sale.DeliveryRecipient;
            createdSale.DeliveryRecipientAddress = sale.DeliveryRecipientAddress;
            createdSale.Transporter = sale.Transporter;

            string crmApiUrl;

            if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                EcommerceCrmConfig data = ReadEcommerceCrmConfig();

#if DEBUG
                //crmApiUrl = $"http://localhost:35981/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
                crmApiUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#else
                        crmApiUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#endif
            } else {
                throw new InvalidOperationException("CRM endpoint is not configured.");
            }

            string payload = JsonSerializer.Serialize(createdSale);


            QueueEcommerceSaleUpdate(crmApiUrl, payload, "Quick sale update");

            // sale = saleRepository.GetByNetId(createdSale.NetUid);

            long statusId = _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection).Add(new RetailPaymentStatus {
                RetailPaymentStatusType = RetailPaymentStatusType.New
            });

            _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection)
                .Add(new RetailClientPaymentImage {
                    RetailClientId = retailClient.Id,
                    SaleId = sale.Id,
                    RetailPaymentStatusId = statusId
                });

            return await _paymentLinkService.GenerateSalePaymentInfoMessage(retailClientNetId, createdSale.NetUid);
    }

    public Task<List<OrderItem>> RemoveUnavailableProducts(List<OrderItem> orderItems, long retailClientId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

            Storage storage = storageRepository
                .GetWithHighestPriority();
            if (storage == null || !storage.OrganizationId.HasValue)
                throw new InvalidOperationException("Retail storage is not configured.");

            _ = retailClientId;

            foreach (OrderItem orderItem in orderItems) {
                ApplyAuthoritativeRetailProduct(connection, storage, storage.ForVatProducts, orderItem);

                ProductAvailability productAvailability = productAvailabilityRepository.GetByProductAndStorageIds(orderItem.ProductId, storage.Id);

                if (productAvailability == null || productAvailability.Amount.Equals(0))
                    orderItem.IsMisplacedItem = true;
            }

            return Task.FromResult(orderItems);
    }

    public async Task SendPaymentImageToCrm(Guid saleNetId, Guid clientNetId, PaymentConfirmationImageModel paymentImage) {
        if (saleNetId == Guid.Empty || clientNetId == Guid.Empty)
            throw new ArgumentException("A valid checkout is required.");

        using (IDbConnection authorizationConnection = _connectionFactory.NewSqlConnection()) {
            Sale sale = _saleRepositoriesFactory.NewSaleRepository(authorizationConnection).GetByNetId(saleNetId);
            if (sale?.RetailClient?.NetUid != clientNetId)
                throw new ArgumentException("A valid checkout is required.");
        }

        UriBuilder crmApiUrl;

            // URI

            if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                EcommerceCrmConfig data = ReadEcommerceCrmConfig();
#if DEBUG
                //crmApiUrl = new UriBuilder($"http://localhost:35981/api/v1/{CultureInfo.CurrentCulture}/sales/payment/save");
                crmApiUrl = new UriBuilder($"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/payment/save");
#else
                        crmApiUrl = new UriBuilder($"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/payment/save");
#endif
            } else {
                throw new InvalidOperationException("CRM endpoint is not configured.");
            }

            crmApiUrl.Query = $"saleNetId={saleNetId}&clientNetId={clientNetId}";

            string payload = JsonSerializer.Serialize(paymentImage);

        using HttpClient httpClient = _httpClientFactory.CreateClient(
            EcommerceInternalHttpClientDefaults.ClientName);
        using HttpRequestMessage requestMessage = new(HttpMethod.Post, crmApiUrl.Uri) {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        using HttpResponseMessage responseMessage =
            await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

        if (!responseMessage.IsSuccessStatusCode) {
            string responseContent = await responseMessage.Content.ReadAsStringAsync();
            string responseErrorMessage = ExtractErrorMessage(responseContent);
            throw new Exception(responseErrorMessage);
        }
    }

    public Task<SaleStatistic> GetSaleByNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        Sale saleFromDb = saleRepository.GetByNetId(netId);

        CalculatePricingsForSaleWithDynamicPrices(saleFromDb, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
            _currencyRepositoriesFactory.NewCurrencyRepository(connection));

        dynamic[] toReturnData = new dynamic[LifeCycleLineStatuses.STATUSES.Length];

        FormLifeCycleLine(saleRepository, saleFromDb.NetUid, toReturnData);

        List<SaleExchangeRate> saleExchangeRates = _saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).GetAllBySaleNetId(saleFromDb.NetUid);

        SaleStatistic saleInfo = new() {
            Sale = saleFromDb,
            LifeCycleLine = toReturnData.ToList(),
            SaleExchangeRates = saleExchangeRates
        };

        return Task.FromResult(saleInfo);
    }

    private static EcommerceCrmConfig ReadEcommerceCrmConfig() {
        try {
            string configPath = NoltFolderManager.GetEcommerceCrmConfigJsonFilePath();
            if (!File.Exists(configPath)) {
                return new EcommerceCrmConfig();
            }

            return JsonSerializer.Deserialize<EcommerceCrmConfig>(File.ReadAllText(configPath), _jsonSerializerOptions)
                   ?? new EcommerceCrmConfig();
        } catch {
            return new EcommerceCrmConfig();
        }
    }

    private static string ExtractErrorMessage(string responseContent) {
        try {
            using JsonDocument document = JsonDocument.Parse(responseContent);
            if (document.RootElement.TryGetProperty("Message", out JsonElement messageElement)) {
                return messageElement.GetString() ?? "Request failed";
            }

            if (document.RootElement.TryGetProperty("message", out messageElement)) {
                return messageElement.GetString() ?? "Request failed";
            }
        } catch {
            // ignore parse errors and fallback to default message
        }

        return "Request failed";
    }

    private static void CalculatePricingsForSaleWithDynamicPrices(
        Sale sale,
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        decimal vatRate = Convert.ToDecimal(sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;

        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        decimal currentExchangeRateEurToUah = exchangeRateRepository.GetExchangeRateToEuroCurrency(uah);

        if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;

                orderItem.TotalAmountEurToUah = orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty);

                orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountEurToUah = decimal.Round(orderItem.TotalAmountEurToUah, 14, MidpointRounding.AwayFromZero);

                if (sale.IsVatSale)
                    orderItem.TotalVat =
                        decimal.Round(
                            orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);
                ;
            }
        else
            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal =
                    decimal.Round(
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero)
                        * orderItem.ExchangeRateAmount,
                        14,
                        MidpointRounding.AwayFromZero
                    );

                orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;
                orderItem.TotalAmountEurToUah = decimal.Round(orderItem.TotalAmountEurToUah, 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountEurToUah = orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty);

                if (sale.IsVatSale)
                    orderItem.TotalVat =
                        decimal.Round(
                            orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.PricePerItem, 14, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);
            }

        sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 14, MidpointRounding.AwayFromZero);
        sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 14, MidpointRounding.AwayFromZero);
        sale.Order.TotalAmountEurToUah = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountEurToUah), 14, MidpointRounding.AwayFromZero);
        sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

        if (sale.IsVatSale)
            sale.Order.TotalVat = sale.Order.TotalAmountLocal * (vatRate / (vatRate + 1));

        if (sale.SaleInvoiceDocument != null) {
            sale.SaleInvoiceDocument.ShippingAmount =
                decimal.Round(
                    sale.SaleInvoiceDocument.ShippingAmountEur * sale.SaleInvoiceDocument.ExchangeRateAmount,
                    14,
                    MidpointRounding.AwayFromZero
                );
            sale.SaleInvoiceDocument.ShippingAmountWithoutVat =
                decimal.Round(
                    sale.SaleInvoiceDocument.ShippingAmountEurWithoutVat * sale.SaleInvoiceDocument.ExchangeRateAmount,
                    14,
                    MidpointRounding.AwayFromZero
                );

            sale.SaleInvoiceDocument.ShippingAmountEur = decimal.Round(sale.SaleInvoiceDocument.ShippingAmountEur, 14, MidpointRounding.AwayFromZero);

            sale.Order.TotalAmount =
                decimal.Round(sale.Order.TotalAmount + sale.SaleInvoiceDocument.ShippingAmountEur, 14, MidpointRounding.AwayFromZero);
            sale.Order.TotalAmountLocal =
                decimal.Round(sale.Order.TotalAmountLocal + sale.SaleInvoiceDocument.ShippingAmount, 14, MidpointRounding.AwayFromZero);
        }

        sale.TotalAmount = sale.Order.TotalAmount;
        sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
        sale.TotalCount = sale.Order.TotalCount;
        sale.TotalAmountEurToUah = sale.Order.TotalAmountEurToUah;
    }

    private static void FormLifeCycleLine(
        ISaleRepository saleRepository,
        Guid saleNetId,
        IList<dynamic> toReturnData) {
        List<dynamic> lifeCycleLine = saleRepository.GetSaleLifeCycleLine(saleNetId);

        for (int index = 0; index < LifeCycleLineStatuses.STATUSES.Length; index++) {
            dynamic result = new ExpandoObject();

            result.Name = nameof(SaleLifeCycleType);
            result.Value = LifeCycleLineStatuses.STATUSES[index];

            if (lifeCycleLine != null && lifeCycleLine.Any()) {
                if (lifeCycleLine.First()?.Value != null && lifeCycleLine.Any(i => i.Value.Equals(LifeCycleLineStatuses.STATUSES[index]))) {
                    dynamic fromList = lifeCycleLine.First(i => i.Value.Equals(LifeCycleLineStatuses.STATUSES[index]));

                    result.Updated = fromList.Updated;
                    result.IsActive = true;
                }
            } else {
                result.Updated = null;
                result.IsActive = false;
            }

            toReturnData[index] = result;
        }
    }
}
