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

    public OfferService(
        IClientRepositoriesFactory clientRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IHttpClientFactory httpClientFactory) {
        _clientRepositoriesFactory = clientRepositoriesFactory;

        _saleRepositoriesFactory = saleRepositoriesFactory;

        _productRepositoriesFactory = productRepositoriesFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        _connectionFactory = connectionFactory;
        _httpClientFactory = httpClientFactory;
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

    public Task<Sale> GenerateNewOrderAndSaleFromOffer(ClientShoppingCart clientShoppingCart, Guid clientNetId, bool addCurrentCartItems) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientShoppingCartRepository shoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

            ClientShoppingCart offer = shoppingCartRepository.GetByNetId(clientShoppingCart.NetUid);

            if (offer == null)
                throw new Exception(OfferResourceNames.OFFER_NOT_EXISTS);
            if (offer.IsOfferProcessed)
                throw new Exception(OfferResourceNames.OFFER_PROCESSED);

            shoppingCartRepository.SetProcessedByNetId(clientShoppingCart.NetUid);

            Order order = new() {
                OrderSource = OrderSource.Offer,
                OrderStatus = OrderStatus.NewOrderCart,
                UserId = offer.CreatedById ?? _userRepositoriesFactory.NewUserRepository(connection).GetManagerOrGBAIdByClientNetId(clientNetId),
                ClientAgreement = _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetActiveByRootClientNetId(clientNetId, offer.IsVatCart),
                ClientShoppingCartId = offer.Id
            };

            order.ClientAgreementId = order.ClientAgreement.Id;

            order.Id = _saleRepositoriesFactory
                .NewOrderRepository(connection)
                .Add(order);

            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            if (clientShoppingCart.OrderItems.Any()) {
                List<OrderItem> offeredOrderItems = new();

                foreach (OrderItem orderItem in clientShoppingCart.OrderItems) {
                    orderItem.ClientShoppingCartId = null;
                    orderItem.OrderId = order.Id;
                    orderItem.ProductId = orderItem.Product.Id;
                    orderItem.UserId = order.UserId;

                    IEnumerable<ProductAvailability> productAvailabilities =
                        productAvailabilityRepository
                            .GetByProductAndOrganizationIds(
                                orderItem.ProductId,
                                order.ClientAgreement.Agreement.Organization.Id,
                                offer.IsVatCart
                            );

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
                                saleSyncCrmUrl =
                                    $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
                            }

                            using HttpClient httpClient = _httpClientFactory.CreateClient();
                            await httpClient.GetAsync(saleSyncCrmUrl, cancellationToken);
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

            if (addCurrentCartItems) {
                ClientShoppingCart currentCart = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection).GetByClientNetId(clientNetId, offer.IsVatCart);

                if (currentCart != null && currentCart.OrderItems.Any())
                    orderItemRepository
                        .Update(
                            currentCart
                                .OrderItems
                                .Select(item => {
                                    item.ClientShoppingCartId = null;
                                    item.OrderId = order.Id;

                                    return item;
                                })
                        );
            }

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

            ISaleNumberRepository saleNumberRepository = _saleRepositoriesFactory.NewSaleNumberRepository(connection);

            SaleNumber lastSaleNumber = saleNumberRepository.GetLastRecordByOrganizationNetId(order.ClientAgreement.Agreement.Organization.NetUid);
            SaleNumber saleNumber;

            if (lastSaleNumber != null && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year))
                saleNumber = new SaleNumber {
                    OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                    Value =
                        $"{order.ClientAgreement.Agreement.Organization.Code}{string.Format("{0:D10}", Convert.ToInt32(lastSaleNumber.Value.Substring(order.ClientAgreement.Agreement.Organization.Code.Length, 10)) + 1)}"
                };
            else
                saleNumber = new SaleNumber {
                    OrganizationId = order.ClientAgreement.Agreement.Organization.Id,
                    Value = $"{order.ClientAgreement.Agreement.Organization.Code}{string.Format("{0:D10}", 1)}"
                };

            sale.SaleNumberId = saleNumberRepository.Add(saleNumber);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            sale.Id = saleRepository.Add(sale);

            sale = saleRepository.GetById(sale.Id);

            BackgroundSyncRunner.Run(async cancellationToken => {
                string saleSyncCrmUrl;

                if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                    EcommerceCrmConfig data = JsonSerializer.Deserialize<EcommerceCrmConfig>(
                        File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()),
                        _jsonSerializerOptions);

                    saleSyncCrmUrl = $"{data?.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={sale.NetUid.ToString()}";
                } else {
                    saleSyncCrmUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={sale.NetUid.ToString()}";
                }

                using HttpClient httpClient = _httpClientFactory.CreateClient();
                await httpClient.GetAsync(saleSyncCrmUrl, cancellationToken);
            }, "Offer sale sync");

            return Task.FromResult(sale);
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
