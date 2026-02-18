using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames.ECommerce;
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
        IHttpClientFactory httpClientFactory) {
        _clientRepositoriesFactory = clientRepositoriesFactory;

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

            if (orderItem.Product != null) orderItem.ProductId = orderItem.Product.Id;

            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            IWorkplaceRepository workplaceRepository = _clientRepositoriesFactory.NewWorkplaceRepository(connection);

            Workplace workplace = null;

            ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(clientNetId);

            if (clientAgreement == null) {
                workplace = workplaceRepository.GetByNetId(clientNetId);
                clientAgreement = clientAgreementRepository.GetSelectedByWorkplaceNetId(workplace.NetUid);
            }

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

                if (orderItems[item].Product != null) orderItems[item].ProductId = orderItems[item].Product.Id;

                IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

                ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, false);
                ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, true);

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

            return Task.FromResult(orderItems);
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
            if (orderItem.Qty <= 0)
                return Task.FromResult(orderItemRepository.GetByIdWithIncludes(orderItem.Id, selectedAgreement.NetUid));

            orderItemRepository.UpdateOverLoadQty(orderItem);

            OrderItem orderItemFromDb = orderItemRepository.GetById(orderItem.Id);

            if (orderItem.Qty.Equals(orderItemFromDb.Qty))
                return Task.FromResult(orderItemRepository.GetByIdWithIncludes(orderItem.Id, selectedAgreement.NetUid));

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

            return Task.FromResult(orderItemRepository.GetByIdWithIncludes(orderItem.Id, selectedAgreement.NetUid));
    }

    public Task<List<OrderItem>> Update(List<OrderItem> orderItems, Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            for (int item = 0; item < orderItems.Count; item++) {
                IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
                IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

                ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, false);
                ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, true);

                if (orderItems[item] == null) continue;
                if (orderItems[item].IsNew()) continue;
                if (orderItems[item].Qty <= 0) {
                    orderItems[item] = orderItemRepository.GetByIdWithIncludes(orderItems[item].Id, nonVatAgreement?.NetUid, vatAgreement?.NetUid);

                    continue;
                }

                OrderItem orderItemFromDb = orderItemRepository.GetById(orderItems[item].Id);

                if (orderItems[item].Qty.Equals(orderItemFromDb.Qty)) {
                    orderItems[item] = orderItemRepository.GetByIdWithIncludes(orderItems[item].Id, nonVatAgreement?.NetUid, vatAgreement?.NetUid);

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

                orderItems[item] = orderItemRepository.GetByIdWithIncludes(orderItems[item].Id, nonVatAgreement?.NetUid, vatAgreement?.NetUid);
            }

            return Task.FromResult(orderItems);
    }

    public Task<IEnumerable<OrderItem>> GetAllItemsFromCurrentShoppingCartByClientNetId(Guid netId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);

            ClientAgreement selectedAgreement = clientAgreementRepository.GetSelectedByClientNetId(netId) ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(netId);
            if (selectedAgreement == null) {
                selectedAgreement = clientAgreementRepository.GetSelectedByClientNotSelectedNetId(netId);
                selectedAgreement.Agreement.IsSelected = true;
                agreementRepository.Update(selectedAgreement.Agreement);
            }

            selectedAgreement = clientAgreementRepository.GetSelectedByClientNetId(netId) ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(netId);

            IEnumerable<OrderItem> orderItems = _saleRepositoriesFactory
                .NewOrderItemRepository(connection)
                .GetAllFromCurrentShoppingByClientNetId(
                    _clientRepositoriesFactory.NewWorkplaceRepository(connection).GetByNetId(netId)?.Id,
                    selectedAgreement.NetUid,
                    selectedAgreement.Agreement.CurrencyId,
                    selectedAgreement.Agreement.OrganizationId,
                    selectedAgreement.Agreement.WithVATAccounting
                );

            return Task.FromResult(orderItems);
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
            }, "Cart item delete availability sync");

            return Task.CompletedTask;
    }

    public Task DeleteAllItemsFromShoppingCartByClientNetId(Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ClientShoppingCart clientShoppingCart = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection).GetByClientNetId(clientNetId, withVat);

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
                    }, "Cart clear availability sync");
                }
            }

        return Task.CompletedTask;
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

            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

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
                saleSyncCrmUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
            }

            using HttpClient httpClient = _httpClientFactory.CreateClient();
            await httpClient.GetAsync(saleSyncCrmUrl, cancellationToken);
        }, "Cart update availability sync");

        return orderItemRepository.GetByIdAndClientAgreementNetIdWithIncludes(existingOrderItem.Id, clientAgreementNetId.Value, currencyId.Value);
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
                saleSyncCrmUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
            }

            using HttpClient httpClient = _httpClientFactory.CreateClient();
            await httpClient.GetAsync(saleSyncCrmUrl, cancellationToken);
        }, "Cart add availability sync");

        return orderItemRepository.GetByIdAndClientAgreementNetIdWithIncludes(orderItem.Id, clientAgreementNetId.Value, currencyId.Value);
    }
}
