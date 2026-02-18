using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Synchronizations;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Messages.Communications.Hubs;
using GBA.Domain.Messages.DataSync;
using GBA.Domain.Messages.Logging;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Domain.TranslationEntities;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace GBA.Services.Actors.DataSync;

public sealed class SyncOutcomeOrdersWorkerActor : ReceiveActor {
    private static readonly Regex _cashRegisterNameReplace = new(@"\(.+\)", RegexOptions.Compiled);

    private readonly string _comment = "1�";

    private readonly IDbConnectionFactory _connectionFactory;

    private readonly IDataSyncRepositoriesFactory _dataSyncRepositoriesFactory;

    private readonly DateTime _defaultDate = new(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly IStringLocalizer<SharedResource> _localizer;

    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public SyncOutcomeOrdersWorkerActor(
        IStringLocalizer<SharedResource> localizer,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDataSyncRepositoriesFactory dataSyncRepositoriesFactory) {
        _localizer = localizer;

        _connectionFactory = connectionFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        _dataSyncRepositoriesFactory = dataSyncRepositoriesFactory;

        Receive<SynchronizeOutcomeOrdersMessage>(ProcessSynchronizeOutcomeOrdersMessage);
    }

    private void ProcessSynchronizeOutcomeOrdersMessage(SynchronizeOutcomeOrdersMessage message) {
        using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
        using IDbConnection amgCConnection = _connectionFactory.NewAmgOneCSqlConnection();
        using IDbConnection remoteSyncConnection = _connectionFactory.NewSqlConnection();
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(remoteSyncConnection);

        IActorRef hubSenderActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

        User currentUser = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

        DateTime currentDate = DateTime.UtcNow;

        SynchronizeOutcomeOrders(hubSenderActorRef, oneCConnection, remoteSyncConnection, currentUser, amgCConnection, message.ForAmg, message.From, message.To);

        ActorReferenceManager.Instance.Get(BaseActorNames.DATA_SYNC_MANAGEMENT_ACTOR).Tell(new OutcomeOrdersSynchronizationFinishedMessage());

        _dataSyncRepositoriesFactory
            .NewDataSyncOperationRepository(remoteSyncConnection)
            .AddWithSpecificDates(new DataSyncOperation {
                UserId = currentUser.Id,
                OperationType = DataSyncOperationType.OutcomeOrders,
                Created = currentDate,
                Updated = currentDate,
                ForAmg = message.ForAmg
            });
    }

    private void SynchronizeOutcomeOrders(
        IActorRef hubSenderActorRef,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        User currentUser,
        IDbConnection amgSyncConnection,
        bool forAmg,
        DateTime fromDate,
        DateTime toDate) {
        try {
            DataSyncOperation operation =
                _dataSyncRepositoriesFactory
                    .NewDataSyncOperationRepository(remoteSyncConnection)
                    .GetLastRecordByOperationType(
                        DataSyncOperationType.Accounting,
                        DataSyncOperationType.OutcomeOrders
                    );

            IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository =
                _dataSyncRepositoriesFactory.NewOutcomeOrdersSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.OUTCOME_DOCUMENTS_SYNC_START]));

            outcomeOrdersSyncRepository.CleanDebtsAndBalances();

            IEnumerable<ClientAgreement> clientAgreements =
                outcomeOrdersSyncRepository.GetAllClientAgreementsToSync();

