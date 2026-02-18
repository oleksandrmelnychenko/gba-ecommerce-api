using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Entities.Sales.SaleShiftStatuses;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Messages.Auditing;
using GBA.Domain.Messages.Communications.Hubs;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.Sales;
using GBA.Domain.Messages.Sales.OrderItems;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Sales;

public sealed class OrderItemsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoryFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public OrderItemsActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoryFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _exchangeRateRepositoryFactory = exchangeRateRepositoryFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        Receive<AddOrderItemMessage>(ProcessAddOrderItemMessage);

        Receive<UpdateOrderItemMessage>(ProcessUpdateOrderItemMessage);

        Receive<DeleteOrderItemMessage>(ProcessDeleteOrderItemMessage);

        ReceiveAsync<ShiftSaleOrderItemsMessage>(ProcessShiftSaleOrderItemsMessage);

        Receive<ShiftSaleOrderItemMessage>(ProcessShiftSaleOrderItemMessage);

        ReceiveAsync<CloseExpiredOrdersMessage>(ProcessCloseExpiredOrdersMessage);
    }

    private void ProcessAddOrderItemMessage(AddOrderItemMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);
            IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoryFactory.NewExchangeRateRepository(connection);

            ClientAgreement clientAgreement =
                _clientRepositoriesFactory
                    .NewClientAgreementRepository(connection)
                    .GetWithOrganizationByNetId(
                        message.ClientAgreementNetId
                    );

            IEnumerable<ProductAvailability> productAvailabilities =
                productAvailabilityRepository
                    .GetByProductAndOrganizationIds(
                        message.OrderItem.Product.Id,
                        clientAgreement.Agreement.Organization.Id,
                        clientAgreement.Agreement.Organization.Culture != "pl" && clientAgreement.Agreement.WithVATAccounting,
                        true,
                        clientAgreement.Agreement.Organization.StorageId
                    );

            Currency uah = currencyRepository.GetUAHCurrencyIfExists();

            decimal currentExchangeRateEurToUah = exchangeRateRepository.GetExchangeRateToEuroCurrency(uah);

            if (productAvailabilities.Any(a => a.Amount > 0)) {
                Sale sale;
                bool isNewSale = false;

                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

                if (message.Sale != null) {
                    sale = message.Sale;

                    isNewSale = true;
                } else {
                    if (!message.SaleNetId.Equals(Guid.Empty)) {
                        sale = saleRepository.GetByNetIdWithAgreement(message.SaleNetId);
                        //sale.UpdateUserId = user.Id;
                        //saleRepository.UpdateUser(sale);

                        //if (sale.IsLocked) {
                        //    if (user.UserRoleId.HasValue) {
                        //        if (!user.UserRole.UserRoleType.Equals(UserRoleType.GBA) && !user.UserRole.UserRoleType.Equals(UserRoleType.HeadSalesAnalytic))
                        //            throw new UserForbidenException();
                        //    } else {
                        //        throw new UserForbidenException();
                        //    }
                        //}
                    } else {
                        sale = saleRepository.GetLastNotMergedNewSaleByClientAgreementNetId(message.ClientAgreementNetId);

                        if (sale == null) {
                            Sale newSale = new() {
                                ClientAgreement = clientAgreement,
                                Order = new Order {
                                    OrderSource = OrderSource.Local
                                },
                                IsVatSale = clientAgreement.Agreement.WithVATAccounting
                            };

                            Sale saleFromDb = saleRepository.GetByNetId(message.SaleNetId);
                            if (saleFromDb == null && message.Sale != null) saleFromDb = saleRepository.GetByNetId(message.Sale.NetUid);

                            if (saleFromDb != null)
                                foreach (OrderItem item in saleFromDb.Order.OrderItems)
                                    if (item.Id.Equals(message.OrderItem.Id) && item.Discount != 0)
                                        message.OrderItem.Discount = item.Discount;

                            ActorReferenceManager
                                .Instance
                                .Get(SalesActorNames.SALES_ACTOR)
                                .Tell(new AddSaleWithStatusesOnlyMessage(newSale, message.UserNetId, message), Sender);

                            return;
                        }
                    }
                }

                Guid saleNetId = sale.NetUid;

                if (message.OrderItem == null || user == null) return;

                if (productAvailabilities.Sum(a => a.Amount) < message.OrderItem.Qty)
                    message.OrderItem.Qty = productAvailabilities.Sum(a => a.Amount);

                double toDecreaseQty = message.OrderItem.Qty;

                foreach (ProductAvailability productAvailability in productAvailabilities.Where(a => a.Amount > 0)) {
                    if (toDecreaseQty == 0d) break;

                    message.OrderItem.Qty = toDecreaseQty;

                    double currentQty =
                        productAvailability.Amount < message.OrderItem.Qty
                            ? productAvailability.Amount
                            : message.OrderItem.Qty;

                    message.OrderItem.IsFromReSale = productAvailability.IsReSaleAvailability;

                    OrderItem orderItemFromDb =
                        orderItemRepository
                            .GetFilteredByIds(
                                sale.OrderId,
                                message.OrderItem.Product.Id,
                                message.OrderItem.IsFromReSale
                            );

                    if (orderItemFromDb != null && !sale.ChangedToInvoice.HasValue) {
                        _saleRepositoriesFactory
                            .NewOrderItemMovementRepository(connection)
                            .Add(new OrderItemMovement {
                                OrderItemId = orderItemFromDb.Id,
                                UserId = user.Id,
                                Qty = currentQty
                            });

                        if (toDecreaseQty > 0) {
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
                                            Qty = toDecreaseQty,
                                            IsReSaleReservation = productAvailability.IsReSaleAvailability
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
                                            Qty = productAvailability.Amount,
                                            IsReSaleReservation = productAvailability.IsReSaleAvailability
                                        });
                                }

                                toDecreaseQty -= productAvailability.Amount;

                                productAvailability.Amount = 0d;
                            }

                            productAvailabilityRepository.Update(productAvailability);
                        }

                        orderItemFromDb.Qty += currentQty;
                        orderItemFromDb.Comment = message.OrderItem.Comment;

                        orderItemRepository.Update(orderItemFromDb);

                        OrderItem orderItem =
                            orderItemRepository
                                .GetWithCalculatedProductPrices(
                                    orderItemFromDb.NetUid,
                                    clientAgreement.NetUid,
                                    clientAgreement.Agreement.OrganizationId ?? 0,
                                    sale.IsVatSale,
                                    orderItemFromDb.IsFromReSale
                                );

                        if (!orderItem.PricePerItem.Equals(decimal.Zero)) {
                            orderItem.Product.CurrentPrice = orderItem.PricePerItem;
                            orderItem.Product.CurrentLocalPrice = orderItem.Product.CurrentPrice * orderItem.ExchangeRateAmount;
                        }

                        orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;

                        orderItem.TotalAmount = decimal.Round(
                            orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty),
                            14,
                            MidpointRounding.AwayFromZero
                        );

                        orderItem.TotalAmountEurToUah = decimal.Round(
                            orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty),
                            14,
                            MidpointRounding.AwayFromZero
                        );

                        orderItem.TotalAmountLocal = decimal.Round(orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14,
                            MidpointRounding.AwayFromZero);

                        orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);

                        foreach (ProductAvailability availability in message.OrderItem.Product.ProductAvailabilities)
                            if (!availability.Storage.ForDefective) {
                                if (availability.Storage.OrganizationId.Equals(clientAgreement.Agreement.OrganizationId))
                                    if (availability.Storage.Locale.ToLower().Equals("pl")) {
                                        if (availability.Storage.ForVatProducts)
                                            message.OrderItem.Product.AvailableQtyPlVAT += availability.Amount;
                                        else
                                            message.OrderItem.Product.AvailableQtyPl += availability.Amount;
                                    } else {
                                        if (availability.Storage.ForVatProducts) {
                                            message.OrderItem.Product.AvailableQtyUkVAT += availability.Amount;

                                            if (availability.Storage.AvailableForReSale)
                                                message.OrderItem.Product.AvailableQtyUkReSale += availability.Amount;
                                        } else {
                                            message.OrderItem.Product.AvailableQtyUk += availability.Amount;
                                        }
                                    }
                                else if (availability.Storage.AvailableForReSale && !availability.Storage.Locale.ToLower().Equals("pl"))
                                    message.OrderItem.Product.AvailableQtyUkReSale += availability.Amount;
                            }


                        Sale saleFromDb = saleRepository.GetByNetId(message.SaleNetId);

                        if (saleFromDb == null && message.Sale != null) saleFromDb = saleRepository.GetByNetId(message.Sale.NetUid);

                        if (saleFromDb != null)
                            foreach (OrderItem item in saleFromDb.Order.OrderItems)
                                if (item.Id.Equals(orderItem.Id) && item.Discount != 0)
                                    orderItem.Discount = item.Discount;

                        ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR)
                            .Forward(new GetOrderItemAndSaleStatisticAndIsNewSaleMessage(orderItem, sale.NetUid, isNewSale));

                        if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) continue;

                        ClientInDebt clientInDebt =
                            _clientRepositoriesFactory
                                .NewClientInDebtRepository(connection)
                                .GetBySaleAndClientAgreementIds(sale.Id, sale.ClientAgreementId);

                        orderItem.Product.CurrentLocalPrice = orderItem.Product.CurrentPrice * orderItem.ExchangeRateAmount;

                        if (clientInDebt != null) {
                            clientAgreement =
                                _clientRepositoriesFactory
                                    .NewClientAgreementRepository(connection)
                                    .GetByNetIdWithAgreementAndDiscountForSpecificProduct(sale.ClientAgreement.NetUid, orderItem.Product.Id);

                            if (clientAgreement.Agreement.Currency.Code.Equals("EUR"))
                                clientInDebt.Debt.Total =
                                    decimal.Round(
                                        clientInDebt.Debt.Total
                                        +
                                        decimal.Round(Convert.ToDecimal(currentQty) * orderItem.Product.CurrentPrice, 14,
                                            MidpointRounding.AwayFromZero),
                                        2,
                                        MidpointRounding.AwayFromZero
                                    );
                            else
                                clientInDebt.Debt.Total += Convert.ToDecimal(currentQty) * orderItem.Product.CurrentLocalPrice;

                            _saleRepositoriesFactory.NewDebtRepository(connection).Update(clientInDebt.Debt);
                        } else {
                            Debt debt = new() {
                                Days = 0,
                                Total = 0
                            };

                            clientAgreement =
                                _clientRepositoriesFactory
                                    .NewClientAgreementRepository(connection)
                                    .GetByNetIdWithAgreementAndDiscountForSpecificProduct(sale.ClientAgreement.NetUid, orderItem.Product.Id);

                            sale = saleRepository.GetByNetId(sale.NetUid);

                            debt.EuroTotal =
                                decimal.Round(Convert.ToDecimal(currentQty) * orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);

                            if (clientAgreement.CurrentAmount >= debt.EuroTotal) {
                                clientAgreement.CurrentAmount = decimal.Round(
                                    clientAgreement.CurrentAmount - debt.EuroTotal,
                                    2,
                                    MidpointRounding.AwayFromZero
                                );

                                _clientRepositoriesFactory
                                    .NewClientAgreementRepository(connection)
                                    .UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                _clientRepositoriesFactory
                                    .NewClientBalanceMovementRepository(connection)
                                    .AddOutMovement(
                                        new ClientBalanceMovement {
                                            ClientAgreementId = clientAgreement.Id,
                                            Amount = debt.EuroTotal,
                                            ExchangeRateAmount = orderItem.ExchangeRateAmount
                                        }
                                    );
                            } else {
                                if (clientAgreement.CurrentAmount > 0) {
                                    debt.EuroTotal = decimal.Round(debt.EuroTotal - clientAgreement.CurrentAmount, 2, MidpointRounding.AwayFromZero);

                                    _clientRepositoriesFactory
                                        .NewClientBalanceMovementRepository(connection)
                                        .AddOutMovement(
                                            new ClientBalanceMovement {
                                                ClientAgreementId = clientAgreement.Id,
                                                Amount = clientAgreement.CurrentAmount,
                                                ExchangeRateAmount = orderItem.ExchangeRateAmount
                                            }
                                        );

                                    clientAgreement.CurrentAmount = decimal.Zero;

                                    _clientRepositoriesFactory
                                        .NewClientAgreementRepository(connection)
                                        .UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                                }

                                if (clientAgreement.Agreement.Currency.Code.Equals("EUR"))
                                    debt.Total = debt.EuroTotal;
                                else
                                    debt.Total = debt.EuroTotal * orderItem.ExchangeRateAmount;

                                clientInDebt = new ClientInDebt {
                                    AgreementId = clientAgreement.AgreementId,
                                    ClientId = clientAgreement.ClientId,
                                    SaleId = sale.Id,
                                    DebtId = _saleRepositoriesFactory.NewDebtRepository(connection).Add(debt)
                                };

                                _clientRepositoriesFactory.NewClientInDebtRepository(connection).Add(clientInDebt);

                                if (!sale.BaseSalePaymentStatus.SalePaymentStatusType.Equals(SalePaymentStatusType.PartialPaid))
                                    _saleRepositoriesFactory
                                        .NewBaseSalePaymentStatusRepository(connection)
                                        .SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid, sale.BaseSalePaymentStatusId);
                            }
                        }

                        message.OrderItem.Id = orderItemFromDb.Id;
                        message.OrderItem.ProductId = orderItemFromDb.ProductId;
                    } else {
                        message.OrderItem.UserId = user.Id;
                        message.OrderItem.OrderId = sale.OrderId;

                        if (message.OrderItem.Product != null) message.OrderItem.ProductId = message.OrderItem.Product.Id;

                        if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaging)) {
                            message.OrderItem.PricePerItem = message.OrderItem.Product.CurrentPrice;
                            message.OrderItem.ExchangeRateAmount =
                                _exchangeRateRepositoryFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCultureFiltered(
                                    message.OrderItem.Product.NetUid,
                                    sale.IsVatSale,
                                    message.OrderItem.IsFromReSale,
                                    clientAgreement.Agreement.Currency.Id
                                );
                        }

                        if (message.OrderItem.Qty > productAvailability.Amount)
                            message.OrderItem.Qty = productAvailability.Amount;

                        long orderItemId = orderItemRepository.Add(message.OrderItem);

                        OrderItem orderItem = orderItemRepository.GetBySaleNetIdAndOrderItemId(
                            sale.NetUid,
                            orderItemId,
                            sale.ClientAgreement.Agreement.OrganizationId ?? 0
                        );

                        if (toDecreaseQty > 0) {
                            if (productAvailability.Amount >= toDecreaseQty) {
                                productReservationRepository
                                    .Add(new ProductReservation {
                                        OrderItemId = orderItem.Id,
                                        ProductAvailabilityId = productAvailability.Id,
                                        Qty = toDecreaseQty,
                                        IsReSaleReservation = productAvailability.IsReSaleAvailability
                                    });

                                productAvailability.Amount -= toDecreaseQty;

                                toDecreaseQty = 0d;
                            } else {
                                productReservationRepository
                                    .Add(new ProductReservation {
                                        OrderItemId = orderItem.Id,
                                        ProductAvailabilityId = productAvailability.Id,
                                        Qty = productAvailability.Amount,
                                        IsReSaleReservation = productAvailability.IsReSaleAvailability
                                    });

                                toDecreaseQty -= productAvailability.Amount;

                                productAvailability.Amount = 0d;
                            }

                            productAvailabilityRepository.Update(productAvailability);
                        }

                        _saleRepositoriesFactory
                            .NewOrderItemMovementRepository(connection)
                            .Add(new OrderItemMovement {
                                OrderItemId = orderItemId,
                                UserId = user.Id,
                                Qty = currentQty
                            });

                        orderItem.Product.ProductAvailabilities = productAvailabilities.ToList();

                        orderItem =
                            orderItemRepository
                                .GetWithCalculatedProductPrices(
                                    orderItem.NetUid,
                                    clientAgreement.NetUid,
                                    clientAgreement.Agreement.OrganizationId ?? 0,
                                    sale.IsVatSale,
                                    orderItem.IsFromReSale
                                );

                        if (!orderItem.PricePerItem.Equals(decimal.Zero)) {
                            orderItem.Product.CurrentPrice = orderItem.PricePerItem;
                            orderItem.Product.CurrentLocalPrice = orderItem.Product.CurrentPrice * orderItem.ExchangeRateAmount;
                        }

                        orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;

                        orderItem.TotalAmount = decimal.Round(
                            orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty),
                            14,
                            MidpointRounding.AwayFromZero
                        );

                        orderItem.TotalAmountEurToUah = decimal.Round(
                            orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty),
                            14,
                            MidpointRounding.AwayFromZero
                        );
                        orderItem.TotalAmountLocal = decimal.Round(orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14,
                            MidpointRounding.AwayFromZero);

                        orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);

                        foreach (ProductAvailability availability in message.OrderItem.Product.ProductAvailabilities)
                            if (!availability.Storage.ForDefective) {
                                if (availability.Storage.OrganizationId.Equals(clientAgreement.Agreement.OrganizationId))
                                    if (availability.Storage.Locale.ToLower().Equals("pl")) {
                                        if (availability.Storage.ForVatProducts)
                                            message.OrderItem.Product.AvailableQtyPlVAT += availability.Amount;
                                        else
                                            message.OrderItem.Product.AvailableQtyPl += availability.Amount;
                                    } else {
                                        if (availability.Storage.ForVatProducts) {
                                            message.OrderItem.Product.AvailableQtyUkVAT += availability.Amount;

                                            if (availability.Storage.AvailableForReSale)
                                                message.OrderItem.Product.AvailableQtyUkReSale += availability.Amount;
                                        } else {
                                            message.OrderItem.Product.AvailableQtyUk += availability.Amount;
                                        }
                                    }
                                else if (availability.Storage.AvailableForReSale && !availability.Storage.Locale.ToLower().Equals("pl"))
                                    message.OrderItem.Product.AvailableQtyUkReSale += availability.Amount;
                            }

                        if (sale.ChangedToInvoice.HasValue) {
                            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(
                                new StoreConsignmentMovementFromNewOrderItemMessage(
                                    orderItem.Id,
                                    sale.Id,
                                    ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR),
                                    Sender,
                                    message.UserNetId
                                )
                            );
                        } else {
                            Sale saleFromDb = saleRepository.GetByNetId(message.SaleNetId);
                            if (saleFromDb == null && message.Sale != null) saleFromDb = saleRepository.GetByNetId(message.Sale.NetUid);

                            if (saleFromDb != null)
                                foreach (OrderItem item in saleFromDb.Order.OrderItems)
                                    if (item.Id.Equals(orderItem.Id) && item.Discount != 0)
                                        orderItem.Discount = item.Discount;

                            ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR)
                                .Forward(new GetOrderItemAndSaleStatisticAndIsNewSaleMessage(orderItem, sale.NetUid, isNewSale));
                        }


                        if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) continue;

                        ClientInDebt clientInDebt =
                            _clientRepositoriesFactory
                                .NewClientInDebtRepository(connection)
                                .GetBySaleAndClientAgreementIds(sale.Id, sale.ClientAgreementId);

                        orderItem.Product.CurrentLocalPrice = orderItem.Product.CurrentPrice * orderItem.ExchangeRateAmount;

                        if (clientInDebt != null) {
                            clientAgreement =
                                _clientRepositoriesFactory
                                    .NewClientAgreementRepository(connection)
                                    .GetByNetIdWithAgreementAndDiscountForSpecificProduct(sale.ClientAgreement.NetUid, orderItem.Product.Id);

                            if (clientAgreement.Agreement.Currency.Code.Equals("EUR"))
                                clientInDebt.Debt.Total =
                                    decimal.Round(
                                        clientInDebt.Debt.Total
                                        +
                                        decimal.Round(Convert.ToDecimal(currentQty) * orderItem.Product.CurrentPrice, 2,
                                            MidpointRounding.AwayFromZero),
                                        2,
                                        MidpointRounding.AwayFromZero
                                    );
                            else
                                clientInDebt.Debt.Total += Convert.ToDecimal(currentQty) * orderItem.Product.CurrentLocalPrice;

                            _saleRepositoriesFactory.NewDebtRepository(connection).Update(clientInDebt.Debt);
                        } else {
                            Debt debt = new() {
                                Days = 0,
                                Total = 0
                            };

                            clientAgreement =
                                _clientRepositoriesFactory
                                    .NewClientAgreementRepository(connection)
                                    .GetByNetIdWithAgreementAndDiscountForSpecificProduct(sale.ClientAgreement.NetUid, orderItem.Product.Id);

                            sale = saleRepository.GetByNetId(sale.NetUid);

                            debt.EuroTotal =
                                decimal.Round(Convert.ToDecimal(currentQty) * orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);

                            if (clientAgreement.CurrentAmount >= debt.EuroTotal) {
                                clientAgreement.CurrentAmount = decimal.Round(
                                    clientAgreement.CurrentAmount - debt.EuroTotal,
                                    2,
                                    MidpointRounding.AwayFromZero
                                );

                                _clientRepositoriesFactory
                                    .NewClientAgreementRepository(connection)
                                    .UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                                _clientRepositoriesFactory
                                    .NewClientBalanceMovementRepository(connection)
                                    .AddOutMovement(
                                        new ClientBalanceMovement {
                                            ClientAgreementId = clientAgreement.Id,
                                            Amount = debt.EuroTotal,
                                            ExchangeRateAmount = orderItem.ExchangeRateAmount
                                        }
                                    );
                            } else {
                                if (clientAgreement.CurrentAmount > 0) {
                                    debt.EuroTotal = decimal.Round(debt.EuroTotal - clientAgreement.CurrentAmount, 2, MidpointRounding.AwayFromZero);

                                    _clientRepositoriesFactory
                                        .NewClientBalanceMovementRepository(connection)
                                        .AddOutMovement(
                                            new ClientBalanceMovement {
                                                ClientAgreementId = clientAgreement.Id,
                                                Amount = clientAgreement.CurrentAmount,
                                                ExchangeRateAmount = orderItem.ExchangeRateAmount
                                            }
                                        );
                                    clientAgreement.CurrentAmount = decimal.Zero;

                                    _clientRepositoriesFactory
                                        .NewClientAgreementRepository(connection)
                                        .UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                                }

                                if (clientAgreement.Agreement.Currency.Code.Equals("EUR"))
                                    debt.Total = debt.EuroTotal;
                                else
                                    debt.Total = debt.EuroTotal * orderItem.ExchangeRateAmount;

                                clientInDebt = new ClientInDebt {
                                    AgreementId = clientAgreement.AgreementId,
                                    ClientId = clientAgreement.ClientId,
                                    SaleId = sale.Id,
                                    DebtId = _saleRepositoriesFactory.NewDebtRepository(connection).Add(debt)
                                };

                                _clientRepositoriesFactory.NewClientInDebtRepository(connection).Add(clientInDebt);

                                if (!sale.BaseSalePaymentStatus.SalePaymentStatusType.Equals(SalePaymentStatusType.PartialPaid))
                                    _saleRepositoriesFactory
                                        .NewBaseSalePaymentStatusRepository(connection)
                                        .SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid, sale.BaseSalePaymentStatusId);
                            }
                        }

                        message.OrderItem.Id = orderItemId;
                    }
                }

                ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR).Tell(new UpdatedSaleNotificationMessage(saleNetId));
            } else {
                message.OrderItem.Product.ProductAvailabilities = productAvailabilities.ToList();

                foreach (ProductAvailability productAvailability in message.OrderItem.Product.ProductAvailabilities)
                    if (!productAvailability.Storage.ForDefective) {
                        if (productAvailability.Storage.OrganizationId.Equals(clientAgreement.Agreement.OrganizationId))
                            if (productAvailability.Storage.Locale.ToLower().Equals("pl")) {
                                if (productAvailability.Storage.ForVatProducts)
                                    message.OrderItem.Product.AvailableQtyPlVAT += productAvailability.Amount;
                                else
                                    message.OrderItem.Product.AvailableQtyPl += productAvailability.Amount;
                            } else {
                                if (productAvailability.Storage.ForVatProducts) {
                                    message.OrderItem.Product.AvailableQtyUkVAT += productAvailability.Amount;

                                    if (productAvailability.Storage.AvailableForReSale)
                                        message.OrderItem.Product.AvailableQtyUkReSale += productAvailability.Amount;
                                } else {
                                    message.OrderItem.Product.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        else if (productAvailability.Storage.AvailableForReSale && !productAvailability.Storage.Locale.ToLower().Equals("pl"))
                            message.OrderItem.Product.AvailableQtyUkReSale += productAvailability.Amount;
                    }

                Sale saleFromDb = saleRepository.GetByNetId(message.SaleNetId);

                if (saleFromDb == null && message.Sale != null) saleFromDb = saleRepository.GetByNetId(message.Sale.NetUid);

                if (saleFromDb != null)
                    foreach (OrderItem item in saleFromDb.Order.OrderItems)
                        if (item.Id.Equals(message.OrderItem.Id) && item.Discount != 0)
                            message.OrderItem.Discount = item.Discount;

                ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR)
                    .Forward(
                        new GetOrderItemAndSaleStatisticAndIsNewSaleMessage(
                            message.OrderItem,
                            Guid.Empty,
                            false,
                            OrderItemsResourceNames.STOLEN
                        )
                    );
            }
        } catch (UserForbidenException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateOrderItemMessage(UpdateOrderItemMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);
            IOrderItemMovementRepository orderItemMovementRepository = _saleRepositoriesFactory.NewOrderItemMovementRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            if (message.OrderItem == null) return;

            OrderItem orderItemFromDb;

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            if (!message.OrderItem.OrderId.HasValue)
                orderItemFromDb = orderItemRepository.GetByNetId(message.OrderItem.NetUid);
            else
                orderItemFromDb = orderItemRepository
                        .GetFilteredByIds(
                            message.OrderItem.OrderId.Value,
                            message.OrderItem.Product.Id,
                            message.OrderItem.IsFromReSale)
                    ;

            Sale sale = saleRepository.GetByOrderId(orderItemFromDb.OrderId ?? 0);
            //sale.UpdateUserId = user.Id;
            //saleRepository.UpdateUser(sale);

            if (sale.IsLocked) {
                if (user.UserRoleId.HasValue) {
                    if (!user.UserRole.UserRoleType.Equals(UserRoleType.GBA) && !user.UserRole.UserRoleType.Equals(UserRoleType.HeadSalesAnalytic))
                        throw new UserForbidenException();
                } else {
                    throw new UserForbidenException();
                }
            }

            double qtyDifference = orderItemFromDb.Qty - message.OrderItem.Qty;

            IEnumerable<ProductAvailability> productAvailabilities =
                productAvailabilityRepository
                    .GetByProductAndOrganizationIds(
                        message.OrderItem.ProductId,
                        sale.ClientAgreement.Agreement.Organization.Id,
                        sale.ClientAgreement.Agreement.Organization.Culture != "pl" && sale.ClientAgreement.Agreement.WithVATAccounting,
                        true,
                        sale.ClientAgreement.Agreement.Organization?.StorageId
                    );

            if (qtyDifference.Equals(0d)) {
                orderItemRepository.Update(message.OrderItem);

                ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR).Forward(new GetOrderItemAndSaleStatisticMessage(orderItemFromDb.Id, sale.NetUid));

                return;
            }

            if (!productAvailabilities.Any(a => a.Amount > 0))
                throw new Exception(OrderItemsResourceNames.STOLEN);

            if (qtyDifference > 0d) {
                //qty decreased
                IEnumerable<OrderItemMovement> movements =
                    orderItemMovementRepository
                        .GetAllByOrderItemId(orderItemFromDb.Id);

                IEnumerable<ProductReservation> reservations =
                    productReservationRepository
                        .GetAllByOrderItemIdWithAvailabilityAndReSaleAvailabilities(
                            orderItemFromDb.Id
                        );

                double availableToDecreaseQty = reservations.Sum(r => r.Qty);

                qtyDifference =
                    availableToDecreaseQty < qtyDifference
                        ? availableToDecreaseQty
                        : qtyDifference;

                if (movements.Any(m => m.UserId.Equals(user.Id))) {
                    if (movements.Where(m => m.UserId.Equals(user.Id)).Sum(m => m.Qty) >= qtyDifference) {
                        double currentQtyDifference = qtyDifference;

                        foreach (OrderItemMovement movement in movements.Where(m => m.UserId.Equals(user.Id)))
                            if (movement.Qty < currentQtyDifference) {
                                currentQtyDifference -= movement.Qty;

                                orderItemMovementRepository.Remove(movement.Id);
                            } else {
                                movement.Qty -= currentQtyDifference;

                                if (movement.Qty.Equals(0d))
                                    orderItemMovementRepository.Remove(movement.Id);
                                else
                                    orderItemMovementRepository.Update(movement);

                                break;
                            }
                    } else {
                        double currentQtyDifference = qtyDifference;

                        foreach (OrderItemMovement movement in movements.OrderBy(m => m.UserId.Equals(user.Id)))
                            if (movement.Qty < currentQtyDifference) {
                                currentQtyDifference -= movement.Qty;

                                orderItemMovementRepository.Remove(movement.Id);
                            } else {
                                movement.Qty -= currentQtyDifference;

                                if (movement.Qty.Equals(0d))
                                    orderItemMovementRepository.Remove(movement.Id);
                                else
                                    orderItemMovementRepository.Update(movement);

                                break;
                            }
                    }
                } else if (movements.Any()) {
                    double currentQtyDifference = qtyDifference;

                    foreach (OrderItemMovement movement in movements)
                        if (movement.Qty < currentQtyDifference) {
                            currentQtyDifference -= movement.Qty;

                            orderItemMovementRepository.Remove(movement.Id);
                        } else {
                            movement.Qty -= currentQtyDifference;

                            if (movement.Qty.Equals(0d))
                                orderItemMovementRepository.Remove(movement.Id);
                            else
                                orderItemMovementRepository.Update(movement);

                            break;
                        }
                }

                double toRestoreAmount = qtyDifference;

                foreach (ProductReservation reservation in reservations) {
                    if (toRestoreAmount.Equals(0d)) break;

                    if (reservation.ReSaleAvailabilities.Any()) {
                        double restoreReSalesQty = toRestoreAmount;

                        foreach (ReSaleAvailability reSaleAvailability in reservation.ReSaleAvailabilities) {
                            if (restoreReSalesQty.Equals(0d)) break;

                            if (restoreReSalesQty > reSaleAvailability.Qty) {
                                reSaleAvailabilityRepository.Delete(reSaleAvailability.Id);

                                restoreReSalesQty -= reSaleAvailability.Qty;
                            } else {
                                reSaleAvailability.Qty -= restoreReSalesQty;

                                reSaleAvailabilityRepository.Update(reSaleAvailability);

                                restoreReSalesQty = 0d;
                            }
                        }
                    }

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
            } else {
                //qty increased
                qtyDifference = 0 - qtyDifference;

                if (qtyDifference > productAvailabilities.Sum(a => a.Amount)) {
                    double availableQty = productAvailabilities.Sum(a => a.Amount);

                    message.OrderItem.Qty -= qtyDifference - availableQty;

                    qtyDifference = availableQty;
                }

                orderItemMovementRepository
                    .Add(new OrderItemMovement {
                        OrderItemId = orderItemFromDb.Id,
                        UserId = user.Id,
                        Qty = qtyDifference
                    });

                double toDecreaseQty = qtyDifference;

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
                                    Qty = toDecreaseQty,
                                    IsReSaleReservation = productAvailability.IsReSaleAvailability
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
                                    Qty = productAvailability.Amount,
                                    IsReSaleReservation = productAvailability.IsReSaleAvailability
                                });
                        }

                        toDecreaseQty -= productAvailability.Amount;

                        productAvailability.Amount = 0d;
                    }

                    productAvailabilityRepository.Update(productAvailability);
                }
            }

            orderItemRepository.Update(message.OrderItem);

            ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR).Forward(new GetOrderItemAndSaleStatisticMessage(orderItemFromDb.Id, sale.NetUid));
        } catch (UserForbidenException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessDeleteOrderItemMessage(DeleteOrderItemMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            OrderItem orderItem = orderItemRepository.GetByNetId(message.OrderItemNetId);

            if (orderItem == null) return;

            Sale sale = saleRepository.GetByOrderItemNetId(orderItem.NetUid);

            if (sale.IsLocked) {
                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

                if (user.UserRoleId.HasValue) {
                    if (!user.UserRole.UserRoleType.Equals(UserRoleType.GBA) && !user.UserRole.UserRoleType.Equals(UserRoleType.HeadSalesAnalytic))
                        throw new UserForbidenException();
                } else {
                    throw new UserForbidenException();
                }
            }

            _saleRepositoriesFactory.NewOrderItemMovementRepository(connection).RemoveAllByOrderItemId(orderItem.Id);

            IEnumerable<ProductReservation> reservations =
                productReservationRepository
                    .GetAllByOrderItemIdWithAvailabilityAndReSaleAvailabilities(
                        orderItem.Id
                    );

            if (reservations.Sum(r => r.Qty) < orderItem.Qty)
                throw new Exception(OrderItemsResourceNames.UNABLE_TO_DELETE);

            foreach (ProductReservation reservation in reservations) {
                foreach (ReSaleAvailability reSaleAvailability in reservation.ReSaleAvailabilities) reSaleAvailabilityRepository.Delete(reSaleAvailability.Id);

                reservation.ProductAvailability.Amount += reservation.Qty;

                productAvailabilityRepository.Update(reservation.ProductAvailability);

                productReservationRepository.Delete(reservation.NetUid);
            }

            orderItemRepository.Remove(orderItem);

            IActorRef hubsActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

            hubsActorRef.Tell(new UpdatedSaleNotificationMessage(sale.NetUid));

            hubsActorRef.Tell(new GetProductNotificationMessage(orderItem.ProductId, sale.ClientAgreement.NetUid));

            Sender.Tell(null);

            Sale saleFromDb = saleRepository.GetByNetIdWithDeletedOrderItems(sale.NetUid);

            if (saleFromDb.Order.OrderItems.Any(o => !o.Deleted)) return;

            if (!sale.OutputSaleMerges.Any()) return;

            SaleMerged saleMerged = sale.OutputSaleMerges.First();

            _saleRepositoriesFactory.NewSaleMergedRepository(connection).Remove(saleMerged.NetUid);

            Sale outputSale = saleRepository.GetByIdWithSaleMerged(saleMerged.OutputSaleId);

            if (!outputSale.InputSaleMerges.Any()) saleRepository.Remove(outputSale.NetUid);
        } catch (UserForbidenException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private async Task ProcessShiftSaleOrderItemsMessage(ShiftSaleOrderItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
        IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
        IProductLocationHistoryRepository productLocationHistoryRepository = _productRepositoriesFactory.NewProductLocationHistoryRepository(connection);
        IOrderItemBaseShiftStatusRepository orderItemShiftStatusRepository = _saleRepositoriesFactory.NewOrderItemBaseShiftStatusRepository(connection);
        IHistoryInvoiceEditRepository historyInvoiceEditRepository = _saleRepositoriesFactory.NewHistoryInvoiceEditRepository(connection);
        IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

        try {
            Sale saleFromDb = saleRepository.GetByNetId(message.Sale.NetUid);
            Sale childSale = saleRepository.GetChildSaleIdIfExist(message.Sale.NetUid);
            List<ShiftedOrderItem> shiftedToStorageOrderItems = new();
            List<ShiftedOrderItem> shiftedToBillOrderItems = new();
            bool isNewSale = false;

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            //1050 

            //if (saleFromDb.IsLocked) {
            //    if (user.UserRoleId.HasValue) {
            //        if (!user.UserRole.UserRoleType.Equals(UserRoleType.GBA) && !user.UserRole.UserRoleType.Equals(UserRoleType.HeadSalesAnalytic))
            //            throw new UserForbidenException();
            //    } else {
            //        throw new UserForbidenException();
            //    }
            //}

            foreach (OrderItem orderItem in message.Sale.Order.OrderItems.Where(e => e.ShiftStatuses.Any(s => s.IsNew())))
            foreach (OrderItemBaseShiftStatus shiftStatus in orderItem.ShiftStatuses.Where(s => s.IsNew())) {
                double availableQty =
                    productReservationRepository
                        .GetAvailableSumByOrderItemIdWithAvailabilityAndReSaleAvailabilities(orderItem.Id);

                if (shiftStatus.Qty > availableQty - orderItem.ReturnedQty)
                    throw new LocalizedException(
                        OrderItemsResourceNames.UNABLE_TO_SHIFT_SPECIFIED_QTY,
                        $"{orderItem.Product.VendorCode} = {availableQty - orderItem.ReturnedQty}"
                    );
            }

            if (message.Sale.Order.OrderItems.Any(o =>
                    o.Qty < o.ShiftStatuses.Where(s => s.IsNew()).Sum(s => s.Qty))) {
                Sender.Tell(new Tuple<SaleStatistic, string>(null, OrderItemsResourceNames.QTY_MORE_THAN_ORDERED));
            } else if (message.Sale.Order.OrderItems.Any(o => o.ShiftStatuses.Any(s => s.Qty < 0))) {
                Sender.Tell(new Tuple<SaleStatistic, string>(null, OrderItemsResourceNames.QTY_LESS_THAN_ZERO));
            } else {
                if (saleFromDb.ShiftStatus == null) {
                    message.Sale.ShiftStatusId = _saleRepositoriesFactory.NewSaleBaseShiftStatusRepository(connection).Add(new SalePartialShiftStatus {
                        Comment = message.Sale?.ShiftStatus?.Comment ?? string.Empty
                    });

                    saleRepository.Update(message.Sale);
                }

                long historyId = 0L;

                if (!message.BillReturn) {
                    HistoryInvoiceEdit historyInvoiceEdit = new() { SaleId = message.Sale.Id, IsDevelopment = false };
                    if (saleFromDb.IsPrinted) historyInvoiceEdit.IsPrinted = true;

                    //else {
                    //    historyInvoiceEdit.ApproveUpdate = true;
                    //}
                    historyId = historyInvoiceEditRepository.Add(historyInvoiceEdit);
                }

                saleRepository.SetIsPrintedActProtocolEditFalse(saleFromDb.Id);


                foreach (OrderItem orderItem in message.Sale.Order.OrderItems.Where(o => o.Qty > 0)) {
                    bool isNew = orderItem.ShiftStatuses.Any(s => s.IsNew());
                    if (!saleFromDb.HistoryInvoiceEdit.Any()) orderItem.InvoiceDocumentQty = orderItem.Qty;


                    if (!isNew) {
                        OrderItemBaseShiftStatus shiftStatus = new();
                        shiftStatus.OrderItemId = orderItem.Id;
                        shiftStatus.UserId = user.Id;
                        shiftStatus.CurrentQty = orderItem.Qty;

                        if (!message.BillReturn)
                            shiftStatus.HistoryInvoiceEditId = historyId;
                        shiftStatus.CurrentId = orderItemShiftStatusRepository.Add(shiftStatus);
                    }

                    foreach (OrderItemBaseShiftStatus shiftStatus in orderItem.ShiftStatuses.Where(s => s.IsNew())) {
                        if (shiftStatus.ShiftStatus.Equals(OrderItemShiftStatus.Store)) {
                            shiftStatus.OrderItemId = orderItem.Id;
                            shiftStatus.UserId = user.Id;
                            shiftStatus.CurrentQty = orderItem.Qty - shiftStatus.Qty;

                            if (!saleFromDb.IsPrinted) orderItem.InvoiceDocumentQty = orderItem.Qty - shiftStatus.Qty;

                            if (!message.BillReturn)
                                shiftStatus.HistoryInvoiceEditId = historyId;
                            shiftStatus.CurrentId = orderItemShiftStatusRepository.Add(shiftStatus);

                            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                                .Tell(new StoreConsignmentMovementFromOrderItemBaseShiftStatusMessage(shiftStatus.CurrentId));

                            shiftedToStorageOrderItems.Add(new ShiftedOrderItem(orderItem.ProductId, orderItem.Id, shiftStatus.Qty, shiftStatus.UserId));
                        } else {
                            if (childSale != null) {
                                if (childSale.Order.OrderItems.Any(o => o.ProductId.Equals(orderItem.ProductId))) {
                                    OrderItem childOrderItem = childSale.Order.OrderItems.First(o => o.ProductId.Equals(orderItem.ProductId));

                                    childOrderItem.Qty += shiftStatus.Qty;

                                    orderItemRepository.Update(childOrderItem);

                                    shiftedToBillOrderItems.Add(new ShiftedOrderItem(
                                        orderItem.ProductId,
                                        childOrderItem.Id,
                                        shiftStatus.Qty,
                                        user.Id,
                                        orderItem.Id)
                                    );
                                } else {
                                    OrderItem childOrderItem = new() {
                                        Comment = orderItem.Comment,
                                        Created = orderItem.Created,
                                        IsValidForCurrentSale = orderItem.IsValidForCurrentSale,
                                        OrderId = childSale.OrderId,
                                        ProductId = orderItem.ProductId,
                                        Qty = shiftStatus.Qty,
                                        UserId = orderItem.UserId,
                                        Vat = orderItem.Vat,
                                        IsFromShiftedItem = true
                                    };

                                    shiftedToBillOrderItems.Add(new ShiftedOrderItem(
                                        orderItem.ProductId,
                                        orderItemRepository.Add(childOrderItem),
                                        shiftStatus.Qty,
                                        orderItem.Id)
                                    );
                                }
                            } else {
                                Order newOrder = new() {
                                    ClientAgreementId = message.Sale.Order.ClientAgreementId,
                                    Created = message.Sale.Order.Created,
                                    OrderSource = message.Sale.Order.OrderSource,
                                    UserId = message.Sale.Order.UserId
                                };

                                ISaleNumberRepository saleNumberRepository = _saleRepositoriesFactory.NewSaleNumberRepository(connection);
                                SaleNumber saleNumber = new();
                                if (message.Sale.ClientAgreement.Agreement?.Organization != null) {
                                    SaleNumber lastSaleNumber =
                                        saleNumberRepository.GetLastRecordByOrganizationNetId(message.Sale.ClientAgreement.Agreement.Organization.NetUid);

                                    Organization organization = message.Sale.ClientAgreement.Agreement.Organization;
                                    string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

                                    if (lastSaleNumber != null && !string.IsNullOrEmpty(lastSaleNumber.Value) && DateTime.Now.Year.Equals(lastSaleNumber.Created.Year))
                                        saleNumber = new SaleNumber {
                                            OrganizationId = organization.Id,
                                            Value =
                                                string.Format(
                                                    "{0}{1}{2}",
                                                    organization.Code,
                                                    currentMonth,
                                                    string.Format("{0:D8}",
                                                        Convert.ToInt32(
                                                            lastSaleNumber.Value.Substring(
                                                                lastSaleNumber.Organization.Code.Length + currentMonth.Length,
                                                                lastSaleNumber.Value.Length - (lastSaleNumber.Organization.Code.Length + currentMonth.Length))) + 1)
                                                )
                                        };
                                    else
                                        saleNumber = new SaleNumber {
                                            OrganizationId = organization.Id,
                                            Value = $"{organization.Code}{currentMonth}{string.Format("{0:D8}", 1)}"
                                        };
                                }

                                newOrder.Id = _saleRepositoriesFactory.NewOrderRepository(connection).Add(newOrder);

                                OrderItem childOrderItem = new() {
                                    Comment = orderItem.Comment,
                                    Created = orderItem.Created,
                                    IsValidForCurrentSale = orderItem.IsValidForCurrentSale,
                                    OrderId = newOrder.Id,
                                    ProductId = orderItem.ProductId,
                                    Qty = shiftStatus.Qty,
                                    UserId = orderItem.UserId ?? user.Id,
                                    Vat = orderItem.Vat,
                                    IsFromShiftedItem = true
                                };

                                shiftedToBillOrderItems.Add(new ShiftedOrderItem(
                                    orderItem.ProductId,
                                    orderItemRepository.Add(childOrderItem),
                                    shiftStatus.Qty,
                                    orderItem.UserId ?? user.Id,
                                    orderItem.Id)
                                );

                                long SaleNumberId = saleNumberRepository.Add(saleNumber);

                                childSale = new Sale {
                                    Comment = message.Sale.Comment,
                                    Created = message.Sale.Created,
                                    OrderId = newOrder.Id,
                                    ClientAgreementId = message.Sale.ClientAgreementId,
                                    ParentNetId = message.Sale.NetUid,
                                    SaleNumberId = SaleNumberId,
                                    UserId = message.Sale.UserId ?? user.Id,
                                    BaseLifeCycleStatus = new NewSaleLifeCycleStatus(),
                                    IsVatSale = message.Sale.IsVatSale
                                };

                                childSale.BaseLifeCycleStatusId =
                                    _saleRepositoriesFactory.NewBaseLifeCycleStatusRepository(connection).Add(childSale.BaseLifeCycleStatus);
                                childSale.BaseSalePaymentStatusId =
                                    _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection).Add(new NotPaidSalePaymentStatus());

                                childSale.Id = saleRepository.Add(childSale);

                                childSale = saleRepository.GetById(childSale.Id);

                                ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR)
                                    .Tell(
                                        new RetrieveAndStoreAuditDataMessage(
                                            message.UserNetId,
                                            childSale.NetUid,
                                            "Sale",
                                            childSale.BaseLifeCycleStatus
                                        )
                                    );

                                isNewSale = true;
                            }

                            shiftStatus.OrderItemId = orderItem.Id;
                            shiftStatus.UserId = user.Id;
                            shiftStatus.SaleId = childSale.Id;
                            shiftStatus.CurrentQty = orderItem.Qty - shiftStatus.Qty;

                            if (!message.BillReturn)
                                shiftStatus.HistoryInvoiceEditId = historyId;
                            shiftStatus.CurrentId = orderItemShiftStatusRepository.Add(shiftStatus);

                            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                                .Tell(new StoreConsignmentMovementFromOrderItemBaseShiftStatusMessage(shiftStatus.CurrentId));
                        }

                        orderItem.Qty -= shiftStatus.Qty;

                        orderItemRepository.Update(orderItem);
                    }
                }

                if (message.BillReturn) {
                    saleFromDb.Order.OrderStatus = OrderStatus.Closed;

                    _saleRepositoriesFactory.NewOrderRepository(connection).Update(saleFromDb.Order);
                }

                UpdateCurrentDebts(
                    message.Sale,
                    _saleRepositoriesFactory,
                    _clientRepositoriesFactory,
                    connection,
                    saleRepository,
                    _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection)
                );

                Sale mergedSale = await ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR)
                    .Ask<Sale>(new GetSaleMergeStatisticWithOrderItemsMerged(saleFromDb.NetUid));

                foreach (OrderItem orderItem in message.Sale.Order.OrderItems.Where(e => e.ShiftStatuses.Any(s => s.IsNew())))
                foreach (OrderItemBaseShiftStatus shiftStatus in orderItem.ShiftStatuses.Where(s => s.IsNew()))
                    if (mergedSale.InputSaleMerges.Any(s =>
                            s.InputSale.Order.OrderItems.Any(o =>
                                o.OrderItemMerges.Any(m => m.OrderItemId.Equals(orderItem.Id))))) {
                        OrderItem mergedOrderItem = mergedSale.InputSaleMerges
                            .First(s =>
                                s.InputSale.Order.OrderItems.Any(o =>
                                    o.OrderItemMerges.Any(m => m.OrderItemId.Equals(orderItem.Id)))).InputSale.Order
                            .OrderItems.First(o => o.OrderItemMerges.Any(m => m.OrderItemId.Equals(orderItem.Id)));

                        mergedOrderItem.Qty -= shiftStatus.Qty;

                        orderItemRepository.Update(mergedOrderItem);
                    }

                SaleStatistic toReturnSaleStatistic =
                    await ActorReferenceManager.Instance.Get(SalesActorNames.GET_SALE_BY_NET_ID_ACTOR)
                        .Ask<SaleStatistic>(new GetSaleByNetIdMessage(message.Sale.NetUid));

                IActorRef hubsActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

                if (isNewSale)
                    hubsActorRef.Tell(new AddedSaleNotificationMessage(toReturnSaleStatistic.Sale.NetUid));
                else
                    hubsActorRef.Tell(new UpdatedSaleNotificationMessage(toReturnSaleStatistic.Sale.NetUid));

                if (shiftedToStorageOrderItems.Any()) {
                    List<Product> products =
                        UpdatedProductReservationAndAvailabilityOnShiftedToStorage(
                            _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection),
                            _reSaleRepositoriesFactory.NewReSaleItemRepository(connection),
                            _productRepositoriesFactory,
                            _organizationRepositoriesFactory.NewOrganizationRepository(connection),
                            storageRepository,
                            connection,
                            shiftedToStorageOrderItems,
                            saleFromDb.ClientAgreement.Agreement.OrganizationId ?? 0
                        );

                    if (shiftedToBillOrderItems.Any()) {
                        List<Product> shiftedToBillProducts =
                            UpdatedProductReservationAndAvailabilityOnShiftedToBill(
                                _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection),
                                _productRepositoriesFactory,
                                connection,
                                shiftedToBillOrderItems,
                                saleFromDb.ClientAgreement.Agreement.OrganizationId ?? 0
                            );

                        shiftedToBillProducts.ForEach(product => {
                            if (!products.Any(p => p.Id.Equals(product.Id))) products.Add(product);
                        });
                    }

                    foreach (Product product in products) hubsActorRef.Tell(new GetProductNotificationMessage(product.Id, toReturnSaleStatistic.Sale.ClientAgreement.NetUid));

                    Sender.Tell(
                        new Tuple<SaleStatistic, string>(
                            toReturnSaleStatistic,
                            string.Empty
                        )
                    );
                } else {
                    if (shiftedToBillOrderItems.Any()) {
                        List<Product> shiftedToBillProducts =
                            UpdatedProductReservationAndAvailabilityOnShiftedToBill(
                                _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection),
                                _productRepositoriesFactory,
                                connection,
                                shiftedToBillOrderItems,
                                saleFromDb.ClientAgreement.Agreement.OrganizationId ?? 0
                            );

                        foreach (Product product in shiftedToBillProducts)
                            hubsActorRef.Tell(new GetProductNotificationMessage(product.Id, toReturnSaleStatistic.Sale.ClientAgreement.NetUid));

                        Sender.Tell(
                            new Tuple<SaleStatistic, string>(
                                toReturnSaleStatistic,
                                string.Empty
                            )
                        );
                    } else {
                        Sender.Tell(
                            new Tuple<SaleStatistic, string>(
                                toReturnSaleStatistic,
                                string.Empty
                            )
                        );
                    }
                }

                Sale afterShiftSale = saleRepository.GetByNetId(message.Sale.NetUid);

                if (!afterShiftSale.Order.OrderItems.Any()) {
                    afterShiftSale.ShiftStatus.ShiftStatus = SaleShiftStatus.Full;

                    _saleRepositoriesFactory.NewSaleBaseShiftStatusRepository(connection).Update(afterShiftSale.ShiftStatus);
                }
            }
        } catch (UserForbidenException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private async Task ProcessCloseExpiredOrdersMessage(CloseExpiredOrdersMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
            IOrderItemBaseShiftStatusRepository orderItemShiftStatusRepository = _saleRepositoriesFactory.NewOrderItemBaseShiftStatusRepository(connection);
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);
            Sale saleFromDb = saleRepository.GetByNetId(message.Sale.NetUid);
            List<ShiftedOrderItem> shiftedToStorageOrderItems = new();

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            foreach (OrderItem orderItem in message.Sale.Order.OrderItems.Where(e => e.ShiftStatuses.Any(s => s.IsNew())))
            foreach (OrderItemBaseShiftStatus shiftStatus in orderItem.ShiftStatuses.Where(s => s.IsNew())) {
                double availableQty =
                    productReservationRepository
                        .GetAvailableSumByOrderItemIdWithAvailabilityAndReSaleAvailabilities(orderItem.Id);

                if (shiftStatus.Qty > availableQty - orderItem.ReturnedQty)
                    throw new LocalizedException(
                        OrderItemsResourceNames.UNABLE_TO_SHIFT_SPECIFIED_QTY,
                        $"{orderItem.Product.VendorCode} = {availableQty - orderItem.ReturnedQty}"
                    );
            }

            if (message.Sale.Order.OrderItems.Any(o =>
                    o.Qty < o.ShiftStatuses.Where(s => s.IsNew()).Sum(s => s.Qty)))
                Sender.Tell(new Tuple<SaleStatistic, string>(null, OrderItemsResourceNames.QTY_MORE_THAN_ORDERED));
            else if (message.Sale.Order.OrderItems.Any(o => o.ShiftStatuses.Any(s => s.Qty < 0)))
                Sender.Tell(new Tuple<SaleStatistic, string>(null, OrderItemsResourceNames.QTY_LESS_THAN_ZERO));
            else
                foreach (OrderItem orderItem in message.Sale.Order.OrderItems.Where(o => o.Qty > 0))
                foreach (OrderItemBaseShiftStatus shiftStatus in orderItem.ShiftStatuses.Where(s => s.IsNew())) {
                    shiftStatus.OrderItemId = orderItem.Id;
                    shiftStatus.UserId = user.Id;
                    shiftStatus.CurrentQty = orderItem.Qty - shiftStatus.Qty;

                    if (!saleFromDb.IsPrinted) orderItem.InvoiceDocumentQty = orderItem.Qty - shiftStatus.Qty;

                    ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                        .Tell(new StoreConsignmentMovementFromOrderItemBaseShiftStatusMessage(shiftStatus.CurrentId));

                    shiftedToStorageOrderItems.Add(new ShiftedOrderItem(orderItem.ProductId, orderItem.Id, shiftStatus.Qty, shiftStatus.UserId));

                    orderItem.Qty -= shiftStatus.Qty;
                    orderItem.IsClosed = true;
                    orderItemRepository.Update(orderItem);
                }

            saleFromDb.Order.OrderStatus = OrderStatus.Closed;

            _saleRepositoriesFactory.NewOrderRepository(connection).Update(saleFromDb.Order);

            UpdateCurrentDebts(
                message.Sale,
                _saleRepositoriesFactory,
                _clientRepositoriesFactory,
                connection,
                saleRepository,
                _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection)
            );

            SaleStatistic toReturnSaleStatistic =
                await ActorReferenceManager.Instance.Get(SalesActorNames.GET_SALE_BY_NET_ID_ACTOR)
                    .Ask<SaleStatistic>(new GetSaleByNetIdMessage(message.Sale.NetUid));

            UpdatedProductReservationAndAvailabilityOnShiftedToStorage(
                _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection),
                _reSaleRepositoriesFactory.NewReSaleItemRepository(connection),
                _productRepositoriesFactory,
                _organizationRepositoriesFactory.NewOrganizationRepository(connection),
                storageRepository,
                connection,
                shiftedToStorageOrderItems,
                saleFromDb.ClientAgreement.Agreement.OrganizationId ?? 0
            );

            Sender.Tell(
                new Tuple<SaleStatistic, string>(
                    toReturnSaleStatistic,
                    string.Empty
                )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessShiftSaleOrderItemMessage(ShiftSaleOrderItemMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
            IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);
            IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(connection);
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            ISaleExchangeRateRepository saleExchangeRateRepository = _saleRepositoriesFactory.NewSaleExchangeRateRepository(connection);
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

            Sale saleFrom = saleRepository.GetByNetIdWithShiftedItemsWithoutAdditionalIncludes(message.SaleFromNetId);
            Sale saleTo = saleRepository.GetByNetIdWithAgreement(message.SaleToNetId);

            bool isNewToSale = false;

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            if (saleFrom.IsLocked) {
                if (user.UserRoleId.HasValue) {
                    if (!user.UserRole.UserRoleType.Equals(UserRoleType.GBA) && !user.UserRole.UserRoleType.Equals(UserRoleType.HeadSalesAnalytic))
                        throw new UserForbidenException();
                } else {
                    throw new UserForbidenException();
                }
            }

            OrderItem orderItemFrom = saleFrom.Order.OrderItems.FirstOrDefault(o => o.Id.Equals(message.OrderItem.Id));

            if (orderItemFrom == null) {
                Sender.Tell(new Tuple<OrderItem, SaleStatistic, string>(null, null, OrderItemsResourceNames.STOLEN));

                return;
            }

            double availableQty =
                productReservationRepository
                    .GetAvailableSumByOrderItemIdWithAvailabilityAndReSaleAvailabilities(orderItemFrom.Id);

            if (message.OrderItem.Qty > availableQty)
                throw new LocalizedException(
                    OrderItemsResourceNames.UNABLE_TO_SHIFT_SPECIFIED_QTY,
                    $"{orderItemFrom.Product.VendorCode} = {availableQty}"
                );

            if (saleTo == null) {
                if (message.Sale != null) {
                    saleTo = message.Sale;

                    isNewToSale = true;
                } else {
                    saleTo = saleRepository.GetLastNotMergedNewSaleByClientAgreementNetId(message.SaleToNetId);

                    if (saleTo == null) {
                        ClientAgreement clientAgreement = clientAgreementRepository.GetWithOrganizationByNetId(message.SaleToNetId);

                        saleTo = new Sale {
                            ClientAgreement = clientAgreement,
                            Order = new Order {
                                OrderSource = OrderSource.Local
                            }
                        };

                        ActorReferenceManager.Instance.Get(SalesActorNames.SALES_ACTOR)
                            .Tell(new AddSaleWithStatusesOnlyMessage(saleTo, message.UserNetId, message), Sender);

                        return;
                    }
                }
            }

            if (saleFrom.NetUid.Equals(saleTo.NetUid)) {
                Sender.Tell(new Tuple<OrderItem, SaleStatistic, string>(null, null, OrderItemsResourceNames.SELF_SHIFTING));
            } else {
                ClientAgreement clientAgreement =
                    clientAgreementRepository
                        .GetByNetIdWithAgreementAndDiscountForSpecificProduct(
                            saleTo.ClientAgreement.NetUid,
                            orderItemFrom.Product.ProductProductGroups.First().ProductGroupId
                        );
                if (orderItemFrom.Qty < message.OrderItem.Qty)
                    message.OrderItem.Qty = orderItemFrom.Qty;

                OrderItemBaseShiftStatus orderItemBaseShiftStatus = new() {
                    SaleId = saleTo.Id,
                    Qty = message.OrderItem.Qty,
                    UserId = user.Id,
                    OrderItemId = orderItemFrom.Id
                };

                orderItemBaseShiftStatus.CurrentId =
                    _saleRepositoriesFactory.NewOrderItemBaseShiftStatusRepository(connection).Add(orderItemBaseShiftStatus);

                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                    .Tell(new StoreConsignmentMovementFromOrderItemBaseShiftStatusMessage(orderItemBaseShiftStatus.CurrentId));

                orderItemFrom.Qty -= message.OrderItem.Qty;

                orderItemRepository.Update(orderItemFrom);

                OrderItem orderItemTo;

                if (saleTo.Order.OrderItems.Any(o => o.ProductId.Equals(orderItemFrom.ProductId))) {
                    orderItemTo = saleTo.Order.OrderItems.First(o => o.ProductId.Equals(orderItemFrom.ProductId));

                    orderItemTo.Qty += message.OrderItem.Qty;

                    orderItemRepository.Update(orderItemTo);
                } else {
                    orderItemTo = new OrderItem {
                        IsValidForCurrentSale = orderItemFrom.IsValidForCurrentSale,
                        ProductId = orderItemFrom.ProductId,
                        Qty = message.OrderItem.Qty,
                        UserId = user.Id,
                        OrderId = saleTo.OrderId
                    };

                    orderItemTo.Id = orderItemRepository.Add(orderItemTo);
                }

                if (saleFrom.ShiftStatus == null) {
                    saleFrom.ShiftStatusId = _saleRepositoriesFactory.NewSaleBaseShiftStatusRepository(connection).Add(new SaleBaseShiftStatus {
                        ShiftStatus = SaleShiftStatus.Partial
                    });

                    saleRepository.Update(saleFrom);
                }

                IEnumerable<ProductReservation> reservationsFrom =
                    productReservationRepository
                        .GetAllByOrderItemIdWithAvailabilityAndReSaleAvailabilities(
                            orderItemFrom.Id
                        );

                double toDecreaseAmount = message.OrderItem.Qty;

                List<Tuple<ProductReservation, double>> usedReservations = new();

                foreach (ProductReservation reservation in reservationsFrom) {
                    if (toDecreaseAmount.Equals(0d)) break;

                    if (reservation.ReSaleAvailabilities.Any()) {
                        double restoreReSalesQty = toDecreaseAmount;

                        foreach (ReSaleAvailability reSaleAvailability in reservation.ReSaleAvailabilities) {
                            if (restoreReSalesQty.Equals(0d)) break;

                            if (restoreReSalesQty > reSaleAvailability.Qty) {
                                reSaleAvailabilityRepository.Delete(reSaleAvailability.Id);

                                restoreReSalesQty -= reSaleAvailability.Qty;
                            } else {
                                reSaleAvailability.Qty -= restoreReSalesQty;

                                reSaleAvailabilityRepository.Update(reSaleAvailability);

                                restoreReSalesQty = 0d;
                            }
                        }
                    }

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

                foreach ((ProductReservation productReservation, double usedQty) in usedReservations) {
                    ProductReservation reservation =
                        productReservationRepository
                            .GetByOrderItemAndProductAvailabilityIds(
                                orderItemTo.Id,
                                productReservation.ProductAvailabilityId
                            );

                    if (reservation != null) {
                        reservation.Qty += usedQty;

                        productReservationRepository.Update(reservation);
                    } else {
                        productReservationRepository.Add(new ProductReservation {
                            ProductAvailabilityId = productReservation.ProductAvailabilityId,
                            OrderItemId = orderItemTo.Id,
                            Qty = usedQty,
                            IsReSaleReservation = productReservation.IsReSaleReservation
                        });
                    }
                }

                ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR)
                    .Forward(new GetOrderItemAndSaleStatisticsMessage(message.SaleFromNetId, saleTo.NetUid, orderItemTo.Id, isNewToSale));

                if (!saleFrom.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
                    UpdateSaleFromDebts(
                        message,
                        _clientRepositoriesFactory,
                        _exchangeRateRepositoryFactory,
                        connection,
                        saleRepository,
                        clientInDebtRepository,
                        debtRepository,
                        saleExchangeRateRepository,
                        _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                        saleFrom,
                        orderItemFrom);

                if (!saleTo.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
                    UpdateSaleToDebts(
                        message,
                        _exchangeRateRepositoryFactory,
                        connection,
                        saleRepository,
                        orderItemRepository,
                        clientInDebtRepository,
                        debtRepository,
                        clientAgreementRepository,
                        _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection),
                        saleTo,
                        clientAgreement,
                        orderItemTo);
            }
        } catch (UserForbidenException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private static void UpdateSaleToDebts(
        ShiftSaleOrderItemMessage message,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IDbConnection connection,
        ISaleRepository saleRepository,
        IOrderItemRepository orderItemRepository,
        IClientInDebtRepository clientInDebtRepository,
        IDebtRepository debtRepository,
        IClientAgreementRepository clientAgreementRepository,
        IClientBalanceMovementRepository clientBalanceMovementRepository,
        Sale saleTo,
        ClientAgreement clientAgreement,
        OrderItem predefinedOrderItem) {
        ClientInDebt clientInDebt =
            clientInDebtRepository
                .GetBySaleAndClientAgreementIds(
                    saleTo.Id,
                    clientAgreement.Id
                );

        saleTo = saleRepository.GetByNetId(saleTo.NetUid);

        OrderItem orderItem = saleTo.Order.OrderItems.First(o => o.Id.Equals(predefinedOrderItem.Id));

        decimal currentExchangeRateAmountFiltered = exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCultureFiltered(
            orderItem.Product.NetUid,
            saleTo.IsVatSale,
            orderItem.IsFromReSale,
            saleTo.ClientAgreement.Agreement.Currency.Id
        );

        orderItem.PricePerItem = orderItem.Product.CurrentPrice;
        orderItem.ExchangeRateAmount = currentExchangeRateAmountFiltered;

        orderItemRepository.Update(orderItem);

        if (clientInDebt != null) {
            if (clientAgreement.Agreement.Currency.Code.Equals("EUR"))
                clientInDebt.Debt.Total =
                    decimal.Round(
                        clientInDebt.Debt.Total
                        +
                        decimal.Round(Convert.ToDecimal(message.OrderItem.Qty) * orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero),
                        2,
                        MidpointRounding.AwayFromZero
                    );
            else
                clientInDebt.Debt.Total = clientInDebt.Debt.Total + Convert.ToDecimal(message.OrderItem.Qty) * orderItem.Product.CurrentLocalPrice;

            debtRepository.Update(clientInDebt.Debt);
        } else {
            Debt debt = new() {
                Days = 0,
                Total = 0,
                EuroTotal = decimal.Round(
                    Convert.ToDecimal(message.OrderItem.Qty) * orderItem.Product.CurrentPrice,
                    2,
                    MidpointRounding.AwayFromZero
                )
            };


            if (clientAgreement.CurrentAmount >= debt.EuroTotal) {
                clientAgreement.CurrentAmount = decimal.Round(clientAgreement.CurrentAmount - debt.EuroTotal, 2, MidpointRounding.AwayFromZero);

                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                clientBalanceMovementRepository
                    .AddOutMovement(
                        new ClientBalanceMovement {
                            ClientAgreementId = clientAgreement.Id,
                            Amount = debt.EuroTotal,
                            ExchangeRateAmount = orderItem.ExchangeRateAmount
                        }
                    );
            } else {
                if (clientAgreement.CurrentAmount > 0) {
                    debt.EuroTotal = decimal.Round(debt.EuroTotal - clientAgreement.CurrentAmount, 2, MidpointRounding.AwayFromZero);

                    clientBalanceMovementRepository
                        .AddOutMovement(
                            new ClientBalanceMovement {
                                ClientAgreementId = clientAgreement.Id,
                                Amount = clientAgreement.CurrentAmount,
                                ExchangeRateAmount = orderItem.ExchangeRateAmount
                            }
                        );

                    clientAgreement.CurrentAmount = decimal.Zero;

                    clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                }

                if (clientAgreement.Agreement.Currency.Code.Equals("EUR"))
                    debt.Total = debt.EuroTotal;
                else
                    debt.Total = debt.EuroTotal * orderItem.ExchangeRateAmount;

                clientInDebt = new ClientInDebt {
                    AgreementId = clientAgreement.AgreementId,
                    ClientId = clientAgreement.ClientId,
                    SaleId = saleTo.Id,
                    DebtId = debtRepository.Add(debt)
                };

                clientInDebtRepository.Add(clientInDebt);
            }
        }
    }

    private static void UpdateSaleFromDebts(
        ShiftSaleOrderItemMessage message,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoryFactory,
        IDbConnection connection,
        ISaleRepository saleRepository,
        IClientInDebtRepository clientInDebtRepository,
        IDebtRepository debtRepository,
        ISaleExchangeRateRepository saleExchangeRateRepository,
        IClientBalanceMovementRepository clientBalanceMovementRepository,
        Sale saleFrom,
        OrderItem orderItemFrom) {
        IClientAgreementRepository clientAgreementRepository = clientRepositoriesFactory.NewClientAgreementRepository(connection);

        ClientInDebt clientInDebt =
            clientInDebtRepository
                .GetBySaleAndClientAgreementIds(
                    saleFrom.Id,
                    saleFrom.ClientAgreementId
                );

        ClientAgreement clientAgreementFromDb =
            clientAgreementRepository
                .GetByNetIdWithAgreementAndDiscountForSpecificProduct(
                    saleFrom.ClientAgreement.NetUid,
                    orderItemFrom.Product.ProductProductGroups.First().ProductGroupId
                );

        saleFrom = saleRepository.GetByNetIdWithShiftedItems(saleFrom.NetUid);

        decimal currentPrice = saleFrom.Order.OrderItems.First(o => o.Id.Equals(orderItemFrom.Id)).Product.ProductPricings.First().Price;

        decimal exchangeRateAmount = decimal.Zero;

        if (clientInDebt != null) {
            if (clientAgreementFromDb.Agreement.Currency.Code.Equals("EUR")) {
                clientInDebt.Debt.Total -= currentPrice * Convert.ToDecimal(message.OrderItem.Qty);
            } else {
                SaleExchangeRate saleExchangeRate = saleExchangeRateRepository.GetEuroSaleExchangeRateBySaleNetId(message.SaleFromNetId);

                if (saleExchangeRate != null) {
                    clientInDebt.Debt.Total -= currentPrice * Convert.ToDecimal(message.OrderItem.Qty) * saleExchangeRate.Value;
                } else {
                    ExchangeRate exchangeRate = exchangeRateRepositoryFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

                    clientInDebt.Debt.Total -= currentPrice * Convert.ToDecimal(message.OrderItem.Qty) * exchangeRate.Amount;
                }
            }

            if (clientInDebt.Debt.Total <= 0) {
                debtRepository.Remove(clientInDebt.Debt.NetUid);

                clientRepositoriesFactory.NewClientInDebtRepository(connection).Remove(clientInDebt.NetUid);
            } else {
                debtRepository.Update(clientInDebt.Debt);
            }
        } else {
            clientAgreementFromDb.CurrentAmount =
                Math.Round(clientAgreementFromDb.CurrentAmount + Convert.ToDecimal(message.OrderItem.Qty) * currentPrice, 2);

            clientBalanceMovementRepository
                .AddInMovement(
                    new ClientBalanceMovement {
                        ClientAgreementId = clientAgreementFromDb.Id,
                        Amount = Convert.ToDecimal(message.OrderItem.Qty) * currentPrice,
                        ExchangeRateAmount = !exchangeRateAmount.Equals(decimal.Zero) ? exchangeRateAmount : 1m
                    }
                );

            clientAgreementRepository.UpdateAmountByNetId(clientAgreementFromDb.NetUid, clientAgreementFromDb.CurrentAmount);
        }
    }

    private static List<Product> UpdatedProductReservationAndAvailabilityOnShiftedToBill(
        IReSaleAvailabilityRepository reSaleAvailabilityRepository,
        IProductRepositoriesFactory productRepositoriesFactory,
        IDbConnection connection,
        List<ShiftedOrderItem> shiftedToBillOrderItems,
        long organizationId) {
        IProductReservationRepository productReservationRepository = productRepositoriesFactory.NewProductReservationRepository(connection);

        shiftedToBillOrderItems.ForEach(shifted => {
            IEnumerable<ProductReservation> reservationsFrom =
                productReservationRepository
                    .GetAllByOrderItemIdWithAvailabilityAndReSaleAvailabilities(
                        shifted.OldOrderItem ?? 0
                    );

            double toDecreaseAmount = shifted.Qty;

            List<Tuple<ProductReservation, double>> usedReservations = new();

            foreach (ProductReservation reservation in reservationsFrom) {
                if (toDecreaseAmount.Equals(0d)) break;

                // Return to Sale doesn't affect ReSale 1287
                // if (reservation.ReSaleAvailabilities.Any()) {
                //     double restoreReSalesQty = toDecreaseAmount;
                //
                //     foreach (ReSaleAvailability reSaleAvailability in reservation.ReSaleAvailabilities) {
                //         if (restoreReSalesQty.Equals(0d)) break;
                //
                //         if (restoreReSalesQty >= reSaleAvailability.Qty) {
                //             reSaleAvailabilityRepository.Delete(reSaleAvailability.Id);
                //
                //             restoreReSalesQty -= reSaleAvailability.Qty;
                //         } else {
                //             reSaleAvailability.Qty -= restoreReSalesQty;
                //             reSaleAvailability.RemainingQty -= restoreReSalesQty;
                //
                //             reSaleAvailabilityRepository.Update(reSaleAvailability);
                //
                //             restoreReSalesQty = 0d;
                //         }
                //     }
                // }

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

            foreach ((ProductReservation productReservation, double usedQty) in usedReservations) {
                ProductReservation reservation =
                    productReservationRepository
                        .GetByOrderItemAndProductAvailabilityIds(
                            shifted.OrderItemId,
                            productReservation.ProductAvailabilityId
                        );

                if (reservation != null) {
                    reservation.Qty += usedQty;

                    productReservationRepository.Update(reservation);
                } else {
                    productReservationRepository.Add(new ProductReservation {
                        ProductAvailabilityId = productReservation.ProductAvailabilityId,
                        OrderItemId = shifted.OrderItemId,
                        Qty = usedQty,
                        IsReSaleReservation = productReservation.IsReSaleReservation
                    });
                }
            }
        });

        List<Product> products =
            productRepositoriesFactory
                .NewGetMultipleProductsRepository(connection)
                .GetAllByIds(
                    shiftedToBillOrderItems.Select(s => s.ProductId),
                    organizationId
                );

        return products;
    }

    private static List<Product> UpdatedProductReservationAndAvailabilityOnShiftedToStorage(
        IReSaleAvailabilityRepository reSaleAvailabilityRepository,
        IReSaleItemRepository reSaleItemRepository,
        IProductRepositoriesFactory productRepositoriesFactory,
        IOrganizationRepository organizationRepository,
        IStorageRepository storageRepository,
        IDbConnection connection,
        List<ShiftedOrderItem> shiftedToStorageOrderItems,
        long organizationId) {
        IProductAvailabilityRepository productAvailabilityRepository = productRepositoriesFactory.NewProductAvailabilityRepository(connection);
        IProductReservationRepository productReservationRepository = productRepositoriesFactory.NewProductReservationRepository(connection);
        IProductTransferRepository productTransferRepository = productRepositoriesFactory.NewProductTransferRepository(connection);
        IProductTransferItemRepository productTransferItemRepository = productRepositoriesFactory.NewProductTransferItemRepository(connection);

        foreach (ShiftedOrderItem shifted in shiftedToStorageOrderItems) {
            IEnumerable<ProductReservation> reservations =
                productReservationRepository
                    .GetAllByOrderItemIdWithAvailabilityAndReSaleAvailabilities(
                        shifted.OrderItemId
                    );

            double toRestoreAmount = shifted.Qty;

            ReSaleAvailabilityWithTotalsModel reSaleAvailabilityByProductId =
                reSaleAvailabilityRepository.GetActualReSaleAvailabilityByProductId(shifted.ProductId);

            double availableInReSale = reSaleAvailabilityByProductId.TotalQty;

            foreach (ProductReservation reservation in reservations) {
                if (toRestoreAmount.Equals(0d)) break;

                if (reservation.ReSaleAvailabilities.Any()) {
                    double restoreReSalesQty = Math.Min(reservation.Qty, toRestoreAmount);

                    foreach (ReSaleAvailability reSaleAvailability in reservation.ReSaleAvailabilities) {
                        if (restoreReSalesQty.Equals(0d)) break;

                        if (availableInReSale < restoreReSalesQty) {
                            double transferQty = restoreReSalesQty - availableInReSale;
                            restoreReSalesQty -= transferQty;

                            Storage storage = storageRepository.GetReSale();
                            ProductAvailability productAvailability =
                                productAvailabilityRepository.GetByProductAndStorageIds(reservation.ProductAvailability.ProductId, storage.Id);

                            if (productAvailability != null) {
                                productAvailability.Amount += transferQty;
                                productAvailabilityRepository.Update(productAvailability);
                            } else {
                                productAvailabilityRepository.Add(
                                    new ProductAvailability {
                                        Amount = transferQty,
                                        ProductId = reservation.ProductAvailability.ProductId,
                                        StorageId = storage.Id
                                    });
                            }

                            toRestoreAmount = restoreReSalesQty;

                            CreateProductTransferFromShiftedItems(
                                transferQty,
                                shifted.UserId,
                                storage, reservation,
                                productTransferRepository,
                                productTransferItemRepository);
                        }

                        if (restoreReSalesQty >= reSaleAvailability.RemainingQty) {
                            restoreReSalesQty -= reSaleAvailability.RemainingQty;
                            reSaleAvailability.Qty -= reSaleAvailability.RemainingQty;
                            reSaleAvailability.RemainingQty = 0;
                        } else {
                            reSaleAvailability.RemainingQty -= restoreReSalesQty;
                            reSaleAvailability.Qty -= restoreReSalesQty;
                        }

                        if (reSaleAvailability.Qty == 0)
                            reSaleAvailabilityRepository.Delete(reSaleAvailability.Id);
                        else
                            reSaleAvailabilityRepository.Update(reSaleAvailability);
                    }
                }

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

                    reservation.ProductAvailability.Amount += toRestoreAmount;

                    productReservationRepository.Delete(reservation.NetUid);
                }

                productAvailabilityRepository.Update(reservation.ProductAvailability);
            }
        }

        List<Product> products =
            productRepositoriesFactory
                .NewGetMultipleProductsRepository(connection)
                .GetAllByIds(
                    shiftedToStorageOrderItems.Select(s => s.ProductId),
                    organizationId
                );

        return products;
    }

    private static void CreateProductTransferFromShiftedItems(
        double transferQty,
        long userId,
        Storage storage,
        ProductReservation reservation,
        IProductTransferRepository productTransferRepository,
        IProductTransferItemRepository productTransferItemRepository) {
        Organization organization = storage.Organization;

        ProductTransfer productTransfer = new();
        productTransfer.FromDate =
            productTransfer.FromDate.Year.Equals(1)
                ? DateTime.UtcNow
                : TimeZoneInfo.ConvertTimeToUtc(productTransfer.FromDate);
        productTransfer.ResponsibleId = userId;
        productTransfer.OrganizationId = organization.Id;
        productTransfer.FromStorageId = reservation.ProductAvailability.StorageId;
        productTransfer.ToStorageId = storage.Id;

        if (string.IsNullOrEmpty(productTransfer.Number)) {
            ProductTransfer lastRecord = productTransferRepository.GetLastRecord(organization.Id);

            if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                productTransfer.Number =
                    string.Format(
                        "{0}{1}",
                        organization.Code,
                        string.IsNullOrEmpty(organization.Code)
                            ? 1
                            : Convert.ToInt32(lastRecord.Number.Substring(organization.Code.Length, lastRecord.Number.Length - organization.Code.Length)) + 1
                    );
            else
                productTransfer.Number =
                    $"{organization.Code}1";
        }

        long productTransferId = productTransfer.Id = productTransferRepository.Add(productTransfer);

        Dictionary<long, long> productTransferItemProductAvailability = new();
        ProductTransferItem transferItem = new();
        transferItem.ProductId = reservation.ProductAvailability.ProductId;
        transferItem.ProductTransferId = productTransfer.Id;
        transferItem.Qty = transferQty;
        transferItem.Id = productTransferItemRepository.Add(transferItem);

        ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
            .Tell(new StoreConsignmentFromProductTransferWithReSaleMessage(
                productTransferId,
                productTransferItemProductAvailability,
                productTransfer.IsManagement,
                userId: productTransfer.ResponsibleId)
            );
    }

    private static void UpdateCurrentDebts(
        Sale messageSale,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IDbConnection connection,
        ISaleRepository saleRepository,
        IClientBalanceMovementRepository clientBalanceMovementRepository) {
        IClientAgreementRepository clientAgreementRepository = clientRepositoriesFactory.NewClientAgreementRepository(connection);

        Sale sale = saleRepository.GetByNetIdWithShiftedItemsWithoutAdditionalIncludes(messageSale.NetUid);
        ClientAgreement clientAgreement = clientAgreementRepository.GetByNetId(sale.ClientAgreement.NetUid);
        ClientInDebt clientInDebt = clientRepositoriesFactory.NewClientInDebtRepository(connection).GetActiveByClientAgreementId(messageSale.ClientAgreementId);

        if (clientInDebt != null) {
            SaleExchangeRate saleExchangeRate = saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).GetEuroSaleExchangeRateBySaleNetId(messageSale.NetUid);

            if (!clientAgreement.Agreement.Currency.Code.Equals("EUR")) {
                if (saleExchangeRate != null)
                    clientInDebt.Debt.EuroTotal = Convert.ToDecimal(clientInDebt.Debt.Total) / saleExchangeRate.Value;
                else
                    clientInDebt.Debt.EuroTotal = Convert.ToDecimal(clientInDebt.Debt.Total);
            } else {
                clientInDebt.Debt.EuroTotal = Convert.ToDecimal(clientInDebt.Debt.Total);
            }

            foreach (OrderItem orderItem in messageSale.Order.OrderItems.Where(e => e.ShiftStatuses.Any(s => s.IsNew())))
            foreach (OrderItemBaseShiftStatus shiftStatus in orderItem.ShiftStatuses.Where(s => s.IsNew())) {
                OrderItem orderItemFromDb = sale.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id));

                if (clientAgreement.Agreement.Currency.Code.Equals("EUR")) {
                    clientInDebt.Debt.Total -= orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftStatus.Qty);
                    clientInDebt.Debt.EuroTotal -= orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftStatus.Qty);
                } else {
                    if (saleExchangeRate != null)
                        clientInDebt.Debt.Total -= orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftStatus.Qty) * saleExchangeRate.Value;
                    else
                        clientInDebt.Debt.Total -= orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftStatus.Qty);

                    clientInDebt.Debt.EuroTotal -= orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftStatus.Qty);
                }
            }

            if (clientInDebt.Debt.Total <= 0) {
                saleRepositoriesFactory.NewDebtRepository(connection).Remove(clientInDebt.Debt.NetUid);
                clientRepositoriesFactory.NewClientInDebtRepository(connection).Remove(clientInDebt.NetUid);

                if (clientInDebt.Debt.EuroTotal < decimal.Zero) {
                    clientAgreement.CurrentAmount =
                        Math.Round(clientAgreement.CurrentAmount + Math.Abs(clientInDebt.Debt.EuroTotal), 2);

                    clientBalanceMovementRepository
                        .AddInMovement(
                            new ClientBalanceMovement {
                                ClientAgreementId = clientAgreement.Id,
                                Amount = Math.Abs(clientInDebt.Debt.EuroTotal),
                                ExchangeRateAmount = saleExchangeRate?.Value ?? 1m
                            }
                        );

                    clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
                }
            } else {
                saleRepositoriesFactory.NewDebtRepository(connection).Update(clientInDebt.Debt);
            }
        } else if (!messageSale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
            decimal toRestoreAmount =
                (from orderItem in messageSale.Order.OrderItems.Where(e => e.ShiftStatuses.Any(s => s.IsNew()))
                    from shiftStatus in orderItem.ShiftStatuses.Where(s => s.IsNew())
                    let orderItemFromDb = sale.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id))
                    where orderItemFromDb != null
                    select orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftStatus.Qty)).Sum();

            if (toRestoreAmount <= decimal.Zero) return;

            clientAgreement.CurrentAmount =
                Math.Round(clientAgreement.CurrentAmount + toRestoreAmount, 2);

            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

            clientBalanceMovementRepository
                .AddInMovement(
                    new ClientBalanceMovement {
                        ClientAgreementId = clientAgreement.Id,
                        Amount = toRestoreAmount,
                        ExchangeRateAmount = 1m
                    }
                );
        }
    }
}