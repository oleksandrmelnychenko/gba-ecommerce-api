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
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames.ECommerce;
using GBA.Common.Search;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Infrastructure;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.Extensions.Http;
using GBA.Common.Models;

namespace GBA.Services.Services.Clients;

public sealed class ClientShoppingCartService : IClientShoppingCartService {
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private static OrderItem NormalizeOverLordQty(OrderItem orderItem) {
        if (orderItem.OverLordQty <= 0 && orderItem.Qty > 0) orderItem.OverLordQty = orderItem.Qty;

        return orderItem;
    }

    private decimal ResolveLiveProductExchangeRate(
        IDbConnection connection,
        Product product,
        ClientAgreement clientAgreement) {
        long currencyId = clientAgreement?.Agreement?.CurrencyId ??
            throw new InvalidOperationException("Client agreement currency is not configured.");
        decimal exchangeRateAmount = _exchangeRateRepositoriesFactory
            .NewExchangeRateRepository(connection)
            .GetEuroExchangeRateByCurrentCultureFiltered(
                product.NetUid,
                clientAgreement.Agreement.WithVATAccounting,
                false,
                currencyId);

        if (exchangeRateAmount <= 0m)
            throw new InvalidOperationException(
                $"A valid exchange rate is not configured for product {product.NetUid} and currency {currencyId}.");

        return exchangeRateAmount;
    }

    private OrderItem ApplyAuthoritativeProduct(
        IDbConnection connection,
        ClientAgreement clientAgreement,
        OrderItem orderItem) {
        if (orderItem == null || !double.IsFinite(orderItem.Qty) ||
            orderItem.Qty <= 0 || orderItem.Qty > 100000)
            throw new ArgumentException("Order item quantity is invalid.");

        IGetSingleProductRepository productRepository =
            _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        Guid productNetId = orderItem.Product?.NetUid ?? Guid.Empty;
        if (productNetId == Guid.Empty && orderItem.ProductId > 0)
            productNetId = productRepository.GetById(orderItem.ProductId)?.NetUid ?? Guid.Empty;
        if (productNetId == Guid.Empty)
            throw new ArgumentException("A valid product is required.");

        Product product = productRepository.GetProductByNetId(
            productNetId,
            clientAgreement.NetUid,
            clientAgreement.Agreement.WithVATAccounting,
            clientAgreement.Agreement.CurrencyId,
            clientAgreement.Agreement.OrganizationId);
        if (!EcommercePurchasability.IsPurchasable(product))
            throw new ArgumentException(EcommercePurchasability.NotAvailableMessage);
        if (!EcommercePurchasability.HasSellablePrice(product))
            throw new ArgumentException(EcommercePurchasability.NotPricedMessage);

        decimal exchangeRateAmount = ResolveLiveProductExchangeRate(
            connection,
            product,
            clientAgreement);
        product.CurrentLocalPrice = decimal.Round(
            product.CurrentPrice * exchangeRateAmount, 2, MidpointRounding.AwayFromZero);

        orderItem.Product = product;
        orderItem.ProductId = product.Id;
        orderItem.PricePerItem = product.CurrentPrice;
        orderItem.ExchangeRateAmount = exchangeRateAmount;
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
        orderItem.IsValidForCurrentSale = true;
        orderItem.Vat = clientAgreement.Agreement.WithVATAccounting
            ? Convert.ToDecimal(clientAgreement.Agreement.Organization?.VatRate?.Value ?? 0)
            : 0;

        return orderItem;
    }

    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISearchReindexSignal _reindexSignal;

    public ClientShoppingCartService(
        IClientRepositoriesFactory clientRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory,
        IHttpClientFactory httpClientFactory,
        ISearchReindexSignal reindexSignal) {
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _reindexSignal = reindexSignal;

        _saleRepositoriesFactory = saleRepositoriesFactory;

        _productRepositoriesFactory = productRepositoriesFactory;

        _storageRepositoryFactory = storageRepositoryFactory;

        _pricingRepositoriesFactory = pricingRepositoriesFactory;

        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        _connectionFactory = connectionFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;
        _httpClientFactory = httpClientFactory;
    }