            foreach (ClientAgreement clientAgreement in clientAgreements
                         .Where(x => forAmg ? x.OriginalClientAmgCode.HasValue : x.OriginalClientFenixCode.HasValue)) {
                if (!clientAgreement.OriginalClientAmgCode.HasValue && !clientAgreement.OriginalClientFenixCode.HasValue) continue;

                IEnumerable<SyncSettlement> syncSettlements =
                    forAmg
                        ? clientAgreement.OriginalClientAmgCode.HasValue
                            ? outcomeOrdersSyncRepository.GetAmgSyncSettlements(
                                fromDate,
                                toDate,
                                clientAgreement.OriginalClientAmgCode.Value,
                                clientAgreement.Agreement.Organization.Name,
                                clientAgreement.Agreement.Name,
                                clientAgreement.Agreement.Currency.CodeOneC,
                                clientAgreement.Agreement.Pricing.Name
                            )
                            : Array.Empty<SyncSettlement>()
                        : clientAgreement.OriginalClientFenixCode.HasValue
                            ? outcomeOrdersSyncRepository
                                .GetSyncSettlements(
                                    fromDate,
                                    toDate,
                                    clientAgreement.OriginalClientFenixCode.Value,
                                    clientAgreement.Agreement.Organization.Name,
                                    clientAgreement.Agreement.Name,
                                    clientAgreement.Agreement.Currency.CodeOneC,
                                    clientAgreement.Agreement.Pricing.Name
                                )
                            : Array.Empty<SyncSettlement>();

                if (!syncSettlements.Any())
                    foreach (SyncSettlement syncSettlement in syncSettlements)
                        switch (syncSettlement.SettlementType) {
                            case SyncSettlementType.SaleReturn:
                                SyncSaleReturn(
                                    outcomeOrdersSyncRepository,
                                    clientAgreement,
                                    syncSettlement,
                                    currentUser.Id,
                                    forAmg
                                );

                                break;
                            case SyncSettlementType.IncomePaymentOrder:
                                SyncIncomePaymentOrder(
                                    outcomeOrdersSyncRepository,
                                    clientAgreement,
                                    syncSettlement,
                                    currentUser.Id,
                                    forAmg
                                );

                                break;
                            case SyncSettlementType.OutcomePaymentOrder:
                                SyncOutcomePaymentOrder(
                                    outcomeOrdersSyncRepository,
                                    clientAgreement,
                                    syncSettlement,
                                    currentUser.Id,
                                    forAmg
                                );

                                break;
                            case SyncSettlementType.IncomeCashOrder:
                                SyncIncomeCashOrder(
                                    outcomeOrdersSyncRepository,
                                    clientAgreement,
                                    syncSettlement,
                                    currentUser.Id,
                                    forAmg
                                );

                                break;
                            case SyncSettlementType.Sale:
                                SyncSale(
                                    outcomeOrdersSyncRepository,
                                    clientAgreement,
                                    syncSettlement,
                                    currentUser.Id,
                                    forAmg
                                );

                                break;
                        }

                SyncOrders(
                    outcomeOrdersSyncRepository,
                    clientAgreement,
                    fromDate,
                    toDate,
                    currentUser.Id,
                    forAmg
                );

                SyncAccounting(
                    outcomeOrdersSyncRepository,
                    clientAgreement,
                    currentUser.Id,
                    forAmg
                );
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.OUTCOME_DOCUMENTS_SYNC_END]));
        } catch (Exception exc) {
            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddDataSyncLogMessage(
                        "SYNC_ERROR Outcome Orders",
                        $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                        JsonConvert.SerializeObject(new {
                            exc.Message,
                            exc.StackTrace
                        })
                    )
                );
        }
    }

    private void SyncSaleReturn(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        ClientAgreement clientAgreement,
        SyncSettlement syncSettlement,
        long currentUserId,
        bool forAmg) {
        IEnumerable<SyncSaleReturnItem> saleReturnItems =
            forAmg
                ? outcomeOrdersSyncRepository.GetAmgSaleReturnItemsBySourceId(syncSettlement.DocumentRef)
                : outcomeOrdersSyncRepository.GetSaleReturnItemsBySourceId(syncSettlement.DocumentRef);

        if (!saleReturnItems.Any()) return;

        Storage storage =
            outcomeOrdersSyncRepository.GetStorageIfExists();

        if (storage == null) {
            storage = new Storage {
                Name = _comment,
                ForDefective = true,
                ForVatProducts = true,
                Locale = "uk",
                Deleted = true
            };

            storage.Id = outcomeOrdersSyncRepository.Add(storage);
        }

        SaleReturn saleReturn = null;

        foreach (SyncSaleReturnItem saleReturnItem in saleReturnItems) {
            Product product =
                outcomeOrdersSyncRepository.GetProductBySourceCodeWithIncludes(saleReturnItem.ProductCode, forAmg);

            if (product == null) continue;

            if (saleReturn == null) {
                saleReturn = new SaleReturn {
                    CreatedById = currentUserId,
                    Number = saleReturnItem.DocumentNumber,
                    FromDate = saleReturnItem.DocumentDate.AddYears(-2000),
                    ClientId = clientAgreement.ClientId
                };

                saleReturn.Id = outcomeOrdersSyncRepository.Add(saleReturn);
            }

            OrderItem orderItem =
                outcomeOrdersSyncRepository
                    .GetLastOrderItemByClientAgreementAndProductIdsIfExists(
                        clientAgreement.Id,
                        product.Id
                    );

            if (orderItem == null) {
                Order order = outcomeOrdersSyncRepository.GetLastOrderByClientAgreementId(clientAgreement.Id);

                if (order == null) {
                    Sale sale = new() {
                        Order = new Order {
                            UserId = currentUserId,
                            ClientAgreementId = clientAgreement.Id,
                            OrderSource = OrderSource.Local,
                            OrderStatus = OrderStatus.Sale,
                            Created = _defaultDate,
                            Updated = _defaultDate
                        },
                        SaleNumberId = outcomeOrdersSyncRepository.Add(new SaleNumber {
                            Value = "1C",
                            OrganizationId = clientAgreement.Agreement.Organization.Id
                        }),
                        BaseLifeCycleStatusId = outcomeOrdersSyncRepository.Add(new ReceivedSaleLifeCycleStatus()),
                        BaseSalePaymentStatusId = outcomeOrdersSyncRepository.Add(new NotPaidSalePaymentStatus()),
                        TransporterId = 57,
                        Comment = _comment,
                        ChangedToInvoice = _defaultDate,
                        Created = _defaultDate,
                        Updated = _defaultDate,
                        ChangedToInvoiceById = currentUserId,
                        IsVatSale = clientAgreement.Agreement.IsAccounting,
                        IsLocked = clientAgreement.Agreement.IsAccounting,
                        IsPaymentBillDownloaded = false,
                        ClientAgreementId = clientAgreement.Id,
                        UserId = currentUserId
                    };

                    sale.OrderId = sale.Order.Id = outcomeOrdersSyncRepository.Add(sale.Order);

                    sale.Id = outcomeOrdersSyncRepository.Add(sale);

                    order = sale.Order;
                }

                orderItem = new OrderItem {
                    Comment = _comment,
                    ProductId = product.Id,
                    Qty = 0,
                    PricePerItem = 0,
                    PricePerItemWithoutVat = 0,
                    ExchangeRateAmount = 0,
                    OrderId = order.Id
                };

                orderItem.Id = outcomeOrdersSyncRepository.AddWithId(orderItem);
            }

            ProductAvailability availability =
                outcomeOrdersSyncRepository
                    .GetAvailability(
                        product.Id,
                        storage.Id
                    );

            if (availability == null) {
                availability = new ProductAvailability {
                    StorageId = storage.Id,
                    ProductId = product.Id,
                    Amount = saleReturnItem.Quantity
                };

                outcomeOrdersSyncRepository.Add(availability);
            } else {
                availability.Amount += saleReturnItem.Quantity;

                outcomeOrdersSyncRepository.Update(availability);
            }

            outcomeOrdersSyncRepository.Add(new SaleReturnItem {
                OrderItemId = orderItem.Id,
                StorageId = storage.Id,
                CreatedById = currentUserId,
                SaleReturnId = saleReturn.Id,
                Amount = saleReturnItem.Price,
                ExchangeRateAmount = saleReturnItem.RateExchange,
                SaleReturnItemStatus = SaleReturnItemStatus.Defect
            });
        }
    }

    private void SyncIncomePaymentOrder(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        ClientAgreement clientAgreement,
        SyncSettlement syncSettlement,
        long currentUserId,
        bool forAmg) {
        SyncIncomePaymentOrder syncOrder =
            forAmg
                ? outcomeOrdersSyncRepository.GetAmgIncomePaymentOrderBySourceId(syncSettlement.DocumentRef)
                : outcomeOrdersSyncRepository.GetIncomePaymentOrderBySourceId(syncSettlement.DocumentRef);

        if (syncOrder == null) return;

        PaymentRegister register =
            outcomeOrdersSyncRepository.GetPaymentRegister(syncOrder.OrganizationAccountName, syncOrder.CurrencyCode)
            ??
            outcomeOrdersSyncRepository
                .GetPaymentRegister(
                    _cashRegisterNameReplace.Replace(syncOrder.OrganizationAccountName, string.Empty).Trim(),
                    syncOrder.CurrencyCode
                );

        if (register == null) return;

        syncOrder.RateExchange = syncOrder.RateExchange <= 0 ? 1 : syncOrder.RateExchange;

        IncomePaymentOrder incomePaymentOrder = new() {
            Number = syncOrder.DocumentNumber,
            FromDate = syncOrder.DocumentDate.AddYears(-2000),
            EuroAmount = decimal.Round(syncOrder.DocumentValue / syncOrder.RateExchange, 4, MidpointRounding.AwayFromZero),
            ExchangeRate = syncOrder.RateExchange,
            Comment = _comment + " " + syncOrder.Comment,
            ClientAgreementId = clientAgreement.Id,
            ClientId = clientAgreement.ClientId,
            Amount = syncOrder.DocumentValue,
            UserId = currentUserId,
            PaymentRegisterId = register.Id,
            CurrencyId = register.PaymentCurrencyRegisters.First().CurrencyId,
            OrganizationId = clientAgreement.Agreement.OrganizationId ?? register.OrganizationId
        };

        incomePaymentOrder.Id = outcomeOrdersSyncRepository.Add(incomePaymentOrder);

        PaymentMovement movement =
            outcomeOrdersSyncRepository.GetPaymentMovementByName(syncOrder.ArticleCashSpendingName);

        if (movement == null) {
            movement = new PaymentMovement {
                OperationName = syncOrder.ArticleCashSpendingName
            };

            movement.Id = outcomeOrdersSyncRepository.Add(movement);
        }

        outcomeOrdersSyncRepository.Add(new PaymentMovementOperation {
            IncomePaymentOrderId = incomePaymentOrder.Id,
            PaymentMovementId = movement.Id
        });
    }

    private void SyncOutcomePaymentOrder(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        ClientAgreement clientAgreement,
        SyncSettlement syncSettlement,
        long currentUserId,
        bool forAmg) {
        SyncOutcomePaymentOrder syncOrder =
            forAmg
                ? outcomeOrdersSyncRepository.GetAmgOutcomePaymentOrderBySourceId(syncSettlement.DocumentRef)
                : outcomeOrdersSyncRepository.GetOutcomePaymentOrderBySourceId(syncSettlement.DocumentRef);

        if (syncOrder == null) return;

        PaymentRegister register =
            outcomeOrdersSyncRepository.GetPaymentRegister(syncOrder.OrganizationAccountName, syncOrder.CurrencyCode)
            ??
            outcomeOrdersSyncRepository
                .GetPaymentRegister(
                    _cashRegisterNameReplace.Replace(syncOrder.OrganizationAccountName, string.Empty).Trim(),
                    syncOrder.CurrencyCode
                );

        if (register == null) return;

        syncOrder.RateExchange = syncOrder.RateExchange <= 0 ? 1 : syncOrder.RateExchange;

        OutcomePaymentOrder outcomePaymentOrder = new() {
            Number = syncOrder.DocumentNumber,
            FromDate = syncOrder.DocumentDate.AddYears(-2000),
            AfterExchangeAmount = decimal.Round(syncOrder.DocumentValue / syncOrder.RateExchange, 4, MidpointRounding.AwayFromZero),
            ExchangeRate = syncOrder.RateExchange,
            Comment = _comment + " " + syncOrder.Comment,
            ClientAgreementId = clientAgreement.Id,
            Amount = syncOrder.DocumentValue,
            UserId = currentUserId,
            PaymentCurrencyRegisterId = register.PaymentCurrencyRegisters.First().Id,
            OrganizationId = clientAgreement.Agreement.OrganizationId ?? register.OrganizationId
        };

        outcomePaymentOrder.Id = outcomeOrdersSyncRepository.Add(outcomePaymentOrder);

        PaymentMovement movement =
            outcomeOrdersSyncRepository.GetPaymentMovementByName(syncOrder.ArticleCashExpendingName);

        if (movement == null) {
            movement = new PaymentMovement {
                OperationName = syncOrder.ArticleCashExpendingName
            };

            movement.Id = outcomeOrdersSyncRepository.Add(movement);
        }

        outcomeOrdersSyncRepository.Add(new PaymentMovementOperation {
            OutcomePaymentOrderId = outcomePaymentOrder.Id,
            PaymentMovementId = movement.Id
        });
    }

    private void SyncIncomeCashOrder(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        ClientAgreement clientAgreement,
        SyncSettlement syncSettlement,
        long currentUserId,
        bool forAmg) {
        SyncIncomeCashOrder syncOrder =
            forAmg
                ? outcomeOrdersSyncRepository.GetAmgIncomeCashOrderBySourceId(syncSettlement.DocumentRef)
                : outcomeOrdersSyncRepository.GetIncomeCashOrderBySourceId(syncSettlement.DocumentRef);

        if (syncOrder == null) return;

        PaymentRegister register =
            outcomeOrdersSyncRepository.GetPaymentRegister(syncOrder.PaymentRegisterName, syncOrder.CurrencyCode)
            ??
            outcomeOrdersSyncRepository
                .GetPaymentRegister(
                    _cashRegisterNameReplace.Replace(syncOrder.PaymentRegisterName, string.Empty).Trim(),
                    syncOrder.CurrencyCode
                );

        if (register == null) return;

        syncOrder.RateExchange = syncOrder.RateExchange <= 0 ? 1 : syncOrder.RateExchange;

        IncomePaymentOrder incomePaymentOrder = new() {
            Number = syncOrder.DocumentNumber,
            FromDate = syncOrder.DocumentDate.AddYears(-2000),
            EuroAmount = decimal.Round(syncOrder.DocumentValue / syncOrder.RateExchange, 4, MidpointRounding.AwayFromZero),
            ExchangeRate = syncOrder.RateExchange,
            Comment = _comment + " " + syncOrder.Comment,
            ClientId = clientAgreement.ClientId,
            ClientAgreementId = clientAgreement.Id,
            Amount = syncOrder.DocumentValue,
            UserId = currentUserId,
            PaymentRegisterId = register.Id,
            CurrencyId = register.PaymentCurrencyRegisters.First().CurrencyId,
            OrganizationId = clientAgreement.Agreement.OrganizationId ?? register.OrganizationId
        };

        incomePaymentOrder.Id = outcomeOrdersSyncRepository.Add(incomePaymentOrder);

        PaymentMovement movement =
            outcomeOrdersSyncRepository.GetPaymentMovementByName(syncOrder.ArticleCashExpendingName);

        if (movement == null) {
            movement = new PaymentMovement {
                OperationName = syncOrder.ArticleCashExpendingName
            };

            movement.Id = outcomeOrdersSyncRepository.Add(movement);
        }

        outcomeOrdersSyncRepository.Add(new PaymentMovementOperation {
            IncomePaymentOrderId = incomePaymentOrder.Id,
            PaymentMovementId = movement.Id
        });
    }

    private void SyncSale(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        ClientAgreement clientAgreement,
        SyncSettlement syncSettlement,
        long currentUserId,
        bool forAmg) {
        IEnumerable<SyncSaleItem> saleItems =
            forAmg
                ? outcomeOrdersSyncRepository.GetAmgSaleItemsBySourceId(syncSettlement.DocumentRef)
                : outcomeOrdersSyncRepository.GetSaleItemsBySourceId(syncSettlement.DocumentRef);

        if (!saleItems.Any()) return;

        Sale sale = null;

        foreach (SyncSaleItem saleItem in saleItems) {
            Product product = outcomeOrdersSyncRepository.GetProductBySourceCodeWithIncludes(saleItem.ProductCode, forAmg);

            if (product == null) continue;

            decimal exchangeRateAmount =
                saleItem.RateExchange > 1
                    ? saleItem.RateExchange
                    : outcomeOrdersSyncRepository
                        .GetExchangeRateAmountToEuroByDate(
                            clientAgreement.Agreement.Currency.Id,
                            saleItem.DocumentDate
                        );

            if (sale == null) {
                sale = new Sale {
                    Order = new Order {
                        UserId = currentUserId,
                        ClientAgreementId = clientAgreement.Id,
                        OrderSource = OrderSource.Local,
                        OrderStatus = OrderStatus.Sale,
                        Created = saleItem.DocumentDate.AddYears(-2000),
                        Updated = saleItem.DocumentDate.AddYears(-2000)
                    },
                    SaleNumberId = outcomeOrdersSyncRepository.Add(new SaleNumber {
                        Value = saleItem.DocumentNumber,
                        OrganizationId = clientAgreement.Agreement.Organization.Id
                    }),
                    BaseLifeCycleStatusId = outcomeOrdersSyncRepository.Add(new ReceivedSaleLifeCycleStatus()),
                    BaseSalePaymentStatusId = outcomeOrdersSyncRepository.Add(new NotPaidSalePaymentStatus()),
                    TransporterId = 57,
                    Comment = _comment,
                    ChangedToInvoice = saleItem.DocumentDate.AddYears(-2000),
                    Created = saleItem.DocumentDate.AddYears(-2000),
                    Updated = saleItem.DocumentDate.AddYears(-2000),
                    ChangedToInvoiceById = currentUserId,
                    IsVatSale = clientAgreement.Agreement.IsAccounting,
                    IsLocked = clientAgreement.Agreement.IsAccounting,
                    IsPaymentBillDownloaded = false,
                    ClientAgreementId = clientAgreement.Id,
                    UserId = currentUserId,
                    IsImported = true
                };

                sale.OrderId = sale.Order.Id = outcomeOrdersSyncRepository.Add(sale.Order);

                sale.Id = outcomeOrdersSyncRepository.Add(sale);
            }

            decimal pricePerItem = saleItem.Price / Convert.ToDecimal(saleItem.Quantity);

            OrderItem orderItem = new() {
                ProductId = product.Id,
                OrderId = sale.Order.Id,
                Qty = saleItem.Quantity,
                UserId = currentUserId,
                Comment = _comment,
                IsValidForCurrentSale = true,
                PricePerItem =
                    exchangeRateAmount > 0
                        ? decimal.Round(pricePerItem / exchangeRateAmount, 4, MidpointRounding.AwayFromZero)
                        : decimal.Round(pricePerItem * (0 - exchangeRateAmount), 4, MidpointRounding.AwayFromZero),
                ExchangeRateAmount = exchangeRateAmount,
                PricePerItemWithoutVat =
                    exchangeRateAmount > 0
                        ? decimal.Round(pricePerItem / exchangeRateAmount, 4, MidpointRounding.AwayFromZero)
                        : decimal.Round(pricePerItem * (0 - exchangeRateAmount), 4, MidpointRounding.AwayFromZero)
            };

            orderItem.Id = outcomeOrdersSyncRepository.AddWithId(orderItem);

            IEnumerable<ProductAvailability> productAvailabilities =
                outcomeOrdersSyncRepository
                    .GetAvailabilities(
                        product.Id,
                        clientAgreement.Agreement.Organization.Id,
                        clientAgreement.Agreement.Organization.Culture != "pl" && clientAgreement.Agreement.WithVATAccounting
                    );

            foreach (ProductAvailability availability in productAvailabilities) {
                if (orderItem.Qty.Equals(0d)) break;

                ProductReservation reservation = new() {
                    OrderItemId = orderItem.Id,
                    ProductAvailabilityId = availability.Id
                };

                if (availability.Amount >= orderItem.Qty) {
                    availability.Amount -= orderItem.Qty;

                    reservation.Qty = orderItem.Qty;

                    orderItem.Qty = 0d;
                } else {
                    orderItem.Qty -= availability.Amount;

                    reservation.Qty = availability.Amount;

                    availability.Amount = 0d;
                }

                outcomeOrdersSyncRepository.Update(availability);

                outcomeOrdersSyncRepository.Add(reservation);
            }
        }
    }

    private void SyncOrders(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        ClientAgreement clientAgreement,
        DateTime fromDate,
        DateTime toDate,
        long currentUserId,
        bool forAmg) {
        IEnumerable<SyncOrderItem> orderItems =
            forAmg
                ? clientAgreement.OriginalClientAmgCode.HasValue
                    ? outcomeOrdersSyncRepository.GetAmgAllSyncOrderItems(
                        fromDate,
                        toDate,
                        clientAgreement.OriginalClientAmgCode.Value,
                        clientAgreement.Agreement.Organization.Name,
                        clientAgreement.Agreement.Name,
                        clientAgreement.Agreement.Currency.CodeOneC,
                        clientAgreement.Agreement.Pricing.Name
                    )
                    : Array.Empty<SyncOrderItem>()
                : clientAgreement.OriginalClientFenixCode.HasValue
                    ? outcomeOrdersSyncRepository
                        .GetAllSyncOrderItems(
                            fromDate,
                            toDate,
                            clientAgreement.OriginalClientFenixCode.Value,
                            clientAgreement.Agreement.Organization.Name,
                            clientAgreement.Agreement.Name,
                            clientAgreement.Agreement.Currency.CodeOneC,
                            clientAgreement.Agreement.Pricing.Name
                        )
                    : Array.Empty<SyncOrderItem>();

        if (!orderItems.Any()) return;

        string documentNumber = string.Empty;
        DateTime documentDate = DateTime.MinValue;

        Sale sale = null;

        foreach (SyncOrderItem syncOrderItem in orderItems) {
            if (!documentNumber.Equals(syncOrderItem.DocumentNumber) ||
                !documentDate.Equals(syncOrderItem.DocumentDate)) {
                sale = null;

                documentNumber = syncOrderItem.DocumentNumber;
                documentDate = syncOrderItem.DocumentDate;
            }

            Product product = outcomeOrdersSyncRepository.GetProductBySourceCodeWithIncludes(syncOrderItem.ProductCode, forAmg);

            if (product == null) continue;

            if (sale == null) {
                sale = new Sale {
                    Order = new Order {
                        UserId = currentUserId,
                        ClientAgreementId = clientAgreement.Id,
                        OrderSource = OrderSource.Local,
                        OrderStatus = OrderStatus.Sale,
                        Created = syncOrderItem.DocumentDate.AddYears(-2000),
                        Updated = syncOrderItem.DocumentDate.AddYears(-2000)
                    },
                    SaleNumberId = outcomeOrdersSyncRepository.Add(new SaleNumber {
                        Value = syncOrderItem.DocumentNumber,
                        OrganizationId = clientAgreement.Agreement.Organization.Id
                    }),
                    BaseLifeCycleStatusId = outcomeOrdersSyncRepository.Add(new NewSaleLifeCycleStatus()),
                    BaseSalePaymentStatusId = outcomeOrdersSyncRepository.Add(new NotPaidSalePaymentStatus()),
                    TransporterId = 57,
                    Comment = _comment,
                    ChangedToInvoice = syncOrderItem.DocumentDate.AddYears(-2000),
                    Created = syncOrderItem.DocumentDate.AddYears(-2000),
                    Updated = syncOrderItem.DocumentDate.AddYears(-2000),
                    ChangedToInvoiceById = currentUserId,
                    IsVatSale = clientAgreement.Agreement.IsAccounting,
                    IsLocked = clientAgreement.Agreement.IsAccounting,
                    IsPaymentBillDownloaded = false,
                    ClientAgreementId = clientAgreement.Id,
                    UserId = currentUserId,
                    IsImported = true
                };

                sale.OrderId = sale.Order.Id = outcomeOrdersSyncRepository.Add(sale.Order);

                sale.Id = outcomeOrdersSyncRepository.Add(sale);
            }

            OrderItem orderItem = new() {
                ProductId = product.Id,
                OrderId = sale.Order.Id,
                Qty = syncOrderItem.Qty,
                UserId = currentUserId,
                Comment = _comment,
                IsValidForCurrentSale = true
            };

            orderItem.Id = outcomeOrdersSyncRepository.AddWithId(orderItem);

            IEnumerable<ProductAvailability> productAvailabilities =
                outcomeOrdersSyncRepository
                    .GetAvailabilities(
                        product.Id,
                        clientAgreement.Agreement.Organization.Id,
                        clientAgreement.Agreement.Organization.Culture != "pl" && clientAgreement.Agreement.WithVATAccounting
                    );

            foreach (ProductAvailability availability in productAvailabilities) {
                if (orderItem.Qty.Equals(0d)) break;

                ProductReservation reservation = new() {
                    OrderItemId = orderItem.Id,
                    ProductAvailabilityId = availability.Id
                };

                if (availability.Amount >= orderItem.Qty) {
                    availability.Amount -= orderItem.Qty;

                    reservation.Qty = orderItem.Qty;

                    orderItem.Qty = 0d;
                } else {
                    orderItem.Qty -= availability.Amount;

                    reservation.Qty = availability.Amount;

                    availability.Amount = 0d;
                }

                outcomeOrdersSyncRepository.Update(availability);

                outcomeOrdersSyncRepository.Add(reservation);
            }
        }
    }

    private void SyncAccounting(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        ClientAgreement clientAgreement,
        long currentUserId,
        bool forAmg) {
        IEnumerable<SyncAccounting> accounting =
            forAmg
                ? clientAgreement.OriginalClientAmgCode.HasValue
                    ? outcomeOrdersSyncRepository
                        .GetAmgSyncAccountingFiltered(
                            clientAgreement.OriginalClientAmgCode.Value,
                            clientAgreement.Agreement.Name,
                            clientAgreement.Agreement.Organization.Name
                        )
                    : Array.Empty<SyncAccounting>()
                : clientAgreement.OriginalClientFenixCode.HasValue
                    ? outcomeOrdersSyncRepository
                        .GetSyncAccountingFiltered(
                            clientAgreement.OriginalClientFenixCode.Value,
                            clientAgreement.Agreement.Name,
                            clientAgreement.Agreement.Organization.Name
                        )
                    : Array.Empty<SyncAccounting>();

        if (!accounting.Any()) return;

        SyncAccounting syncAccounting = accounting.First();

        syncAccounting.Value = accounting.Sum(a => a.Value);

        decimal exchangeRateAmount =
            outcomeOrdersSyncRepository
                .GetExchangeRateAmountToEuroByDate(
                    clientAgreement.Agreement.Currency.Id,
                    syncAccounting.Date
                );

        if (syncAccounting.Value < decimal.Zero) {
            syncAccounting.Value = 0 - syncAccounting.Value;

            clientAgreement.CurrentAmount =
                exchangeRateAmount > 0
                    ? decimal.Round(syncAccounting.Value / exchangeRateAmount, 2, MidpointRounding.AwayFromZero)
                    : decimal.Round(syncAccounting.Value * (0 - exchangeRateAmount), 2, MidpointRounding.AwayFromZero);

            outcomeOrdersSyncRepository.Update(clientAgreement);
        } else {
            Sale preExistingSale =
                outcomeOrdersSyncRepository
                    .GetSaleIfExists(
                        clientAgreement.Id,
                        syncAccounting.Number,
                        syncAccounting.Date
                    );

            if (preExistingSale != null) {
                outcomeOrdersSyncRepository.Add(new ClientInDebt {
                    DebtId = outcomeOrdersSyncRepository.Add(new Debt {
                        Total = syncAccounting.Value,
                        Created = syncAccounting.Date,
                        Updated = syncAccounting.Date
                    }),
                    ClientId = clientAgreement.ClientId,
                    AgreementId = clientAgreement.AgreementId,
                    SaleId = preExistingSale.Id,
                    Created = syncAccounting.Date,
                    Updated = syncAccounting.Date
                });
            } else {
                Order order = new() {
                    UserId = currentUserId,
                    ClientAgreementId = clientAgreement.Id,
                    OrderSource = OrderSource.Local,
                    OrderStatus = OrderStatus.Sale,
                    Created = syncAccounting.Date,
                    Updated = syncAccounting.Date
                };

                order.Id = outcomeOrdersSyncRepository.Add(order);

                outcomeOrdersSyncRepository.Add(new ClientInDebt {
                    DebtId = outcomeOrdersSyncRepository.Add(new Debt {
                        Total = syncAccounting.Value,
                        Created = syncAccounting.Date,
                        Updated = syncAccounting.Date
                    }),
                    ClientId = clientAgreement.ClientId,
                    AgreementId = clientAgreement.AgreementId,
                    SaleId = outcomeOrdersSyncRepository.Add(new Sale {
                        OrderId = order.Id,
                        SaleNumberId = outcomeOrdersSyncRepository.Add(new SaleNumber {
                            Value = syncAccounting.Number,
                            OrganizationId = clientAgreement.Agreement.Organization.Id
                        }),
                        BaseLifeCycleStatusId = outcomeOrdersSyncRepository.Add(new ReceivedSaleLifeCycleStatus()),
                        BaseSalePaymentStatusId = outcomeOrdersSyncRepository.Add(new NotPaidSalePaymentStatus()),
                        TransporterId = 57,
                        Comment = "��� ����� � 1�",
                        ChangedToInvoice = syncAccounting.Date,
                        Created = syncAccounting.Date,
                        Updated = syncAccounting.Date,
                        ChangedToInvoiceById = currentUserId,
                        IsVatSale = true,
                        IsLocked = true,
                        IsPaymentBillDownloaded = false,
                        ClientAgreementId = clientAgreement.Id,
                        UserId = currentUserId
                    }),
                    Created = syncAccounting.Date,
                    Updated = syncAccounting.Date
                });

                Product product =
                    outcomeOrdersSyncRepository.GetDevProduct();

                if (product == null) {
                    MeasureUnit measureUnit =
                        outcomeOrdersSyncRepository.GetMeasureUnit();

                    if (measureUnit == null) {
                        measureUnit = new MeasureUnit {
                            Name = " ",
                            Deleted = true
                        };

                        measureUnit.Id = outcomeOrdersSyncRepository.Add(measureUnit);

                        outcomeOrdersSyncRepository.Add(new MeasureUnitTranslation {
                            MeasureUnitId = measureUnit.Id,
                            CultureCode = "uk",
                            Name = " "
                        });
                        outcomeOrdersSyncRepository.Add(new MeasureUnitTranslation {
                            MeasureUnitId = measureUnit.Id,
                            CultureCode = "pl",
                            Name = " "
                        });
                    }

                    product = new Product {
                        VendorCode = "����",
                        Name = "��� �����",
                        NamePL = "��� �����",
                        NameUA = "��� �����",
                        Description = "��� �����",
                        DescriptionPL = "��� �����",
                        DescriptionUA = "��� �����",
                        Deleted = true,
                        MeasureUnitId = measureUnit.Id
                    };

                    product.Id = outcomeOrdersSyncRepository.Add(product);
                }

                outcomeOrdersSyncRepository.Add(new OrderItem {
                    ProductId = product.Id,
                    OrderId = order.Id,
                    Qty = 1,
                    UserId = currentUserId,
                    Comment = "��� ����� � 1�",
                    IsValidForCurrentSale = true,
                    PricePerItem =
                        exchangeRateAmount > 0
                            ? decimal.Round(syncAccounting.Value / exchangeRateAmount, 4, MidpointRounding.AwayFromZero)
                            : decimal.Round(syncAccounting.Value * (0 - exchangeRateAmount), 4, MidpointRounding.AwayFromZero),
                    ExchangeRateAmount = exchangeRateAmount,
                    PricePerItemWithoutVat =
                        exchangeRateAmount > 0
                            ? decimal.Round(syncAccounting.Value / exchangeRateAmount, 4, MidpointRounding.AwayFromZero)
                            : decimal.Round(syncAccounting.Value * (0 - exchangeRateAmount), 4, MidpointRounding.AwayFromZero)
                });
            }
        }
    }
}