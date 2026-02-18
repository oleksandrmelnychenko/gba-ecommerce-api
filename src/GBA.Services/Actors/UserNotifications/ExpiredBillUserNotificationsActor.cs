using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Entities.Sales.SaleShiftStatuses;
using GBA.Domain.Entities.UserNotifications;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.UserNotifications.ExpiredBillUserNotifications;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.UserNotifications.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.UserNotifications;

public sealed class ExpiredBillUserNotificationsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IUserNotificationRepositoriesFactory _userNotificationRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public ExpiredBillUserNotificationsActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IUserNotificationRepositoriesFactory userNotificationRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _userNotificationRepositoriesFactory = userNotificationRepositoriesFactory;

        Receive<GetAllExpiredBillUserNotificationsFilteredMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            if (user.UserRoleId.HasValue
                && (user.UserRole.UserRoleType.Equals(UserRoleType.SalesAnalytic) || user.UserRole.UserRoleType.Equals(UserRoleType.HeadSalesAnalytic))) {
                IEnumerable<ExpiredBillUserNotification> notifications =
                    _userNotificationRepositoriesFactory
                        .NewExpiredBillUserNotificationRepository(connection)
                        .GetAllFiltered(
                            message.Value,
                            user.Id,
                            message.OrderByManager,
                            message.WithProcessed
                        );

                if (notifications.Any()) {
                    List<Sale> sales =
                        _saleRepositoriesFactory
                            .NewSaleRepository(connection)
                            .GetAllByIds(
                                notifications
                                    .Select(n => n.SaleId)
                                    .ToList(),
                                true
                            );

                    CalculatePricingForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

                    foreach (Sale sale in sales)
                        notifications
                            .First(n => n.SaleId.Equals(sale.Id))
                            .Sale = sale;
                }

                Sender.Tell(notifications);
            } else {
                Sender.Tell(new List<ExpiredBillUserNotification>());
            }
        });

        Receive<GetLockedExpiredBillUserNotificationByCurrentUserMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            if (user == null)
                Sender.Tell(null);
            else
                Sender.Tell(
                    _userNotificationRepositoriesFactory
                        .NewExpiredBillUserNotificationRepository(connection)
                        .GetLockedNotificationByUserId(
                            user.Id
                        )
                );
        });

        Receive<GetExpiredBillUserNotificationByNetIdMessage>(message => {
            try {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                ExpiredBillUserNotification notification =
                    _userNotificationRepositoriesFactory
                        .NewExpiredBillUserNotificationRepository(connection)
                        .GetByNetId(
                            message.NetId
                        );

                Sender.Tell(notification);
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });

        Receive<LockExpiredBillUserNotificationByNetIdMessage>(message => {
            try {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                IExpiredBillUserNotificationRepository notificationRepository = _userNotificationRepositoriesFactory.NewExpiredBillUserNotificationRepository(connection);

                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                ExpiredBillUserNotification notification =
                    notificationRepository
                        .GetByNetId(
                            message.NotificationNetId
                        );

                if (notification.Locked)
                    throw new Exception(
                        notification.LockedById.HasValue
                            ? $"Notification locked by {notification.LockedBy.LastName}"
                            : "Notification locked"
                    );
                if (notification.Processed)
                    throw new Exception(
                        notification.ProcessedById.HasValue
                            ? $"Notification processed by {notification.ProcessedBy.LastName}"
                            : "Notification processed"
                    );

                notificationRepository.UnlockAllNotificationsByUserId(user.Id);

                notification.Locked = true;
                notification.LockedById = user.Id;

                notification.LastViewedById = user.Id;

                notificationRepository.Update(notification);

                Sender.Tell(
                    notificationRepository
                        .GetByNetId(
                            message.NotificationNetId
                        )
                );
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });

        Receive<UnlockExpiredBillUserNotificationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IExpiredBillUserNotificationRepository notificationRepository = _userNotificationRepositoriesFactory.NewExpiredBillUserNotificationRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            ExpiredBillUserNotification notification =
                notificationRepository
                    .GetByNetId(
                        message.NotificationNetId
                    );

            notification.Locked = false;
            notification.LockedById = null;

            notification.LastViewedById = user.Id;

            notificationRepository.Update(notification);

            Sender.Tell(
                notificationRepository
                    .GetByNetId(
                        message.NotificationNetId
                    )
            );
        });

        Receive<DeferExpiredBillUserNotificationByNetIdMessage>(message => {
            try {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                IExpiredBillUserNotificationRepository notificationRepository = _userNotificationRepositoriesFactory.NewExpiredBillUserNotificationRepository(connection);

                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                ExpiredBillUserNotification notification =
                    notificationRepository
                        .GetByNetId(
                            message.NotificationNetId
                        );

                if (notification.Locked && !notification.LockedById.Equals(user.Id))
                    throw new Exception(
                        notification.LockedById.HasValue
                            ? $"Notification locked by {notification.LockedBy.LastName}"
                            : "Notification locked"
                    );
                if (notification.Processed)
                    throw new Exception(
                        notification.ProcessedById.HasValue
                            ? $"Notification processed by {notification.ProcessedBy.LastName}"
                            : "Notification processed"
                    );

                notification.AppliedAction = ExpiredBillUserNotificationAppliedAction.Defer;

                notification.Processed = true;
                notification.Locked = false;
                notification.Deferred = true;

                notification.LockedById = null;

                notification.ProcessedById = user.Id;
                notification.LastViewedById = user.Id;

                notificationRepository.Update(notification);

                Sender.Tell(
                    notificationRepository
                        .GetByNetId(
                            message.NotificationNetId
                        )
                );
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });

        Receive<ShiftExpiredBillUserNotificationByNetIdMessage>(message => {
            try {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
                IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
                IOrderItemBaseShiftStatusRepository orderItemShiftStatusRepository = _saleRepositoriesFactory.NewOrderItemBaseShiftStatusRepository(connection);
                IExpiredBillUserNotificationRepository notificationRepository = _userNotificationRepositoriesFactory.NewExpiredBillUserNotificationRepository(connection);

                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                ExpiredBillUserNotification notification =
                    notificationRepository
                        .GetByNetId(
                            message.NotificationNetId
                        );

                if (notification.Locked && !notification.LockedById.Equals(user.Id))
                    throw new Exception(
                        notification.LockedById.HasValue
                            ? $"Notification locked by {notification.LockedBy.LastName}"
                            : "Notification locked"
                    );
                if (notification.Processed)
                    throw new Exception(
                        notification.ProcessedById.HasValue
                            ? $"Notification processed by {notification.ProcessedBy.LastName}"
                            : "Notification processed"
                    );

                notification.AppliedAction = ExpiredBillUserNotificationAppliedAction.Shifting;

                notification.Processed = true;
                notification.Locked = false;

                notification.LockedById = null;

                notification.ProcessedById = user.Id;
                notification.LastViewedById = user.Id;

                notificationRepository.Update(notification);

                Sale saleFromDb = saleRepository.GetByNetId(notification.Sale.NetUid);

                if (saleFromDb.ShiftStatus == null) {
                    saleFromDb.ShiftStatusId = _saleRepositoriesFactory.NewSaleBaseShiftStatusRepository(connection).Add(new SaleFullShiftStatus());

                    saleRepository.Update(saleFromDb);
                }

                List<ShiftedOrderItem> shiftedToStorageOrderItems = new();

                foreach (OrderItem orderItem in saleFromDb.Order.OrderItems) {
                    double shiftedQty = orderItem.Qty - orderItem.ShiftStatuses.Sum(s => s.Qty);

                    if (!(shiftedQty > 0)) continue;

                    OrderItemBaseShiftStatus shiftStatus = new() {
                        OrderItemId = orderItem.Id,
                        Qty = shiftedQty,
                        ShiftStatus = OrderItemShiftStatus.Store,
                        UserId = user.Id
                    };

                    shiftStatus.CurrentId = orderItemShiftStatusRepository.Add(shiftStatus);

                    orderItem.ShiftStatuses.Add(shiftStatus);

                    shiftedToStorageOrderItems.Add(new ShiftedOrderItem(orderItem.ProductId, orderItem.Id, shiftStatus.Qty, shiftStatus.UserId));

                    orderItem.Qty -= shiftStatus.Qty;

                    orderItemRepository.Update(orderItem);
                }

                UpdateCurrentDebts(
                    saleFromDb,
                    _saleRepositoriesFactory,
                    _clientRepositoriesFactory,
                    connection,
                    saleRepository,
                    _clientRepositoriesFactory.NewClientBalanceMovementRepository(connection)
                );

                if (!saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                    IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);
                    IProductPlacementRepository productPlacementRepository = _productRepositoriesFactory.NewProductPlacementRepository(connection);

                    foreach (OrderItem orderItem in saleFromDb.Order.OrderItems.Where(e => e.ShiftStatuses.Any(s => s.IsNew())))
                    foreach (OrderItemBaseShiftStatus shiftStatus in orderItem.ShiftStatuses.Where(s => s.IsNew())) {
                        IEnumerable<ProductLocation> locations =
                            productLocationRepository
                                .GetAllByOrderItemId(
                                    orderItem.Id
                                );

                        double toDecreaseQty = shiftStatus.Qty;

                        foreach (ProductLocation location in locations) {
                            if (toDecreaseQty.Equals(0d)) break;

                            if (location.Qty >= toDecreaseQty) {
                                location.Qty -= toDecreaseQty;

                                location.ProductPlacement.Qty += toDecreaseQty;

                                toDecreaseQty = 0d;

                                if (location.Qty > 0)
                                    productLocationRepository.Update(location);
                                else
                                    productLocationRepository.Remove(location);
                            } else {
                                toDecreaseQty -= location.Qty;

                                location.ProductPlacement.Qty += location.Qty;

                                productLocationRepository.Remove(location);
                            }

                            productPlacementRepository.Restore(location.ProductPlacement);
                        }
                    }
                }

                Sale mergedSale = saleRepository.GetByNetIdWithSaleMergedAndOrderItemsMerged(saleFromDb.NetUid);

                List<SaleMerged> salesMerged = new();

                if (saleFromDb.InputSaleMerges.Any()) AddInputSales(saleRepository, mergedSale, salesMerged);

                mergedSale.InputSaleMerges = salesMerged;

                List<SaleMerged> mergedSalesToRemove =
                    mergedSale
                        .InputSaleMerges
                        .Where(saleMerged => saleMerged.InputSale == null || !saleMerged.InputSale.Order.OrderItems.Any())
                        .ToList();

                mergedSalesToRemove.ForEach(merged => saleFromDb.InputSaleMerges.Remove(merged));

                foreach (OrderItem orderItem in saleFromDb.Order.OrderItems.Where(e => e.ShiftStatuses.Any(s => s.IsNew())))
                foreach (OrderItemBaseShiftStatus shiftStatus in orderItem.ShiftStatuses.Where(s => s.IsNew()))
                    if (mergedSale.InputSaleMerges.Any(s =>
                            s.InputSale.Order.OrderItems.Any(o => o.OrderItemMerges.Any(m => m.OrderItemId.Equals(orderItem.Id))))) {
                        OrderItem mergedOrderItem = mergedSale.InputSaleMerges
                            .First(s => s.InputSale.Order.OrderItems.Any(o => o.OrderItemMerges.Any(m => m.OrderItemId.Equals(orderItem.Id)))).InputSale.Order
                            .OrderItems.First(o => o.OrderItemMerges.Any(m => m.OrderItemId.Equals(orderItem.Id)));

                        mergedOrderItem.Qty -= shiftStatus.Qty;

                        orderItemRepository.Update(mergedOrderItem);
                    }

                if (shiftedToStorageOrderItems.Any())
                    UpdatedProductReservationAndAvailabilityOnShiftedToStorage(
                        _productRepositoriesFactory,
                        connection,
                        shiftedToStorageOrderItems,
                        saleFromDb.ClientAgreement.Agreement.OrganizationId ?? 0
                    );

                Sender.Tell(
                    notificationRepository
                        .GetByNetId(
                            message.NotificationNetId
                        )
                );
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });
    }

    private static void CalculatePricingForSalesWithDynamicPrices(
        List<Sale> sales,
        IExchangeRateRepository exchangeRateRepository) {
        sales.ForEach(sale => {
            if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                ExchangeRate euroExchangeRate = exchangeRateRepository.GetEuroExchangeRateByCurrentCulture();

                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    orderItem.Product.CurrentPrice =
                        decimal.Round(orderItem.Product.CurrentPrice - orderItem.Product.CurrentPrice * orderItem.OneTimeDiscount / 100, 4, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.Product.CurrentPrice * euroExchangeRate.Amount, 4, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
                }
            } else {
                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount =
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal =
                        decimal.Round(
                            decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero)
                            * orderItem.ExchangeRateAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.PricePerItem, 2, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                    //orderItem.TotalAmount = Decimal.Round(orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    //orderItem.TotalAmountLocal = Decimal.Round(orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
                }
            }

            sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

            if (sale.SaleInvoiceDocument != null) {
                sale.SaleInvoiceDocument.ShippingAmount =
                    decimal.Round(
                        sale.SaleInvoiceDocument.ShippingAmountEur * sale.SaleInvoiceDocument.ExchangeRateAmount,
                        2,
                        MidpointRounding.AwayFromZero
                    );
                sale.SaleInvoiceDocument.ShippingAmountWithoutVat =
                    decimal.Round(
                        sale.SaleInvoiceDocument.ShippingAmountEurWithoutVat * sale.SaleInvoiceDocument.ExchangeRateAmount,
                        2,
                        MidpointRounding.AwayFromZero
                    );

                sale.SaleInvoiceDocument.ShippingAmountEur = decimal.Round(sale.SaleInvoiceDocument.ShippingAmountEur, 2, MidpointRounding.AwayFromZero);

                sale.Order.TotalAmount =
                    decimal.Round(sale.Order.TotalAmount + sale.SaleInvoiceDocument.ShippingAmountEur, 2, MidpointRounding.AwayFromZero);
                sale.Order.TotalAmountLocal =
                    decimal.Round(sale.Order.TotalAmountLocal + sale.SaleInvoiceDocument.ShippingAmount, 2, MidpointRounding.AwayFromZero);
            }

            sale.TotalAmount = sale.Order.TotalAmount;
            sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
            sale.TotalCount = sale.Order.TotalCount;
        });
    }

    private static void UpdateCurrentDebts(
        Sale shiftedSale,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IDbConnection connection,
        ISaleRepository saleRepository,
        IClientBalanceMovementRepository clientBalanceMovementRepository) {
        IClientAgreementRepository clientAgreementRepository = clientRepositoriesFactory.NewClientAgreementRepository(connection);

        ClientInDebt clientInDebt = clientRepositoriesFactory.NewClientInDebtRepository(connection).GetActiveByClientAgreementId(shiftedSale.ClientAgreementId);
        Sale sale = saleRepository.GetByNetIdWithShiftedItemsWithoutAdditionalIncludes(shiftedSale.NetUid);
        ClientAgreement clientAgreement = clientAgreementRepository.GetByNetId(sale.ClientAgreement.NetUid);

        if (clientInDebt != null) {
            SaleExchangeRate saleExchangeRate = saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).GetEuroSaleExchangeRateBySaleNetId(shiftedSale.NetUid);

            if (!clientAgreement.Agreement.Currency.Code.Equals("EUR")) {
                if (saleExchangeRate != null)
                    clientInDebt.Debt.EuroTotal = Convert.ToDecimal(clientInDebt.Debt.Total) / saleExchangeRate.Value;
                else
                    clientInDebt.Debt.EuroTotal = Convert.ToDecimal(clientInDebt.Debt.Total);
            } else {
                clientInDebt.Debt.EuroTotal = Convert.ToDecimal(clientInDebt.Debt.Total);
            }

            foreach (OrderItem orderItem in shiftedSale.Order.OrderItems) {
                double shiftedQty = orderItem.Qty - orderItem.ShiftStatuses.Sum(s => s.Qty);

                OrderItem orderItemFromDb = sale.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id));

                if (orderItemFromDb != null) {
                    if (clientAgreement.Agreement.Currency.Code.Equals("EUR")) {
                        clientInDebt.Debt.Total -= orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftedQty);
                        clientInDebt.Debt.EuroTotal -= orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftedQty);
                    } else {
                        if (saleExchangeRate != null)
                            clientInDebt.Debt.Total -= orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftedQty) * saleExchangeRate.Value;
                        else
                            clientInDebt.Debt.Total -= orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftedQty);

                        clientInDebt.Debt.EuroTotal -= orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftedQty);
                    }
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
        } else if (!shiftedSale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
            decimal toRestoreAmount = (from orderItem in shiftedSale.Order.OrderItems.Where(e => e.ShiftStatuses.Any(s => s.IsNew()))
                let shiftedQty = orderItem.Qty - orderItem.ShiftStatuses.Sum(s => s.Qty)
                let orderItemFromDb = sale.Order.OrderItems.First(o => o.Id.Equals(orderItem.Id))
                where orderItemFromDb != null
                select orderItemFromDb.PricePerItem * Convert.ToDecimal(shiftedQty)).Sum();

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

    private static void UpdatedProductReservationAndAvailabilityOnShiftedToStorage(
        IProductRepositoriesFactory productRepositoriesFactory,
        IDbConnection connection,
        List<ShiftedOrderItem> shiftedToStorageOrderItems,
        long organizationId) {
        IProductAvailabilityRepository productAvailabilityRepository = productRepositoriesFactory.NewProductAvailabilityRepository(connection);
        IProductReservationRepository productReservationRepository = productRepositoriesFactory.NewProductReservationRepository(connection);

        shiftedToStorageOrderItems.ForEach(shifted => {
            IEnumerable<ProductReservation> reservations =
                productReservationRepository
                    .GetAllByOrderItemIdWithAvailability(
                        shifted.OrderItemId
                    );

            double toRestoreAmount = shifted.Qty;

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
        });

        productRepositoriesFactory
            .NewGetMultipleProductsRepository(connection)
            .GetAllByIds(shiftedToStorageOrderItems.Select(s => s.ProductId), organizationId);
    }

    private static void AddInputSales(
        ISaleRepository saleRepository,
        Sale saleFromDb,
        ICollection<SaleMerged> salesMerged) {
        foreach (SaleMerged saleMerged in saleFromDb.InputSaleMerges) {
            saleMerged.InputSale = saleRepository.GetByIdWithOrderItemMerged(saleMerged.InputSaleId);

            salesMerged.Add(saleMerged);

            if (saleMerged.InputSale.InputSaleMerges.Any())
                AddInputSales(
                    saleRepository,
                    saleMerged.InputSale,
                    salesMerged
                );
        }
    }
}