    public Task<OrderItem> Add(OrderItem orderItem, Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (orderItem == null)
                throw new Exception("OrderItem can not be empty.");
            if (!orderItem.IsNew())
                throw new Exception("Existing OrderItem is not valid input for current request.");
            if (orderItem.ProductId.Equals(0) && orderItem.Product == null)
                throw new Exception("Product need to be specified.");
            if (orderItem.Qty.Equals(0)) throw new Exception("You need to specify Qty of product that will be added.");

            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            IWorkplaceRepository workplaceRepository = _clientRepositoriesFactory.NewWorkplaceRepository(connection);

            Workplace workplace = null;

            ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(clientNetId);

            if (clientAgreement == null) {
                workplace = workplaceRepository.GetByNetId(clientNetId);
                clientAgreement = clientAgreementRepository.GetSelectedByWorkplaceNetId(workplace.NetUid);
            }

            ApplyAuthoritativeProduct(connection, clientAgreement, orderItem);
            _reindexSignal.Request(orderItem.ProductId);

            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            IEnumerable<ProductAvailability> productAvailabilities;

            if (clientAgreement.Agreement.WithVATAccounting) {
                productAvailabilities =
                    productAvailabilityRepository
                        .GetByProductAndOrganizationIds(
                            orderItem.ProductId,
                            clientAgreement.Agreement.Organization.Id,
                            clientAgreement.Agreement.WithVATAccounting
                        );
            } else {
                List<Storage> allStorages = _storageRepositoryFactory.NewStorageRepository(connection).GetAllNonDefectiveByCurrentLocale().ToList();

                List<Storage> storages = new(allStorages.Where(s => s.AvailableForReSale));
                storages.AddRange(allStorages.Where(e => e.OrganizationId.Equals(clientAgreement.Agreement.OrganizationId)));

                productAvailabilities = productAvailabilityRepository
                    .GetAllByProductAndStorageIds(
                        orderItem.ProductId,
                        storages.Select(e => e.Id).ToList());
            }

            if (!productAvailabilities.Any()) throw new Exception("Product is not available");

            double qtyRemainderProducts = productAvailabilities.Sum(a => a.Amount);

            if (qtyRemainderProducts < orderItem.Qty)
                throw new LocalizedException(
                    ClientShoppingCartResourceNames.INSUFFICIENT_QTY_OF_PRODUCTS,
                    qtyRemainderProducts
                );

            IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

            ClientShoppingCart clientShoppingCart =
                clientShoppingCartRepository.GetByClientAgreementNetId(clientAgreement.NetUid, clientAgreement.Agreement.WithVATAccounting, workplace?.Id);

            Client client =
                _clientRepositoriesFactory
                    .NewClientRepository(connection)
                    .GetByIdWithRegionCode(clientAgreement.ClientId);

            if (clientShoppingCart != null) {
                if (clientShoppingCart.OrderItems.Any(i => i.ProductId.Equals(orderItem.ProductId))) {
                    OrderItem existingOrderItem = clientShoppingCart.OrderItems.First(i => i.ProductId.Equals(orderItem.ProductId));

                    return Task.FromResult(
                        UpdateExistingOrderItemInShoppingCart(
                            orderItem,
                            connection,
                            productAvailabilityRepository,
                            _productRepositoriesFactory.NewProductReservationRepository(connection),
                            productAvailabilities,
                            existingOrderItem,
                            clientAgreement.Agreement.CurrencyId,
                            clientAgreement.NetUid
                        )
                    );
                }

                orderItem.ClientShoppingCartId = clientShoppingCart.Id;

                return Task.FromResult(
                    AddNewItemToShoppingCart(
                        orderItem,
                        connection,
                        productAvailabilityRepository,
                        _productRepositoriesFactory.NewProductReservationRepository(connection),
                        productAvailabilities,
                        clientAgreement?.Agreement.CurrencyId,
                        clientAgreement?.NetUid
                    )
                );
            }

            clientShoppingCart = new ClientShoppingCart {
                ValidUntil = DateTime.Now.Date.AddDays(client.ClearCartAfterDays),
                ClientAgreementId = clientAgreement.Id,
                IsVatCart = clientAgreement.Agreement.WithVATAccounting,
                WorkplaceId = workplace?.Id
            };

            clientShoppingCart.Id = clientShoppingCartRepository.Add(clientShoppingCart);

            orderItem.ClientShoppingCartId = clientShoppingCart.Id;

            return Task.FromResult(
                AddNewItemToShoppingCart(
                    orderItem,
                    connection,
                    productAvailabilityRepository,
                    _productRepositoriesFactory.NewProductReservationRepository(connection),
                    productAvailabilities,
                    clientAgreement?.Agreement.CurrencyId,
                    clientAgreement?.NetUid
                )
            );
    }

