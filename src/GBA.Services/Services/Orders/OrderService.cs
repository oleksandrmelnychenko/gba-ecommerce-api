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
using System.Threading.Tasks;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
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
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.Services.Messengers.Contracts;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GBA.Services.Services.Orders;

public sealed class OrderService : IOrderService {
    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPaymentLinkService _paymentLinkService;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IRetailClientRepositoriesFactory _retailClientRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public OrderService(
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IRetailClientRepositoriesFactory retailClientRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IPaymentLinkService paymentLinkService,
        IHttpClientFactory httpClientFactory) {
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _retailClientRepositoriesFactory = retailClientRepositoriesFactory;
        _connectionFactory = connectionFactory;
        _paymentLinkService = paymentLinkService;
        _httpClientFactory = httpClientFactory;
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

            _ = Task.Run(async () => {
                try {
                    string saleSyncCrmUrl;

                    if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                        dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));

#if DEBUG
                        saleSyncCrmUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={sale.NetUid.ToString()}";
#else
                                saleSyncCrmUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={sale.NetUid.ToString()}";
#endif
                    } else {
                        saleSyncCrmUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={sale.NetUid.ToString()}";
                    }

                    using HttpClient httpClient = _httpClientFactory.CreateClient();
                    await httpClient.GetAsync(saleSyncCrmUrl);
                } catch (Exception exc) {
                    string path = Path.Combine(NoltFolderManager.GetLogFolderPath(), "sync_error_log.txt");

                    File.AppendAllText(
                        path,
                        string.Format(
                            "{0}\r\n{1}\r\n{2}\r\n\r\n",
                            DateTime.UtcNow.ToString("dd.MM.yyyy hh:mm"),
                            exc.Message,
                            exc.InnerException?.Message ?? string.Empty
                        )
                    );
                }
            });

            return Task.FromResult(sale);
    }

    public Task<Order> DynamicallyCalculateTotalPrices(Order order) {
        for (int i = 0; i < order.OrderItems.Count; i++)
            if (order.OrderItems.ElementAt(i).Product != null) {
                order.OrderItems.ElementAt(i).TotalAmount =
                    Math.Round(order.OrderItems.ElementAt(i).Product.CurrentPrice * Convert.ToDecimal(order.OrderItems.ElementAt(i).Qty), 2);
                order.OrderItems.ElementAt(i).TotalAmountLocal =
                    Math.Round(order.OrderItems.ElementAt(i).Product.CurrentLocalPrice * Convert.ToDecimal(order.OrderItems.ElementAt(i).Qty), 2);
                order.OrderItems.ElementAt(i).OverLordTotalAmount =
                    Math.Round(order.OrderItems.ElementAt(i).Product.CurrentPrice * Convert.ToDecimal(order.OrderItems.ElementAt(i).OverLordQty), 2);
                order.OrderItems.ElementAt(i).OverLordTotalAmountLocal =
                    Math.Round(order.OrderItems.ElementAt(i).Product.CurrentLocalPrice * Convert.ToDecimal(order.OrderItems.ElementAt(i).OverLordQty), 2);

                order.TotalAmount = Math.Round(order.TotalAmount + order.OrderItems.ElementAt(i).TotalAmount, 2);
                order.TotalAmountLocal = Math.Round(order.TotalAmountLocal + order.OrderItems.ElementAt(i).TotalAmountLocal, 2);
                order.OverLordTotalAmount = Math.Round(order.OverLordTotalAmount + order.OrderItems.ElementAt(i).OverLordTotalAmount, 2);
                order.OverLordTotalAmountLocal = Math.Round(order.OverLordTotalAmountLocal + order.OrderItems.ElementAt(i).OverLordTotalAmountLocal, 2);
            }

        return Task.FromResult(order);
    }

    public async Task<Sale> GenerateNewSaleWithInvoice(Sale sale, Guid clientNetId, bool isWorkplace) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

            ClientAgreement selectedClientAgreement = isWorkplace
                ? _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetSelectedByWorkplaceNetId(clientNetId)
                : _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetSelectedByClientNetId(clientNetId);

            bool withVat = selectedClientAgreement.Agreement.WithVATAccounting;

            Order order = new() {
                OrderSource = OrderSource.Shop,
                OrderStatus = OrderStatus.NewOrderCart,
                ClientAgreement = selectedClientAgreement
            };

            order.ClientAgreementId = order.ClientAgreement.Id;

            order.Id = _saleRepositoriesFactory
                .NewOrderRepository(connection)
                .Add(order);

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

            if (currentCart == null) {
                currentCart = new ClientShoppingCart {
                    ValidUntil = DateTime.Now.Date.AddDays(client.ClearCartAfterDays),
                    ClientAgreementId = order.ClientAgreement.Id,
                    IsVatCart = order.ClientAgreement.Agreement.WithVATAccounting
                };

                currentCart.Id = clientShoppingCartRepository.Add(currentCart);

                currentCart.OrderItems = sale.Order.OrderItems;
            }

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

                _ = Task.Run(async () => {
                    try {
                        string saleSyncCrmUrl;

                        if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                            dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));

#if DEBUG
                            saleSyncCrmUrl =
                                $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#else
                                    saleSyncCrmUrl =
 $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#endif
                        } else {
                            saleSyncCrmUrl =
                                $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
                        }

                        using HttpClient httpClient = _httpClientFactory.CreateClient();
                        await httpClient.GetAsync(saleSyncCrmUrl);
                    } catch (Exception) {
                        // ignored
                    }
                });
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

            _ = Task.Run(async () => {
                try {
                    string saleSyncCrmUrl;

                    if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                        dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));
#if DEBUG
                        saleSyncCrmUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#else
                                saleSyncCrmUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#endif
                    } else {
                        saleSyncCrmUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
                    }

                    using HttpClient httpClient = _httpClientFactory.CreateClient();
                    await httpClient.GetAsync(saleSyncCrmUrl);
                } catch (Exception) {
                    // ignored
                }
            });

            createdSale.DeliveryRecipient = sale.DeliveryRecipient;
            createdSale.DeliveryRecipientAddress = sale.DeliveryRecipientAddress;
            createdSale.Transporter = sale.Transporter;

            string crmApiUrl;

            if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));
