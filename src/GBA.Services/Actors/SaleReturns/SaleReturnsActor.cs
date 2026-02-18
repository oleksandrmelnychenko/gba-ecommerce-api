using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.Helpers.SaleReturns;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.Products.ProductPlacementMovements;
using GBA.Domain.Messages.SaleReturns;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.SaleReturns.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using Newtonsoft.Json;
using Exception = System.Exception;

namespace GBA.Services.Actors.SaleReturns;

public sealed class SaleReturnsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISaleReturnRepositoriesFactory _saleReturnRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public SaleReturnsActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        ISaleReturnRepositoriesFactory saleReturnRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _saleReturnRepositoriesFactory = saleReturnRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;

        Receive<AddNewSaleReturnMessage>(ProcessAddNewSaleReturnMessage);

        Receive<GetProductQtyForVatStorageMessage>(ProcessGetProductQtyForVatStorageMessage);

        Receive<CancelSaleReturnMessage>(ProcessCancelSaleReturnMessage);
    }

    private void ProcessAddNewSaleReturnMessage(AddNewSaleReturnMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.SaleReturn == null) {
            Sender.Tell(new Tuple<SaleReturn, string>(null, "SaleReturn entity can not be null"));
        } else if (!message.SaleReturn.IsNew()) {
            Sender.Tell(new Tuple<SaleReturn, string>(null, "Existing SaleReturn entity is not valid payload for current request"));
        } else if (!message
                       .SaleReturn
                       .SaleReturnItems
                       .Any(i => i.OrderItem != null && !i.OrderItem.IsNew() && i.Storage != null
                                 && !i.Storage.IsNew() && i.Qty > 0 && i.Qty <= i.OrderItem.Qty)) {
            Sender.Tell(new Tuple<SaleReturn, string>(null,
                "You need to specify at least one SaleReturnItem with non-zero Qty, selected Storage and existing OrderItem"));
        } else if (message.SaleReturn.Client == null || message.SaleReturn.Client.IsNew()) {
            Sender.Tell(new Tuple<SaleReturn, string>(null, "You need to specify Client"));
        } else {
            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

            List<OrderItem> orderItems =
                message.SaleReturn.SaleReturnItems
                    .Where(i => i.OrderItem != null && !i.OrderItem.IsNew() && i.Qty > 0 && i.Qty <= i.OrderItem.Qty)
                    .Select(item => orderItemRepository.GetByIdWithClientInfo(item.OrderItem.Id)).ToList();

            OrderItem firstItem = orderItems.First();

            long clientId = firstItem.Order.Sale.ClientAgreement.ClientId;
            long organizationId = firstItem.Order.Sale.ClientAgreement.Agreement.OrganizationId ?? 0;

            if (!orderItems.All(i => i.Order.Sale.ClientAgreement.ClientId.Equals(clientId))) {
                Sender.Tell(new Tuple<SaleReturn, string>(null, "All items should be from single client"));
            } else if (orderItems.Any(i => i.Order.Sale.ClientAgreement.Agreement.OrganizationId != organizationId)) {
                Sender.Tell(new Tuple<SaleReturn, string>(null, SaleReturnsResourceNames.AGREEMENTS_FROM_SINGE_ORGANIZATION_VALIDATION));
            } else {
                IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(connection);
                ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);
                ISaleReturnRepository saleReturnRepository = _saleReturnRepositoriesFactory.NewSaleReturnRepository(connection);
                IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(connection);
                IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);
                IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
                IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
                ISaleReturnItemRepository saleReturnItemRepository = _saleReturnRepositoriesFactory.NewSaleReturnItemRepository(connection);
                IProductIncomeItemRepository productIncomeItemRepository = _productRepositoriesFactory.NewProductIncomeItemRepository(connection);
                ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);
                IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
                IBaseSalePaymentStatusRepository baseSalePaymentStatusRepository = _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(connection);
                IClientBalanceMovementRepository clientBalanceMovementRepository = _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection);
                ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
                IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
                IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);


                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                message.SaleReturn.CreatedById = user.Id;
                message.SaleReturn.ClientId = message.SaleReturn.Client.Id;

                message.SaleReturn.FromDate =
                    message.SaleReturn.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow
                        : TimeZoneInfo.ConvertTimeToUtc(message.SaleReturn.FromDate);

                List<SaleReturn> returns = new();

                foreach (SaleReturnItem item in message
                             .SaleReturn
                             .SaleReturnItems
                             .Where(i => i.OrderItem != null && !i.OrderItem.IsNew() && i.Qty > 0 && i.Qty <= i.OrderItem.Qty)) {
                    double qtyToReturn = item.Qty;

                    List<long> orderItemIds =
                        item.OrderItem.OrderItemsGroupByProduct != null ? item.OrderItem.OrderItemsGroupByProduct.Select(x => x.Id).ToList() : new List<long>();

                    orderItemIds.Add(item.OrderItem.Id);

                    IEnumerable<OrderItem> allOrderItems = orderItemRepository.GetByIdsWithClientAgreement(orderItemIds);

                    foreach (OrderItem orderItem in allOrderItems.Where(x => x.Qty - x.ReturnedQty > 0)) {
                        if (qtyToReturn.Equals(0)) break;

                        SaleReturn saleReturn;

                        if (returns.Any(r => r.Storage.Id.Equals(item.Storage.Id) && r.ClientAgreementId.Equals(orderItem.Order.Sale.ClientAgreementId))) {
                            saleReturn = returns.First(r => r.Storage.Id.Equals(item.Storage.Id));
                        } else {
                            saleReturn = new SaleReturn {
                                CreatedById = message.SaleReturn.CreatedById,
                                ClientId = message.SaleReturn.ClientId,
                                FromDate = message.SaleReturn.FromDate,
                                ClientAgreementId = orderItem.Order.Sale.ClientAgreementId
                            };

                            string prefix = orderItem.Order.Sale.ClientAgreement.Agreement.Organization.Code ?? string.Empty;

                            SaleReturn lastNumber = saleReturnRepository.GetLastReturnByPrefix(prefix);

                            if (lastNumber != null) {
                                if (orderItem.Order.Sale.ClientAgreement.Agreement.Organization.Culture.ToLower().Equals("pl")) {
                                    if (DateTime.Now.Year.Equals(lastNumber.Created.Year) && DateTime.Now.Month.Equals(lastNumber.Created.Month))
                                        saleReturn.Number =
                                            $"{prefix}{string.Format("{0:D10}", Convert.ToInt32(lastNumber.Number.Substring(prefix.Length, 10)) + 1)}";
                                    else
                                        saleReturn.Number = $"{prefix}{string.Format("{0:D10}", 1)}";
                                } else {
                                    saleReturn.Number = DateTime.Now.Year.Equals(lastNumber.Created.Year)
                                        ? $"{prefix}{string.Format("{0:D10}", Convert.ToInt32(lastNumber.Number.Substring(prefix.Length, 10)) + 1)}"
                                        : $"{prefix}{string.Format("{0:D10}", 1)}";
                                }
                            } else {
                                saleReturn.Number = $"{prefix}{string.Format("{0:D10}", 1)}";
                            }

                            saleReturn.Id = saleReturnRepository.Add(saleReturn);

                            saleReturn.Storage = item.Storage;

                            returns.Add(saleReturn);
                        }

                        item.OrderItemId = orderItem.Id;
                        item.StorageId = item.Storage.Id;
                        item.SaleReturnId = saleReturn.Id;
                        item.CreatedById = user.Id;

                        double orderItemQty = orderItem.Qty - orderItem.ReturnedQty;

                        if (qtyToReturn <= orderItemQty) {
                            orderItem.ReturnedQty += qtyToReturn;

                            item.Qty = qtyToReturn;

                            qtyToReturn = 0;
                        } else {
                            double qtyReturn = orderItemQty;

                            item.Qty = orderItemQty;

                            orderItem.ReturnedQty += orderItemQty;

                            qtyToReturn -= qtyReturn;
                        }

                        orderItemRepository.UpdateReturnedQty(orderItem);

                        if (item.Qty > 0) saleReturnItemRepository.Add(item);
                    }
                }

                for (int i = 0; i < returns.Count; i++) returns[i] = saleReturnRepository.GetById(returns[i].Id);

                List<ProductIncome> productIncomes = new();

                foreach (SaleReturn saleReturn in returns)
                foreach (SaleReturnItem saleReturnItem in saleReturn.SaleReturnItems) {
                    decimal toRestoreAmount =
                        decimal.Round(
                            Convert.ToDecimal(saleReturnItem.Qty) * saleReturnItem.OrderItem.PricePerItem
                            , 14
                            , MidpointRounding.AwayFromZero
                        );

                    RestoreMoney(
                        _saleRepositoriesFactory.NewSaleRepository(connection),
                        clientInDebtRepository,
                        clientAgreementRepository,
                        currencyRepository,
                        exchangeRateRepository,
                        crossExchangeRateRepository,
                        baseSalePaymentStatusRepository,
                        debtRepository,
                        clientBalanceMovementRepository,
                        toRestoreAmount,
                        saleReturnItem,
                        saleReturnItem.OrderItem.ExchangeRateAmount,
                        saleRepository.GetByOrderId(saleReturnItem.OrderItem.OrderId.Value).Id
                    );

                    IEnumerable<ProductReservation> reservations =
                        productReservationRepository
                            .GetAllByOrderItemIdWithAvailabilityAndReSaleAvailabilities(
                                saleReturnItem.OrderItem.Id
                            );

                    double toRestoreQty = saleReturnItem.Qty;

                    foreach (ProductReservation reservation in reservations) {
                        if (toRestoreQty.Equals(0d)) break;

                        ReSaleAvailabilityWithTotalsModel reSaleAvailabilityByProductId =
                            _reSaleRepositoriesFactory
                                .NewReSaleAvailabilityRepository(connection)
                                .GetActualReSaleAvailabilityByProductId(saleReturnItem.OrderItem.ProductId);

                        double availableInReSale = reSaleAvailabilityByProductId.TotalQty;

                        if (!reservation.ProductAvailability.Storage.ForVatProducts) {
                            saleReturnItem.StorageId = reservation.ProductAvailability.StorageId;
                            saleReturnItem.Storage = reservation.ProductAvailability.Storage;
                        }

                        if (reservation.ReSaleAvailabilities.Any()) {
                            double restoreReSalesQty = Math.Min(reservation.Qty, toRestoreQty); // Second problem, toRestoreQty never changes so reservation.Qty always picked

                            Storage reSaleStorage = _storageRepositoryFactory.NewStorageRepository(connection).GetReSale();

                            if (availableInReSale < toRestoreQty && reSaleStorage.Id != saleReturnItem.StorageId) {
                                double resaleStorageQty = toRestoreQty - availableInReSale; // First problem availableInReSale could be bigger than restoreReSaleQty

                                saleReturnItem.Qty -= resaleStorageQty;
                                saleReturnItem.Amount = decimal.Round(saleReturnItem.OrderItem.PricePerItem * (decimal)saleReturnItem.Qty, 14, MidpointRounding.AwayFromZero);

                                if (saleReturnItem.Qty > 0) {
                                    saleReturnItemRepository.Update(saleReturnItem);
                                } else {
                                    saleReturnItemRepository.RemoveById(saleReturnItem.Id);
                                    saleReturnItem.Deleted = true;
                                }

                                toRestoreQty -= resaleStorageQty;
                                restoreReSalesQty -= resaleStorageQty;

                                SaleReturnItem newItem = CreateReSaleReturnItem(saleReturnItem, resaleStorageQty, reSaleStorage, user, saleReturn, saleReturnItemRepository);
                                AddToProductIncomes(newItem, productIncomes, user, saleReturn);
                            }

                            foreach (ReSaleAvailability reSaleAvailability in reservation.ReSaleAvailabilities) {
                                if (restoreReSalesQty.Equals(0d)) break;

                                if (restoreReSalesQty >= reSaleAvailability.RemainingQty) {
                                    restoreReSalesQty -= reSaleAvailability.RemainingQty;
                                    toRestoreQty -= reSaleAvailability.RemainingQty;

                                    reSaleAvailability.Qty -= reSaleAvailability.RemainingQty;
                                    reSaleAvailability.RemainingQty = 0;
                                } else {
                                    reSaleAvailability.RemainingQty -= restoreReSalesQty;
                                    reSaleAvailability.Qty -= restoreReSalesQty;
                                    toRestoreQty -= restoreReSalesQty;
                                }

                                if (reSaleAvailability.Qty == 0)
                                    reSaleAvailabilityRepository.Delete(reSaleAvailability.Id);
                                else
                                    reSaleAvailabilityRepository.Update(reSaleAvailability);
                            }
                        }
                    }

                    if (!saleReturnItem.Deleted && productIncomes.Any(i => i.StorageId.Equals(saleReturnItem.StorageId))) {
                        productIncomes
                            .First(i => i.StorageId.Equals(saleReturnItem.StorageId))
                            .ProductIncomeItems
                            .Add(new ProductIncomeItem {
                                SaleReturnItemId = saleReturnItem.Id,
                                SaleReturnItem = saleReturnItem
                            });
                    } else if (!saleReturnItem.Deleted) {
                        ProductIncome productIncome = new() {
                            StorageId = saleReturnItem.StorageId,
                            Storage = saleReturnItem.Storage,
                            UserId = user.Id,
                            FromDate = saleReturn.FromDate,
                            Number = saleReturn.Number,
                            ProductIncomeType = ProductIncomeType.SaleReturn
                        };

                        productIncome.ProductIncomeItems.Add(new ProductIncomeItem {
                            SaleReturnItemId = saleReturnItem.Id,
                            SaleReturnItem = saleReturnItem
                        });

                        productIncomes.Add(productIncome);
                    }
                }

                foreach (ProductIncome productIncome in productIncomes) {
                    productIncome.Id = productIncomeRepository.Add(productIncome);

                    foreach (ProductIncomeItem item in productIncome.ProductIncomeItems) {
                        item.ProductIncomeId = productIncome.Id;
                        item.Qty = item.SaleReturnItem.Qty;
                        item.RemainingQty = item.SaleReturnItem.Qty;
                        item.SaleReturnItem = item.SaleReturnItem;

                        ProductAvailability productAvailability =
                            productAvailabilityRepository
                                .GetByProductAndStorageIds(
                                    item.SaleReturnItem.OrderItem.ProductId,
                                    item.SaleReturnItem.StorageId
                                );

                        if (productAvailability == null) {
                            productAvailability = new ProductAvailability {
                                Amount = item.SaleReturnItem.Qty,
                                StorageId = item.SaleReturnItem.StorageId,
                                ProductId = item.SaleReturnItem.OrderItem.ProductId
                            };

                            productAvailability.Id = productAvailabilityRepository.AddWithId(productAvailability);
                        } else {
                            productAvailability.Amount += item.SaleReturnItem.Qty;

                            productAvailabilityRepository.Update(productAvailability);
                        }

                        item.ProductAvailability = productAvailability;

                        item.Id = productIncomeItemRepository.Add(item);

                        ActorReferenceManager.Instance.Get(BaseActorNames.PRODUCT_PLACEMENT_ACTOR).Tell(new MoveProductPlacementFromSaleReturnMessage(item.SaleReturnItem));
                    }

                    ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new AddNewConsignmentMessage(productIncome.Id, false));
                }

                Sender.Tell(new Tuple<SaleReturn, string>(returns.LastOrDefault(), string.Empty));

                foreach (SaleReturn saleReturn in returns) saleReturnItemRepository.Update(saleReturn.SaleReturnItems);
            }
        }
    }

    private SaleReturnItem CreateReSaleReturnItem(SaleReturnItem source, double qty, Storage reSaleStorage, User user, SaleReturn saleReturn,
        ISaleReturnItemRepository saleReturnItemRepository) {
        SaleReturnItem newItem = JsonConvert.DeserializeObject<SaleReturnItem>(JsonConvert.SerializeObject(source));
        newItem.Qty = qty;
        newItem.StorageId = reSaleStorage.Id;
        newItem.Storage = reSaleStorage;
        newItem.Amount = decimal.Round(source.OrderItem.PricePerItem * (decimal)qty, 14, MidpointRounding.AwayFromZero);
        newItem.Id = 0;
        newItem.NetUid = Guid.Empty;
        newItem.Id = saleReturnItemRepository.Add(newItem);
        return newItem;
    }

    private void AddToProductIncomes(SaleReturnItem newItem, List<ProductIncome> productIncomes, User user, SaleReturn saleReturn) {
        if (productIncomes.Any(i => i.StorageId.Equals(newItem.StorageId))) {
            productIncomes.First(i => i.StorageId.Equals(newItem.StorageId))
                .ProductIncomeItems.Add(new ProductIncomeItem {
                    SaleReturnItemId = newItem.Id,
                    SaleReturnItem = newItem
                });
        } else {
            ProductIncome productIncome = new() {
                StorageId = newItem.StorageId,
                Storage = newItem.Storage,
                UserId = user.Id,
                FromDate = saleReturn.FromDate,
                Number = saleReturn.Number,
                ProductIncomeType = ProductIncomeType.SaleReturn
            };

            productIncome.ProductIncomeItems.Add(new ProductIncomeItem {
                SaleReturnItemId = newItem.Id,
                SaleReturnItem = newItem
            });

            productIncomes.Add(productIncome);
        }
    }

    private void ProcessGetProductQtyForVatStorageMessage(GetProductQtyForVatStorageMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        double qty = 0;

        Sale sale = _saleRepositoriesFactory
            .NewSaleRepository(connection)
            .GetGroupedOrderItemByProduct(
                message.NetId
            );

        OrderItem groupedOrderItem = sale.Order.OrderItems.First();

        if (groupedOrderItem.ProductReservations.First().IsReSaleReservation) {
            ReSaleAvailabilityWithTotalsModel reSaleAvailabilityByProductId =
                _reSaleRepositoriesFactory
                    .NewReSaleAvailabilityRepository(connection)
                    .GetActualReSaleAvailabilityByProductId(groupedOrderItem.ProductId);

            qty = reSaleAvailabilityByProductId.TotalQty;
        } else if (sale.IsVatSale) {
            qty = groupedOrderItem.Qty;
        } else {
            qty = -1;
        }

        string returnMessage = qty < 0 ? string.Empty : $"���������� �� ��� ����� ������� � ʲ�����Ҳ: {qty}";
        Sender.Tell(returnMessage);
    }

    private void ProcessCancelSaleReturnMessage(CancelSaleReturnMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleReturnRepository saleReturnRepository = _saleReturnRepositoriesFactory.NewSaleReturnRepository(connection);

        SaleReturn saleReturn = saleReturnRepository.GetByNetId(message.SaleReturnNetId);

        if (saleReturn != null) {
            if (!saleReturn.IsCanceled) {
                bool isAllAvailableOnStorage = true;
                string unavailableProductVendorCode = string.Empty;

                IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

                foreach (SaleReturnItem returnItem in saleReturn.SaleReturnItems) {
                    ProductAvailability productAvailability =
                        productAvailabilityRepository.GetByProductAndStorageIds(returnItem.OrderItem.ProductId, returnItem.StorageId);

                    if (productAvailability != null && productAvailability.Amount >= returnItem.Qty) continue;

                    isAllAvailableOnStorage = false;

                    unavailableProductVendorCode = returnItem.OrderItem.Product.VendorCode;

                    break;
                }

                if (isAllAvailableOnStorage) {
                    IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(connection);
                    IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(connection);
                    IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
                    IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
                    ICrossExchangeRateRepository crossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);
                    IClientBalanceMovementRepository clientBalanceMovementRepository = _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection);
                    ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);
                    IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);
                    IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
                    IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);
                    IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);

                    User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                    foreach (SaleReturnItem returnItem in saleReturn.SaleReturnItems) {
                        ProductAvailability productAvailability =
                            productAvailabilityRepository.GetByProductAndStorageIds(returnItem.OrderItem.ProductId, returnItem.StorageId);

                        productAvailability.Amount -= returnItem.Qty;

                        productAvailabilityRepository.Update(productAvailability);

                        if (returnItem.OrderItem.ReturnedQty > returnItem.Qty)
                            returnItem.OrderItem.ReturnedQty -= returnItem.Qty;
                        else
                            returnItem.OrderItem.ReturnedQty = 0;

                        orderItemRepository.UpdateReturnedQty(returnItem.OrderItem);

                        IEnumerable<ProductReservation> reservations =
                            productReservationRepository
                                .GetAllByOrderItemIdWithAvailabilityAndReSaleAvailabilities(
                                    returnItem.OrderItem.Id
                                );

                        double toRestoreAmount = returnItem.Qty;
                        foreach (ProductReservation reservation in reservations) {
                            if (toRestoreAmount.Equals(0d)) break;

                            if (returnItem.Storage.ForVatProducts && reservation.ReSaleAvailabilities.Any()) {
                                foreach (ReSaleAvailability reSaleAvailability in reservation.ReSaleAvailabilities) {
                                    if (toRestoreAmount.Equals(0d)) break;

                                    reSaleAvailability.Qty += toRestoreAmount;
                                    reSaleAvailability.RemainingQty += toRestoreAmount;
                                    toRestoreAmount--;

                                    reSaleAvailabilityRepository.Update(reSaleAvailability);
                                }
                            } else if (returnItem.Storage.ForVatProducts && reservation.IsReSaleReservation) {
                                ReSaleAvailability reSaleAvailability = reSaleAvailabilityRepository.GetByProductReservationId(reservation.Id);

                                if (reSaleAvailability != null) {
                                    reSaleAvailability.Qty += toRestoreAmount;
                                    reSaleAvailability.RemainingQty += toRestoreAmount;
                                    toRestoreAmount--;

                                    reSaleAvailabilityRepository.Update(reSaleAvailability);
                                    reSaleAvailabilityRepository.RestoreReSaleAvailability(reSaleAvailability.Id);
                                }
                            }
                        }

                        decimal toDebtAmount =
                            decimal.Round(
                                Convert.ToDecimal(returnItem.Qty) * returnItem.OrderItem.PricePerItem
                                , 14
                                , MidpointRounding.AwayFromZero
                            );

                        RestoreDebt(
                            clientInDebtRepository,
                            clientAgreementRepository,
                            currencyRepository,
                            exchangeRateRepository,
                            crossExchangeRateRepository,
                            debtRepository,
                            clientBalanceMovementRepository,
                            toDebtAmount,
                            returnItem,
                            returnItem.OrderItem.ExchangeRateAmount
                        );
                    }

                    saleReturn.CanceledById = user.Id;

                    saleReturnRepository.SetCanceled(saleReturn);

                    productIncomeRepository
                        .RemoveAllBySaleReturnItemIds(
                            saleReturn.SaleReturnItems.Select(i => i.Id)
                        );

                    Sender.Tell(saleReturnRepository.GetByNetId(message.SaleReturnNetId));
                } else {
                    Exception exc = new();
                    exc.Data.Add(SaleReturnConsts.VENDOR_CODE_KEY, unavailableProductVendorCode);

                    Sender.Tell(exc);
                }
            } else {
                Sender.Tell(new Exception("SaleReturn is already canceled"));
            }
        } else {
            Sender.Tell(new Exception("SaleReturn with provided NetId does not exists"));
        }
    }

    private static void RestoreDebt(
        IClientInDebtRepository clientInDebtRepository,
        IClientAgreementRepository clientAgreementRepository,
        ICurrencyRepository currencyRepository,
        IExchangeRateRepository exchangeRateRepository,
        ICrossExchangeRateRepository crossExchangeRateRepository,
        IDebtRepository debtRepository,
        IClientBalanceMovementRepository clientBalanceMovementRepository,
        decimal toDebtAmount,
        SaleReturnItem returnItem,
        decimal exchangeRateAmount
    ) {
        ClientAgreement clientAgreement = clientAgreementRepository.GetByIdWithAgreementAndOrganization(returnItem.OrderItem.Order.Sale.ClientAgreementId);

        if (clientAgreement.CurrentAmount < toDebtAmount) {
            clientBalanceMovementRepository
                .AddOutMovement(new ClientBalanceMovement {
                    ClientAgreementId = clientAgreement.Id,
                    Amount = clientAgreement.CurrentAmount
                });

            clientAgreement.CurrentAmount = decimal.Zero;

            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

            toDebtAmount = decimal.Round(toDebtAmount - clientAgreement.CurrentAmount, 14, MidpointRounding.AwayFromZero);

            Currency euroCurrency = currencyRepository.GetEURCurrencyIfExists();

            decimal amountInDebtCurrency = decimal.Round(toDebtAmount * exchangeRateAmount, 14, MidpointRounding.AwayFromZero);

            ClientInDebt clientInDebt =
                clientInDebtRepository.GetBySaleAndClientAgreementIdsWithDeleted(
                    returnItem.OrderItem.Order.Sale.Id,
                    clientAgreement.Id
                );

            if (clientInDebt.Deleted) {
                clientInDebtRepository.Restore(clientInDebt.Id);

                clientInDebt.Debt.Total = amountInDebtCurrency;
            } else {
                clientInDebt.Debt.Total =
                    decimal.Round(clientInDebt.Debt.Total + amountInDebtCurrency, 14, MidpointRounding.AwayFromZero);
            }

            debtRepository.Update(clientInDebt.Debt);
        } else {
            clientBalanceMovementRepository
                .AddOutMovement(new ClientBalanceMovement {
                    ClientAgreementId = clientAgreement.Id,
                    Amount = toDebtAmount
                });

            clientAgreement.CurrentAmount =
                decimal.Round(clientAgreement.CurrentAmount - toDebtAmount, 14, MidpointRounding.AwayFromZero);

            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);
        }
    }

    private static void RestoreMoney(
        ISaleRepository saleRepository,
        IClientInDebtRepository clientInDebtRepository,
        IClientAgreementRepository clientAgreementRepository,
        ICurrencyRepository currencyRepository,
        IExchangeRateRepository exchangeRateRepository,
        ICrossExchangeRateRepository crossExchangeRateRepository,
        IBaseSalePaymentStatusRepository baseSalePaymentStatusRepository,
        IDebtRepository debtRepository,
        IClientBalanceMovementRepository clientBalanceMovementRepository,
        decimal toRestoreAmount,
        SaleReturnItem saleReturnItem,
        decimal exchangeRateAmount,
        long saleId
    ) {
        ClientInDebt clientInDebt =
            clientInDebtRepository
                .GetBySaleAndClientAgreementIds(
                    saleId,
                    saleReturnItem.OrderItem.Order.Sale.ClientAgreementId
                );

        if (clientInDebt != null) {
            decimal amountInDebtCurrency = decimal.Round(toRestoreAmount * exchangeRateAmount, 14, MidpointRounding.AwayFromZero);

            saleReturnItem.Amount = decimal.Round(saleReturnItem.Amount + toRestoreAmount, 14, MidpointRounding.AwayFromZero);
            saleReturnItem.ExchangeRateAmount = exchangeRateAmount;

            if (amountInDebtCurrency.Equals(clientInDebt.Debt.Total)) {
                clientInDebtRepository.Remove(clientInDebt.NetUid);

                baseSalePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, clientInDebt.Sale.BaseSalePaymentStatusId);
                saleRepository.UpdateIsAcceptedToPacking(clientInDebt.Sale.Id, true);
            } else if (amountInDebtCurrency < clientInDebt.Debt.Total) {
                clientInDebt.Debt.Total =
                    decimal.Round(clientInDebt.Debt.Total - amountInDebtCurrency, 14, MidpointRounding.AwayFromZero);

                debtRepository.Update(clientInDebt.Debt);

                baseSalePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid, clientInDebt.Sale.BaseSalePaymentStatusId);
            } else {
                clientInDebtRepository.Remove(clientInDebt.NetUid);

                baseSalePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, clientInDebt.Sale.BaseSalePaymentStatusId);
                saleRepository.UpdateIsAcceptedToPacking(clientInDebt.Sale.Id, true);

                amountInDebtCurrency =
                    decimal.Round(amountInDebtCurrency - clientInDebt.Debt.Total, 14, MidpointRounding.AwayFromZero);

                toRestoreAmount =
                    decimal.Round(amountInDebtCurrency / exchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                RestoreMoney(
                    saleRepository,
                    clientInDebtRepository,
                    clientAgreementRepository,
                    currencyRepository,
                    exchangeRateRepository,
                    crossExchangeRateRepository,
                    baseSalePaymentStatusRepository,
                    debtRepository,
                    clientBalanceMovementRepository,
                    toRestoreAmount,
                    saleReturnItem,
                    exchangeRateAmount,
                    saleId
                );
            }
        } else if (!saleReturnItem.OrderItem.Order.Sale.RetailClientId.HasValue) {
            ClientAgreement clientAgreement =
                clientAgreementRepository.GetByIdWithAgreementAndOrganization(saleReturnItem.OrderItem.Order.Sale.ClientAgreementId);

            clientAgreement.CurrentAmount =
                decimal.Round(clientAgreement.CurrentAmount + toRestoreAmount, 14, MidpointRounding.AwayFromZero);

            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

            clientBalanceMovementRepository
                .AddInMovement(new ClientBalanceMovement {
                    ClientAgreementId = clientAgreement.Id,
                    Amount = toRestoreAmount,
                    ExchangeRateAmount = 1m
                });

            saleReturnItem.ExchangeRateAmount = exchangeRateAmount;
            saleReturnItem.Amount = toRestoreAmount;
        } else {
            saleReturnItem.ExchangeRateAmount = exchangeRateAmount;
            saleReturnItem.Amount = toRestoreAmount;
        }
    }
}