    public Task<List<OrderItem>> Add(List<OrderItem> orderItems, Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            for (int item = 0; item < orderItems.Count; item++) {
                if (orderItems[item] == null) continue;
                if (!orderItems[item].IsNew()) continue;
                if (orderItems[item].ProductId.Equals(0) && orderItems[item].Product == null) continue;
                if (orderItems[item].Qty.Equals(0)) continue;

                IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

                ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, false);
                ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, true);
                ClientAgreement targetAgreement = withVat ? vatAgreement : nonVatAgreement;
                ApplyAuthoritativeProduct(connection, targetAgreement, orderItems[item]);
                _reindexSignal.Request(orderItems[item].ProductId);

                IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

                IEnumerable<ProductAvailability> productAvailabilities =
                    withVat
                        ? productAvailabilityRepository
                            .GetByProductAndOrganizationIds(
                                orderItems[item].ProductId,
                                vatAgreement.Agreement.Organization.Id,
                                true
                            )
                        : productAvailabilityRepository
                            .GetByProductAndOrganizationIds(
                                orderItems[item].ProductId,
                                nonVatAgreement.Agreement.Organization.Id,
                                false
                            );

                if (!productAvailabilities.Any()) continue;

                if (productAvailabilities.Sum(a => a.Amount) < orderItems[item].Qty)
                    orderItems[item].Qty = productAvailabilities.Sum(a => a.Amount);

                IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

                ClientShoppingCart clientShoppingCart = clientShoppingCartRepository.GetByClientNetId(clientNetId, withVat);

                Client client =
                    _clientRepositoriesFactory
                        .NewClientRepository(connection)
                        .GetByIdWithRegionCode(
                            withVat
                                ? vatAgreement.ClientId
                                : nonVatAgreement.ClientId
                        );

                if (clientShoppingCart != null) {
                    if (clientShoppingCart.OrderItems.Any(i => i.ProductId.Equals(orderItems[item].ProductId))) {
                        OrderItem existingOrderItem = clientShoppingCart.OrderItems.First(i => i.ProductId.Equals(orderItems[item].ProductId));

                        orderItems[item] = UpdateExistingOrderItemInShoppingCart(
                            orderItems[item],
                            connection,
                            productAvailabilityRepository,
                            _productRepositoriesFactory.NewProductReservationRepository(connection),
                            productAvailabilities,
                            existingOrderItem,
                            null,
                            nonVatAgreement?.NetUid,
                            vatAgreement?.NetUid
                        );

                        continue;
                    }

                    orderItems[item].ClientShoppingCartId = clientShoppingCart.Id;

                    orderItems[item] = AddNewItemToShoppingCart(
                        orderItems[item],
                        connection,
                        productAvailabilityRepository,
                        _productRepositoriesFactory.NewProductReservationRepository(connection),
                        productAvailabilities,
                        null,
                        nonVatAgreement?.NetUid,
                        vatAgreement?.NetUid
                    );

                    continue;
                }

                clientShoppingCart = new ClientShoppingCart {
                    ValidUntil = DateTime.Now.Date.AddDays(client.ClearCartAfterDays),
                    ClientAgreementId = withVat ? vatAgreement.Id : nonVatAgreement.Id,
                    IsVatCart = withVat
                };

                clientShoppingCart.Id = clientShoppingCartRepository.Add(clientShoppingCart);

                orderItems[item].ClientShoppingCartId = clientShoppingCart.Id;