#if DEBUG
                crmApiUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#else
                        crmApiUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#endif
            } else {
                crmApiUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
            }

            string payload = JsonConvert.SerializeObject(createdSale, Formatting.None, new JsonSerializerSettings {
                ContractResolver = new DefaultContractResolver()
            });

            using HttpClient httpClient = _httpClientFactory.CreateClient();
            HttpResponseMessage responseMessage =
                await httpClient.PostAsync(
                    crmApiUrl,
                    new StringContent(payload, Encoding.UTF8, "application/json")
                );

            string responseContent = await responseMessage.Content.ReadAsStringAsync();
            dynamic responseData = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (!responseMessage.IsSuccessStatusCode)
                throw new Exception(responseData.Message);

            sale = saleRepository.GetByNetId(createdSale.NetUid);

            return sale;
    }

    public async Task<string> GenerateNewRetailSale(Sale sale, Guid retailClientNetId, bool fullPayment) {
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

            Storage storage = storageRepository.GetWithHighestPriority();

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

            foreach (OrderItem orderItem in sale.Order.OrderItems.Where(i => i.IsNew() && i.Qty > 0)) {
                ProductAvailability productAvailability =
                    productAvailabilityRepository.GetByProductAndStorageIds(orderItem.Product.Id, storage.Id);

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

                _ = Task.Run(async () => {
                    try {
                        string saleSyncCrmUrl;

                        if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                            dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));

#if DEBUG
                            saleSyncCrmUrl =
                                $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#else
                                    saleSyncCrmUrl =
 $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#endif
                        } else {
                            saleSyncCrmUrl =
                                $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
                        }

                        using HttpClient httpClient = _httpClientFactory.CreateClient();
                        await httpClient.GetAsync(saleSyncCrmUrl);
                    } catch (Exception) {
                        // ignored
                    }
                });
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

            _ = Task.Run(async () => {
                try {
                    string saleSyncCrmUrl;

                    if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                        dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));

#if DEBUG
                        saleSyncCrmUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#else
                                saleSyncCrmUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#endif
                    } else {
                        saleSyncCrmUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
                    }

                    using HttpClient httpClient = _httpClientFactory.CreateClient();
                    await httpClient.GetAsync(saleSyncCrmUrl);
                } catch (Exception) {
                    // ignored
                }
            });

            createdSale.DeliveryRecipient = sale.DeliveryRecipient;
            createdSale.DeliveryRecipientAddress = sale.DeliveryRecipientAddress;
            createdSale.Transporter = sale.Transporter;

            string crmApiUrl;

            if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));