                orderItems[item] = AddNewItemToShoppingCart(
                    orderItems[item],
                    connection,
                    productAvailabilityRepository,
                    _productRepositoriesFactory.NewProductReservationRepository(connection),
                    productAvailabilities,
                    null,
                    nonVatAgreement?.NetUid,
                    vatAgreement?.NetUid
                );
            }

            return Task.FromResult(orderItems.Select(NormalizeOverLordQty).ToList());
    }

    public Task<OrderItem> Update(OrderItem orderItem, Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            ClientAgreement selectedAgreement = clientAgreementRepository.GetSelectedByClientNetId(clientNetId);

            if (selectedAgreement == null) {
                IWorkplaceRepository workplaceRepository = _clientRepositoriesFactory.NewWorkplaceRepository(connection);

                Workplace workplace = workplaceRepository.GetByNetId(clientNetId);
                selectedAgreement = clientAgreementRepository.GetSelectedByWorkplaceNetId(workplace.NetUid);
            }

            if (orderItem == null)
                throw new Exception("OrderItem can not be empty.");
            if (orderItem.IsNew())
                throw new Exception("New OrderItem is not valid input for current request.");
            if (!double.IsFinite(orderItem.Qty) || orderItem.Qty > 100000 ||
                !double.IsFinite(orderItem.OverLordQty) || orderItem.OverLordQty > 100000)
                throw new ArgumentException("Order item quantity is invalid.");

            Workplace currentWorkplace = _clientRepositoriesFactory
                .NewWorkplaceRepository(connection)
                .GetByNetId(clientNetId);
            ClientShoppingCart authorizedCart = _clientRepositoriesFactory
                .NewClientShoppingCartRepository(connection)
                .GetByClientAgreementNetId(
                    selectedAgreement.NetUid,
                    selectedAgreement.Agreement.WithVATAccounting,
                    currentWorkplace?.Id);
            OrderItem authorizedOrderItem = authorizedCart?.OrderItems
                .SingleOrDefault(item => item.Id == orderItem.Id);
            if (authorizedOrderItem == null)
                throw new ArgumentException("Order item is not part of the current shopping cart.");

            orderItem.ProductId = authorizedOrderItem.ProductId;
            orderItem.Product = authorizedOrderItem.Product;
            if (orderItem.OverLordQty <= 0) orderItem.OverLordQty = orderItem.Qty;

            if (orderItem.Qty <= 0)
                return Task.FromResult(NormalizeOverLordQty(orderItemRepository.GetByIdWithIncludes(orderItem.Id, selectedAgreement.NetUid)));

            orderItemRepository.UpdateOverLoadQty(orderItem);

            _reindexSignal.Request(orderItem.ProductId);

            OrderItem orderItemFromDb = orderItemRepository.GetById(orderItem.Id);

            if (orderItem.Qty.Equals(orderItemFromDb.Qty))
                return Task.FromResult(NormalizeOverLordQty(orderItemRepository.GetByIdWithIncludes(orderItem.Id, selectedAgreement.NetUid)));

            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

            IEnumerable<ProductAvailability> productAvailabilities;

            if (selectedAgreement.Agreement.WithVATAccounting) {
                productAvailabilities =
                    productAvailabilityRepository
                        .GetByProductAndOrganizationIds(
                            orderItem.ProductId,
                            selectedAgreement.Agreement.Organization.Id,
                            selectedAgreement.Agreement.WithVATAccounting
                        );
            } else {
                List<Storage> allStorages = _storageRepositoryFactory.NewStorageRepository(connection).GetAllNonDefectiveByCurrentLocale().ToList();

                List<Storage> storages = new(allStorages.Where(s => s.AvailableForReSale));
                storages.AddRange(allStorages.Where(e => e.OrganizationId.Equals(selectedAgreement.Agreement.OrganizationId)));

                productAvailabilities = productAvailabilityRepository
                    .GetAllByProductAndStorageIds(
                        orderItem.ProductId,
                        storages.Select(e => e.Id).ToList());
            }

            double qtyDifference = orderItemFromDb.Qty - orderItem.Qty;

            if (qtyDifference < 0) {
                if (productAvailabilities.Sum(a => a.Amount) < Math.Abs(qtyDifference)) {
                    orderItem.Qty -= Math.Abs(qtyDifference) - productAvailabilities.Sum(a => a.Amount);

                    qtyDifference = 0 - productAvailabilities.Sum(a => a.Amount);
                }

                double toDecreaseQty = Math.Abs(qtyDifference);

                foreach (ProductAvailability productAvailability in productAvailabilities.Where(a => a.Amount > 0)) {
                    if (toDecreaseQty.Equals(0d)) break;

                    ProductReservation productReservation =
                        productReservationRepository
                            .GetByOrderItemAndProductAvailabilityIds(
                                orderItemFromDb.Id,
                                productAvailability.Id
                            );

                    if (productAvailability.Amount >= toDecreaseQty) {
                        if (productReservation != null) {
                            productReservation.Qty += toDecreaseQty;

                            productReservationRepository.Update(productReservation);
                        } else {
                            productReservationRepository
                                .Add(new ProductReservation {
                                    OrderItemId = orderItemFromDb.Id,
                                    ProductAvailabilityId = productAvailability.Id,
                                    Qty = toDecreaseQty
                                });
                        }

                        productAvailability.Amount -= toDecreaseQty;

                        toDecreaseQty = 0d;
                    } else {
                        if (productReservation != null) {
                            productReservation.Qty += productAvailability.Amount;

                            productReservationRepository.Update(productReservation);
                        } else {
                            productReservationRepository
                                .Add(new ProductReservation {
                                    OrderItemId = orderItemFromDb.Id,
                                    ProductAvailabilityId = productAvailability.Id,
                                    Qty = productAvailability.Amount
                                });
                        }

                        toDecreaseQty -= productAvailability.Amount;

                        productAvailability.Amount = 0d;
                    }

                    productAvailabilityRepository.Update(productAvailability);
                }
            } else {
                IEnumerable<ProductReservation> reservations =
                    productReservationRepository
                        .GetAllByOrderItemIdWithAvailability(
                            orderItemFromDb.Id
                        );

                double toRestoreAmount = qtyDifference;

                foreach (ProductReservation reservation in reservations) {
                    if (toRestoreAmount.Equals(0d)) break;

                    if (reservation.Qty >= toRestoreAmount) {
                        reservation.Qty -= toRestoreAmount;

                        reservation.ProductAvailability.Amount += toRestoreAmount;

                        toRestoreAmount = 0d;

                        if (reservation.Qty > 0)
                            productReservationRepository.Update(reservation);
                        else
                            productReservationRepository.Delete(reservation.NetUid);
                    } else {
                        toRestoreAmount -= reservation.Qty;

                        reservation.ProductAvailability.Amount += reservation.Qty;

                        productReservationRepository.Delete(reservation.NetUid);
                    }

                    productAvailabilityRepository.Update(reservation.ProductAvailability);
                }
            }

            orderItemRepository.UpdateQty(orderItem);
            orderItemRepository.UpdateOverLoadQty(orderItem);

            return Task.FromResult(NormalizeOverLordQty(orderItemRepository.GetByIdWithIncludes(orderItem.Id, selectedAgreement.NetUid)));
    }

    public Task<List<OrderItem>> Update(List<OrderItem> orderItems, Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            for (int item = 0; item < orderItems.Count; item++) {
                IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
                IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

                ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, false);
                ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, true);
                ClientAgreement targetAgreement = withVat ? vatAgreement : nonVatAgreement;
                ClientShoppingCart authorizedCart = _clientRepositoriesFactory
                    .NewClientShoppingCartRepository(connection)
                    .GetByClientAgreementNetId(targetAgreement.NetUid, withVat);

                if (orderItems[item] == null) continue;
                if (orderItems[item].IsNew()) continue;
                if (!double.IsFinite(orderItems[item].Qty) || orderItems[item].Qty > 100000)
                    throw new ArgumentException("Order item quantity is invalid.");
                OrderItem authorizedOrderItem = authorizedCart?.OrderItems
                    .SingleOrDefault(cartItem => cartItem.Id == orderItems[item].Id);
                if (authorizedOrderItem == null)
                    throw new ArgumentException("Order item is not part of the current shopping cart.");
                orderItems[item].ProductId = authorizedOrderItem.ProductId;
                orderItems[item].Product = authorizedOrderItem.Product;
                if (orderItems[item].Qty <= 0) {
                    orderItems[item] = NormalizeOverLordQty(orderItemRepository.GetByIdWithIncludes(orderItems[item].Id, nonVatAgreement?.NetUid, vatAgreement?.NetUid));

                    continue;
                }

                OrderItem orderItemFromDb = orderItemRepository.GetById(orderItems[item].Id);

                _reindexSignal.Request(orderItems[item].ProductId);

                if (orderItems[item].Qty.Equals(orderItemFromDb.Qty)) {
                    orderItems[item] = NormalizeOverLordQty(orderItemRepository.GetByIdWithIncludes(orderItems[item].Id, nonVatAgreement?.NetUid, vatAgreement?.NetUid));

                    continue;
                }

                IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
                IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

                IEnumerable<ProductAvailability> productAvailabilities =
                    withVat
                        ? productAvailabilityRepository
                            .GetByProductAndOrganizationIds(
                                orderItemFromDb.ProductId,
                                vatAgreement.Agreement.Organization.Id,
                                true
                            )
                        : productAvailabilityRepository
                            .GetByProductAndOrganizationIds(
                                orderItemFromDb.ProductId,
                                nonVatAgreement.Agreement.Organization.Id,
                                false
                            );

                double qtyDifference = orderItemFromDb.Qty - orderItems[item].Qty;

                if (qtyDifference < 0) {
                    if (productAvailabilities.Sum(a => a.Amount) < Math.Abs(qtyDifference)) {
                        orderItems[item].Qty -= Math.Abs(qtyDifference) - productAvailabilities.Sum(a => a.Amount);

                        qtyDifference = 0 - productAvailabilities.Sum(a => a.Amount);
                    }

                    double toDecreaseQty = Math.Abs(qtyDifference);

                    foreach (ProductAvailability productAvailability in productAvailabilities.Where(a => a.Amount > 0)) {
                        if (toDecreaseQty.Equals(0d)) break;

                        ProductReservation productReservation =
                            productReservationRepository
                                .GetByOrderItemAndProductAvailabilityIds(
                                    orderItemFromDb.Id,
                                    productAvailability.Id
                                );

                        if (productAvailability.Amount >= toDecreaseQty) {
                            if (productReservation != null) {
                                productReservation.Qty += toDecreaseQty;

                                productReservationRepository.Update(productReservation);
                            } else {
                                productReservationRepository
                                    .Add(new ProductReservation {
                                        OrderItemId = orderItemFromDb.Id,
                                        ProductAvailabilityId = productAvailability.Id,
                                        Qty = toDecreaseQty
                                    });
                            }

                            productAvailability.Amount -= toDecreaseQty;

                            toDecreaseQty = 0d;
                        } else {
                            if (productReservation != null) {
                                productReservation.Qty += productAvailability.Amount;

                                productReservationRepository.Update(productReservation);
                            } else {
                                productReservationRepository
                                    .Add(new ProductReservation {
                                        OrderItemId = orderItemFromDb.Id,
                                        ProductAvailabilityId = productAvailability.Id,
                                        Qty = productAvailability.Amount
                                    });
                            }

                            toDecreaseQty -= productAvailability.Amount;

                            productAvailability.Amount = 0d;
                        }

                        productAvailabilityRepository.Update(productAvailability);
                    }
                } else {
                    IEnumerable<ProductReservation> reservations =
                        productReservationRepository
                            .GetAllByOrderItemIdWithAvailability(
                                orderItemFromDb.Id
                            );

                    double toRestoreAmount = qtyDifference;

                    foreach (ProductReservation reservation in reservations) {
                        if (toRestoreAmount.Equals(0d)) break;

                        if (reservation.Qty >= toRestoreAmount) {
                            reservation.Qty -= toRestoreAmount;

                            reservation.ProductAvailability.Amount += toRestoreAmount;

                            toRestoreAmount = 0d;

                            if (reservation.Qty > 0)
                                productReservationRepository.Update(reservation);
                            else
                                productReservationRepository.Delete(reservation.NetUid);
                        } else {
                            toRestoreAmount -= reservation.Qty;

                            reservation.ProductAvailability.Amount += reservation.Qty;

                            productReservationRepository.Delete(reservation.NetUid);
                        }

                        productAvailabilityRepository.Update(reservation.ProductAvailability);
                    }
                }

                orderItemRepository.UpdateQty(orderItems[item]);

                orderItems[item] = NormalizeOverLordQty(orderItemRepository.GetByIdWithIncludes(orderItems[item].Id, nonVatAgreement?.NetUid, vatAgreement?.NetUid));
            }

            return Task.FromResult(orderItems.Select(NormalizeOverLordQty).ToList());
    }

    public Task<IEnumerable<OrderItem>> GetAllItemsFromCurrentShoppingCartByClientNetId(Guid netId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);

            ClientAgreement selectedAgreement = clientAgreementRepository.GetSelectedByClientNetId(netId) ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(netId);
            if (selectedAgreement == null) {
                selectedAgreement = clientAgreementRepository.GetSelectedByClientNotSelectedNetId(netId);
                if (selectedAgreement?.Agreement == null)
                    return Task.FromResult(Enumerable.Empty<OrderItem>());

                selectedAgreement.Agreement.IsSelected = true;
                agreementRepository.Update(selectedAgreement.Agreement);
            }

            IEnumerable<OrderItem> orderItems = _saleRepositoriesFactory
                .NewOrderItemRepository(connection)
                .GetAllFromCurrentShoppingByClientNetId(
                    _clientRepositoriesFactory.NewWorkplaceRepository(connection).GetByNetId(netId)?.Id,
                    selectedAgreement.NetUid,
                    selectedAgreement.Agreement.CurrencyId,
                    selectedAgreement.Agreement.OrganizationId,
                    selectedAgreement.Agreement.WithVATAccounting
                );

            return Task.FromResult(orderItems.Select(NormalizeOverLordQty));
    }

    public Task DeleteItemFromShoppingCartByNetId(Guid itemNetId, Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(clientNetId);

            Workplace workplace = null;

            if (clientAgreement == null) {
                clientAgreement = clientAgreementRepository.GetSelectedByWorkplaceNetId(clientNetId);
                workplace = _clientRepositoriesFactory.NewWorkplaceRepository(connection).GetByNetId(clientNetId);
            }

            ClientShoppingCart clientShoppingCart =
                clientShoppingCartRepository.GetByClientAgreementNetId(clientAgreement.NetUid, clientAgreement.Agreement.WithVATAccounting, workplace?.Id);

            if (!clientShoppingCart.OrderItems.Any(i => i.NetUid.Equals(itemNetId))) return Task.CompletedTask;

            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

            OrderItem orderItem = orderItemRepository.GetByNetIdWithProduct(itemNetId);

            if (orderItem == null) return Task.CompletedTask;

            orderItemRepository.Remove(itemNetId);

            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            IEnumerable<ProductReservation> reservations =
                productReservationRepository
                    .GetAllByOrderItemIdWithAvailability(
                        orderItem.Id
                    );

            foreach (ProductReservation reservation in reservations) {
                reservation.ProductAvailability.Amount += reservation.Qty;

                productAvailabilityRepository.Update(reservation.ProductAvailability);

                productReservationRepository.Delete(reservation.NetUid);
            }

            _reindexSignal.Request(orderItem.ProductId);

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
            }, "Cart item delete availability sync");

            return Task.CompletedTask;
    }

    public Task DeleteAllItemsFromShoppingCartByClientNetId(Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(clientNetId);

            Workplace workplace = null;

            if (clientAgreement == null) {
                clientAgreement = clientAgreementRepository.GetSelectedByWorkplaceNetId(clientNetId);
                workplace = _clientRepositoriesFactory.NewWorkplaceRepository(connection).GetByNetId(clientNetId);
            }

            if (clientAgreement == null) return Task.CompletedTask;

            ClientShoppingCart clientShoppingCart = _clientRepositoriesFactory
                .NewClientShoppingCartRepository(connection)
                .GetByClientAgreementNetId(clientAgreement.NetUid, clientAgreement.Agreement.WithVATAccounting, workplace?.Id);

            if (clientShoppingCart != null && clientShoppingCart.OrderItems.Any()) {
                IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

                foreach (OrderItem orderItem in clientShoppingCart.OrderItems) {
                    orderItemRepository.Remove(orderItem.NetUid);

                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
                    IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

                    IEnumerable<ProductReservation> reservations =
                        productReservationRepository
                            .GetAllByOrderItemIdWithAvailability(
                                orderItem.Id
                            );

                    foreach (ProductReservation reservation in reservations) {
                        reservation.ProductAvailability.Amount += reservation.Qty;

                        productAvailabilityRepository.Update(reservation.ProductAvailability);

                        productReservationRepository.Delete(reservation.NetUid);
                    }

                    _reindexSignal.Request(orderItem.ProductId);

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
                    }, "Cart clear availability sync");
                }
            }

        return Task.CompletedTask;
    }

    public Task<int> ReleaseExpiredCartsAsync() {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            List<ClientShoppingCart> expiredCarts = clientShoppingCartRepository.GetAllExistingExpiredClientShoppingCarts();

            int releasedReservations = 0;

            foreach (ClientShoppingCart cart in expiredCarts) {
                foreach (OrderItem orderItem in cart.OrderItems) {
                    IEnumerable<ProductReservation> reservations =
                        productReservationRepository.GetAllByOrderItemIdWithAvailability(orderItem.Id);

                    foreach (ProductReservation reservation in reservations) {
                        reservation.ProductAvailability.Amount += reservation.Qty;

                        productAvailabilityRepository.Update(reservation.ProductAvailability);

                        productReservationRepository.Delete(reservation.NetUid);

                        releasedReservations++;
                    }

                    _reindexSignal.Request(orderItem.ProductId);

                    orderItemRepository.Remove(orderItem.NetUid);
                }

                clientShoppingCartRepository.Remove(cart.NetUid);
            }

            return Task.FromResult(releasedReservations);
    }

    public Task<Tuple<bool, string>> VerifyProductAvailability(OrderItem orderItem) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (orderItem == null)
                throw new Exception("OrderItem can not be empty.");
            if (!orderItem.IsNew())
                throw new Exception("Existing OrderItem is not valid input for current request.");
            if (orderItem.ProductId.Equals(0) && orderItem.Product == null)
                throw new Exception("Product need to be specified.");
            if (orderItem.Qty.Equals(0)) throw new Exception("You need to specify Qty of product that will be added.");

            if (orderItem.Product != null) orderItem.ProductId = orderItem.Product.Id;

            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection)
                .GetWithHighestPriority();

            ClientAgreement clientAgreement = clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(
                _clientRepositoriesFactory.NewClientRepository(connection).GetRetailClient().NetUid,
                storage.OrganizationId.Value,
                storage.ForVatProducts);

            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            IEnumerable<ProductAvailability> productAvailabilities;

            if (clientAgreement.Agreement.WithVATAccounting) {
                productAvailabilities =
                    productAvailabilityRepository
                        .GetByProductAndOrganizationIds(
                            orderItem.ProductId,
                            clientAgreement.Agreement.Organization.Id,
                            clientAgreement.Agreement.WithVATAccounting
                        );
            } else {
                List<Storage> allStorages = _storageRepositoryFactory.NewStorageRepository(connection).GetAllNonDefectiveByCurrentLocale().ToList();

                List<Storage> storages = new(allStorages.Where(s => s.AvailableForReSale));
                storages.AddRange(allStorages.Where(e => e.OrganizationId.Equals(clientAgreement.Agreement.OrganizationId)));

                productAvailabilities = productAvailabilityRepository
                    .GetAllByProductAndStorageIds(
                        orderItem.ProductId,
                        storages.Select(e => e.Id).ToList());
            }

            if (!productAvailabilities.Any()) return Task.FromResult(new Tuple<bool, string>(false, "Product is not available"));

            double qtyRemainderProducts = productAvailabilities.Sum(a => a.Amount);

            if (qtyRemainderProducts < orderItem.Qty) return Task.FromResult(new Tuple<bool, string>(false, "Product is not available"));

            return Task.FromResult(new Tuple<bool, string>(true, string.Empty));
    }

    private OrderItem UpdateExistingOrderItemInShoppingCart(
        OrderItem orderItem,
        IDbConnection connection,
        IProductAvailabilityRepository productAvailabilityRepository,
        IProductReservationRepository productReservationRepository,
        IEnumerable<ProductAvailability> productAvailabilities,
        OrderItem existingOrderItem,
        long? currencyId,
        Guid? clientAgreementNetId,
        Guid? vatAgreementNetId = null) {
        IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

        existingOrderItem.Qty += orderItem.Qty;

        orderItemRepository.Update(existingOrderItem);

        double toDecreaseQty = orderItem.Qty;

        foreach (ProductAvailability productAvailability in productAvailabilities.Where(a => a.Amount > 0)) {
            if (toDecreaseQty.Equals(0d)) break;

            ProductReservation productReservation =
                productReservationRepository
                    .GetByOrderItemAndProductAvailabilityIds(
                        existingOrderItem.Id,
                        productAvailability.Id
                    );

            if (productAvailability.Amount >= toDecreaseQty) {
                if (productReservation != null) {
                    productReservation.Qty += toDecreaseQty;

                    productReservationRepository.Update(productReservation);
                } else {
                    productReservationRepository
                        .Add(new ProductReservation {
                            OrderItemId = existingOrderItem.Id,
                            ProductAvailabilityId = productAvailability.Id,
                            Qty = toDecreaseQty
                        });
                }

                productAvailability.Amount -= toDecreaseQty;

                toDecreaseQty = 0d;
            } else {
                if (productReservation != null) {
                    productReservation.Qty += productAvailability.Amount;

                    productReservationRepository.Update(productReservation);
                } else {
                    productReservationRepository
                        .Add(new ProductReservation {
                            OrderItemId = existingOrderItem.Id,
                            ProductAvailabilityId = productAvailability.Id,
                            Qty = productAvailability.Amount
                        });
                }

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

                saleSyncCrmUrl = $"{data?.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
            } else {
                throw new InvalidOperationException("CRM endpoint is not configured.");
            }

            using HttpClient httpClient = _httpClientFactory.CreateClient(
                EcommerceInternalHttpClientDefaults.ClientName);
            await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
        }, "Cart update availability sync");

        return NormalizeOverLordQty(orderItemRepository.GetByIdAndClientAgreementNetIdWithIncludes(existingOrderItem.Id, clientAgreementNetId.Value, currencyId.Value));
        ;
    }

    private OrderItem AddNewItemToShoppingCart(OrderItem orderItem,
        IDbConnection connection,
        IProductAvailabilityRepository productAvailabilityRepository,
        IProductReservationRepository productReservationRepository,
        IEnumerable<ProductAvailability> productAvailabilities,
        long? currencyId,
        Guid? clientAgreementNetId,
        Guid? vatAgreementNetId = null) {
        IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

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

                saleSyncCrmUrl = $"{data?.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
            } else {
                throw new InvalidOperationException("CRM endpoint is not configured.");
            }

            using HttpClient httpClient = _httpClientFactory.CreateClient(
                EcommerceInternalHttpClientDefaults.ClientName);
            await httpClient.PostAsync(saleSyncCrmUrl, null, cancellationToken);
        }, "Cart add availability sync");

        return NormalizeOverLordQty(orderItemRepository.GetByIdAndClientAgreementNetIdWithIncludes(orderItem.Id, clientAgreementNetId.Value, currencyId.Value));
    }
}