#if DEBUG
                //crmApiUrl = $"http://localhost:35981/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
                crmApiUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#else
                        crmApiUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#endif
            } else {
                crmApiUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
            }

            string payload = JsonConvert.SerializeObject(createdSale, Formatting.None, new JsonSerializerSettings {
                ContractResolver = new DefaultContractResolver()
            });


            using HttpClient httpClient = _httpClientFactory.CreateClient();
            HttpResponseMessage responseMessage =
                await httpClient.PostAsync(
                    crmApiUrl,
                    new StringContent(payload, Encoding.UTF8, "application/json")
                );

            string responseContent = await responseMessage.Content.ReadAsStringAsync();
            dynamic responseData = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (!responseMessage.IsSuccessStatusCode)
                throw new Exception(responseData.Message);

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

            Storage storage = storageRepository.GetWithHighestPriority();

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

                _ = Task.Run(async () => {
                    try {
                        string saleSyncCrmUrl;

                        if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                            dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));

#if DEBUG
                            saleSyncCrmUrl =
                                $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#else
                                    saleSyncCrmUrl =
 $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
#endif
                        } else {
                            saleSyncCrmUrl =
                                $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/products/sync/availability?netId={orderItem.Product.NetUid.ToString()}";
                        }

                        using HttpClient httpClient = _httpClientFactory.CreateClient();
                        await httpClient.GetAsync(saleSyncCrmUrl);
                    } catch (Exception) {
                        // ignored
                    }
                });
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

            _ = Task.Run(async () => {
                try {
                    string saleSyncCrmUrl;

                    if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                        dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));

#if DEBUG
                        saleSyncCrmUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#else
                                saleSyncCrmUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
#endif
                    } else {
                        saleSyncCrmUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/sync/new?netId={createdSale.NetUid.ToString()}";
                    }

                    using HttpClient httpClient = _httpClientFactory.CreateClient();
                    await httpClient.GetAsync(saleSyncCrmUrl);
                } catch (Exception) {
                    // ignored
                }
            });

            createdSale.DeliveryRecipient = sale.DeliveryRecipient;
            createdSale.DeliveryRecipientAddress = sale.DeliveryRecipientAddress;
            createdSale.Transporter = sale.Transporter;

            string crmApiUrl;

            if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));

#if DEBUG
                //crmApiUrl = $"http://localhost:35981/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
                crmApiUrl = $"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#else
                        crmApiUrl = $"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
#endif
            } else {
                crmApiUrl = $"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/update/ecommerce";
            }

            string payload = JsonConvert.SerializeObject(createdSale, Formatting.None, new JsonSerializerSettings {
                ContractResolver = new DefaultContractResolver()
            });


            using HttpClient httpClient = _httpClientFactory.CreateClient();
            HttpResponseMessage responseMessage =
                await httpClient.PostAsync(
                    crmApiUrl,
                    new StringContent(payload, Encoding.UTF8, "application/json")
                );

            string responseContent = await responseMessage.Content.ReadAsStringAsync();
            dynamic responseData = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (!responseMessage.IsSuccessStatusCode)
                throw new Exception(responseData.Message);

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

            Storage storage = storageRepository.GetWithHighestPriority();

            foreach (OrderItem orderItem in orderItems) {
                ProductAvailability productAvailability = productAvailabilityRepository.GetByProductAndStorageIds(orderItem.Product.Id, storage.Id);

                if (productAvailability == null || productAvailability.Amount.Equals(0))
                    orderItem.IsMisplacedItem = true;
            }

            if (!orderItems.All(i => i.IsMisplacedItem)) {
                orderItems.ForEach(i => i.IsMisplacedItem = false);

                return Task.FromResult(orderItems);
            }

            MisplacedSale misplacedSale = new() { RetailClientId = retailClientId };

            long misplacedSaleId = _saleRepositoriesFactory.NewMisplacedSaleRepository(connection).Add(misplacedSale);

            orderItems.ForEach(i => {
                i.MisplacedSaleId = misplacedSaleId;
                i.ProductId = i.Product.Id;
            });

            _saleRepositoriesFactory.NewOrderItemRepository(connection).Add(orderItems);

            return Task.FromResult(orderItems);
    }

    public async Task SendPaymentImageToCrm(Guid saleNetId, Guid clientNetId, PaymentConfirmationImageModel paymentImage) {
        UriBuilder crmApiUrl;

            // URI

            if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
                dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()));
#if DEBUG
                //crmApiUrl = new UriBuilder($"http://localhost:35981/api/v1/{CultureInfo.CurrentCulture}/sales/payment/save");
                crmApiUrl = new UriBuilder($"{data.CrmServerUrl}/api/v1/{CultureInfo.CurrentCulture}/sales/payment/save");
#else
                        crmApiUrl = new UriBuilder($"{data.CrmServerUrlRelease}/api/v1/{CultureInfo.CurrentCulture}/sales/payment/save");
#endif
            } else {
                crmApiUrl = new UriBuilder($"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/payment/save");
            }

            crmApiUrl.Query = $"saleNetId={saleNetId}&clientNetId={clientNetId}";

            string payload = JsonConvert.SerializeObject(paymentImage, Formatting.None, new JsonSerializerSettings {
                ContractResolver = new DefaultContractResolver()
            });

        using HttpClient httpClient = _httpClientFactory.CreateClient();
        HttpResponseMessage responseMessage =
            await httpClient.PostAsync(
                crmApiUrl.Uri,
                new StringContent(payload, Encoding.UTF8, "application/json")
            );

        string responseContent = await responseMessage.Content.ReadAsStringAsync();
        dynamic responseData = JsonConvert.DeserializeObject<dynamic>(responseContent);

        if (!responseMessage.IsSuccessStatusCode)
            throw new Exception(responseData.Message);
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
