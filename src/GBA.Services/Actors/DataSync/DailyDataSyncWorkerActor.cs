using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Util.Internal;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Synchronizations;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.EntityHelpers.Supplies.PackingLists;
using GBA.Domain.Helpers.PaymentOrders;
using GBA.Domain.Messages.Communications.Hubs;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.DataSync;
using GBA.Domain.Messages.DepreciatedOrders;
using GBA.Domain.Messages.Logging;
using GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;
using GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;
using GBA.Domain.Messages.PaymentOrders.PaymentRegisters;
using GBA.Domain.Messages.Products.Transfers;
using GBA.Domain.Messages.Supplies.Invoices;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Transporters.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using static GBA.Common.Helpers.DateTimeHelper;

namespace GBA.Services.Actors.DataSync;

public sealed class DailyDataSyncWorkerActor : ReceiveActor {
    private const string STORAGE_3 = "�����-3";

    private const string DEFAULT_AGREEMENT_NAME = "�������� ������";

    private const string DEFAULT_ORGANIZATION_AMG = "��� ���� �������Ļ";

    private const string SUPPLY_PRICING_NAME = "��";

    private static readonly Regex _cashRegisterNameReplace = new(@"\(.+\)", RegexOptions.Compiled);
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IDataSyncRepositoriesFactory _dataSyncRepositoriesFactory;

    private readonly string _defaultComment = "��� ������� � 1�.";
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly ITransporterRepositoriesFactory _transporterRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    private readonly string[] eurCodes;

    private readonly string[] plnCodes;

    private readonly string[] storageThreeNames;

    private readonly string[] usdCodes;

    public DailyDataSyncWorkerActor(
        IStringLocalizer<SharedResource> localizer,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDataSyncRepositoriesFactory dataSyncRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        ITransporterRepositoriesFactory transporterRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _localizer = localizer;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _dataSyncRepositoriesFactory = dataSyncRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _transporterRepositoriesFactory = transporterRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        ReceiveAsync<SynchronizeDailyDataMessage>(ProcessSynchronizeDailyDataMessage);

        eurCodes = new[] { "978", "979", "555" };

        usdCodes = new[] { "840", "556" };

        plnCodes = new[] { "830", "831", "835" };

        storageThreeNames = new[] { "�����-3", "����� -3" };
    }

    private async Task ProcessSynchronizeDailyDataMessage(SynchronizeDailyDataMessage message) {
        using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
        using IDbConnection amgCConnection = _connectionFactory.NewAmgOneCSqlConnection();
        using IDbConnection remoteSyncConnection = _connectionFactory.NewSqlConnection();
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(remoteSyncConnection);

        IActorRef hubSenderActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

        User currentUser = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

        DateTime currentDate = DateTime.UtcNow;

        await SyncExhcnageRates(
            currentUser,
            message.ForAmg,
            amgCConnection,
            oneCConnection,
            remoteSyncConnection,
            hubSenderActorRef
        );

        await SyncDocuments(message.To, message.From, currentUser, message.ForAmg, message.Types, amgCConnection, oneCConnection, remoteSyncConnection, hubSenderActorRef);

        ActorReferenceManager.Instance.Get(BaseActorNames.DATA_SYNC_MANAGEMENT_ACTOR).Tell(new DailySynchronizationFinishedMessage());

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

    private async Task SyncExhcnageRates(
        User currentUser,
        bool forAmg,
        IDbConnection amgCConnection,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IActorRef hubSenderActorRef) {
        hubSenderActorRef.Tell(
            new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.EXCHANGE_RATE_HISTORY_SYNC_START]));

        IAccountingSyncRepository accountingSyncRepository =
            _dataSyncRepositoriesFactory.NewAccountingSyncRepository(oneCConnection, remoteSyncConnection, amgCConnection);
        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(remoteSyncConnection);

        IEnumerable<Currency> currencies =
            accountingSyncRepository.GetAllCurrencies();

        Currency eur = currencyRepository.GetEURCurrencyIfExists();
        if (eur == null) {
            currencyRepository.Add(new Currency {
                Code = "EUR",
                Name = "����",
                CodeOneC = "978"
            });

            eur = currencyRepository.GetEURCurrencyIfExists();
        }

        Currency usd = currencyRepository.GetUSDCurrencyIfExists();
        if (usd == null) {
            currencyRepository.Add(new Currency {
                Code = "USD",
                Name = "������ ���",
                CodeOneC = "840"
            });

            usd = currencyRepository.GetUSDCurrencyIfExists();
        }

        Currency pln = currencyRepository.GetPLNCurrencyIfExists();
        if (pln == null) {
            currencyRepository.Add(new Currency {
                Code = "PLN",
                Name = "Polish zloty",
                CodeOneC = "830"
            });

            pln = currencyRepository.GetPLNCurrencyIfExists();
        }

        Currency uah = currencyRepository.GetUAHCurrencyIfExists();
        if (uah == null) {
            currencyRepository.Add(new Currency {
                Code = "UAH",
                Name = "UAH",
                CodeOneC = "980"
            });

            uah = currencyRepository.GetUAHCurrencyIfExists();
        }

        foreach (Currency currency in currencies.Where(c => string.IsNullOrEmpty(c.CodeOneC))) {
            switch (currency.Code) {
                case "EUR":
                    currency.CodeOneC = "978";
                    break;
                case "USD":
                    currency.CodeOneC = "840";
                    break;
                case "PLN":
                    currency.CodeOneC = "830";
                    break;
                case "UAH":
                    currency.CodeOneC = "980";
                    break;
            }

            accountingSyncRepository.Update(currency);
        }

        IEnumerable<SyncExchangeRate> syncExchangeRates =
            forAmg ? accountingSyncRepository.GetAmgAllSyncExchangeRates() : accountingSyncRepository.GetAllSyncExchangeRates();

        if (forAmg) {
            accountingSyncRepository.CleanGovExchangeRateHistory();

            IEnumerable<GovExchangeRate> exchangeRates = accountingSyncRepository.GetAllGovExchangeRates();

            foreach (GovExchangeRate exchangeRate in exchangeRates) {
                decimal amount = 1;

                if (exchangeRate.Code == eur.Code)
                    amount = syncExchangeRates.Last(x => eurCodes.Contains(x.CurrencyCode)).RateExchange;
                else if (exchangeRate.Code == usd.Code)
                    amount = syncExchangeRates.Last(x => usdCodes.Contains(x.CurrencyCode)).RateExchange;
                else if (exchangeRate.Code == pln.Code) amount = syncExchangeRates.Last(x => plnCodes.Contains(x.CurrencyCode)).RateExchange;

                exchangeRate.Amount = amount;
            }

            accountingSyncRepository.Update(exchangeRates);

            foreach (SyncExchangeRate syncExchangeRate in syncExchangeRates) {
                GovExchangeRate exchangeRate = null;

                if (eurCodes.Contains(syncExchangeRate.CurrencyCode))
                    exchangeRate =
                        exchangeRates.FirstOrDefault(e => e.Code == eur.Code);
                else if (usdCodes.Contains(syncExchangeRate.CurrencyCode))
                    exchangeRate =
                        exchangeRates.FirstOrDefault(e => e.Code == usd.Code);
                else if (plnCodes.Contains(syncExchangeRate.CurrencyCode))
                    exchangeRate =
                        exchangeRates.FirstOrDefault(e => e.Code == pln.Code);

                if (exchangeRate == null) continue;

                accountingSyncRepository.Add(new GovExchangeRateHistory {
                    GovExchangeRateId = exchangeRate.Id,
                    Amount = syncExchangeRate.RateExchange,
                    UpdatedById = currentUser.Id,
                    Created = syncExchangeRate.Date,
                    Updated = syncExchangeRate.Date
                });
            }
        } else {
            accountingSyncRepository.CleanExchangeRateHistory();

            IEnumerable<ExchangeRate> exchangeRates = accountingSyncRepository.GetAllUahExchangeRates();

            foreach (ExchangeRate exchangeRate in exchangeRates) {
                decimal amount = 1;

                if (exchangeRate.Code == eur.Code)
                    amount = syncExchangeRates.Last(x => eurCodes.Contains(x.CurrencyCode)).RateExchange;
                else if (exchangeRate.Code == usd.Code)
                    amount = syncExchangeRates.Last(x => usdCodes.Contains(x.CurrencyCode)).RateExchange;
                else if (exchangeRate.Code == pln.Code) amount = syncExchangeRates.Last(x => plnCodes.Contains(x.CurrencyCode)).RateExchange;

                exchangeRate.Amount = amount;
            }

            accountingSyncRepository.Update(exchangeRates);

            foreach (SyncExchangeRate syncExchangeRate in syncExchangeRates) {
                ExchangeRate exchangeRate = null;

                if (eurCodes.Contains(syncExchangeRate.CurrencyCode))
                    exchangeRate =
                        exchangeRates.FirstOrDefault(e => e.Code == eur.Code);
                else if (usdCodes.Contains(syncExchangeRate.CurrencyCode))
                    exchangeRate =
                        exchangeRates.FirstOrDefault(e => e.Code == usd.Code);
                else if (plnCodes.Contains(syncExchangeRate.CurrencyCode))
                    exchangeRate =
                        exchangeRates.FirstOrDefault(e => e.Code == pln.Code);

                if (exchangeRate == null) continue;

                accountingSyncRepository.Add(new ExchangeRateHistory {
                    ExchangeRateId = exchangeRate.Id,
                    Amount = syncExchangeRate.RateExchange,
                    UpdatedById = currentUser.Id,
                    Created = syncExchangeRate.Date,
                    Updated = syncExchangeRate.Date
                });
            }
        }

        hubSenderActorRef.Tell(
            new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.EXCHANGE_RATE_HISTORY_SYNC_END]));
    }

    private async Task SyncDocuments(
        DateTime to,
        DateTime from,
        User currentUser,
        bool forAmg,
        SyncProductConsignmentType[] types,
        IDbConnection amgSyncConnection,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IActorRef hubSenderActorRef) {
        try {
            IConsignmentsSyncRepository consignmentsSyncRepository =
                _dataSyncRepositoriesFactory.NewConsignmentsSyncRepository(oneCConnection, amgSyncConnection, remoteSyncConnection);
            ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(remoteSyncConnection);
            IGovCrossExchangeRateRepository govCrossExchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(remoteSyncConnection);
            IGovExchangeRateRepository govExchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(remoteSyncConnection);
            ICrossExchangeRateRepository crossExchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(remoteSyncConnection);
            IExchangeRateRepository exchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(remoteSyncConnection);
            IClientsSyncRepository clientsSyncRepository =
                _dataSyncRepositoriesFactory
                    .NewClientsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
            IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository =
                _dataSyncRepositoriesFactory.NewOutcomeOrdersSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
            ISupplyInvoiceMergedServiceRepository supplyInvoiceMergedServiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceMergedServiceRepository(remoteSyncConnection);
            IPackingListPackageOrderItemSupplyServiceRepository packingListPackageOrderItemSupplyServiceRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemSupplyServiceRepository(remoteSyncConnection);

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.CONSIGNMENTS_SYNC_START]));

            List<SyncConsignment> syncConsignments = new();

            List<string> exceptionList = new();

            foreach (SyncProductConsignmentType type in types.OrderBy(x => x))
                switch (type) {
                    case SyncProductConsignmentType.Order:
                        if (forAmg) {
                            IEnumerable<SyncConsignmentDocumentInfo> syncConsignmentDocuments =
                                consignmentsSyncRepository.GetSyncConsignmentDocumentInfos(from.AddYears(2000), to.AddYears(2000));

                            IEnumerable<SyncConsignment> syncOrderConsignments =
                                consignmentsSyncRepository.GetAmgFilteredSyncOrderConsignments(syncConsignmentDocuments.ToArray());

                            syncConsignments.AddRange(syncOrderConsignments);
                        } else {
                            IEnumerable<SyncConsignment> syncOrderConsignments =
                                consignmentsSyncRepository.GetFilteredSyncOrderConsignments(from.AddYears(2000), to.AddYears(2000));

                            syncConsignments.AddRange(syncOrderConsignments);
                        }

                        break;
                    case SyncProductConsignmentType.Capitalization:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCT_CAPITALIZATION_SYNC_START]));

                        IEnumerable<SyncConsignment> capitalizationConsignments;

                        if (forAmg)
                            capitalizationConsignments = consignmentsSyncRepository.GetAmgFilteredSyncCapitalizationConsignments(from.AddYears(2000), to.AddYears(2000));
                        else
                            capitalizationConsignments = consignmentsSyncRepository.GetFilteredSyncCapitalizationConsignments(from.AddYears(2000), to.AddYears(2000));

                        syncConsignments.AddRange(capitalizationConsignments);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.CONSIGNMENTS_SYNC_END]));
                        break;
                    case SyncProductConsignmentType.SaleReturn:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.CLIENT_RETURN_SYNC_START]));
                        IEnumerable<SyncConsignment> saleReturnConsignments;

                        if (forAmg)
                            saleReturnConsignments = consignmentsSyncRepository.GetAmgFilteredSyncReturnConsignments(from.AddYears(2000), to.AddYears(2000));
                        else
                            saleReturnConsignments = consignmentsSyncRepository.GetFilteredSyncReturnConsignments(from.AddYears(2000), to.AddYears(2000));

                        syncConsignments.AddRange(saleReturnConsignments);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.CLIENT_RETURN_SYNC_END]));
                        break;
                    case SyncProductConsignmentType.ProductTransfers:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCT_TRANSFER_SYNC_START]));

                        await SyncProductTransfers(outcomeOrdersSyncRepository, currentUser, forAmg, from, to);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCT_TRANSFER_SYNC_END]));
                        break;
                    case SyncProductConsignmentType.DepreciatedOrders:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCT_DEPRECIATION_SYNC_START]));

                        await SyncDepreciatedOrders(outcomeOrdersSyncRepository, currentUser, forAmg, from, to);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCT_DEPRECIATION_SYNC_END]));
                        break;
                    case SyncProductConsignmentType.ActProductTransfers:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.ACT_PRODUCT_TRANSFER_SYNC_START]));

                        await SyncActProductTransfers(outcomeOrdersSyncRepository, currentUser, forAmg, from, to);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.ACT_PRODUCT_TRANSFER_SYNC_END]));
                        break;
                    case SyncProductConsignmentType.Sales:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.SALES_SYNC_START]));

                        string exception = SyncOrdersAndSales(outcomeOrdersSyncRepository, remoteSyncConnection, currencyRepository, crossExchangeRateRepository,
                            exchangeRateRepository, from, to, currentUser, forAmg);

                        if (!string.IsNullOrEmpty(exception))
                            exceptionList.Add(exception);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.SALES_SYNC_END]));
                        break;
                    case SyncProductConsignmentType.IncomeBankOrder:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.INCOME_ORDERS_BANK_SYNC_START]));

                        string incomeBankOrderExceptionMessage =
                            await SyncIncomeBankCashOrders(outcomeOrdersSyncRepository, remoteSyncConnection, from, to, currentUser, forAmg, true);

                        if (!string.IsNullOrEmpty(incomeBankOrderExceptionMessage))
                            exceptionList.Add(incomeBankOrderExceptionMessage);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.INCOME_ORDERS_BANK_SYNC_END]));
                        break;
                    case SyncProductConsignmentType.IncomeCashOrder:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.INCOME_ORDERS_CASH_SYNC_START]));

                        string incomeCashOrderExceptionMessage =
                            await SyncIncomeBankCashOrders(outcomeOrdersSyncRepository, remoteSyncConnection, from, to, currentUser, forAmg, false);

                        if (!string.IsNullOrEmpty(incomeCashOrderExceptionMessage))
                            exceptionList.Add(incomeCashOrderExceptionMessage);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.INCOME_ORDERS_CASH_SYNC_END]));
                        break;
                    case SyncProductConsignmentType.OutcomeBankOrder:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.OUTCOME_ORDERS_BANK_SYNC_START]));

                        string outcomeBankOrderExceptionsMessage =
                            await SyncOutcomeBankCashOrders(outcomeOrdersSyncRepository, remoteSyncConnection, from, to, currentUser, forAmg, true);

                        if (!string.IsNullOrEmpty(outcomeBankOrderExceptionsMessage))
                            exceptionList.Add(outcomeBankOrderExceptionsMessage);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.OUTCOME_ORDERS_BANK_SYNC_END]));
                        break;

                    case SyncProductConsignmentType.OutcomeCashOrder:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.OUTCOME_ORDERS_CASH_SYNC_START]));

                        string outcomeCashOrderExceptionsMessage =
                            await SyncOutcomeBankCashOrders(outcomeOrdersSyncRepository, remoteSyncConnection, from, to, currentUser, forAmg, false);

                        if (!string.IsNullOrEmpty(outcomeCashOrderExceptionsMessage))
                            exceptionList.Add(outcomeCashOrderExceptionsMessage);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.OUTCOME_ORDERS_CASH_SYNC_END]));
                        break;
                    case SyncProductConsignmentType.InternalMovementOfFunds:
                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.INTERNAL_MOVEMENT_OF_FUNDS_START]));

                        string InternalMovementOfFundsMessage =
                            await SyncInternalMovementOfFunds(outcomeOrdersSyncRepository, remoteSyncConnection, from, to, currentUser, forAmg, false);

                        if (!string.IsNullOrEmpty(InternalMovementOfFundsMessage))
                            exceptionList.Add(InternalMovementOfFundsMessage);

                        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.INTERNAL_MOVEMENT_OF_FUNDS_END]));
                        break;
                }

            if (exceptionList.Any())
                hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(exceptionList.Join(Environment.NewLine), true));

            if (!syncConsignments.Any()) {
                hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.CONSIGNMENTS_SYNC_END], true));
                return;
            }

            // long[] productAmgCodes = new long[] {
            //     399542
            //  };
            //
            // long[] productFenixCodes = new long[] {
            //     399542
            //  };

            // syncConsignments = syncConsignments
            //  .Where(x => x.DocumentArrivalNumber == "FS32023000000334").ToList();
            //.Where(x => forAmg ? productAmgCodes.Contains(x.ProductCode) : productFenixCodes.Contains(x.ProductCode)).ToList();

            //IEnumerable<Consignment> consignmentsTest =
            //    consignmentsSyncRepository.GetAllConsignmentsToDelete();

            //consignmentsTest = consignmentsTest
            //    // .Where(x => x.DocumentArrivalNumber == "FS32023000000090")
            //    .Where(x => x.ConsignmentItems.Any(y => y.Product.SourceAmgCode == 464751 || y.Product.SourceFenixCode == 464649));

            //if (forAmg) {
            //    consignmentsSyncRepository.CleanAllConsignmentsToDelete();

            //    foreach (Consignment consignmentToDelete in consignmentsTest) {
            //        foreach (ConsignmentItem consignmentItem in consignmentToDelete.ConsignmentItems) {
            //            if (!consignmentItem.Product.ProductAvailabilities.Any()) continue;

            //            ProductAvailability availability = consignmentItem.Product.ProductAvailabilities.First();

            //            availability = consignmentsSyncRepository.GetProductAvailabilityById(availability.Id);

            //            availability.Amount -= consignmentItem.RemainingQty;

            //            if (availability.Amount < 0) availability.Amount = 0;

            //            consignmentsSyncRepository.Update(availability);
            //        }
            //    }
            //}

            IEnumerable<Client> clients =
                consignmentsSyncRepository.GetAllClients();

            StringBuilder builder = new();

            builder.Append("(-1");

            foreach (long productCode in syncConsignments.Select(p => p.ProductCode).Distinct()) builder.Append($",{productCode}");

            builder.Append(")");

            IEnumerable<Product> products =
                consignmentsSyncRepository.GetAllProductsByProductCodes(builder.ToString(), forAmg);

            IEnumerable<IGrouping<Tuple<string, string>, SyncConsignment>> groupedSyncConsignments =
                syncConsignments.OrderBy(x => x.DocumentDate).GroupBy(x => new Tuple<string, string>(x.DocumentIdInString, x.StorageName));

            List<Tuple<long, DateTime?, bool, bool, DateTime, decimal>> invoiceWithDatesIds = new();

            Dictionary<long, Tuple<decimal, string>> documentValues = new();

            IEnumerable<SupplyInvoice> existInvoices = consignmentsSyncRepository.GetExistSupplyInvoices();

            Currency eur = currencyRepository.GetEURCurrencyIfExists();

            Currency usd = currencyRepository.GetUSDCurrencyIfExists();

            Currency pln = currencyRepository.GetPLNCurrencyIfExists();

            Currency uah = currencyRepository.GetUAHCurrencyIfExists();

            List<Storage> storages =
                consignmentsSyncRepository.GetAllStorages();

            List<Organization> organizations =
                consignmentsSyncRepository.GetAllOrganizations();

            List<Pricing> pricings =
                clientsSyncRepository.GetAllPricings();

            SupplyOrganization devSupplyOrganization = consignmentsSyncRepository.GetDevSupplyOrganization(_defaultComment);

            if (devSupplyOrganization == null) {
                devSupplyOrganization = new SupplyOrganization {
                    Created = DateTime.Now,
                    Updated = DateTime.Now,
                    Name = _defaultComment
                };

                devSupplyOrganization.Id = consignmentsSyncRepository.Add(devSupplyOrganization);

                SupplyOrganizationAgreement devSupplyOrganizationAgreement = new() {
                    Created = DateTime.Now,
                    CurrencyId = eur.Id,
                    Updated = DateTime.Now,
                    Name = _defaultComment,
                    SupplyOrganizationId = devSupplyOrganization.Id,
                    ExistFrom = DateTime.Now,
                    ExistTo = DateTime.Now,
                    Number = _defaultComment,
                    OrganizationId = organizations.FirstOrDefault(x => x.Name == DEFAULT_ORGANIZATION_AMG).Id
                };

                devSupplyOrganizationAgreement.Id = consignmentsSyncRepository.Add(devSupplyOrganizationAgreement);

                devSupplyOrganization.SupplyOrganizationAgreements.Add(devSupplyOrganizationAgreement);
            }

            ConsumableProduct consumableProduct = consignmentsSyncRepository.GetConsumablesProductByKey(_defaultComment);

            IEnumerable<Consignment> consignments =
                consignmentsSyncRepository.GetAllConsignmentsToDelete();

            IEnumerable<Client> deletedClients = outcomeOrdersSyncRepository.GetDeletedClients();

            long[] mainClientIds = deletedClients.Where(x => x.MainClientId.HasValue).Select(x => x.MainClientId.Value).ToArray();

            IEnumerable<Client> mainClients = outcomeOrdersSyncRepository.GetClientsByIds(mainClientIds);

            foreach (IGrouping<Tuple<string, string>, SyncConsignment> groupedSyncConsignment in groupedSyncConsignments) {
                string comment = "��� ������� � 1�.";

                IEnumerable<SyncConsignment> currentSyncConsignments = groupedSyncConsignment.Select(x => x).OrderBy(x => x.ProductCode);

                if (!currentSyncConsignments.Any()) continue;

                SyncConsignment firstSyncConsignment = currentSyncConsignments.First();

                IEnumerable<SyncConsignmentSpecification> specifications = forAmg
                    ? consignmentsSyncRepository.GetAmgAllSyncConsignmentSpecifications(firstSyncConsignment.DocumentId)
                    : consignmentsSyncRepository.GetFenixAllSyncConsignmentSpecifications(firstSyncConsignment.DocumentId);

                IEnumerable<SyncConsignmentSpend> consignmentSpends = forAmg
                    ? consignmentsSyncRepository.GetAmgConsignmentSpendsByDocumentId(firstSyncConsignment.DocumentId)
                    : consignmentsSyncRepository.GetFenixConsignmentSpendsByDocumentId(firstSyncConsignment.DocumentId);

                //DocumentDate from amg and DocumentArrivalDate from fenix
                SupplyInvoice existInvoice = existInvoices.FirstOrDefault(x => !string.IsNullOrEmpty(x.Number) &&
                                                                               x.DateCustomDeclaration.HasValue &&
                                                                               firstSyncConsignment.DocumentArrivalDate.HasValue &&
                                                                               x.SupplyOrder != null && x.SupplyOrder.DateFrom.HasValue &&
                                                                               x.Number.TrimEnd().TrimStart() ==
                                                                               firstSyncConsignment.DocumentArrivalNumber.TrimEnd().TrimStart() &&
                                                                               x.SupplyOrder.DateFrom.Value.Date ==
                                                                               firstSyncConsignment.DocumentArrivalDate.Value.AddYears(-2000).Date);

                bool fromInvoice = false;

                if (existInvoice != null && existInvoice.PackingLists.Any())
                    if (existInvoice.PackingLists.First().PackingListPackageOrderItems.Any()) {
                        PackingListPackageOrderItem item = existInvoice.PackingLists.First().PackingListPackageOrderItems.First();

                        if (item != null && item.ProductIncomeItem != null && item.ProductIncomeItem.ProductIncome != null &&
                            item.ProductIncomeItem.ProductIncome.Storage != null)
                            fromInvoice = firstSyncConsignment.StorageName != item.ProductIncomeItem.ProductIncome.Storage.Name;
                    }

                // if (existInvoice == null || fromInvoice) {
                //     IEnumerable<IGrouping<long, SyncConsignment>> groupedProducts =
                //         currentSyncConsignments
                //             .GroupBy(x => x.ProductCode)
                //             .Where(x => x.Select(y => y).Count() > 1);
                //
                //     if (groupedProducts.Any()) { 
                //         foreach (IGrouping<long, SyncConsignment> groupedProduct in groupedProducts) {
                //             IEnumerable<SyncConsignment> existProductConsignments = groupedProduct.Select(x => x);
                //
                //             SyncConsignment lastExistConsignments = existProductConsignments.Last();
                //
                //             double totalQty = lastExistConsignments.Qty;
                //
                //             foreach (SyncConsignment existProductConsignment in existProductConsignments.Where(x => x.IncomeQty.HasValue)) {
                //                 if (totalQty == 0) {
                //                     existProductConsignment.Qty = 0;
                //
                //                     continue;
                //                 }
                //
                //                 if (totalQty >= existProductConsignment.IncomeQty.Value) {
                //                     existProductConsignment.Qty = existProductConsignment.IncomeQty.Value;
                //
                //                     totalQty -= existProductConsignment.Qty;
                //                 } else {
                //                     existProductConsignment.Qty = totalQty;
                //
                //                     totalQty = 0;
                //                 }
                //             }
                //         }
                //     }
                // }

                Currency currency;

                if (eurCodes.Contains(firstSyncConsignment.CurrencyCode))
                    currency = eur;
                else if (usdCodes.Contains(firstSyncConsignment.CurrencyCode))
                    currency = usd;
                else if (plnCodes.Contains(firstSyncConsignment.CurrencyCode))
                    currency = pln;
                else
                    currency = uah;

                if (currency == null) continue;

                if (existInvoice == null || fromInvoice) {
                    Storage storage = storages.FirstOrDefault(s => s.Name == firstSyncConsignment.StorageName);

                    if (storage == null) continue;

                    Organization organization = organizations.FirstOrDefault(o => o.Id == storage.OrganizationId);

                    if (organization == null) continue;

                    Client client = clients.FirstOrDefault(c =>
                        (firstSyncConsignment.TypeDocument == SyncConsignmentType.ClientReturn ||
                         c.ClientInRole.ClientType.Type == ClientTypeType.Provider) &&
                        (forAmg ? c.SourceAmgCode == firstSyncConsignment.ClientCode : c.SourceFenixCode == firstSyncConsignment.ClientCode));

                    if (client == null || client.Deleted) {
                        client = deletedClients.FirstOrDefault(x =>
                            forAmg ? x.SourceAmgCode == firstSyncConsignment.ClientCode : x.SourceFenixCode == firstSyncConsignment.ClientCode);

                        if (client != null && client.MainClientId.HasValue)
                            client = mainClients.FirstOrDefault(x => x.Id == client.MainClientId.Value);

                        if (client == null) continue;
                    }

                    ClientAgreement clientAgreement = client.ClientAgreements.FirstOrDefault(x =>
                        forAmg ? x.Agreement.SourceAmgCode == firstSyncConsignment.AgreementCode : x.Agreement.SourceFenixCode == firstSyncConsignment.AgreementCode);

                    if (clientAgreement == null) {
                        clientAgreement = client.ClientAgreements.FirstOrDefault(x => x.Agreement.IsDefaultForSyncConsignment &&
                                                                                      x.Agreement.Currency.Code == currency.Code);

                        if (clientAgreement == null && !forAmg) {
                            bool isManagement;
                            bool isAccounting;
                            bool withVat;

                            if (organization.IsVatAgreements) {
                                isManagement = false;
                                isAccounting = true;
                                withVat = true;
                            } else {
                                isManagement = true;
                                isAccounting = false;
                                withVat = false;
                            }

                            ClientAgreement newAgreement = new() {
                                Agreement = new Agreement {
                                    Name = DEFAULT_AGREEMENT_NAME,
                                    CurrencyId = currency.Id,
                                    OrganizationId = organization.Id,
                                    ProviderPricing = new ProviderPricing {
                                        Name = SUPPLY_PRICING_NAME,
                                        BasePricingId = pricings.First(p => p.Name.Equals(SUPPLY_PRICING_NAME)).Id
                                    },
                                    IsAccounting = isAccounting,
                                    IsActive = true,
                                    IsControlAmountDebt = true,
                                    IsControlNumberDaysDebt = true,
                                    IsManagementAccounting = isManagement,
                                    WithVATAccounting = withVat,
                                    DeferredPayment = string.Empty,
                                    TermsOfPayment = string.Empty,
                                    IsPrePaymentFull = true,
                                    IsPrePayment = false,
                                    PrePaymentPercentages = 0,
                                    WithAgreementLine = true,
                                    IsDefaultForSyncConsignment = true
                                },
                                ProductReservationTerm = 3,
                                ClientId = client.Id
                            };

                            newAgreement.Agreement.ProviderPricingId = clientsSyncRepository.Add(newAgreement.Agreement.ProviderPricing);

                            newAgreement.AgreementId = clientsSyncRepository.Add(newAgreement.Agreement);

                            newAgreement.Id = clientsSyncRepository.Add(newAgreement);

                            newAgreement.Agreement.Currency = currency;

                            clientAgreement = newAgreement;

                            client.ClientAgreements.Add(newAgreement);
                        }

                        if (clientAgreement != null && storageThreeNames.Contains(storage.Name) && !forAmg)
                            continue;
                    }

                    if (clientAgreement == null) continue;

                    Consignment consignment = new() {
                        OrganizationId = organization.Id,
                        StorageId = storage.Id,
                        FromDate = ConvertDateTimeToUtcInUkraineTimeZone(firstSyncConsignment.DocumentDate.AddYears(-2000)),
                        ProductIncome = new ProductIncome {
                            Comment = firstSyncConsignment.Comment,
                            Number = firstSyncConsignment.DocumentNumber,
                            FromDate = ConvertDateTimeToUtcInUkraineTimeZone(firstSyncConsignment.DocumentDate.AddYears(-2000)),
                            UserId = currentUser.Id,
                            StorageId = storage.Id,
                            IsHide = false,
                            IsFromOneC = true
                        },
                        IsImportedFromOneC = true
                    };

                    consignment.ProductIncomeId = consignmentsSyncRepository.Add(consignment.ProductIncome);

                    consignment.Id = consignmentsSyncRepository.Add(consignment);

                    foreach (SyncConsignment syncConsignment in currentSyncConsignments.OrderBy(x => x.DocumentDate)) {
                        if (!products.Any(p => p.VendorCode.Equals(syncConsignment.VendorCode))) continue;

                        if (!forAmg && syncConsignment.DocumentArrivalDate.HasValue &&
                            syncConsignment.DocumentArrivalDate.Value.Year > 2001 &&
                            syncConsignment.DocumentArrivalDate.Value.Year < 4000)
                            syncConsignment.DocumentArrivalDate = new DateTime(syncConsignment.DocumentDate.Year,
                                syncConsignment.DocumentArrivalDate.Value.Month,
                                syncConsignment.DocumentArrivalDate.Value.Day);

                        Product product = products.First(p => p.VendorCode.Equals(syncConsignment.VendorCode));

                        SyncConsignmentSpecification specification = specifications
                            .FirstOrDefault(s => s.ProductVendorCode == product.VendorCode);

                        double incomeQty = syncConsignment.IncomeQty.HasValue ? syncConsignment.IncomeQty.Value : syncConsignment.Qty;

                        ConsignmentItem consignmentItem;

                        bool isIncomeToManagementStorage = !storageThreeNames.Contains(syncConsignment.IncomeStorageName);

                        bool isTransferFromStorageThree = !isIncomeToManagementStorage &&
                                                          syncConsignment.StorageName != syncConsignment.IncomeStorageName;

                        if (syncConsignment.TypeDocument == SyncConsignmentType.Capitalization || syncConsignment.TypeDocument == SyncConsignmentType.ClientReturn) {
                            if (client != null)
                                comment += $" ���������� �� \"{client.FullName}\"";
                            else
                                comment += " �������������";

                            string specCode = syncConsignment.UKTVEDCode;
                            string specName = syncConsignment.UKTVEDName;

                            double qtyProducts = syncConsignment.Qty > 0 ? syncConsignment.Qty : syncConsignment.IncomeQty ?? 1;

                            decimal netPricePerItem = syncConsignment.Value / Convert.ToDecimal(qtyProducts);

                            decimal dutyPercent = 0;
                            decimal customValue = 0;
                            decimal duty = 0;
                            decimal vatPercent = 0;
                            decimal specificationVat = 0;

                            decimal exchangeRateAmount = 1;

                            if (isTransferFromStorageThree) {
                                GovExchangeRate govExchangeRate =
                                    consignmentsSyncRepository
                                        .GetGovByCurrencyIdAndCode(clientAgreement.Agreement.CurrencyId.Value, eur.Code,
                                            syncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000));

                                exchangeRateAmount = govExchangeRate?.Amount ?? 1;
                            } else {
                                ExchangeRate exchangeRate =
                                    consignmentsSyncRepository
                                        .GetByCurrencyIdAndCode(
                                            clientAgreement.Agreement.CurrencyId.Value,
                                            eur.Code,
                                            syncConsignment.DocumentDate.AddYears(-2000));

                                exchangeRateAmount = exchangeRate?.Amount ?? 1;
                            }

                            consignmentItem =
                                new ConsignmentItem {
                                    ProductId = product.Id,
                                    ProductIncomeItem = new ProductIncomeItem {
                                        ProductIncomeId = consignment.ProductIncomeId,
                                        Qty = incomeQty,
                                        RemainingQty = syncConsignment.Qty,
                                        ProductCapitalizationItem = new ProductCapitalizationItem {
                                            ProductCapitalization = new ProductCapitalization {
                                                Comment = syncConsignment.Comment,
                                                OrganizationId = organization.Id,
                                                FromDate = ConvertDateTimeToUtcInUkraineTimeZone(specification?.CustomsDeclarationDate?.AddYears(-2000) ??
                                                                                                 syncConsignment.DocumentDate.AddYears(-2000)),
                                                Number =
                                                    syncConsignment.DocumentArrivalDate != null
                                                        ? syncConsignment.DocumentArrivalNumber
                                                        : syncConsignment.DocumentNumber,
                                                ResponsibleId = currentUser.Id,
                                                StorageId = storage.Id
                                            },
                                            ProductId = product.Id,
                                            Qty = syncConsignment.Qty,
                                            RemainingQty = syncConsignment.Qty,
                                            UnitPrice = netPricePerItem,
                                            Weight = syncConsignment.WeightBruttoPer
                                        }
                                    },
                                    NetPrice = netPricePerItem,
                                    Price = netPricePerItem,
                                    Weight = syncConsignment.WeightBruttoPer,
                                    AccountingPrice = netPricePerItem,
                                    RemainingQty = syncConsignment.Qty,
                                    Qty = incomeQty,
                                    DutyPercent = dutyPercent,
                                    ProductSpecification = new ProductSpecification {
                                        ProductId = product.Id,
                                        AddedById = currentUser.Id,
                                        Locale = organization.Culture,
                                        SpecificationCode = specCode,
                                        Name = specName,
                                        DutyPercent = dutyPercent,
                                        CustomsValue = customValue,
                                        Duty = duty,
                                        VATPercent = vatPercent,
                                        VATValue = specificationVat,
                                        OrderProductSpecification = new OrderProductSpecification {
                                            Created = DateTime.Now,
                                            Updated = DateTime.Now,
                                            Qty = incomeQty,
                                            UnitPrice = netPricePerItem
                                        }
                                    },
                                    ConsignmentId = consignment.Id,
                                    ExchangeRate = exchangeRateAmount
                                };

                            consignmentItem.ProductIncomeItem.ProductCapitalizationItem.ProductCapitalizationId =
                                consignment.ConsignmentItems.FirstOrDefault()?.ProductIncomeItem?.ProductCapitalizationItem?.ProductCapitalizationId
                                ??
                                consignmentsSyncRepository.Add(
                                    consignmentItem.ProductIncomeItem.ProductCapitalizationItem.ProductCapitalization
                                );

                            consignmentItem.ProductIncomeItem.ProductCapitalizationItemId =
                                consignmentsSyncRepository.Add(
                                    consignmentItem.ProductIncomeItem.ProductCapitalizationItem
                                );

                            consignmentItem.ProductIncomeItemId =
                                consignmentsSyncRepository.Add(consignmentItem.ProductIncomeItem);

                            consignmentItem.ProductSpecificationId =
                                consignmentsSyncRepository.Add(consignmentItem.ProductSpecification);

                            consignmentItem.Id = consignmentsSyncRepository.Add(consignmentItem);

                            consignmentsSyncRepository.Add(new ConsignmentItemMovement {
                                Qty = syncConsignment.Qty,
                                IsIncomeMovement = true,
                                ProductIncomeItemId = consignmentItem.ProductIncomeItemId,
                                ConsignmentItemId = consignmentItem.Id,
                                MovementType = ConsignmentItemMovementType.Capitalization
                            });

                            consignment.ConsignmentItems.Add(consignmentItem);

                            consignmentsSyncRepository.Add(new ProductPlacement {
                                Qty = syncConsignment.Qty,
                                ProductId = product.Id,
                                StorageId = storage.Id,
                                ConsignmentItemId = consignmentItem.Id,
                                RowNumber = "N",
                                CellNumber = "N",
                                StorageNumber = "N"
                            });
                        } else {
                            DateTime? dateCustomDeclaration = specification?.CustomsDeclarationDate != null
                                ? specification.CustomsDeclarationDate?.AddYears(-2000)
                                : syncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000);

                            DateTime dateForExchangeRate =
                                forAmg
                                    ? syncConsignment.DocumentDate.AddYears(-2000)
                                    : syncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000);

                            GovExchangeRate govExchangeRate =
                                consignmentsSyncRepository
                                    .GetGovByCurrencyIdAndCode(clientAgreement.Agreement.CurrencyId.Value, eur.Code, dateForExchangeRate);

                            GovExchangeRate govExchangeRateUahToEur =
                                consignmentsSyncRepository
                                    .GetGovByCurrencyIdAndCode(uah.Id, eur.Code, dateForExchangeRate);

                            GovExchangeRate govExchangeRateUahToUsd =
                                consignmentsSyncRepository
                                    .GetGovByCurrencyIdAndCode(uah.Id, usd.Code, dateForExchangeRate);

                            ExchangeRate exchangeRate =
                                consignmentsSyncRepository
                                    .GetByCurrencyIdAndCode(clientAgreement.Agreement.CurrencyId.Value, eur.Code, dateForExchangeRate);

                            ExchangeRate exchangeRateUahToEur =
                                consignmentsSyncRepository
                                    .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateForExchangeRate);

                            ExchangeRate exchangeRateUahToUsd =
                                consignmentsSyncRepository
                                    .GetByCurrencyIdAndCode(uah.Id, usd.Code, dateForExchangeRate);

                            decimal exchangeRateAmount = 1;

                            if (isTransferFromStorageThree) {
                                if (clientAgreement.Agreement.Currency.Code == usd.Code)
                                    exchangeRateAmount = govExchangeRateUahToEur.Amount / govExchangeRateUahToUsd.Amount;
                                else if (clientAgreement.Agreement.Currency.Code != eur.Code) exchangeRateAmount = govExchangeRate.Amount;
                            } else if (isIncomeToManagementStorage) {
                                if (clientAgreement.Agreement.Currency.Code == usd.Code)
                                    exchangeRateAmount = exchangeRateUahToEur.Amount / exchangeRateUahToUsd.Amount;
                                else if (clientAgreement.Agreement.Currency.Code != eur.Code) exchangeRateAmount = exchangeRate.Amount;
                            } else {
                                if (clientAgreement.Agreement.Currency.Code == usd.Code)
                                    exchangeRateAmount = govExchangeRateUahToEur.Amount / govExchangeRateUahToUsd.Amount;
                                else if (clientAgreement.Agreement.Currency.Code != eur.Code) exchangeRateAmount = govExchangeRate.Amount;
                            }

                            decimal totalValue = syncConsignment.TotalValue;

                            bool supplierFromUkraine = !client.IsNotResident;

                            decimal customValue = syncConsignment.CustomsValue;

                            decimal duty = specification?.Duty ?? 0;

                            decimal dutyPercent = syncConsignment.CustomsRate;

                            decimal totalPriceWithoutVat = totalValue;

                            decimal vatFromIncome = 0;

                            decimal vatAmount = 0;

                            decimal vatFromSpecification = specification?.Vat ?? 0;

                            decimal dutyFromSpecification = specification?.Duty ?? 0;

                            if (specification != null) {
                                IEnumerable<SyncConsignment> syncConsignmentsByProducts = groupedSyncConsignment.Where(x => x.ProductCode == syncConsignment.ProductCode);

                                if (syncConsignmentsByProducts.Any() && syncConsignmentsByProducts.Count() > 1) {
                                    // TODO Remove comments if it's correct
                                    // double qtyByProducts = syncConsignmentsByProducts.Sum(x => x.IncomeQty ?? x.Qty);
                                    //
                                    // decimal vatByOne = vatFromSpecification / Convert.ToDecimal(qtyByProducts);
                                    // decimal dutyByOne = duty / Convert.ToDecimal(qtyByProducts);
                                    //
                                    // vatFromSpecification = vatByOne * Convert.ToDecimal(syncConsignment.IncomeQty ?? syncConsignment.Qty);
                                    // dutyFromSpecification = dutyByOne * Convert.ToDecimal(syncConsignment.IncomeQty ?? syncConsignment.Qty);

                                    decimal totalAmountByProducts = syncConsignmentsByProducts.Sum(e => e.TotalValue);

                                    decimal vatByOne = decimal.Round(specification.Vat / totalAmountByProducts, 14, MidpointRounding.AwayFromZero);
                                    decimal dutyByOne = decimal.Round(duty / totalAmountByProducts, 14, MidpointRounding.AwayFromZero);

                                    vatFromSpecification = vatByOne * totalValue;
                                    dutyFromSpecification = dutyByOne * totalValue;
                                }
                            }

                            if (forAmg) {
                                if (supplierFromUkraine) {
                                    switch (firstSyncConsignment.VatTypeAmg) {
                                        case SyncVatEnumAmg.FourTeen:
                                            vatFromIncome = 14;
                                            break;
                                        case SyncVatEnumAmg.Twenty:
                                            vatFromIncome = 20;
                                            break;
                                        case SyncVatEnumAmg.Seven:
                                            vatFromIncome = 7;
                                            break;
                                        case SyncVatEnumAmg.TwentyThree:
                                            vatFromIncome = 23;
                                            break;
                                    }

                                    vatAmount = decimal.Round(totalValue * (vatFromIncome / (100 + vatFromIncome)), 2, MidpointRounding.AwayFromZero);

                                    totalPriceWithoutVat -= vatAmount;
                                } else {
                                    vatAmount = decimal.Round(specification != null ? vatFromSpecification : syncConsignment.TotalVat, 2, MidpointRounding.AwayFromZero);

                                    decimal customAndDuty = syncConsignment.CustomsValue + dutyFromSpecification;

                                    if (customAndDuty > 0)
                                        vatFromIncome = decimal.Round(vatAmount * 100 / customAndDuty, 2, MidpointRounding.AwayFromZero);
                                }
                            } else {
                                if (supplierFromUkraine) {
                                    vatFromIncome = 0;

                                    switch (firstSyncConsignment.VatTypeFenix) {
                                        case SyncVatEnumFenix.Twenty:
                                            vatFromIncome = 20;
                                            break;
                                        case SyncVatEnumFenix.Seven:
                                            vatFromIncome = 7;
                                            break;
                                        case SyncVatEnumFenix.TwentyThree:
                                            vatFromIncome = 23;
                                            break;
                                    }

                                    if (syncConsignment.Vat != 0) {
                                        vatAmount = decimal.Round(totalValue * (vatFromIncome / (100 + vatFromIncome)), 2, MidpointRounding.AwayFromZero);

                                        totalPriceWithoutVat -= vatAmount;
                                    }
                                } else {
                                    vatAmount = decimal.Round(specification != null ? vatFromSpecification : syncConsignment.TotalVat, 2, MidpointRounding.AwayFromZero);

                                    decimal customAndDuty = syncConsignment.CustomsValue + dutyFromSpecification;

                                    if (customAndDuty > 0)
                                        vatFromIncome = decimal.Round(vatAmount * 100 / customAndDuty, 2, MidpointRounding.AwayFromZero);
                                }
                            }

                            decimal netPricePerItem = decimal.Round(totalPriceWithoutVat / Convert.ToDecimal(incomeQty), 6, MidpointRounding.AwayFromZero);

                            decimal netPricePerItemEur =
                                decimal.Round(syncConsignment.NetValue / exchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                            if (client != null)
                                comment += $" ���������� �� \"{client.FullName}\"";
                            else
                                continue;

                            string specCode = syncConsignment.UKTVEDCode;
                            string specName = syncConsignment.UKTVEDName;

                            if (consignment.ConsignmentItems.Any()) {
                                ConsignmentItem existingItem = consignment.ConsignmentItems.First();

                                consignmentItem =
                                    new ConsignmentItem {
                                        ProductId = product.Id,
                                        ProductIncomeItem = new ProductIncomeItem {
                                            ProductIncomeId = consignment.ProductIncomeId,
                                            Qty = incomeQty,
                                            RemainingQty = syncConsignment.Qty,
                                            PackingListPackageOrderItem = new PackingListPackageOrderItem {
                                                PackingListId = existingItem.ProductIncomeItem.PackingListPackageOrderItem.PackingListId,
                                                Qty = incomeQty,
                                                PlacedQty = syncConsignment.Qty,
                                                RemainingQty = syncConsignment.Qty,
                                                UploadedQty = syncConsignment.Qty,
                                                NetWeight = syncConsignment.WeightPer,
                                                GrossWeight = syncConsignment.WeightBruttoPer,
                                                VatAmount = vatFromIncome == 0 ? 0 : vatAmount,
                                                VatPercent = vatFromIncome,
                                                UnitPrice = netPricePerItem,
                                                UnitPriceEur = netPricePerItemEur,
                                                GrossUnitPriceEur = netPricePerItemEur,
                                                ExchangeRateAmount = exchangeRateAmount,
                                                ExchangeRateAmountUahToEur = govExchangeRateUahToEur.Amount,
                                                SupplyInvoiceOrderItem = new SupplyInvoiceOrderItem {
                                                    Qty = syncConsignment.Qty,
                                                    Weight = syncConsignment.WeightBruttoPer,
                                                    SupplyInvoiceId = existingItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoiceId,
                                                    UnitPrice = netPricePerItem,
                                                    SupplyOrderItem = new SupplyOrderItem {
                                                        ProductId = product.Id,
                                                        Qty = syncConsignment.Qty,
                                                        GrossWeight = syncConsignment.WeightBruttoPer,
                                                        NetWeight = syncConsignment.WeightPer,
                                                        SupplyOrderId = existingItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrderId,
                                                        UnitPrice = netPricePerItem,
                                                        TotalAmount = decimal.Round(netPricePerItem * Convert.ToDecimal(incomeQty), 2, MidpointRounding.AwayFromZero)
                                                    },
                                                    ProductId = product.Id
                                                },
                                                ProductIsImported = syncConsignment?.IsImported ?? false,
                                                UnitPriceEurWithVat = netPricePerItemEur
                                            }
                                        },
                                        NetPrice = netPricePerItem,
                                        Price = netPricePerItemEur,
                                        AccountingPrice = netPricePerItemEur,
                                        Weight = syncConsignment.WeightBruttoPer,
                                        RemainingQty = syncConsignment.Qty,
                                        Qty = incomeQty,
                                        DutyPercent = dutyPercent,
                                        ProductSpecification = new ProductSpecification {
                                            ProductId = product.Id,
                                            AddedById = currentUser.Id,
                                            Locale = organization.Culture,
                                            SpecificationCode = specCode,
                                            Name = specName,
                                            DutyPercent = dutyPercent,
                                            CustomsValue = customValue,
                                            Duty = dutyFromSpecification,
                                            VATPercent = vatFromIncome,
                                            VATValue = vatFromIncome == 0 ? 0 : vatAmount,
                                            OrderProductSpecification = new OrderProductSpecification {
                                                Created = DateTime.Now,
                                                Updated = DateTime.Now,
                                                Qty = incomeQty,
                                                UnitPrice = netPricePerItem
                                            }
                                        },
                                        ConsignmentId = consignment.Id,
                                        ExchangeRate = syncConsignment.RateExchange
                                    };
                            } else {
                                consignmentItem =
                                    new ConsignmentItem {
                                        ProductId = product.Id,
                                        ProductIncomeItem = new ProductIncomeItem {
                                            ProductIncomeId = consignment.ProductIncomeId,
                                            Qty = incomeQty,
                                            RemainingQty = syncConsignment.Qty,
                                            PackingListPackageOrderItem = new PackingListPackageOrderItem {
                                                PackingList = new PackingList {
                                                    SupplyInvoice = new SupplyInvoice {
                                                        SupplyOrder = new SupplyOrder {
                                                            Comment = syncConsignment.Comment,
                                                            ClientId = client.Id,
                                                            ClientAgreementId = clientAgreement.Id,
                                                            OrganizationId = organization.Id,
                                                            SupplyOrderNumber = new SupplyOrderNumber {
                                                                Number = "00000000000"
                                                            },
                                                            IsPlaced = true,
                                                            IsOrderArrived = true,
                                                            IsFullyPlaced = true,
                                                            IsCompleted = true,
                                                            IsOrderShipped = true,
                                                            IsDocumentSet = true,
                                                            IsPartiallyPlaced = true,
                                                            IsGrossPricesCalculated = true,
                                                            Deleted = true,
                                                            DateFrom = ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000)),
                                                            OrderShippedDate = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                                               ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000)),
                                                            CompleteDate = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                                           ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000)),
                                                            ShipArrived = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                                          ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000)),
                                                            VechicalArrived = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                                              ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000)),
                                                            PlaneArrived = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                                           ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000)),
                                                            OrderArrivedDate = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                                               ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000)),
                                                            TransportationType = SupplyTransportationType.Vehicle,
                                                            SupplyProForm = new SupplyProForm {
                                                                Number = "0",
                                                                NetPrice = 0m,
                                                                ServiceNumber = "0"
                                                            },
                                                            AdditionalPaymentFromDate = null
                                                        },
                                                        DateFrom = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                                   ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000)),
                                                        Number = syncConsignment.DocumentArrivalNumber,
                                                        ServiceNumber = "0",
                                                        Comment = syncConsignment.Comment,
                                                        IsShipped = true,
                                                        IsFullyPlaced = true,
                                                        IsPartiallyPlaced = true,
                                                        DateCustomDeclaration =
                                                            forAmg
                                                                ? ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000))
                                                                : syncConsignment.DocumentArrivalDate?.AddYears(-2000),
                                                        NumberCustomDeclaration =
                                                            specification != null ? specification.NumberDeclarationDate : syncConsignment.DocumentArrivalNumber,
                                                        NetPrice = syncConsignment.DocumentValue
                                                    },
                                                    MarkNumber = "0",
                                                    InvNo = syncConsignment.DocumentArrivalNumber,
                                                    PlNo = syncConsignment.DocumentArrivalNumber,
                                                    Comment = syncConsignment.Comment,
                                                    FromDate = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                               ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000)),
                                                    IsPlaced = true
                                                },
                                                Qty = incomeQty,
                                                PlacedQty = incomeQty,
                                                RemainingQty = syncConsignment.Qty,
                                                UploadedQty = incomeQty,
                                                NetWeight = syncConsignment.WeightPer,
                                                GrossWeight = syncConsignment.WeightBruttoPer,
                                                VatAmount = vatFromIncome == 0 ? 0 : vatAmount,
                                                VatPercent = vatFromIncome,
                                                UnitPrice = netPricePerItem,
                                                UnitPriceEur = netPricePerItemEur,
                                                GrossUnitPriceEur = netPricePerItemEur,
                                                ExchangeRateAmount = exchangeRateAmount,
                                                IsPlaced = true,
                                                ExchangeRateAmountUahToEur = govExchangeRateUahToEur.Amount,
                                                SupplyInvoiceOrderItem = new SupplyInvoiceOrderItem {
                                                    Qty = incomeQty,
                                                    Weight = syncConsignment.WeightBruttoPer,
                                                    UnitPrice = netPricePerItem,
                                                    SupplyOrderItem = new SupplyOrderItem {
                                                        ProductId = product.Id,
                                                        Qty = incomeQty,
                                                        GrossWeight = syncConsignment.WeightBruttoPer,
                                                        NetWeight = syncConsignment.WeightPer,
                                                        UnitPrice = netPricePerItem,
                                                        TotalAmount = decimal.Round(netPricePerItem * Convert.ToDecimal(incomeQty), 2, MidpointRounding.AwayFromZero)
                                                    },
                                                    ProductId = product.Id
                                                },
                                                ProductIsImported = syncConsignment?.IsImported ?? false,
                                                UnitPriceEurWithVat = netPricePerItemEur
                                            }
                                        },
                                        Price = netPricePerItemEur,
                                        NetPrice = netPricePerItem,
                                        AccountingPrice = netPricePerItemEur,
                                        Weight = syncConsignment.WeightBruttoPer,
                                        RemainingQty = syncConsignment.Qty,
                                        Qty = incomeQty,
                                        DutyPercent = dutyPercent,
                                        ProductSpecification = new ProductSpecification {
                                            ProductId = product.Id,
                                            AddedById = currentUser.Id,
                                            Locale = organization.Culture,
                                            SpecificationCode = specCode,
                                            Name = specName,
                                            DutyPercent = dutyPercent,
                                            Duty = dutyFromSpecification,
                                            VATPercent = vatFromIncome,
                                            VATValue = vatFromIncome == 0 ? 0 : vatAmount,
                                            CustomsValue = customValue,
                                            OrderProductSpecification = new OrderProductSpecification {
                                                Created = DateTime.Now,
                                                Updated = DateTime.Now,
                                                Qty = incomeQty,
                                                UnitPrice = netPricePerItem
                                            }
                                        },
                                        ConsignmentId = consignment.Id,
                                        ExchangeRate = syncConsignment.RateExchange
                                    };

                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrder.SupplyOrderNumberId =
                                    consignmentsSyncRepository.Add(
                                        consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrder.SupplyOrderNumber);

                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrder.SupplyProFormId =
                                    consignmentsSyncRepository.Add(
                                        consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrder.SupplyProForm);

                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem.SupplyOrderId =
                                    consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrderId =
                                        consignmentsSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrder);

                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyInvoiceId =
                                    consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoiceId =
                                        consignmentsSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice);

                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingListId =
                                    consignmentsSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList);

                                invoiceWithDatesIds.Add(new Tuple<long, DateTime?, bool, bool, DateTime, decimal>(
                                    consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyInvoiceId,
                                    dateCustomDeclaration,
                                    isTransferFromStorageThree,
                                    isIncomeToManagementStorage,
                                    dateForExchangeRate,
                                    vatFromIncome));

                                documentValues.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyInvoiceId,
                                    new Tuple<decimal, string>(syncConsignment.DocumentValue, storage.Name));

                                if (consignmentSpends.Any()) {
                                    decimal totalValueSpend = 0;

                                    decimal documentValueExchangeRateAmount = 1;

                                    if (isTransferFromStorageThree)
                                        documentValueExchangeRateAmount = GetGovExchangeRateOnDateToEur(
                                            currency,
                                            dateForExchangeRate,
                                            govCrossExchangeRateRepository,
                                            govExchangeRateRepository,
                                            currencyRepository);
                                    else if (isIncomeToManagementStorage)
                                        documentValueExchangeRateAmount = GetExchangeRateOnDateToEur(
                                            currency,
                                            dateForExchangeRate,
                                            crossExchangeRateRepository,
                                            exchangeRateRepository,
                                            currencyRepository);
                                    else
                                        documentValueExchangeRateAmount = GetGovExchangeRateOnDateToEur(
                                            currency,
                                            dateCustomDeclaration ?? DateTime.Now,
                                            govCrossExchangeRateRepository,
                                            govExchangeRateRepository,
                                            currencyRepository);

                                    foreach (SyncConsignmentSpend consignmentSpend in consignmentSpends) {
                                        decimal valueSpend = consignmentSpend.Amount;
                                        Currency supplyCurrency;

                                        if (usdCodes.Contains(consignmentSpend.CurrencyCode))
                                            supplyCurrency = usd;
                                        else if (eurCodes.Contains(consignmentSpend.CurrencyCode))
                                            supplyCurrency = eur;
                                        else if (plnCodes.Contains(consignmentSpend.CurrencyCode))
                                            supplyCurrency = pln;
                                        else
                                            supplyCurrency = uah;

                                        decimal serviceExchangeRateAmount = 1;

                                        if (isTransferFromStorageThree)
                                            serviceExchangeRateAmount = GetGovExchangeRateOnDateToEur(
                                                supplyCurrency,
                                                dateForExchangeRate,
                                                govCrossExchangeRateRepository,
                                                govExchangeRateRepository,
                                                currencyRepository);
                                        else if (isIncomeToManagementStorage)
                                            serviceExchangeRateAmount = GetExchangeRateOnDateToEur(
                                                supplyCurrency,
                                                dateForExchangeRate,
                                                crossExchangeRateRepository,
                                                exchangeRateRepository,
                                                currencyRepository);
                                        else
                                            serviceExchangeRateAmount = GetGovExchangeRateOnDateToEur(
                                                supplyCurrency,
                                                dateCustomDeclaration ?? DateTime.Now,
                                                govCrossExchangeRateRepository,
                                                govExchangeRateRepository,
                                                currencyRepository);

                                        //Get spend for current document
                                        decimal documentValueInEur = firstSyncConsignment.DocumentValue / exchangeRateAmount;
                                        decimal totalInvoicesInEur = consignmentSpend.TotalSpend / serviceExchangeRateAmount;

                                        decimal valueSpendInEur = valueSpend / serviceExchangeRateAmount;

                                        decimal spendOnCurrentInvoice = documentValueInEur * valueSpendInEur / totalInvoicesInEur;

                                        totalValueSpend += decimal.Round(spendOnCurrentInvoice, 14, MidpointRounding.AwayFromZero);
                                    }

                                    DeliveryProductProtocolNumber protocolNumber = new() {
                                        Created = DateTime.Now,
                                        Updated = DateTime.Now,
                                        Deleted = false,
                                        Number = _defaultComment
                                    };

                                    protocolNumber.Id = consignmentsSyncRepository.Add(protocolNumber);

                                    DeliveryProductProtocol protocol = new() {
                                        Created = DateTime.Now,
                                        Updated = DateTime.Now,
                                        Deleted = false,
                                        TransportationType = SupplyTransportationType.Vehicle,
                                        UserId = currentUser.Id,
                                        Comment = syncConsignment.Comment,
                                        FromDate = ConvertDateTimeToUtcInUkraineTimeZone(syncConsignment.DocumentDate.AddYears(-2000)),
                                        IsCompleted = true,
                                        IsPartiallyPlaced = true,
                                        IsPlaced = true,
                                        OrganizationId = clientAgreement.Agreement.OrganizationId.Value,
                                        DeliveryProductProtocolNumberId = protocolNumber.Id,
                                        IsShipped = true
                                    };

                                    protocol.Id = consignmentsSyncRepository.Add(protocol);

                                    consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.Id =
                                        consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyInvoiceId;

                                    consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.DeliveryProductProtocolId = protocol.Id;

                                    consignmentsSyncRepository.UpdateInvoiceInProtocol(
                                        consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice);

                                    long serviceId = consignmentsSyncRepository.Add(new MergedService {
                                        Created = DateTime.Now,
                                        Updated = DateTime.Now,
                                        Deleted = false,
                                        IsActive = true,
                                        FromDate = DateTime.Now,
                                        GrossPrice = !forAmg ? totalValueSpend : 0,
                                        Vat = 0,
                                        VatPercent = 0,
                                        Number = _defaultComment,
                                        ServiceNumber = _defaultComment,
                                        Name = _defaultComment,
                                        UserId = currentUser.Id,
                                        SupplyOrganizationAgreementId = devSupplyOrganization.SupplyOrganizationAgreements.First().Id,
                                        SupplyOrganizationId = devSupplyOrganization.Id,
                                        AccountingGrossPrice = forAmg ? totalValueSpend : 0,
                                        AccountingNetPrice = 0,
                                        AccountingVat = 0,
                                        AccountingVatPercent = 0,
                                        DeliveryProductProtocolId = protocol.Id,
                                        IsCalculatedValue = true,
                                        IsAutoCalculatedValue = true,
                                        SupplyExtraChargeType = SupplyExtraChargeType.Price,
                                        AccountingSupplyCostsWithinCountry = 0,
                                        IsIncludeAccountingValue = true,
                                        ConsumableProductId = consumableProduct.Id,
                                        SupplyPaymentTaskId = null,
                                        SupplyOrderUkraineId = null,
                                        AccountingPaymentTaskId = null,
                                        SupplyInformationTaskId = null,
                                        AccountingExchangeRate = null,
                                        ExchangeRate = null,
                                        ActProvidingServiceDocumentId = null,
                                        SupplyServiceAccountDocumentId = null,
                                        AccountingActProvidingServiceId = null,
                                        ActProvidingServiceId = null
                                    });

                                    consignmentsSyncRepository.Add(new SupplyInvoiceMergedService {
                                        Created = DateTime.Now,
                                        Deleted = false,
                                        Updated = DateTime.Now,
                                        Value = !forAmg ? totalValueSpend : 0,
                                        AccountingValue = forAmg ? totalValueSpend : 0,
                                        IsCalculatedValue = true,
                                        MergedServiceId = serviceId,
                                        SupplyInvoiceId = consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyInvoiceId
                                    });
                                }
                            }

                            consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItemId =
                                consignmentsSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem);

                            consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItemId =
                                consignmentsSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem);

                            consignmentItem.ProductIncomeItem.PackingListPackageOrderItemId =
                                consignmentsSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem);

                            consignmentItem.ProductIncomeItemId =
                                consignmentsSyncRepository.Add(consignmentItem.ProductIncomeItem);

                            consignmentItem.ProductSpecificationId =
                                consignmentsSyncRepository.Add(consignmentItem.ProductSpecification);

                            consignmentItem.ProductSpecification.OrderProductSpecification.ProductSpecificationId =
                                consignmentItem.ProductSpecificationId;
                            consignmentItem.ProductSpecification.OrderProductSpecification.SupplyInvoiceId =
                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyInvoiceId;

                            consignmentsSyncRepository.Add(consignmentItem.ProductSpecification.OrderProductSpecification);

                            consignmentItem.Id = consignmentsSyncRepository.Add(consignmentItem);

                            consignmentsSyncRepository.Add(new ConsignmentItemMovement {
                                Qty = syncConsignment.Qty,
                                IsIncomeMovement = true,
                                ProductIncomeItemId = consignmentItem.ProductIncomeItemId,
                                ConsignmentItemId = consignmentItem.Id,
                                MovementType = ConsignmentItemMovementType.Income
                            });

                            consignment.ConsignmentItems.Add(consignmentItem);

                            consignmentsSyncRepository.Add(new ProductPlacement {
                                Qty = syncConsignment.Qty,
                                ProductId = product.Id,
                                StorageId = storage.Id,
                                ConsignmentItemId = consignmentItem.Id,
                                RowNumber = "N",
                                CellNumber = "N",
                                StorageNumber = "N"
                            });
                        }

                        if (product.ProductAvailabilities.Any(a => a.StorageId.Equals(storage.Id))) {
                            ProductAvailability availability =
                                product.ProductAvailabilities.First(a => a.StorageId.Equals(storage.Id));

                            availability.Amount += syncConsignment.Qty;

                            consignmentsSyncRepository.Update(availability);
                        } else {
                            ProductAvailability availability = new() {
                                Amount = syncConsignment.Qty,
                                ProductId = product.Id,
                                StorageId = storage.Id
                            };

                            availability.Id = consignmentsSyncRepository.Add(availability);

                            product.ProductAvailabilities.Add(availability);
                        }
                    }
                } else if (!forAmg) {
                    bool isIncomeToManagementStorage = !storageThreeNames.Contains(firstSyncConsignment.IncomeStorageName);

                    bool isTransferFromStorageThree = !isIncomeToManagementStorage &&
                                                      firstSyncConsignment.StorageName != firstSyncConsignment.IncomeStorageName;

                    DateTime dateForExchange = firstSyncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                               firstSyncConsignment.DocumentDate.AddYears(-2000);

                    IEnumerable<PackingListPackageOrderItem> packigListItems = existInvoice.PackingLists.SelectMany(x => x.PackingListPackageOrderItems);

                    DateTime? dateCustomDeclaration = firstSyncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? firstSyncConsignment.DocumentDate.AddYears(-2000);

                    IEnumerable<ProductIncome> existIncomes = packigListItems.Select(x => x.ProductIncomeItem.ProductIncome).DistinctBy(x => x.Id);

                    DateTime dateConsignment = ConvertDateTimeToUtcInUkraineTimeZone(firstSyncConsignment.DocumentDate.AddYears(-2000));

                    foreach (ProductIncome existIncome in existIncomes) existIncome.FromDate = dateConsignment;

                    IEnumerable<Consignment> existConsignments = consignments.Where(x =>
                        x.ConsignmentItems.Any(y =>
                            y.ProductIncomeItem.PackingListPackageOrderItem != null &&
                            y.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.Number == firstSyncConsignment.DocumentArrivalNumber));

                    foreach (Consignment existConsignment in existConsignments) existConsignment.FromDate = dateConsignment;

                    Currency currencyFromInvoice = consignmentsSyncRepository.GetCurrencyByInvoice(existInvoice.Id) ?? currency;

                    if (consignmentSpends.Any()) {
                        decimal totalValueSpend = 0;

                        decimal documentValueExchangeRateAmount = 1;

                        if (isTransferFromStorageThree)
                            documentValueExchangeRateAmount = GetGovExchangeRateOnDateToEur(
                                currencyFromInvoice,
                                dateForExchange,
                                govCrossExchangeRateRepository,
                                govExchangeRateRepository,
                                currencyRepository);
                        else if (isIncomeToManagementStorage)
                            documentValueExchangeRateAmount = GetExchangeRateOnDateToEur(
                                currencyFromInvoice,
                                dateForExchange,
                                crossExchangeRateRepository,
                                exchangeRateRepository,
                                currencyRepository);
                        else
                            documentValueExchangeRateAmount = GetGovExchangeRateOnDateToEur(
                                currencyFromInvoice,
                                dateCustomDeclaration ?? DateTime.Now,
                                govCrossExchangeRateRepository,
                                govExchangeRateRepository,
                                currencyRepository);

                        foreach (SyncConsignmentSpend consignmentSpend in consignmentSpends) {
                            Currency supplyCurrency;

                            if (usdCodes.Contains(consignmentSpend.CurrencyCode))
                                supplyCurrency = usd;
                            else if (eurCodes.Contains(consignmentSpend.CurrencyCode))
                                supplyCurrency = eur;
                            else if (plnCodes.Contains(consignmentSpend.CurrencyCode))
                                supplyCurrency = pln;
                            else
                                supplyCurrency = uah;

                            decimal valueSpend = consignmentSpend.Amount;

                            decimal serviceExchangeRateAmount = 1;

                            if (isTransferFromStorageThree)
                                serviceExchangeRateAmount = GetGovExchangeRateOnDateToEur(
                                    supplyCurrency,
                                    dateForExchange,
                                    govCrossExchangeRateRepository,
                                    govExchangeRateRepository,
                                    currencyRepository);
                            else if (isIncomeToManagementStorage)
                                serviceExchangeRateAmount = GetExchangeRateOnDateToEur(
                                    supplyCurrency,
                                    dateForExchange,
                                    crossExchangeRateRepository,
                                    exchangeRateRepository,
                                    currencyRepository);
                            else
                                serviceExchangeRateAmount = GetGovExchangeRateOnDateToEur(
                                    supplyCurrency,
                                    existInvoice.DateCustomDeclaration ?? DateTime.Now,
                                    govCrossExchangeRateRepository,
                                    govExchangeRateRepository,
                                    currencyRepository);

                            //Get spend for current document
                            decimal documentValueInEur = firstSyncConsignment.DocumentValue / documentValueExchangeRateAmount;
                            decimal totalSpendInEur = consignmentSpend.TotalSpend / serviceExchangeRateAmount;

                            valueSpend *= documentValueInEur / totalSpendInEur;

                            totalValueSpend += decimal.Round(valueSpend / serviceExchangeRateAmount, 14, MidpointRounding.AwayFromZero);
                        }

                        ProcessUpdateSupplyManagePrice(
                            existInvoice,
                            firstSyncConsignment.DocumentValue,
                            totalValueSpend, currentUser,
                            dateCustomDeclaration,
                            isTransferFromStorageThree,
                            isIncomeToManagementStorage,
                            dateForExchange,
                            existInvoice.DateCustomDeclaration);


                        //List<SupplyInvoiceMergedService> supplyInvoiceMergedServices =
                        //    supplyInvoiceMergedServiceRepository.GetBySupplyInvoiceId(existInvoice.Id);

                        //decimal govExchangeRateFromUahToEur = 1;

                        //if (isTransferFromStorageThree) {
                        //    GovExchangeRate govExchangeRate =
                        //        govExchangeRateRepository
                        //            .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateForExchange);

                        //    govExchangeRateFromUahToEur = govExchangeRate.Amount;
                        //} else if (isIncomeToManagementStorage) {
                        //    ExchangeRate exchangeRate =
                        //        exchangeRateRepository
                        //            .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateForExchange);

                        //    govExchangeRateFromUahToEur = exchangeRate.Amount;
                        //} else {
                        //    GovExchangeRate govExchangeRate =
                        //        govExchangeRateRepository
                        //            .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateConsignment);

                        //    govExchangeRateFromUahToEur = govExchangeRate.Amount;
                        //}

                        //foreach (SupplyInvoiceMergedService supplyInvoiceMergedService in supplyInvoiceMergedServices) {
                        //    Currency currencyFromService = currencyRepository.GetByMergedServiceId(supplyInvoiceMergedService.MergedServiceId);

                        //    string serviceName = supplyInvoiceMergedService.MergedService.ConsumableProduct.Name + " " +
                        //                         supplyInvoiceMergedService.MergedService.Number;

                        //    decimal govExchangeRateAmount;

                        //    if (isTransferFromStorageThree) {
                        //        govExchangeRateAmount =
                        //            supplyInvoiceMergedService.MergedService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                        //                currencyFromService,
                        //                dateForExchange,
                        //                govExchangeRateRepository,
                        //                currencyRepository
                        //            );
                        //    } else if (isIncomeToManagementStorage) {
                        //        govExchangeRateAmount =
                        //            supplyInvoiceMergedService.MergedService.ExchangeRate ?? GetExchangeRateOnDateToUah(
                        //                currencyFromService,
                        //                dateForExchange,
                        //                exchangeRateRepository,
                        //                currencyRepository
                        //            );
                        //    } else {
                        //        govExchangeRateAmount =
                        //            supplyInvoiceMergedService.MergedService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                        //                currencyFromService,
                        //                dateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                        //                govExchangeRateRepository,
                        //                currencyRepository
                        //            );
                        //    }

                        //    decimal govAccountingExchangeRateAmount;

                        //    if (isTransferFromStorageThree) {
                        //        govAccountingExchangeRateAmount =
                        //            supplyInvoiceMergedService.MergedService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                        //                currencyFromService,
                        //                dateForExchange,
                        //                govExchangeRateRepository,
                        //                currencyRepository
                        //            );
                        //    } else if (isIncomeToManagementStorage) {
                        //        govAccountingExchangeRateAmount =
                        //            supplyInvoiceMergedService.MergedService.AccountingExchangeRate ?? GetExchangeRateOnDateToUah(
                        //                currencyFromService,
                        //                dateForExchange,
                        //                exchangeRateRepository,
                        //                currencyRepository
                        //            );
                        //    } else {
                        //        govAccountingExchangeRateAmount =
                        //            supplyInvoiceMergedService.MergedService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                        //                currencyFromService,
                        //                dateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                        //                govExchangeRateRepository,
                        //                currencyRepository
                        //            );
                        //    }

                        //    decimal accountingServicePrice = govAccountingExchangeRateAmount < 0
                        //                ? supplyInvoiceMergedService.AccountingValue / (0 - govAccountingExchangeRateAmount)
                        //                : supplyInvoiceMergedService.AccountingValue * govAccountingExchangeRateAmount;

                        //    decimal grossServicePrice = govAccountingExchangeRateAmount < 0
                        //        ? supplyInvoiceMergedService.Value / (0 - govAccountingExchangeRateAmount)
                        //        : supplyInvoiceMergedService.Value * govAccountingExchangeRateAmount;

                        //    decimal invoicePrice = govAccountingExchangeRateAmount < 0
                        //                ? existInvoice.NetPrice / (0 - govAccountingExchangeRateAmount)
                        //                : existInvoice.NetPrice * govAccountingExchangeRateAmount;

                        //    decimal accountingGrossPercentCurrentService = accountingServicePrice * 100 / invoicePrice;

                        //    decimal grossPercentCurrentService = grossServicePrice * 100 / invoicePrice;

                        //    foreach (PackingList packingList in existInvoice.PackingLists) {
                        //        foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        //            decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPriceUah * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        //            decimal valueOnCurrentPackListItem =
                        //                decimal.Round(
                        //                    (totalNetPricePackingListItem * grossPercentCurrentService / 100) / govExchangeRateFromUahToEur,
                        //                    14,
                        //                    MidpointRounding.AwayFromZero
                        //                );

                        //            decimal accountingValueOnCurrentPackListItem =
                        //                decimal.Round(
                        //                    (totalNetPricePackingListItem * accountingGrossPercentCurrentService / 100) / govExchangeRateFromUahToEur,
                        //                    14,
                        //                    MidpointRounding.AwayFromZero
                        //            );

                        //            PackingListPackageOrderItemSupplyService existItem =
                        //                packingListPackageOrderItemSupplyServiceRepository
                        //                    .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyInvoiceMergedService.MergedService.Id,
                        //                        TypeService.MergedService);

                        //            if (existItem == null) {
                        //                PackingListPackageOrderItemSupplyService newItem =
                        //                    new PackingListPackageOrderItemSupplyService {
                        //                        CurrencyId = currencyFromService.Id,
                        //                        Name = serviceName,
                        //                        PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                        //                        MergedServiceId = supplyInvoiceMergedService.MergedService.Id,
                        //                        ManagementValue = valueOnCurrentPackListItem,
                        //                        ExchangeRateDate = dateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                        //                        Updated = DateTime.Now
                        //                    };

                        //                if (supplyInvoiceMergedService.MergedService.IsIncludeAccountingValue)
                        //                    newItem.NetValue = accountingValueOnCurrentPackListItem;
                        //                else
                        //                    newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                        //                packingListPackageOrderItemSupplyServiceRepository
                        //                    .New(newItem);
                        //            } else {
                        //                existItem.ManagementValue = valueOnCurrentPackListItem;

                        //                if (existItem.Deleted)
                        //                    existItem.Deleted = false;

                        //                existItem.ExchangeRateDate = dateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created;

                        //                existItem.CurrencyId = currencyFromService.Id;

                        //                if (supplyInvoiceMergedService.MergedService.IsIncludeAccountingValue)
                        //                    existItem.NetValue = accountingValueOnCurrentPackListItem;
                        //                else
                        //                    existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                        //                packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        //            }
                        //        }
                        //    }
                        //}
                    }

                    consignmentsSyncRepository.Update(existIncomes);

                    consignmentsSyncRepository.Update(existConsignments);
                }
            }

            consignmentsSyncRepository.UpdateActiveSpecification();

            if (invoiceWithDatesIds.Any()) {
                List<long> invoiceIds = invoiceWithDatesIds.Select(x => x.Item1).ToList();

                IEnumerable<SupplyInvoice> allSupplyInvoices = consignmentsSyncRepository.GetSupplyInvoiceByIds(invoiceIds);

                IEnumerable<IGrouping<long, SupplyInvoice>> groupedSupplyInvoices = allSupplyInvoices.GroupBy(x => x.SupplyOrderId);

                foreach (IGrouping<long, SupplyInvoice> groupedSupplyInvoice in groupedSupplyInvoices) {
                    IEnumerable<SupplyInvoice> supplyInvoices = groupedSupplyInvoice.Select(x => x);

                    long[] currentInvoiceIds = supplyInvoices.Select(x => x.Id).ToArray();

                    ProcessUpdateSupplyInvoiceGrossPrice(
                        new UpdateSupplyInvoiceItemGrossPriceMessage(
                            currentInvoiceIds,
                            currentUser.NetUid
                        ), documentValues,
                        invoiceWithDatesIds,
                        forAmg);
                }

                IEnumerable<ConsignmentItem> consignmentItems = consignmentsSyncRepository.GetConsignmentItemsByInvoiceIds(invoiceIds);

                if (consignmentItems.Any()) {
                    foreach (ConsignmentItem consignmentItem in consignmentItems) {
                        consignmentItem.NetPrice = consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.UnitPriceEur;
                        consignmentItem.AccountingPrice = consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.AccountingGrossUnitPriceEur;
                        consignmentItem.Price = consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.AccountingGrossUnitPriceEur;
                    }

                    consignmentsSyncRepository.UpdatePrices(consignmentItems);
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.CONSIGNMENTS_SYNC_END], true));
        } catch (Exception exc) {
            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_ERROR], true, true));

            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddDataSyncLogMessage(
                        "SYNC_ERROR Consignments",
                        $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                        JsonConvert.SerializeObject(new {
                            exc.Message,
                            exc.StackTrace
                        })
                    )
                );
        }
    }

    // private async Task SynchronizeData(
    //     IActorRef hubSenderActorRef,
    //     IDbConnection oneCConnection,
    //     IDbConnection remoteSyncConnection,
    //     User currentUser,
    //     IDbConnection amgSyncConnection,
    //     SynchronizeDailyDataMessage message) {
    //     try {
    //         DataSyncOperation operation =
    //             _dataSyncRepositoriesFactory
    //                 .NewDataSyncOperationRepository(remoteSyncConnection)
    //                 .GetLastRecordByOperationType(
    //                     DataSyncOperationType.Accounting,
    //                     DataSyncOperationType.OutcomeOrders
    //                 );
    //
    //         IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository =
    //             _dataSyncRepositoriesFactory.NewOutcomeOrdersSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
    //         IDeliveryRecipientRepository deliveryRecipientRepository = _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(remoteSyncConnection);
    //         ITransporterRepository transporterRepository = _transporterRepositoriesFactory.NewTransporterRepository(remoteSyncConnection);
    //         ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(remoteSyncConnection);
    //
    //         //hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.DAILY_SYNC_START]));
    //
    //         outcomeOrdersSyncRepository.CleanDebtsAndBalances();
    //
    //         await SyncProductTransfers(outcomeOrdersSyncRepository, currentUser, message.ForAmg, message.From, message.To);
    //         await SyncDepreciatedOrders(outcomeOrdersSyncRepository, currentUser, message.ForAmg, message.From, message.To);
    //         await SyncActProductTransfers(outcomeOrdersSyncRepository, currentUser, message.ForAmg, message.From, message.To);
    //
    //         // IEnumerable<ClientAgreement> clientAgreements = outcomeOrdersSyncRepository.GetAllClientAgreementsToSync();
    //         //
    //         // foreach (ClientAgreement clientAgreement in clientAgreements
    //         //     .Where(x => (message.ForAmg ? x.OriginalClientAmgCode.HasValue : x.OriginalClientFenixCode.HasValue))) {
    //         //     if (!clientAgreement.OriginalClientAmgCode.HasValue && !clientAgreement.OriginalClientFenixCode.HasValue) continue;
    //         //
    //         //     IEnumerable<SyncSettlement> syncSettlements =
    //         //         message.ForAmg
    //         //             ? (clientAgreement.OriginalClientAmgCode.HasValue
    //         //                 ? outcomeOrdersSyncRepository.GetAmgSyncSettlements(
    //         //                     message.From,
    //         //                     message.To,
    //         //                     clientAgreement.OriginalClientAmgCode.Value,
    //         //                     clientAgreement.Agreement.Organization.Name,
    //         //                     clientAgreement.Agreement.Name,
    //         //                     clientAgreement.Agreement.Currency.CodeOneC,
    //         //                     clientAgreement.Agreement.Pricing.Name
    //         //                 )
    //         //                 : Array.Empty<SyncSettlement>())
    //         //             : (clientAgreement.OriginalClientFenixCode.HasValue
    //         //                 ? outcomeOrdersSyncRepository
    //         //                     .GetSyncSettlements(
    //         //                         message.From,
    //         //                         message.To,
    //         //                         clientAgreement.OriginalClientFenixCode.Value,
    //         //                         clientAgreement.Agreement.Organization.Name,
    //         //                         clientAgreement.Agreement.Name,
    //         //                         clientAgreement.Agreement.Currency.CodeOneC,
    //         //                         clientAgreement.Agreement.Pricing.Name
    //         //                     )
    //         //                 : Array.Empty<SyncSettlement>());
    //         //
    //         //
    //         //     if (syncSettlements.Any())
    //         //         foreach (SyncSettlement syncSettlement in syncSettlements) {
    //         //             switch (syncSettlement.SettlementType) {
    //         //                 case SyncSettlementType.SaleReturn:
    //         //                     await SyncSaleReturn(
    //         //                         outcomeOrdersSyncRepository,
    //         //                         _clientRepositoriesFactory.NewClientRepository(remoteSyncConnection),
    //         //                         clientAgreement,
    //         //                         syncSettlement,
    //         //                         currentUser,
    //         //                         message.ForAmg
    //         //                     );
    //         //
    //         //                     break;
    //         //                 case SyncSettlementType.IncomePaymentOrder:
    //         //                     await SyncIncomePaymentOrder(
    //         //                         outcomeOrdersSyncRepository,
    //         //                         saleRepository,
    //         //                         clientAgreement,
    //         //                         syncSettlement,
    //         //                         currentUser,
    //         //                         message.ForAmg
    //         //                     );
    //         //
    //         //                     break;
    //         //                 // case SyncSettlementType.OutcomePaymentOrder:
    //         //                 //     await SyncOutcomePaymentOrder(
    //         //                 //         outcomeOrdersSyncRepository,
    //         //                 //         clientAgreement,
    //         //                 //         syncSettlement,
    //         //                 //         currentUser,
    //         //                 //         message.ForAmg
    //         //                 //     );
    //         //                 //
    //         //                 //     break;
    //         //                 case SyncSettlementType.IncomeCashOrder:
    //         //                     await SyncIncomeCashOrder(
    //         //                         outcomeOrdersSyncRepository,
    //         //                         saleRepository,
    //         //                         clientAgreement,
    //         //                         syncSettlement,
    //         //                         currentUser
    //         //                     );
    //         //
    //         //                     break;
    //         //                 case SyncSettlementType.Sale:
    //         //                     await SyncSale(
    //         //                         outcomeOrdersSyncRepository,
    //         //                         transporterRepository,
    //         //                         deliveryRecipientRepository,
    //         //                         clientAgreement,
    //         //                         syncSettlement,
    //         //                         currentUser,
    //         //                         message.ForAmg
    //         //                     );
    //         //
    //         //                     break;
    //         //                 default:
    //         //                     break;
    //         //             }
    //         //         }
    //         //
    //         //     await SyncOrders(
    //         //         outcomeOrdersSyncRepository,
    //         //         clientAgreement,
    //         //         currentUser,
    //         //         message.ForAmg,
    //         //         message.From,
    //         //         message.To
    //         //     );
    //
    //         // hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.DAILY_SYNC_END]));
    //     } catch (Exception exc) {
    //         ActorReferenceManager
    //             .Instance
    //             .Get(BaseActorNames.LOG_MANAGER_ACTOR)
    //             .Tell(
    //                 new AddDataSyncLogMessage(
    //                     "SYNC_ERROR Outcome Orders",
    //                     $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
    //                     JsonConvert.SerializeObject(new {
    //                         exc.Message,
    //                         exc.StackTrace
    //                     })
    //                 )
    //             );
    //     }
    // }

    private async Task SyncProductTransfers(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        User currentUser,
        bool forAmg,
        DateTime fromDate,
        DateTime toDate) {
        IEnumerable<SyncProductTransferItem> syncProductTransferItems =
            forAmg
                ? outcomeOrdersSyncRepository.GetAllAmgProductTransferItems(fromDate, toDate)
                : outcomeOrdersSyncRepository.GetAllProductTransferItems(fromDate, toDate);

        if (!syncProductTransferItems.Any()) return;

        IEnumerable<IGrouping<string, SyncProductTransferItem>> groupedProductTransfers = syncProductTransferItems.GroupBy(i => i.DocumentIdInString);

        foreach (IGrouping<string, SyncProductTransferItem> groupedProductTransfer in groupedProductTransfers) {
            SyncProductTransferItem syncProductTransferItem = groupedProductTransfer.First();

            ProductTransfer productTransfer = new() {
                Number = syncProductTransferItem.Number,
                FromDate = ConvertDateTimeToUtcInUkraineTimeZone(syncProductTransferItem.DocumentDate.AddYears(-2000)),
                Comment = syncProductTransferItem.Comment,
                ResponsibleId = currentUser.Id,
                FromStorage = outcomeOrdersSyncRepository.GetStorageByName(syncProductTransferItem.StorageFrom),
                ToStorage = outcomeOrdersSyncRepository.GetStorageByName(syncProductTransferItem.StorageTo),
                Organization = outcomeOrdersSyncRepository.GetOrganizationByName(syncProductTransferItem.OrganizationName),
                IsManagement = !forAmg && syncProductTransferItem.StorageFrom.ToLower().Replace(" ", string.Empty).Equals(STORAGE_3)
            };

            foreach (SyncProductTransferItem item in groupedProductTransfer)
                productTransfer.ProductTransferItems.Add(new ProductTransferItem {
                    Qty = item.Qty,
                    Product = outcomeOrdersSyncRepository.GetProductBySourceCode(item.SourceProductCode, forAmg)
                });

            object result =
                await ActorReferenceManager.Instance.Get(ProductsActorNames.PRODUCT_TRANSFERS_ACTOR).Ask<object>(
                    new AddNewProductTransferMessage(productTransfer, currentUser.NetUid, null, null, null)
                );

            if (result is Exception exc)
                ActorReferenceManager
                    .Instance
                    .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                    .Tell(
                        new AddDataSyncLogMessage(
                            "SYNC_ERROR ProductTransfer",
                            $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                            JsonConvert.SerializeObject(new {
                                exc.Message,
                                exc.StackTrace
                            })
                        )
                    );
        }
    }

    private async Task SyncDepreciatedOrders(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        User currentUser,
        bool forAmg,
        DateTime fromDate,
        DateTime toDate) {
        IEnumerable<SyncDepreciatedOrderItem> syncDepreciatedOrderItems = outcomeOrdersSyncRepository.GetAllDepreciatedOrderItems(fromDate, toDate);

        if (!syncDepreciatedOrderItems.Any()) return;

        IEnumerable<IGrouping<string, SyncDepreciatedOrderItem>> groupedDepreciatedOrders = syncDepreciatedOrderItems.GroupBy(i => i.DocumentIdInString);

        foreach (IGrouping<string, SyncDepreciatedOrderItem> groupedDepreciatedOrder in groupedDepreciatedOrders) {
            SyncDepreciatedOrderItem syncDepreciatedOrderItem = groupedDepreciatedOrder.First();

            DepreciatedOrder depreciatedOrder = new() {
                Number = syncDepreciatedOrderItem.Number,
                FromDate = ConvertDateTimeToUtcInUkraineTimeZone(syncDepreciatedOrderItem.DocumentDate.AddYears(-2000)),
                Comment = syncDepreciatedOrderItem.Comment,
                ResponsibleId = currentUser.Id,
                Storage = outcomeOrdersSyncRepository.GetStorageByName(syncDepreciatedOrderItem.Storage),
                Organization = outcomeOrdersSyncRepository.GetOrganizationByName(syncDepreciatedOrderItem.OrganizationName),
                IsManagement = syncDepreciatedOrderItem.Storage.ToLower().Replace(" ", string.Empty).Equals(STORAGE_3)
            };

            foreach (SyncDepreciatedOrderItem item in groupedDepreciatedOrder)
                depreciatedOrder.DepreciatedOrderItems.Add(new DepreciatedOrderItem {
                    Qty = item.Qty,
                    Product = outcomeOrdersSyncRepository.GetProductBySourceCode(item.SourceProductCode, false)
                });

            object result = await ActorReferenceManager.Instance.Get(DepreciatedActorNames.DEPRECIATED_ORDERS_ACTOR)
                .Ask<object>(new AddNewDepreciatedOrderMessage(depreciatedOrder, currentUser.NetUid));

            if (result is Exception exc)
                ActorReferenceManager
                    .Instance
                    .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                    .Tell(
                        new AddDataSyncLogMessage(
                            "SYNC_ERROR DepreciatedOrder",
                            $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                            JsonConvert.SerializeObject(new {
                                exc.Message,
                                exc.StackTrace
                            })
                        )
                    );
        }
    }

    private async Task SyncActProductTransfers(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        User currentUser,
        bool forAmg,
        DateTime fromDate,
        DateTime toDate) {
        IEnumerable<SyncProductTransferItem> syncProductTransferItems = outcomeOrdersSyncRepository.GetAllActProductTransferItems(fromDate, toDate);

        if (!syncProductTransferItems.Any()) return;

        IEnumerable<IGrouping<string, SyncProductTransferItem>> groupedProductTransfers = syncProductTransferItems.GroupBy(i => i.DocumentIdInString);

        foreach (IGrouping<string, SyncProductTransferItem> groupedProductTransfer in groupedProductTransfers) {
            SyncProductTransferItem syncProductTransferItem = groupedProductTransfer.First();

            ProductTransfer productTransfer = new() {
                Number = syncProductTransferItem.Number,
                FromDate = ConvertDateTimeToUtcInUkraineTimeZone(syncProductTransferItem.DocumentDate.AddYears(-2000)),
                Comment = syncProductTransferItem.Comment,
                ResponsibleId = currentUser.Id,
                FromStorage = outcomeOrdersSyncRepository.GetStorageByName(syncProductTransferItem.StorageFrom),
                ToStorage = outcomeOrdersSyncRepository.GetStorageByName(syncProductTransferItem.StorageTo),
                Organization = outcomeOrdersSyncRepository.GetOrganizationByName(syncProductTransferItem.OrganizationName),
                IsManagement = true
            };

            foreach (SyncProductTransferItem item in groupedProductTransfer)
                productTransfer.ProductTransferItems.Add(new ProductTransferItem {
                    Qty = item.Qty,
                    Product = outcomeOrdersSyncRepository.GetProductBySourceCode(item.SourceProductCode, forAmg)
                });

            object result =
                await ActorReferenceManager.Instance.Get(ProductsActorNames.PRODUCT_TRANSFERS_ACTOR).Ask<object>(
                    new AddNewProductTransferMessage(productTransfer, currentUser.NetUid, null, null, null)
                );

            if (result is Exception exc)
                ActorReferenceManager
                    .Instance
                    .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                    .Tell(
                        new AddDataSyncLogMessage(
                            "SYNC_ERROR ProductTransfer",
                            $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                            JsonConvert.SerializeObject(new {
                                exc.Message,
                                exc.StackTrace
                            })
                        )
                    );
        }
    }

    private string SyncOrdersAndSales(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        IDbConnection remoteConnection,
        ICurrencyRepository currencyRepository,
        ICrossExchangeRateRepository crossExchangeRateRepository,
        IExchangeRateRepository exchangeRateRepository,
        DateTime fromDate,
        DateTime toDate,
        User currentUser,
        bool forAmg) {
        //IEnumerable<SyncOrderItem> orderItems =
        //    forAmg
        //        ? (clientAgreement.OriginalClientAmgCode.HasValue
        //            ? outcomeOrdersSyncRepository.GetAmgAllSyncOrderItems(
        //                fromDate,
        //                DateTime.Now,
        //                clientAgreement.OriginalClientAmgCode.Value,
        //                clientAgreement.Agreement.Organization.Name,
        //                clientAgreement.Agreement.Name,
        //                clientAgreement.Agreement.Currency.CodeOneC,
        //                clientAgreement.Agreement.Pricing.Name
        //            )
        //            : Array.Empty<SyncOrderItem>())
        //        : (clientAgreement.OriginalClientFenixCode.HasValue
        //            ? outcomeOrdersSyncRepository
        //                .GetAllSyncOrderItems(
        //                    fromDate,
        //                    DateTime.Now,
        //                    clientAgreement.OriginalClientFenixCode.Value,
        //                    clientAgreement.Agreement.Organization.Name,
        //                    clientAgreement.Agreement.Name,
        //                    clientAgreement.Agreement.Currency.CodeOneC,
        //                    clientAgreement.Agreement.Pricing.Name
        //                )
        //            : Array.Empty<SyncOrderItem>());

        IEnumerable<SyncOrderSaleItem> orderItems = forAmg
            ? outcomeOrdersSyncRepository.GetAmgFilteredSyncOrderSaleItems(fromDate, toDate)
            : outcomeOrdersSyncRepository.GetFilteredSyncOrderSaleItems(fromDate, toDate);

        if (!orderItems.Any()) return string.Empty;

        long[] productCodes = orderItems.Select(x => x.ProductCode).Distinct().ToArray();

        long[] clientCodes = orderItems.Select(x => x.ClientCode).Distinct().ToArray();

        IEnumerable<Product> products = outcomeOrdersSyncRepository.GetProductByCodes(productCodes, forAmg);

        IEnumerable<Client> clients = outcomeOrdersSyncRepository.GetClientsWithData(clientCodes, forAmg);

        IEnumerable<Client> deletedClients = outcomeOrdersSyncRepository.GetDeletedClients();

        long[] mainClientIds = deletedClients.Where(x => x.MainClientId.HasValue).Select(x => x.MainClientId.Value).Distinct().ToArray();

        IEnumerable<Client> mainClients = outcomeOrdersSyncRepository.GetClientsByIds(mainClientIds);

        IEnumerable<Organization> organizations = outcomeOrdersSyncRepository.GetAllOrganizations();

        IEnumerable<Storage> storages = outcomeOrdersSyncRepository.GetAllStorages();

        Currency eur = currencyRepository.GetEURCurrencyIfExists();

        Currency usd = currencyRepository.GetUSDCurrencyIfExists();

        Currency pln = currencyRepository.GetPLNCurrencyIfExists();

        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        IEnumerable<IGrouping<Tuple<string>, SyncOrderSaleItem>> groupedSyncOrderItems =
            orderItems.GroupBy(x => new Tuple<string>(x.OrderIdInString));

        List<string> exceptionList = new();

        foreach (IGrouping<Tuple<string>, SyncOrderSaleItem> groupedOrder in groupedSyncOrderItems) {
            if (!groupedOrder.Any()) continue;

            SyncOrderSaleItem firstItem = groupedOrder.First();

            Client client = clients.FirstOrDefault(x => forAmg ? x.SourceAmgCode == firstItem.ClientCode : x.SourceFenixCode == firstItem.ClientCode);

            if (client == null || client.Deleted) {
                client = deletedClients.FirstOrDefault(x => forAmg ? x.SourceAmgCode == firstItem.ClientCode : x.SourceFenixCode == firstItem.ClientCode);

                if (client != null && client.MainClientId.HasValue)
                    client = mainClients.FirstOrDefault(x => x.Id == client.MainClientId.Value);

                if (client == null) continue;
            }

            ClientAgreement clientAgreement = client.ClientAgreements.FirstOrDefault(x =>
                forAmg ? x.Agreement.SourceAmgCode == firstItem.AgreementCode : x.Agreement.SourceFenixCode == firstItem.AgreementCode);

            if (clientAgreement == null) {
                clientAgreement = client.ClientAgreements.FirstOrDefault(x => x.Agreement.IsDefaultForSyncConsignment);

                if (clientAgreement == null) continue;
            }

            Storage storage = storages.FirstOrDefault(s => s.Name == firstItem.Storage);

            if (storage == null) continue;

            Organization organization = organizations.FirstOrDefault(o => o.Name == firstItem.Organization);

            if (organization == null) continue;

            Currency currency;

            if (eurCodes.Contains(firstItem.CurrencyCode))
                currency = eur;
            else if (usdCodes.Contains(firstItem.CurrencyCode))
                currency = usd;
            else if (plnCodes.Contains(firstItem.CurrencyCode))
                currency = pln;
            else
                currency = uah;

            Order order = new() {
                UserId = currentUser.Id,
                ClientAgreementId = clientAgreement.Id,
                OrderSource = OrderSource.Local,
                OrderStatus = firstItem.IsSale ? OrderStatus.Sale : OrderStatus.NewOrderCart,
                Created = firstItem.OrderDateTime.AddYears(-2000),
                Updated = firstItem.OrderDateTime.AddYears(-2000)
            };

            order.Id = outcomeOrdersSyncRepository.Add(order);

            decimal total = 0m;
            decimal totalEuro = 0m;

            foreach (SyncOrderSaleItem syncOrderItem in groupedOrder) {
                Product product = products.FirstOrDefault(x =>
                    forAmg ? x.SourceAmgCode.Equals(syncOrderItem.ProductCode) : x.SourceFenixCode.Equals(syncOrderItem.ProductCode));

                if (product == null) continue;

                decimal qtyInDecimal = Convert.ToDecimal(syncOrderItem.Qty);

                //decimal euroExchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(remoteConnection).GetEuroExchangeRateByCurrentCultureFiltered(
                //    product.NetUid,
                //    syncOrderItem.WithVat,
                //    false, // ?
                //    clientAgreement.Agreement.Currency.Id
                //);

                decimal euroExchangeRate = GetExchangeRateOnDateToEur(clientAgreement.Agreement.Currency, firstItem.OrderDateTime.AddYears(-2000), crossExchangeRateRepository,
                    exchangeRateRepository, currencyRepository);

                decimal priceInEuro = decimal.Round(syncOrderItem.Price / euroExchangeRate / qtyInDecimal, 14, MidpointRounding.AwayFromZero);
                decimal vatInEuro = decimal.Round(syncOrderItem.Vat / euroExchangeRate, 14, MidpointRounding.AwayFromZero);

                OrderItem orderItem = new() {
                    ProductId = product.Id,
                    OrderId = order.Id,
                    Qty = syncOrderItem.Qty,
                    UserId = currentUser.Id,
                    IsValidForCurrentSale = true,
                    DiscountAmount = syncOrderItem.UnitPrice * Convert.ToDecimal(syncOrderItem.Qty) * (syncOrderItem.Discount / 100m),
                    ExchangeRateAmount = euroExchangeRate,
                    PricePerItem = priceInEuro,
                    PricePerItemWithoutVat = priceInEuro,
                    Vat = syncOrderItem.Vat
                    //PricePerItem = decimal.Round(priceInEuro / qtyInDecimal, 14, MidpointRounding.AwayFromZero),
                    //PricePerItemWithoutVat = decimal.Round((priceInEuro - vatInEuro) / qtyInDecimal, 14, MidpointRounding.AwayFromZero),
                };

                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty) * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                total =
                    decimal.Round(total + orderItem.TotalAmountLocal, 14, MidpointRounding.AwayFromZero);
                totalEuro =
                    decimal.Round(totalEuro + orderItem.TotalAmount, 14, MidpointRounding.AwayFromZero);

                orderItem.Id = outcomeOrdersSyncRepository.AddWithId(orderItem);

                IEnumerable<ProductAvailability> productAvailabilities =
                    outcomeOrdersSyncRepository
                        .GetAvailabilities(
                            product.Id,
                            clientAgreement.Agreement.Organization.Id,
                            clientAgreement.Agreement.Organization.Culture != "pl" && clientAgreement.Agreement.WithVATAccounting
                        );

                double availabilities = productAvailabilities.Sum(x => x.Amount);

                if (availabilities < orderItem.Qty) {
                    if (!exceptionList.Contains(syncOrderItem.OrderNumber))
                        exceptionList.Add(syncOrderItem.OrderNumber);

                    continue;
                }

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

            if (firstItem.IsSale) {
                long deliveryRecipientId = outcomeOrdersSyncRepository.AddDeliveryRecipient(new DeliveryRecipient { ClientId = clientAgreement.ClientId });
                long deliveryRecipientAddressId =
                    outcomeOrdersSyncRepository.AddDeliverRecipientAddress(new DeliveryRecipientAddress { DeliveryRecipientId = deliveryRecipientId });

                Sale sale = new() {
                    OrderId = order.Id,
                    SaleNumberId = outcomeOrdersSyncRepository.Add(new SaleNumber {
                        Value = firstItem.SaleNumber,
                        OrganizationId = organization.Id
                    }),
                    BaseLifeCycleStatusId = outcomeOrdersSyncRepository.Add(new BaseLifeCycleStatus { SaleLifeCycleType = SaleLifeCycleType.Packaged }),
                    BaseSalePaymentStatusId = outcomeOrdersSyncRepository.Add(new NotPaidSalePaymentStatus()),
                    TransporterId = 57,
                    Comment = firstItem.SaleComment,
                    ChangedToInvoice = firstItem.SaleDateTime.HasValue ? firstItem.SaleDateTime.Value.AddYears(-2000) : firstItem.OrderDateTime.AddYears(-2000),
                    Created = firstItem.SaleDateTime.HasValue ? firstItem.SaleDateTime.Value.AddYears(-2000) : firstItem.OrderDateTime.AddYears(-2000),
                    Updated = firstItem.SaleDateTime.HasValue ? firstItem.SaleDateTime.Value.AddYears(-2000) : firstItem.OrderDateTime.AddYears(-2000),
                    ChangedToInvoiceById = currentUser.Id,
                    IsVatSale = clientAgreement.Agreement.IsAccounting,
                    IsLocked = clientAgreement.Agreement.IsAccounting,
                    IsPaymentBillDownloaded = false,
                    ClientAgreementId = clientAgreement.Id,
                    ClientAgreement = clientAgreement,
                    UserId = currentUser.Id,
                    IsImported = true,
                    DeliveryRecipientAddressId = deliveryRecipientAddressId,
                    DeliveryRecipientId = deliveryRecipientId,
                    IsPrintedPaymentInvoice = true
                };

                sale.Id = outcomeOrdersSyncRepository.Add(sale);

                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                    .Tell(new StoreConsignmentMovementFromSaleMessage(sale.Id, Sender, string.Empty));

                List<SaleExchangeRate> saleExchangeRates =
                    _exchangeRateRepositoriesFactory
                        .NewExchangeRateRepository(remoteConnection)
                        .GetAllByCulture()
                        .Select(exchangeRate =>
                            new SaleExchangeRate {
                                ExchangeRateId = exchangeRate.Id,
                                Value = exchangeRate.Amount,
                                SaleId = sale.Id
                            }
                        ).ToList();

                _saleRepositoriesFactory.NewSaleExchangeRateRepository(remoteConnection).Add(saleExchangeRates);

                HandleClientsDebtForCurrentSale(remoteConnection, sale, clientAgreement, total, totalEuro, firstItem);
            } else {
                long deliveryRecipientId = outcomeOrdersSyncRepository.AddDeliveryRecipient(new DeliveryRecipient { ClientId = clientAgreement.ClientId });
                long deliveryRecipientAddressId =
                    outcomeOrdersSyncRepository.AddDeliverRecipientAddress(new DeliveryRecipientAddress { DeliveryRecipientId = deliveryRecipientId });

                Sale sale = new() {
                    OrderId = order.Id,
                    SaleNumberId = outcomeOrdersSyncRepository.Add(new SaleNumber {
                        Value = firstItem.OrderNumber,
                        OrganizationId = organization.Id
                    }),
                    BaseLifeCycleStatusId = outcomeOrdersSyncRepository.Add(new BaseLifeCycleStatus { SaleLifeCycleType = SaleLifeCycleType.New }),
                    BaseSalePaymentStatusId = outcomeOrdersSyncRepository.Add(new NotPaidSalePaymentStatus()),
                    TransporterId = 57,
                    Comment = firstItem.SaleComment,
                    //ChangedToInvoice = firstItem.SaleDateTime.HasValue ? firstItem.SaleDateTime.Value.AddYears(-2000) : firstItem.OrderDateTime.AddYears(-2000),
                    Created = firstItem.SaleDateTime.HasValue ? firstItem.SaleDateTime.Value.AddYears(-2000) : firstItem.OrderDateTime.AddYears(-2000),
                    Updated = firstItem.SaleDateTime.HasValue ? firstItem.SaleDateTime.Value.AddYears(-2000) : firstItem.OrderDateTime.AddYears(-2000),
                    ChangedToInvoiceById = currentUser.Id,
                    IsVatSale = clientAgreement.Agreement.IsAccounting,
                    IsLocked = clientAgreement.Agreement.IsAccounting,
                    IsPaymentBillDownloaded = false,
                    ClientAgreementId = clientAgreement.Id,
                    ClientAgreement = clientAgreement,
                    UserId = currentUser.Id,
                    IsImported = true,
                    DeliveryRecipientAddressId = deliveryRecipientAddressId,
                    DeliveryRecipientId = deliveryRecipientId
                };

                sale.Id = outcomeOrdersSyncRepository.Add(sale);
            }
        }

        return exceptionList.Any()
            ? GetCurrentDateInString() + _localizer[SharedResourceNames.CONSIGNMENTS_SYNC_EXCEPTION_LIST] + exceptionList.Join(", ") + Environment.NewLine
            : string.Empty;

        //string documentNumber = string.Empty;
        //DateTime documentDate = DateTime.MinValue;

        //Sale sale = null;

        //foreach (SyncOrderSaleItem syncOrderItem in orderItems) {
        //    if (!documentNumber.Equals(syncOrderItem.DocumentNumber) ||
        //        !documentDate.Equals(syncOrderItem.DocumentDate)) {
        //        sale = null;

        //        documentNumber = syncOrderItem.DocumentNumber;
        //        documentDate = syncOrderItem.DocumentDate;
        //    }

        //    Product product = outcomeOrdersSyncRepository.GetProductBySourceCodeWithIncludes(syncOrderItem.ProductCode, forAmg);

        //    if (product == null) continue;

        //    if (sale == null) {
        //        sale = new Sale {
        //            Order = new Order {
        //                UserId = currentUser.Id,
        //                ClientAgreementId = clientAgreement.Id,
        //                OrderSource = OrderSource.Local,
        //                OrderStatus = OrderStatus.Sale,
        //                Created = syncOrderItem.DocumentDate.AddYears(-2000),
        //                Updated = syncOrderItem.DocumentDate.AddYears(-2000)
        //            },
        //            SaleNumberId = outcomeOrdersSyncRepository.Add(new SaleNumber {
        //                Value = syncOrderItem.DocumentNumber,
        //                OrganizationId = clientAgreement.Agreement.Organization.Id
        //            }),
        //            BaseLifeCycleStatusId = outcomeOrdersSyncRepository.Add(new NewSaleLifeCycleStatus()),
        //            BaseSalePaymentStatusId = outcomeOrdersSyncRepository.Add(new NotPaidSalePaymentStatus()),
        //            TransporterId = 57,
        //            Comment = _comment,
        //            ChangedToInvoice = syncOrderItem.DocumentDate.AddYears(-2000),
        //            Created = syncOrderItem.DocumentDate.AddYears(-2000),
        //            Updated = syncOrderItem.DocumentDate.AddYears(-2000),
        //            ChangedToInvoiceById = currentUser.Id,
        //            IsVatSale = clientAgreement.Agreement.IsAccounting,
        //            IsLocked = clientAgreement.Agreement.IsAccounting,
        //            IsPaymentBillDownloaded = false,
        //            ClientAgreementId = clientAgreement.Id,
        //            UserId = currentUser.Id,
        //            IsImported = true
        //        };

        //        sale.OrderId = sale.Order.Id = outcomeOrdersSyncRepository.Add(sale.Order);

        //        sale.Id = outcomeOrdersSyncRepository.Add(sale);
        //    }

        //    OrderItem orderItem = new OrderItem {
        //        ProductId = product.Id,
        //        OrderId = sale.Order.Id,
        //        Qty = syncOrderItem.Qty,
        //        UserId = currentUser.Id,
        //        Comment = _comment,
        //        IsValidForCurrentSale = true
        //    };

        //    orderItem.Id = outcomeOrdersSyncRepository.AddWithId(orderItem);

        //    IEnumerable<ProductAvailability> productAvailabilities =
        //        outcomeOrdersSyncRepository
        //            .GetAvailabilities(
        //                product.Id,
        //                clientAgreement.Agreement.Organization.Id,
        //                clientAgreement.Agreement.Organization.Culture != "pl" && clientAgreement.Agreement.WithVATAccounting
        //            );

        //    foreach (ProductAvailability availability in productAvailabilities) {
        //        if (orderItem.Qty.Equals(0d)) break;

        //        ProductReservation reservation = new ProductReservation {
        //            OrderItemId = orderItem.Id,
        //            ProductAvailabilityId = availability.Id
        //        };

        //        if (availability.Amount >= orderItem.Qty) {
        //            availability.Amount -= orderItem.Qty;

        //            reservation.Qty = orderItem.Qty;

        //            orderItem.Qty = 0d;
        //        } else {
        //            orderItem.Qty -= availability.Amount;

        //            reservation.Qty = availability.Amount;

        //            availability.Amount = 0d;
        //        }

        //        outcomeOrdersSyncRepository.Update(availability);

        //        outcomeOrdersSyncRepository.Add(reservation);
        //    }
        //}
    }

    private void HandleClientsDebtForCurrentSale(
        IDbConnection remoteConnection,
        Sale sale,
        ClientAgreement clientAgreement,
        decimal total,
        decimal totalEuro,
        SyncOrderSaleItem firstItem) {
        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(remoteConnection);
        IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(remoteConnection);
        IClientBalanceMovementRepository clientBalanceMovementRepository = _clientRepositoriesFactory.NewClientBalanceMovementRepository(remoteConnection);
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(remoteConnection);
        IBaseSalePaymentStatusRepository baseSalePaymentStatusRepository = _saleRepositoriesFactory.NewBaseSalePaymentStatusRepository(remoteConnection);
        IDebtRepository debtRepository = _saleRepositoriesFactory.NewDebtRepository(remoteConnection);

        ClientInDebt clientInDebtFromDb = clientInDebtRepository.GetBySaleAndClientAgreementIds(sale.Id, clientAgreement.Id);

        if (sale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("eur")) total = totalEuro;

        if (clientAgreement.CurrentAmount >= totalEuro) {
            clientAgreement.CurrentAmount = Math.Round(clientAgreement.CurrentAmount - totalEuro, 14);

            clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

            clientBalanceMovementRepository
                .AddOutMovement(
                    new ClientBalanceMovement {
                        ClientAgreementId = clientAgreement.Id,
                        Amount = totalEuro,
                        ExchangeRateAmount = !firstItem.ExchangeRate.Equals(decimal.Zero) ? firstItem.ExchangeRate : 1m
                    }
                );

            baseSalePaymentStatusRepository.SetSalePaymentStatusTypeById(SalePaymentStatusType.Paid, sale.BaseSalePaymentStatusId);
            saleRepository.UpdateIsAcceptedToPacking(sale.Id, true);
        } else {
            if (clientAgreement.CurrentAmount > decimal.Zero) {
                totalEuro = decimal.Round(totalEuro - clientAgreement.CurrentAmount, 14, MidpointRounding.AwayFromZero);

                clientBalanceMovementRepository
                    .AddOutMovement(
                        new ClientBalanceMovement {
                            ClientAgreementId = clientAgreement.Id,
                            Amount = clientAgreement.CurrentAmount,
                            ExchangeRateAmount = !firstItem.ExchangeRate.Equals(decimal.Zero) ? firstItem.ExchangeRate : 1m
                        }
                    );

                clientAgreement.CurrentAmount = decimal.Zero;

                clientAgreementRepository.UpdateAmountByNetId(clientAgreement.NetUid, clientAgreement.CurrentAmount);

                baseSalePaymentStatusRepository
                    .SetSalePaymentStatusTypeById(SalePaymentStatusType.PartialPaid, sale.BaseSalePaymentStatusId);

                if (!clientAgreement.Agreement.Currency.Code.Equals("EUR"))
                    total = totalEuro * firstItem.ExchangeRate;
                else
                    total = totalEuro;
            }

            if (clientInDebtFromDb != null) {
                clientInDebtFromDb.Debt.Total = total;

                debtRepository.Update(clientInDebtFromDb.Debt);
            } else {
                Debt debt = new() {
                    Days = 0,
                    Total = total,
                    Created = sale.ChangedToInvoice ?? DateTime.Now
                };

                ClientInDebt clientInDebt = new() {
                    AgreementId = clientAgreement.AgreementId,
                    ClientId = clientAgreement.ClientId,
                    DebtId = debtRepository.AddWithCreatedDate(debt),
                    SaleId = sale.Id
                };

                clientInDebtRepository.Add(clientInDebt);
            }
        }
    }

    private async Task<string> SyncIncomeBankCashOrders(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        IDbConnection remoteConnection,
        DateTime fromDate,
        DateTime toDate,
        User currentUser,
        bool forAmg,
        bool isBankOrder) {
        List<string> exceptionList = new();

        IEnumerable<SyncIncomeCashBankOrder> syncIncomePaymentOrders = new List<SyncIncomeCashBankOrder>();

        if (isBankOrder)
            syncIncomePaymentOrders = forAmg
                ? outcomeOrdersSyncRepository.GetAmgFilteredSyncIncomeBankOrders(fromDate, toDate)
                : outcomeOrdersSyncRepository.GetFilteredSyncIncomeBankOrders(fromDate, toDate);
        else
            syncIncomePaymentOrders = forAmg
                ? outcomeOrdersSyncRepository.GetAmgFilteredSyncIncomeCashOrders(fromDate, toDate)
                : outcomeOrdersSyncRepository.GetFilteredSyncIncomeCashOrders(fromDate, toDate);


        //var fromAmg = outcomeOrdersSyncRepository.GetAmgFilteredSyncInternalMovementCashOrders(fromDate, toDate);
        //var fromFenix = outcomeOrdersSyncRepository.GetFilteredSyncInternalMovementCashOrders(fromDate, toDate);

        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(remoteConnection);

        long[] clientCodes = syncIncomePaymentOrders
            .Where(e => e.ClientCode.HasValue)
            .Select(x => x.ClientCode.Value)
            .Distinct()
            .ToArray();

        IEnumerable<Client> clients = outcomeOrdersSyncRepository.GetClientsWithData(clientCodes, forAmg);
        IEnumerable<SupplyOrganization> supplyOrganizations = outcomeOrdersSyncRepository.GetSupplyOrganizationWithData(clientCodes, forAmg);
        IEnumerable<User> users = _userRepositoriesFactory.NewUserRepository(remoteConnection).GetAll();

        IEnumerable<Client> deletedClients = outcomeOrdersSyncRepository.GetDeletedClients();

        long[] mainClientIds = deletedClients.Where(x => x.MainClientId.HasValue).Select(x => x.MainClientId.Value).ToArray();

        IEnumerable<Client> mainClients = outcomeOrdersSyncRepository.GetClientsByIds(mainClientIds);

        IEnumerable<PaymentMovement> paymentMovements = outcomeOrdersSyncRepository.GetAllPaymentMovementOperations();

        foreach (SyncIncomeCashBankOrder syncIncomeOrder in syncIncomePaymentOrders) {
            PaymentMovement paymentMovement = paymentMovements.FirstOrDefault(x => x.OperationName == syncIncomeOrder.ArticlesOfMoneyAccounts);

            Currency ordersCurrency = currencyRepository.GetByOneCCode(syncIncomeOrder.CurrencyCode.ToString());

            syncIncomeOrder.CashPaymentRegister = syncIncomeOrder.CashPaymentRegister != null
                ? _cashRegisterNameReplace.Replace(syncIncomeOrder.CashPaymentRegister, string.Empty).Trim()
                : syncIncomeOrder.CashPaymentRegister;

            PaymentRegister paymentRegister = outcomeOrdersSyncRepository.GetPaymentRegister(syncIncomeOrder.OrganizationPaymentRegister, ordersCurrency.CodeOneC) ??
                                              outcomeOrdersSyncRepository.GetPaymentRegister(syncIncomeOrder.CashPaymentRegister, ordersCurrency.CodeOneC);

            double vatFromIncome = 0d;

            if (forAmg)
                vatFromIncome = syncIncomeOrder.VatTypeAmg switch {
                    SyncVatEnumAmg.FourTeen => 14,
                    SyncVatEnumAmg.Twenty => 20,
                    SyncVatEnumAmg.Seven => 7,
                    SyncVatEnumAmg.TwentyThree => 23,
                    _ => vatFromIncome
                };
            else
                vatFromIncome = syncIncomeOrder.VatTypeFenix switch {
                    SyncVatEnumFenix.Twenty => 20,
                    SyncVatEnumFenix.Seven => 7,
                    SyncVatEnumFenix.TwentyThree => 23,
                    _ => 0
                };

            IncomePaymentOrder newIncome = new() {
                Amount = syncIncomeOrder.TotalValue,
                VatPercent = vatFromIncome,
                VAT = syncIncomeOrder.TotalVat,
                Organization = outcomeOrdersSyncRepository.GetOrganizationByName(syncIncomeOrder.Organization),
                Currency = ordersCurrency,
                PaymentRegister = paymentRegister,
                FromDate = syncIncomeOrder.FromDate.AddYears(-2000).ToUniversalTime(),
                IsManagementAccounting = syncIncomeOrder.IsManagementAccounting,
                IsAccounting = syncIncomeOrder.IsAccounting,
                OperationType = syncIncomeOrder.TypeOperation,
                PaymentPurpose = syncIncomeOrder.PaymentPurpose,
                Comment = syncIncomeOrder.Comment,
                ArrivalNumber = syncIncomeOrder.DocumentArrivalNumber,
                Number = syncIncomeOrder.Number
                // Income has no arrival date
            };

            if (paymentMovement != null)
                newIncome.PaymentMovementOperation = new PaymentMovementOperation {
                    PaymentMovement = paymentMovement,
                    PaymentMovementId = paymentMovement?.Id ?? 0
                };

            if (newIncome.Organization == null) continue;
            if (newIncome.PaymentRegister == null) continue;
            if (newIncome.Currency == null) continue;

            Client client = GetClientForDocumentIfExists(forAmg, clients, syncIncomeOrder.ClientCode, syncIncomeOrder.AgreementCode);

            if (client == null || client.Deleted) {
                client = deletedClients.FirstOrDefault(x => forAmg ? x.SourceAmgCode == syncIncomeOrder.ClientCode : x.SourceFenixCode == syncIncomeOrder.ClientCode);

                if (client != null && client.MainClientId.HasValue)
                    client = mainClients.FirstOrDefault(x => x.Id == client.MainClientId.Value);
            }

            if (client == null) {
                SupplyOrganization supplyOrganization =
                    supplyOrganizations.FirstOrDefault(s => forAmg ? s.SourceAmgCode == syncIncomeOrder.ClientCode : s.SourceFenixCode == syncIncomeOrder.ClientCode);

                if (supplyOrganization != null) {
                    newIncome.SupplyOrganization = supplyOrganization;
                    newIncome.SupplyOrganizationAgreement = supplyOrganization.SupplyOrganizationAgreements.FirstOrDefault(s =>
                        forAmg ? s.SourceAmgCode == syncIncomeOrder.AgreementCode : s.SourceFenixCode == syncIncomeOrder.AgreementCode);
                }
            } else {
                ClientAgreement clientAgreement = client.ClientAgreements.FirstOrDefault(x =>
                    forAmg ? x.Agreement.SourceAmgCode == syncIncomeOrder.AgreementCode : x.Agreement.SourceFenixCode == syncIncomeOrder.AgreementCode);

                newIncome.Client = client;
                newIncome.ClientAgreement = clientAgreement;
            }

            if (newIncome.OperationType.Equals(OperationType.ReturnFromColleague)) {
                string[] userName = syncIncomeOrder.FromUser.Split(' ');

                User colleague = users.FirstOrDefault(user =>
                    user.LastName.Equals(userName[0]) &&
                    user.FirstName.Equals(userName[1]) &&
                    user.MiddleName.Equals(userName[2]));
                newIncome.Colleague = colleague;
            }

            (IncomePaymentOrder resultIncome, string errorMessage) =
                await ActorReferenceManager.Instance.Get(PaymentOrdersActorNames.INCOME_PAYMENT_ORDER_ACTOR).Ask<Tuple<IncomePaymentOrder, string>>(
                    new AddNewIncomePaymentOrderMessage(newIncome, true, currentUser.NetUid),
                    TimeSpan.FromSeconds(30)
                );

            if (!string.IsNullOrEmpty(errorMessage)) exceptionList.Add(errorMessage);
        }

        return exceptionList.Any()
            ? GetCurrentDateInString() + _localizer[SharedResourceNames.ORDERS_SYNC_EXCEPTION_LIST] + exceptionList.Join(", ")
            : string.Empty;
    }

    private async Task<string> SyncInternalMovementOfFunds(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        IDbConnection remoteConnection,
        DateTime fromDate,
        DateTime toDate,
        User currentUser,
        bool forAmg,
        bool isBankOrder) {
        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(remoteConnection);
        IAccountingSyncRepository accountingSyncRepository = _dataSyncRepositoriesFactory.NewAccountingSyncRepository(remoteConnection, remoteConnection, remoteConnection);

        List<PaymentRegisterTransfer> paymentRegisterTransfers = new();
        IEnumerable<SyncIncomeCashBankOrder> syncIncomePaymentOrders = new List<SyncIncomeCashBankOrder>();

        syncIncomePaymentOrders = forAmg
            ? outcomeOrdersSyncRepository.GetAmgFilteredSyncInternalMovementCashOrders(fromDate, toDate)
            : outcomeOrdersSyncRepository.GetFilteredSyncInternalMovementCashOrders(fromDate, toDate);

        List<PaymentRegisterTransfer> allPaymentRegisterTransfer = await ActorReferenceManager.Instance.Get(PaymentOrdersActorNames.BASE_PAYMENT_REGISTER_TRANSFER_GET_ACTOR)
            .Ask<List<PaymentRegisterTransfer>>(
                new GetAllPaymentRegisterTransfersByPaymentRegisterNetIdMessage(
                    DateTime.Now.AddYears(-20),
                    DateTime.Now.AddYears(20),
                    PaymentRegisterTransferType.All,
                    null,
                    null
                )
            );

        IEnumerable<User> users = _userRepositoriesFactory.NewUserRepository(remoteConnection).GetAll();

        List<PaymentRegister> paymentRegisters =
            _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(remoteConnection).GetAll(null, "", null);

        IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository =
            _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(remoteConnection);

        List<PaymentCurrencyRegister> PaymentCurrencyRegister =
            paymentCurrencyRegisterRepository.GetAll();

        foreach (PaymentCurrencyRegister payment in PaymentCurrencyRegister)
        foreach (PaymentRegister paymentReg in paymentRegisters)
            if (payment.PaymentRegisterId == paymentReg.Id)
                payment.PaymentRegister = paymentReg;

        foreach (SyncIncomeCashBankOrder sync in syncIncomePaymentOrders) {
            if (allPaymentRegisterTransfer.Any(x => x.Number == sync.Number)) continue;

            PaymentMovement paymentMovementDev = accountingSyncRepository.GetDevPaymentMovement(sync.ArticlesOfMoneyAccounts);

            PaymentRegisterTransfer paymentRegisterTransfer = new() {
                Amount = sync.TotalValue,
                Comment = sync.Comment,
                Type = PaymentRegisterTransferType.Income,
                TypeOfOperation = TransferOperationType.FundsTransfer,
                Number = sync.Number,
                PaymentMovementOperation = new PaymentMovementOperation(),
                FromDate = ConvertDateTimeToUtcInUkraineTimeZone(sync.FromDate.AddYears(-2000))
            };

            paymentRegisterTransfer.PaymentMovementOperation.PaymentMovement = paymentMovementDev;

            string[] cashPaymentRegister = sync.CashPaymentRegister.Split(" (");
            string[] clientPaymentRegister = sync.ClientPaymentRegister.Split(" (");
            string[] userInfo = sync.Responsible.Split(' ');

            foreach (User user in users)
                if (userInfo[0] == user.LastName && userInfo[1] == user.FirstName && userInfo[2] == user.MiddleName) {
                    paymentRegisterTransfer.User = user;
                    paymentRegisterTransfer.UserId = user.Id;
                }

            if (paymentRegisterTransfer.User == null) paymentRegisterTransfer.User = currentUser;

            foreach (PaymentCurrencyRegister payments in PaymentCurrencyRegister) {
                if (cashPaymentRegister.FirstOrDefault() == payments.PaymentRegister.Name && sync.CurrencyCode.ToString() == payments.Currency.CodeOneC)
                    paymentRegisterTransfer.FromPaymentCurrencyRegister = payments;

                if (clientPaymentRegister.FirstOrDefault() == payments.PaymentRegister.Name && sync.CurrencyCode.ToString() == payments.Currency.CodeOneC)
                    paymentRegisterTransfer.ToPaymentCurrencyRegister = payments;
            }

            paymentRegisterTransfers.Add(paymentRegisterTransfer);
        }

        paymentRegisterTransfers.ForEach(x => AddNewPaymentRegisterTransferMessage(x, x.User.NetUid, remoteConnection));
        return string.Empty;
    }

    private void AddNewPaymentRegisterTransferMessage(PaymentRegisterTransfer PaymentRegisterTransfer, Guid UserNetId, IDbConnection remoteConnection) {
        if (PaymentRegisterTransfer == null) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterTransferResourceNames.PAYMENT_REGISTER_TRANSFER_EMPTY, false, null));
        } else if (!PaymentRegisterTransfer.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterTransferResourceNames.PAYMENT_REGISTER_TRANSFER_EMPTY, false, null));
        } else if (PaymentRegisterTransfer.FromPaymentCurrencyRegister == null && PaymentRegisterTransfer.FromPaymentCurrencyRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.FROM_REGISTER_NOT_SPECIFIED, false, null));
        } else if (PaymentRegisterTransfer.ToPaymentCurrencyRegister == null && PaymentRegisterTransfer.ToPaymentCurrencyRegisterId.Equals(0)) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.TO_REGISTER_NOT_SPECIFIED, false, null));
        } else if (PaymentRegisterTransfer.Amount <= decimal.Zero) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.AMOUNT_NOT_SPECIFIED, false, null));
        } else if (PaymentRegisterTransfer.FromPaymentCurrencyRegister != null && PaymentRegisterTransfer.FromPaymentCurrencyRegister.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(
                PaymentRegisterTransferResourceNames.PAYMENT_CURRENCY_REGISTER_IS_NEW, false, null));
        } else if (PaymentRegisterTransfer.ToPaymentCurrencyRegister != null && PaymentRegisterTransfer.ToPaymentCurrencyRegister.IsNew()) {
            Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(
                PaymentRegisterTransferResourceNames.PAYMENT_CURRENCY_REGISTER_IS_NEW, false, null));
        } else {
            if (PaymentRegisterTransfer.FromPaymentCurrencyRegister != null)
                PaymentRegisterTransfer.FromPaymentCurrencyRegisterId = PaymentRegisterTransfer.FromPaymentCurrencyRegister.Id;
            if (PaymentRegisterTransfer.ToPaymentCurrencyRegister != null)
                PaymentRegisterTransfer.ToPaymentCurrencyRegisterId = PaymentRegisterTransfer.ToPaymentCurrencyRegister.Id;
            if (PaymentRegisterTransfer.FromPaymentCurrencyRegisterId.Equals(PaymentRegisterTransfer.ToPaymentCurrencyRegisterId)) {
                Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterTransferResourceNames.CAN_NOT_TRANSFER_MONEY, false, null));
            } else {
                IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository =
                    _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(remoteConnection);

                PaymentCurrencyRegister fromToPaymentCurrencyRegister =
                    paymentCurrencyRegisterRepository.GetById(PaymentRegisterTransfer.FromPaymentCurrencyRegisterId);
                PaymentCurrencyRegister toToPaymentCurrencyRegister =
                    paymentCurrencyRegisterRepository.GetById(PaymentRegisterTransfer.ToPaymentCurrencyRegisterId);

                if (fromToPaymentCurrencyRegister == null || toToPaymentCurrencyRegister == null) {
                    Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.TO_CURRENCY_NOT_SPECIFIED, false, null));
                } else if (!fromToPaymentCurrencyRegister.CurrencyId.Equals(toToPaymentCurrencyRegister.CurrencyId)) {
                    Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.FROM_CURRENCY_NOT_SPECIFIED, false, null));
                } else {
                    IPaymentRegisterTransferRepository paymentRegisterTransferRepository =
                        _paymentOrderRepositoriesFactory.NewPaymentRegisterTransferRepository(remoteConnection);

                    PaymentRegisterTransfer.UserId = _userRepositoriesFactory.NewUserRepository(remoteConnection).GetByNetIdWithoutIncludes(UserNetId).Id;

                    PaymentRegisterTransfer lastRecord = paymentRegisterTransferRepository.GetLastRecord();

                    PaymentRegisterTransfer.Number = PaymentRegisterTransfer.Number;

                    //PaymentRegisterTransfer.FromDate = DateTime.UtcNow;

                    PaymentRegisterTransfer.Id = paymentRegisterTransferRepository.Add(PaymentRegisterTransfer);

                    if (PaymentRegisterTransfer.PaymentMovementOperation != null &&
                        (!PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovementId.Equals(0) ||
                         PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovement != null)
                       ) {
                        if (PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovement != null)
                            PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovementId =
                                PaymentRegisterTransfer.PaymentMovementOperation.PaymentMovement.Id;

                        PaymentRegisterTransfer.PaymentMovementOperation.PaymentRegisterTransferId = PaymentRegisterTransfer.Id;

                        _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(remoteConnection)
                            .Add(PaymentRegisterTransfer.PaymentMovementOperation);
                    }

                    fromToPaymentCurrencyRegister.Amount = Math.Round(fromToPaymentCurrencyRegister.Amount - PaymentRegisterTransfer.Amount, 2);
                    toToPaymentCurrencyRegister.Amount = Math.Round(toToPaymentCurrencyRegister.Amount + PaymentRegisterTransfer.Amount, 2);

                    paymentCurrencyRegisterRepository.UpdateAmount(fromToPaymentCurrencyRegister);
                    paymentCurrencyRegisterRepository.UpdateAmount(toToPaymentCurrencyRegister);

                    Sender.Tell(new Tuple<string, bool, PaymentRegisterTransfer>(PaymentRegisterResourceNames.TRANSFERED_SUCCESSFUL, true,
                        paymentRegisterTransferRepository.GetById(PaymentRegisterTransfer.Id)));
                }
            }
        }
    }

    private async Task<string> SyncOutcomeBankCashOrders(
        IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
        IDbConnection remoteConnection,
        DateTime fromDate,
        DateTime toDate,
        User currentUser,
        bool forAmg,
        bool isBankOrder) {
        List<string> exceptionList = new();
        IEnumerable<SyncOutcomeCashBankOrder> syncOutcomePaymentOrders = new List<SyncOutcomeCashBankOrder>();

        if (isBankOrder)
            syncOutcomePaymentOrders = forAmg
                ? outcomeOrdersSyncRepository.GetAmgFilteredSyncOutcomeBankOrders(fromDate, toDate)
                : outcomeOrdersSyncRepository.GetFilteredSyncOutcomeBankOrders(fromDate, toDate);
        else
            syncOutcomePaymentOrders = forAmg
                ? outcomeOrdersSyncRepository.GetAmgFilteredSyncOutcomeCashOrders(fromDate, toDate)
                : outcomeOrdersSyncRepository.GetFilteredSyncOutcomeCashOrders(fromDate, toDate);

        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(remoteConnection);

        long[] clientCodes = syncOutcomePaymentOrders
            .Where(e => e.ClientCode.HasValue)
            .Select(x => x.ClientCode.Value)
            .Distinct()
            .ToArray();

        IEnumerable<Client> clients = outcomeOrdersSyncRepository.GetClientsWithData(clientCodes, forAmg);
        IEnumerable<SupplyOrganization> supplyOrganizations = outcomeOrdersSyncRepository.GetSupplyOrganizationWithData(clientCodes, forAmg);
        IEnumerable<User> users = _userRepositoriesFactory.NewUserRepository(remoteConnection).GetAll();

        IEnumerable<PaymentMovement> paymentMovements = outcomeOrdersSyncRepository.GetAllPaymentMovementOperations();

        IEnumerable<Client> deletedClients = outcomeOrdersSyncRepository.GetDeletedClients();

        long[] mainClientIds = deletedClients.Where(x => x.MainClientId.HasValue).Select(x => x.MainClientId.Value).ToArray();

        IEnumerable<Client> mainClients = outcomeOrdersSyncRepository.GetClientsByIds(mainClientIds);

        foreach (SyncOutcomeCashBankOrder syncOutcomeOrder in syncOutcomePaymentOrders) {
            PaymentMovement paymentMovement = paymentMovements.FirstOrDefault(x => x.OperationName == syncOutcomeOrder.ArticlesOfMoneyAccounts);

            Currency ordersCurrency = currencyRepository.GetByOneCCode(syncOutcomeOrder.CurrencyCode.ToString());

            syncOutcomeOrder.CashPaymentRegister = !string.IsNullOrEmpty(syncOutcomeOrder.CashPaymentRegister)
                ? _cashRegisterNameReplace.Replace(syncOutcomeOrder.CashPaymentRegister, string.Empty).Trim()
                : string.Empty;
            PaymentRegister paymentRegister = outcomeOrdersSyncRepository.GetPaymentRegister(syncOutcomeOrder.PaymentRegisterOrganization, ordersCurrency.CodeOneC) ??
                                              outcomeOrdersSyncRepository.GetPaymentRegister(syncOutcomeOrder.CashPaymentRegister, ordersCurrency.CodeOneC);

            if (paymentRegister == null) {
                exceptionList.Add(syncOutcomeOrder.Number);
                continue;
            }

            PaymentCurrencyRegister paymentCurrencyRegister =
                paymentRegister.PaymentCurrencyRegisters.First(r =>
                    r.CurrencyId.Equals(ordersCurrency.Id));

            double vatFromOutcome = 0d;

            if (forAmg)
                vatFromOutcome = syncOutcomeOrder.VatTypeAmg switch {
                    SyncVatEnumAmg.FourTeen => 14,
                    SyncVatEnumAmg.Twenty => 20,
                    SyncVatEnumAmg.Seven => 7,
                    SyncVatEnumAmg.TwentyThree => 23,
                    _ => vatFromOutcome
                };
            else
                vatFromOutcome = syncOutcomeOrder.VatTypeFenix switch {
                    SyncVatEnumFenix.Twenty => 20,
                    SyncVatEnumFenix.Seven => 7,
                    SyncVatEnumFenix.TwentyThree => 23,
                    _ => 0
                };

            decimal vatAmount = Math.Round(syncOutcomeOrder.TotalValue * Convert.ToDecimal(vatFromOutcome) / (100 + Convert.ToDecimal(vatFromOutcome)), 2);

            OutcomePaymentOrder newOutcome = new() {
                Amount = syncOutcomeOrder.TotalValue,
                VatPercent = vatFromOutcome,
                VAT = vatAmount,
                Organization = outcomeOrdersSyncRepository.GetOrganizationByName(syncOutcomeOrder.Organization),
                IsUnderReport = syncOutcomeOrder.TypeOperation == OperationType.TransferToColleague,
                IsAccounting = syncOutcomeOrder.IsAccounting,
                IsManagementAccounting = syncOutcomeOrder.IsManagementAccounting,
                PaymentCurrencyRegister = paymentCurrencyRegister,
                FromDate = syncOutcomeOrder.FromDate.AddYears(-2000).ToUniversalTime(),
                Number = syncOutcomeOrder.Number,
                AdvanceNumber = syncOutcomeOrder.TypeOperation == OperationType.TransferToColleague
                    ? syncOutcomeOrder.Number
                    : string.Empty,
                Comment = syncOutcomeOrder.Comment,
                PaymentPurpose = syncOutcomeOrder.PaymentPurpose,
                User = currentUser,
                UserId = currentUser.Id,
                OperationType = syncOutcomeOrder.TypeOperation
            };

            if (paymentMovement != null)
                newOutcome.PaymentMovementOperation = new PaymentMovementOperation {
                    PaymentMovement = paymentMovement
                };

            if (newOutcome.Organization == null) continue;
            if (newOutcome.PaymentCurrencyRegister == null) continue;

            Client client = GetClientForDocumentIfExists(forAmg, clients, syncOutcomeOrder.ClientCode, syncOutcomeOrder.AgreementCode);

            if (client == null || client.Deleted) {
                client = deletedClients.FirstOrDefault(x => forAmg ? x.SourceAmgCode == syncOutcomeOrder.ClientCode : x.SourceFenixCode == syncOutcomeOrder.ClientCode);

                if (client != null && client.MainClientId.HasValue)
                    client = mainClients.FirstOrDefault(x => x.Id == client.MainClientId.Value);
            }

            if (client == null) {
                SupplyOrganization supplyOrganization =
                    supplyOrganizations
                        .OrderBy(s => s.Deleted)
                        .FirstOrDefault(s => forAmg ? s.SourceAmgCode == syncOutcomeOrder.ClientCode : s.SourceFenixCode == syncOutcomeOrder.ClientCode);

                if (supplyOrganization != null) {
                    newOutcome.ConsumableProductOrganization = supplyOrganization;
                    newOutcome.SupplyOrganizationAgreement = supplyOrganization.SupplyOrganizationAgreements
                        .OrderBy(s => s.Deleted)
                        .FirstOrDefault(s =>
                            forAmg ? s.SourceAmgCode == syncOutcomeOrder.AgreementCode : s.SourceFenixCode == syncOutcomeOrder.AgreementCode);
                }
            } else {
                ClientAgreement clientAgreement = client.ClientAgreements.FirstOrDefault(x =>
                    forAmg ? x.Agreement.SourceAmgCode == syncOutcomeOrder.AgreementCode : x.Agreement.SourceFenixCode == syncOutcomeOrder.AgreementCode);

                newOutcome.Client = client;
                newOutcome.ClientAgreement = clientAgreement;
            }

            if (newOutcome.OperationType.Equals(OperationType.TransferToColleague)) {
                string[] userName = syncOutcomeOrder.EmployeeName.Split(' ');

                User colleague = users
                    .OrderBy(user => user.Deleted)
                    .FirstOrDefault(user =>
                        user.LastName.Equals(userName[0]) &&
                        user.FirstName.Equals(userName[1]) &&
                        user.MiddleName.Equals(userName[2]));
                newOutcome.Colleague = colleague;
            }

            if (newOutcome.OperationType == OperationType.CurrencyTransfering) {
                string exceptionMessage = CreateOutcomePaymentOrderForCurrencyTransfer(
                    newOutcome,
                    currencyRepository,
                    _exchangeRateRepositoriesFactory.NewExchangeRateRepository(remoteConnection),
                    _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(remoteConnection),
                    _paymentOrderRepositoriesFactory.NewOutcomePaymentOrderRepository(remoteConnection),
                    _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(remoteConnection),
                    _paymentOrderRepositoriesFactory.NewPaymentMovementOperationRepository(remoteConnection)
                );

                if (!string.IsNullOrEmpty(exceptionMessage)) exceptionList.Add(exceptionMessage);
            } else {
                (OutcomePaymentOrder outcome, string errorMessage) =
                    await ActorReferenceManager.Instance.Get(PaymentOrdersActorNames.OUTCOME_PAYMENT_ORDER_ACTOR).Ask<Tuple<OutcomePaymentOrder, string>>(
                        new AddNewOutcomePaymentOrderMessage(newOutcome, currentUser.NetUid),
                        TimeSpan.FromSeconds(30)
                    );

                if (!string.IsNullOrEmpty(errorMessage)) exceptionList.Add(newOutcome.Number);
            }
        }

        return exceptionList.Any()
            ? GetCurrentDateInString() + _localizer[SharedResourceNames.ORDERS_SYNC_EXCEPTION_LIST] + exceptionList.Join(", ") + Environment.NewLine
            : string.Empty;
    }

    private static string CreateOutcomePaymentOrderForCurrencyTransfer(
        OutcomePaymentOrder outcomePaymentOrder,
        ICurrencyRepository currencyRepository,
        IExchangeRateRepository exchangeRateRepository,
        ICrossExchangeRateRepository crossExchangeRateRepository,
        IOutcomePaymentOrderRepository outcomePaymentOrderRepository,
        IPaymentCurrencyRegisterRepository paymentCurrencyRegisterRepository,
        IPaymentMovementOperationRepository paymentMovementRepository) {
        if (outcomePaymentOrder.Organization != null) outcomePaymentOrder.OrganizationId = outcomePaymentOrder.Organization.Id;
        if (outcomePaymentOrder.PaymentCurrencyRegister != null)
            outcomePaymentOrder.PaymentCurrencyRegisterId = outcomePaymentOrder.PaymentCurrencyRegister.Id;
        if (outcomePaymentOrder.FromDate.Year.Equals(1)) outcomePaymentOrder.FromDate = DateTime.UtcNow;

        PaymentCurrencyRegister paymentCurrencyRegister = paymentCurrencyRegisterRepository.GetById(outcomePaymentOrder.PaymentCurrencyRegisterId);

        if (paymentCurrencyRegister.Amount < outcomePaymentOrder.Amount) return outcomePaymentOrder.Number;

        paymentCurrencyRegister.Amount = Math.Round(paymentCurrencyRegister.Amount - outcomePaymentOrder.Amount, 2);

        paymentCurrencyRegisterRepository.UpdateAmount(paymentCurrencyRegister);

        if (outcomePaymentOrder.EuroAmount.Equals(decimal.Zero)) {
            Currency euroCurrency = currencyRepository.GetEURCurrencyIfExists();

            PaymentOrdersCurrencyConvertor paymentOrdersCurrencyConvertor = new(
                euroCurrency,
                paymentCurrencyRegister.Currency,
                outcomePaymentOrder.FromDate,
                exchangeRateRepository,
                crossExchangeRateRepository);

            outcomePaymentOrder.EuroAmount = paymentOrdersCurrencyConvertor.ConvertAmountToEuro(outcomePaymentOrder.Amount);
        }

        outcomePaymentOrder.Id = outcomePaymentOrderRepository.Add(outcomePaymentOrder);

        if (outcomePaymentOrder.PaymentMovementOperation.PaymentMovement != null)
            outcomePaymentOrder.PaymentMovementOperation.PaymentMovementId =
                outcomePaymentOrder.PaymentMovementOperation.PaymentMovement.Id;

        outcomePaymentOrder.PaymentMovementOperation.OutcomePaymentOrderId = outcomePaymentOrder.Id;

        paymentMovementRepository.Add(outcomePaymentOrder.PaymentMovementOperation);

        return string.Empty;
    }

    private static Client GetClientForDocumentIfExists(bool forAmg, IEnumerable<Client> clients, long? clientCode, long? agreementCode) {
        Client client = clients.OrderBy(c => c.Deleted).FirstOrDefault(c => forAmg
            ? c.SourceAmgCode == clientCode && c.ClientAgreements.Any(ca => ca.Agreement.SourceAmgCode == agreementCode)
            : c.SourceFenixCode == clientCode && c.ClientAgreements.Any(ca => ca.Agreement.SourceFenixCode == agreementCode));

        if (client == null)
            client = clients.FirstOrDefault(c => forAmg
                ? c.SourceAmgCode.Equals(clientCode)
                : c.SourceFenixCode.Equals(clientCode));

        return client;
    }

    //private static IEnumerable<PaymentMovement> GetPaymentMovementsOrSyncIfThereAreNone(IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository, string[] paymentMovementsFromOneC) {
    //    IEnumerable<PaymentMovement> paymentMovements = outcomeOrdersSyncRepository.GetAllPaymentMovementOperations();

    //    string[] paymentMovementsFromGba = paymentMovements.Select(x => x.OperationName).ToArray();

    //    IEnumerable<string> newPaymentMovements = paymentMovementsFromOneC.Distinct().Where(name => !paymentMovementsFromGba.Contains(name));

    //    if (newPaymentMovements.Any()) {
    //        foreach (string newPaymentMovement in newPaymentMovements) {
    //            PaymentMovement paymentMovement = new PaymentMovement() {
    //                Created = DateTime.UtcNow,
    //                Updated = DateTime.UtcNow,
    //                OperationName = newPaymentMovement
    //            };

    //            paymentMovement.Id = outcomeOrdersSyncRepository.AddPaymentMovement(paymentMovement);

    //            PaymentMovementTranslation paymentMovementTranslation = new PaymentMovementTranslation() {
    //                Created = DateTime.UtcNow,
    //                Updated = DateTime.UtcNow,
    //                CultureCode = "uk",
    //                Name = newPaymentMovement,
    //                PaymentMovementId = paymentMovement.Id
    //            };

    //            outcomeOrdersSyncRepository.AddPaymentMovementTranslation(paymentMovementTranslation);
    //        }

    //        paymentMovements = outcomeOrdersSyncRepository.GetAllPaymentMovementOperations();
    //    }

    //    return paymentMovements;
    //}

    // private async Task SyncSaleReturn(
    //     IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
    //     IClientRepository clientRepository,
    //     ClientAgreement clientAgreement,
    //     SyncSettlement syncSettlement,
    //     User user,
    //     bool forAmg) {
    //     IEnumerable<SyncSaleReturnItem> saleReturnItems =
    //         forAmg
    //             ? outcomeOrdersSyncRepository.GetAmgSaleReturnItemsBySourceId(syncSettlement.DocumentRef)
    //             : outcomeOrdersSyncRepository.GetSaleReturnItemsBySourceId(syncSettlement.DocumentRef);
    //
    //     if (!saleReturnItems.Any()) return;
    //
    //     SaleReturn saleReturn = null;
    //
    //     foreach (SyncSaleReturnItem syncSaleReturnItem in saleReturnItems) {
    //         Product product =
    //             outcomeOrdersSyncRepository.GetProductBySourceCodeWithIncludes(syncSaleReturnItem.ProductCode, forAmg);
    //
    //         if (product == null) continue;
    //
    //         Client client = clientRepository.GetByIdWithRegionCode(clientAgreement.ClientId);
    //
    //         if (saleReturn == null) {
    //             saleReturn = new SaleReturn {
    //                 CreatedById = user.Id,
    //                 Number = syncSaleReturnItem.DocumentNumber,
    //                 FromDate = syncSaleReturnItem.DocumentDate.AddYears(-2000),
    //                 ClientId = client.Id,
    //                 Client = client
    //             };
    //         }
    //
    //         OrderItem orderItem =
    //             outcomeOrdersSyncRepository
    //                 .GetOrderItemBySaleNumberAndProductCode(
    //                     syncSaleReturnItem.SaleNumber,
    //                     syncSaleReturnItem.ProductCode
    //                 );
    //
    //         Storage storage = outcomeOrdersSyncRepository.GetStorageByName(syncSaleReturnItem.Storage);
    //
    //         saleReturn.SaleReturnItems.Add(new SaleReturnItem {
    //             Qty = syncSaleReturnItem.Quantity,
    //             SaleReturnItemStatus = SaleReturnItemStatus.Defect, // ?
    //             Storage = storage,
    //             OrderItem = orderItem
    //         });
    //     }
    //
    //     Tuple<SaleReturn, string> result =
    //         await ActorReferenceManager.Instance.Get(SaleReturnsActorNames.SALE_RETURNS_ACTOR)
    //             .Ask<Tuple<SaleReturn, string>>(new AddNewSaleReturnMessage(saleReturn, user.NetUid));
    // }
    //

    //
    // private async Task SyncSale(
    //     IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
    //     ITransporterRepository transporterRepository,
    //     IDeliveryRecipientRepository deliveryRecipientRepository,
    //     ClientAgreement clientAgreement,
    //     SyncSettlement syncSettlement,
    //     User currentUser,
    //     bool forAmg) {
    //     IEnumerable<SyncSaleItem> saleItems =
    //         forAmg
    //             ? outcomeOrdersSyncRepository.GetAmgSaleItemsBySourceId(syncSettlement.DocumentRef)
    //             : outcomeOrdersSyncRepository.GetSaleItemsBySourceId(syncSettlement.DocumentRef);
    //
    //     if (!saleItems.Any()) return;
    //
    //     Sale sale = null;
    //
    //     foreach (SyncSaleItem saleItem in saleItems) {
    //         Product product = outcomeOrdersSyncRepository.GetProductBySourceCodeWithIncludes(saleItem.ProductCode, forAmg);
    //
    //         if (product == null) continue;
    //
    //         if (sale == null) {
    //             OrderItem orderItem = new OrderItem {
    //                 ProductId = product.Id,
    //                 Product = product,
    //                 Qty = saleItem.Quantity,
    //                 UserId = currentUser.Id,
    //                 Comment = _comment,
    //                 IsValidForCurrentSale = true,
    //             };
    //
    //             object result =
    //                 await ActorReferenceManager.Instance.Get(SalesActorNames.ORDER_ITEMS_ACTOR).Ask<object>(
    //                     new AddOrderItemMessage(orderItem, clientAgreement.NetUid, Guid.Empty, currentUser.NetUid, saleItem.DocumentNumber)
    //                 );
    //
    //             switch (result) {
    //                 case Exception exception:
    //                     throw exception;
    //                 case Tuple<OrderItem, string> tuple:
    //                     if (!string.IsNullOrEmpty(tuple.Item2)) throw new Exception(tuple.Item2);
    //
    //                     sale = outcomeOrdersSyncRepository.GetSaleByOrderItemId(tuple.Item1.Id);
    //                     break;
    //             }
    //         } else {
    //             OrderItem orderItem = new OrderItem {
    //                 ProductId = product.Id,
    //                 Product = product,
    //                 Qty = saleItem.Quantity,
    //                 UserId = currentUser.Id,
    //                 Comment = _comment,
    //                 IsValidForCurrentSale = true,
    //             };
    //
    //             object result =
    //                 await ActorReferenceManager.Instance.Get(SalesActorNames.ORDER_ITEMS_ACTOR).Ask<object>(
    //                     new AddOrderItemMessage(orderItem, clientAgreement.NetUid, sale.NetUid, currentUser.NetUid)
    //                 );
    //
    //             switch (result) {
    //                 case Exception exception:
    //                     throw exception;
    //                 case Tuple<OrderItem, string> tuple:
    //                     if (!string.IsNullOrEmpty(tuple.Item2)) throw new Exception(tuple.Item2);
    //
    //                     break;
    //             }
    //         }
    //     }
    //
    //     if (sale == null) return;
    //
    //     (SaleStatistic saleStatisticAfterAdd, string message) =
    //         await ActorReferenceManager.Instance.Get(SalesActorNames.BASE_SALES_GET_ACTOR).Ask<Tuple<SaleStatistic, string>>(
    //             new GetSaleStatisticWithResourceNameByNetIdMessage(
    //                 sale.NetUid,
    //                 SaleResourceNames.UPDATED,
    //                 false,
    //                 false
    //             )
    //         );
    //
    //     Sale saleToUpdate = saleStatisticAfterAdd.Sale;
    //
    //     DeliveryRecipient deliveryRecipient = deliveryRecipientRepository.GetAllRecipientsByClientNetId(saleToUpdate.ClientAgreement.Client.NetUid).FirstOrDefault();
    //
    //     if (deliveryRecipient != null) {
    //         saleToUpdate.DeliveryRecipient = deliveryRecipient;
    //         saleToUpdate.DeliveryRecipientId = deliveryRecipient.Id;
    //
    //         if (deliveryRecipient.DeliveryRecipientAddresses.Any()) {
    //             saleToUpdate.DeliveryRecipientAddress = deliveryRecipient.DeliveryRecipientAddresses.First();
    //             saleToUpdate.DeliveryRecipientAddress.Id = deliveryRecipient.DeliveryRecipientAddresses.First().Id;
    //         }
    //     }
    //
    //     Transporter transporter = transporterRepository.GetAll().OrderByDescending(t => t.Priority).FirstOrDefault();
    //
    //     if (transporter != null) {
    //         saleToUpdate.Transporter = transporter;
    //     }
    //
    //     saleToUpdate.BaseLifeCycleStatus = new BaseLifeCycleStatus { SaleLifeCycleType = SaleLifeCycleType.Packaging };
    //
    //     await ActorReferenceManager.Instance.Get(SalesActorNames.SALES_ACTOR)
    //         .Ask<Tuple<SaleStatistic, string>>(new UpdateSaleMessage(saleToUpdate, Guid.Parse(currentUser.NetUid.ToString())));
    // }
    //
    // private async Task SyncIncomePaymentOrder(
    //     IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
    //     ISaleRepository saleRepository,
    //     ClientAgreement clientAgreement,
    //     SyncSettlement syncSettlement,
    //     User user,
    //     bool forAmg) {
    //     SyncIncomePaymentOrder syncIncomePaymentOrder =
    //         forAmg
    //             ? outcomeOrdersSyncRepository.GetAmgIncomePaymentOrderBySourceId(syncSettlement.DocumentRef)
    //             : outcomeOrdersSyncRepository.GetIncomePaymentOrderBySourceId(syncSettlement.DocumentRef);
    //
    //     if (syncIncomePaymentOrder == null) return;
    //
    //     PaymentRegister register =
    //         outcomeOrdersSyncRepository.GetPaymentRegister(syncIncomePaymentOrder.OrganizationAccountName, syncIncomePaymentOrder.CurrencyCode)
    //         ??
    //         outcomeOrdersSyncRepository
    //             .GetPaymentRegister(
    //                 _cashRegisterNameReplace.Replace(syncIncomePaymentOrder.OrganizationAccountName, string.Empty).Trim(),
    //                 syncIncomePaymentOrder.CurrencyCode
    //             );
    //
    //     if (register == null) return;
    //
    //     syncIncomePaymentOrder.RateExchange = syncIncomePaymentOrder.RateExchange <= 0 ? 1 : syncIncomePaymentOrder.RateExchange;
    //
    //     IncomePaymentOrder incomePaymentOrder = new IncomePaymentOrder {
    //         Number = syncIncomePaymentOrder.DocumentNumber,
    //         FromDate = syncIncomePaymentOrder.DocumentDate.AddYears(-2000),
    //         AfterExchangeAmount = decimal.Round(syncIncomePaymentOrder.DocumentValue / syncIncomePaymentOrder.RateExchange, 4, MidpointRounding.AwayFromZero),
    //         ExchangeRate = syncIncomePaymentOrder.RateExchange,
    //         Comment = _comment + " " + syncIncomePaymentOrder.Comment,
    //         ClientAgreementId = clientAgreement.Id,
    //         ClientId = clientAgreement.ClientId,
    //         Amount = syncIncomePaymentOrder.DocumentValue,
    //         UserId = user.Id,
    //         PaymentRegisterId = register.Id,
    //         CurrencyId = register.PaymentCurrencyRegisters.First().CurrencyId,
    //         OrganizationId = clientAgreement.Agreement.OrganizationId ?? register.OrganizationId
    //     };
    //
    //     PaymentMovement movement =
    //         outcomeOrdersSyncRepository.GetPaymentMovementByName(syncIncomePaymentOrder.ArticleCashSpendingName);
    //
    //     if (movement == null) {
    //         movement = new PaymentMovement {
    //             OperationName = syncIncomePaymentOrder.ArticleCashSpendingName
    //         };
    //
    //         movement.Id = outcomeOrdersSyncRepository.Add(movement);
    //     }
    //
    //     PaymentMovementOperation paymentMovementOperation = new PaymentMovementOperation {
    //         IncomePaymentOrderId = incomePaymentOrder.Id,
    //         PaymentMovementId = movement.Id,
    //         PaymentMovement = movement
    //     };
    //
    //     incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
    //
    //     if (_articleCashSpendingNames.Contains(syncIncomePaymentOrder.ArticleCashSpendingName)) {
    //         IEnumerable<SyncIncomePaymentOrderSale> incomePaymentOrderSales = forAmg
    //             ? outcomeOrdersSyncRepository.GetAllAmgIncomePaymentOrderSalesBySourceId(syncIncomePaymentOrder.DocumentRef)
    //             : outcomeOrdersSyncRepository.GetAllIncomePaymentOrderSalesBySourceId(syncIncomePaymentOrder.DocumentRef, false);
    //
    //         foreach (SyncIncomePaymentOrderSale incomePaymentOrderSale in incomePaymentOrderSales.Where(i => !string.IsNullOrEmpty(i.SaleNumber))) {
    //             incomePaymentOrder.IncomePaymentOrderSales.Add(new IncomePaymentOrderSale {
    //                 Sale = saleRepository.GetSaleBySaleNumber(incomePaymentOrderSale.SaleNumber)
    //             });
    //         }
    //     }
    //
    //     (IncomePaymentOrder resultIncome, string errorMessage) =
    //         await ActorReferenceManager.Instance.Get(PaymentOrdersActorNames.INCOME_PAYMENT_ORDER_ACTOR).Ask<Tuple<IncomePaymentOrder, string>>(
    //             new AddNewIncomePaymentOrderMessage(incomePaymentOrder, false, user.NetUid),
    //             TimeSpan.FromSeconds(15)SyncOrganization
    //         );
    //
    //     if (!string.IsNullOrEmpty(errorMessage)) { }
    // }
    //
    // // This operation only for fenix
    // private async Task SyncIncomeCashOrder(
    //     IOutcomeOrdersSyncRepository outcomeOrdersSyncRepository,
    //     ISaleRepository saleRepository,
    //     ClientAgreement clientAgreement,
    //     SyncSettlement syncSettlement,
    //     User currentUser) {
    //     SyncIncomeCashOrder syncOrder =
    //         outcomeOrdersSyncRepository.GetIncomeCashOrderBySourceId(syncSettlement.DocumentRef);
    //
    //     if (syncOrder == null) return;
    //
    //     PaymentRegister register =
    //         outcomeOrdersSyncRepository.GetPaymentRegister(syncOrder.PaymentRegisterName, syncOrder.CurrencyCode)
    //         ??
    //         outcomeOrdersSyncRepository
    //             .GetPaymentRegister(
    //                 _cashRegisterNameReplace.Replace(syncOrder.PaymentRegisterName, string.Empty).Trim(),
    //                 syncOrder.CurrencyCode
    //             );
    //
    //     if (register == null) return;
    //
    //     syncOrder.RateExchange = syncOrder.RateExchange <= 0 ? 1 : syncOrder.RateExchange;
    //
    //     IncomePaymentOrder incomePaymentOrder = new IncomePaymentOrder {
    //         Number = syncOrder.DocumentNumber,
    //         FromDate = syncOrder.DocumentDate.AddYears(-2000),
    //         AfterExchangeAmount = decimal.Round(syncOrder.DocumentValue / syncOrder.RateExchange, 4, MidpointRounding.AwayFromZero),
    //         ExchangeRate = syncOrder.RateExchange,
    //         Comment = _comment + " " + syncOrder.Comment,
    //         ClientAgreementId = clientAgreement.Id,
    //         ClientId = clientAgreement.ClientId,
    //         Amount = syncOrder.DocumentValue,
    //         UserId = currentUser.Id,
    //         PaymentRegisterId = register.Id,
    //         CurrencyId = register.PaymentCurrencyRegisters.First().CurrencyId,
    //         OrganizationId = clientAgreement.Agreement.OrganizationId ?? register.OrganizationId
    //     };
    //
    //     PaymentMovement movement =
    //         outcomeOrdersSyncRepository.GetPaymentMovementByName(syncOrder.ArticleCashExpendingName);
    //
    //     if (movement == null) {
    //         movement = new PaymentMovement {
    //             OperationName = syncOrder.ArticleCashExpendingName
    //         };
    //
    //         movement.Id = outcomeOrdersSyncRepository.Add(movement);
    //     }
    //
    //     PaymentMovementOperation paymentMovementOperation = new PaymentMovementOperation {
    //         IncomePaymentOrderId = incomePaymentOrder.Id,
    //         PaymentMovementId = movement.Id,
    //         PaymentMovement = movement
    //     };
    //
    //     incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
    //
    //     if (_articleCashSpendingNames.Contains(syncOrder.ArticleCashExpendingName)) {
    //         IEnumerable<SyncIncomePaymentOrderSale> incomePaymentOrderSales =
    //             outcomeOrdersSyncRepository.GetAllIncomePaymentOrderSalesBySourceId(syncOrder.DocumentRef, true);
    //
    //         foreach (SyncIncomePaymentOrderSale incomePaymentOrderSale in incomePaymentOrderSales.Where(i => !string.IsNullOrEmpty(i.SaleNumber))) {
    //             incomePaymentOrder.IncomePaymentOrderSales.Add(new IncomePaymentOrderSale {
    //                 Sale = saleRepository.GetSaleBySaleNumber(incomePaymentOrderSale.SaleNumber)
    //             });
    //         }
    //     }
    //
    //     (IncomePaymentOrder resultIncome, string errorMessage) =
    //         await ActorReferenceManager.Instance.Get(PaymentOrdersActorNames.INCOME_PAYMENT_ORDER_ACTOR).Ask<Tuple<IncomePaymentOrder, string>>(
    //             new AddNewIncomePaymentOrderMessage(incomePaymentOrder, false, currentUser.NetUid)
    //             // TimeSpan.FromSeconds(15)
    //         );
    //
    //     if (!string.IsNullOrEmpty(errorMessage)) { }
    // }

    private static decimal GetGovExchangeRateOnDateToEur(
        Currency from,
        DateTime onDate,
        IGovCrossExchangeRateRepository govCrossExchangeRateRepository,
        IGovExchangeRateRepository govExchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        Currency eur = currencyRepository.GetEURCurrencyIfExists();
        Currency usd = currencyRepository.GetUSDCurrencyIfExists();

        if (from.Id.Equals(eur.Id))
            return 1m;

        if (from.Code.Equals(usd.Code)) {
            GovCrossExchangeRate govCrossExchangeRate =
                govCrossExchangeRateRepository
                    .GetByCurrenciesIds(eur.Id, from.Id, onDate);

            return govCrossExchangeRate?.Amount ?? 1m;
        }

        GovExchangeRate govExchangeRate =
            govExchangeRateRepository
                .GetByCurrencyIdAndCode(
                    eur.Id, from.Code, onDate)
            ??
            govExchangeRateRepository
                .GetByCurrencyIdAndCode(
                    from.Id, eur.Code, onDate);

        return govExchangeRate?.Amount ?? 1m;
    }

    private static decimal GetExchangeRateOnDateToEur(
        Currency from,
        DateTime onDate,
        ICrossExchangeRateRepository crossExchangeRateRepository,
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        Currency eur = currencyRepository.GetEURCurrencyIfExists();
        Currency usd = currencyRepository.GetUSDCurrencyIfExists();

        if (from.Id.Equals(eur.Id))
            return 1m;

        if (from.Code.Equals(usd.Code)) {
            CrossExchangeRate crossExchangeRate =
                crossExchangeRateRepository
                    .GetByCurrenciesIds(eur.Id, from.Id, onDate);

            return crossExchangeRate?.Amount ?? 1m;
        }

        ExchangeRate exchangeRate =
            exchangeRateRepository
                .GetByCurrencyIdAndCode(
                    eur.Id, from.Code, onDate)
            ??
            exchangeRateRepository
                .GetByCurrencyIdAndCode(
                    from.Id, eur.Code, onDate);

        return exchangeRate?.Amount ?? 1m;
    }

    private void ProcessUpdateSupplyManagePrice(
        SupplyInvoice invoice,
        decimal totalNetValue,
        decimal totalSpend,
        User currentUser,
        DateTime? dateConsignment,
        bool isTransferFromStorageThree,
        bool isIncomeManagementStorage,
        DateTime dateForExchange,
        DateTime? dateCustomDeclaration) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderRepository supplyOrderRepository =
                _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            ICurrencyRepository currencyRepository =
                _currencyRepositoriesFactory.NewCurrencyRepository(connection);
            IGovExchangeRateRepository govExchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);
            IExchangeRateRepository exchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);
            IGovCrossExchangeRateRepository govCrossExchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(connection);
            ICrossExchangeRateRepository crossExchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewCrossExchangeRateRepository(connection);

            Currency eur = currencyRepository.GetEURCurrencyIfExists();
            Currency uah = currencyRepository.GetUAHCurrencyIfExists();
            Currency pln = currencyRepository.GetPLNCurrencyIfExists();

            decimal govExchangeRateFromInvoiceAmount = 1;
            decimal govExchangeRateFromUahToEur = 1;

            if (isTransferFromStorageThree) {
                govExchangeRateFromInvoiceAmount =
                    GetGovExchangeRateOnDateToUah(
                        eur,
                        dateForExchange,
                        govExchangeRateRepository,
                        currencyRepository
                    );

                GovExchangeRate govExchangeRate =
                    govExchangeRateRepository
                        .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateForExchange);

                govExchangeRateFromUahToEur = govExchangeRate.Amount;
            } else if (isIncomeManagementStorage) {
                govExchangeRateFromInvoiceAmount =
                    GetExchangeRateOnDateToUah(
                        eur,
                        dateForExchange,
                        exchangeRateRepository,
                        currencyRepository
                    );

                ExchangeRate exchangeRate =
                    exchangeRateRepository
                        .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateForExchange);

                govExchangeRateFromUahToEur = exchangeRate.Amount;
            } else {
                govExchangeRateFromInvoiceAmount =
                    GetGovExchangeRateOnDateToUah(
                        eur,
                        dateConsignment ?? invoice.Created,
                        govExchangeRateRepository,
                        currencyRepository
                    );

                GovExchangeRate govExchangeRate =
                    govExchangeRateRepository
                        .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateConsignment ?? invoice.Created);

                govExchangeRateFromUahToEur = govExchangeRate.Amount;
            }

            foreach (PackingList packListFromDb in invoice.PackingLists)
            foreach (PackingListPackageOrderItem packingListItem in packListFromDb.PackingListPackageOrderItems)
                if (!packingListItem.ExchangeRateAmount.Equals(0))
                    packingListItem.UnitPriceUah =
                        govExchangeRateFromInvoiceAmount > 0
                            ? packingListItem.UnitPrice * packingListItem.ExchangeRateAmount
                            : Math.Abs(
                                packingListItem.UnitPrice / packingListItem.ExchangeRateAmount);
                else
                    packingListItem.UnitPriceUah = packingListItem.UnitPrice;

            Currency currencyFromInvoice = supplyOrderRepository.GetCurrencyByInvoice(invoice.Id);

            decimal documentValueExchangeRateAmount = 1;

            if (isTransferFromStorageThree)
                documentValueExchangeRateAmount = GetGovExchangeRateOnDateToEur(
                    currencyFromInvoice,
                    dateForExchange,
                    govCrossExchangeRateRepository,
                    govExchangeRateRepository,
                    currencyRepository);
            else if (isIncomeManagementStorage)
                documentValueExchangeRateAmount = GetExchangeRateOnDateToEur(
                    currencyFromInvoice,
                    dateForExchange,
                    crossExchangeRateRepository,
                    exchangeRateRepository,
                    currencyRepository);
            else
                documentValueExchangeRateAmount = GetGovExchangeRateOnDateToEur(
                    currencyFromInvoice,
                    dateCustomDeclaration ?? DateTime.Now,
                    govCrossExchangeRateRepository,
                    govExchangeRateRepository,
                    currencyRepository);

            decimal totalNetValueInEur = totalNetValue;

            if (!documentValueExchangeRateAmount.Equals(0)) {
                if (currencyFromInvoice.Code == uah.Code || currencyFromInvoice.Code == pln.Code)
                    totalNetValueInEur = totalNetValue / documentValueExchangeRateAmount;
                else
                    totalNetValueInEur =
                        documentValueExchangeRateAmount > 0
                            ? totalNetValue * documentValueExchangeRateAmount
                            : Math.Abs(
                                totalNetValue / documentValueExchangeRateAmount);
            }

            decimal grossPercent = totalSpend * 100 / totalNetValueInEur;

            foreach (PackingList packingList in invoice.PackingLists) {
                IEnumerable<IGrouping<long, PackingListPackageOrderItem>> itemGroupByProducts =
                    packingList.PackingListPackageOrderItems.GroupBy(x => x.SupplyInvoiceOrderItem.ProductId);

                foreach (PackingListPackageOrderItem packingListItem in packingList.PackingListPackageOrderItems) {
                    double totalQtyForSpecification = itemGroupByProducts
                        .Where(x => x.Key == packingListItem.SupplyInvoiceOrderItem.ProductId)
                        .SelectMany(x => x)
                        .Sum(x => x.Qty);

                    ProductSpecification actuallyProductSpecification =
                        _productRepositoriesFactory.NewProductSpecificationRepository(connection)
                            .GetByProductAndSupplyInvoiceIdsIfExists(
                                packingListItem.SupplyInvoiceOrderItem.Product.Id,
                                invoice.Id);

                    decimal productSpecificationValues =
                        actuallyProductSpecification != null ? actuallyProductSpecification.Duty + packingListItem.VatAmount : 0;

                    decimal specificationValue = productSpecificationValues / govExchangeRateFromUahToEur / Convert.ToDecimal(totalQtyForSpecification);

                    decimal vatEur = packingListItem.UnitPriceEurWithVat - packingListItem.UnitPriceEur;

                    decimal accountingValue = packingListItem.AccountingGrossUnitPriceEur - vatEur - specificationValue - packingListItem.UnitPriceEur;

                    decimal syncManageValue = decimal.Round(
                        packingListItem.UnitPriceEurWithVat * grossPercent / 100,
                        14,
                        MidpointRounding.AwayFromZero
                    );

                    packingListItem.GrossUnitPriceEur = syncManageValue - accountingValue;
                }

                packingListPackageOrderItemRepository.Update(packingList.PackingListPackageOrderItems);
            }

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new UpdateConsignmentItemGrossPriceMessage(
                new List<long> { invoice.Id },
                currentUser.NetUid
            ));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateSupplyInvoiceGrossPrice(
        UpdateSupplyInvoiceItemGrossPriceMessage message,
        Dictionary<long, Tuple<decimal, string>> documentValues,
        IEnumerable<Tuple<long, DateTime?, bool, bool, DateTime, decimal>> invoiceWithDateIds,
        bool forAmg) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderRepository supplyOrderRepository =
                _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            ICurrencyRepository currencyRepository =
                _currencyRepositoriesFactory.NewCurrencyRepository(connection);
            IGovExchangeRateRepository govExchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);
            IExchangeRateRepository exchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);
            ISupplyInvoiceMergedServiceRepository supplyInvoiceMergedServiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceMergedServiceRepository(connection);
            IPackingListPackageOrderItemSupplyServiceRepository packingListPackageOrderItemSupplyServiceRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemSupplyServiceRepository(connection);

            List<SupplyInvoice> supplyInvoices =
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetByIds(message.SupplyInvoiceIds);

            List<long> supplyInvoicesWithCompletedProtocolIds = new();

            Currency eur = currencyRepository.GetEURCurrencyIfExists();
            Currency uah = currencyRepository.GetUAHCurrencyIfExists();

            User user = _userRepositoriesFactory.NewUserRepository(connection)
                .GetByNetId(message.UserNetId);

            foreach (SupplyInvoice invoice in supplyInvoices) {
                DeliveryProductProtocol protocol = new();

                decimal totalNetValue = documentValues[invoice.Id].Item1;

                string storageName = documentValues[invoice.Id].Item2;

                Tuple<long, DateTime?, bool, bool, DateTime, decimal> invoiceWithDateId =
                    invoiceWithDateIds.FirstOrDefault(x => x.Item1 == invoice.Id);

                DateTime? dateCustomDeclaration = invoiceWithDateId.Item2 ?? invoice.DateCustomDeclaration;

                bool isTransferFromStorageThree = invoiceWithDateId.Item3;
                bool isIncomeFromStorageThree = invoiceWithDateId.Item4;
                DateTime dateForExchange = invoiceWithDateId.Item5;
                decimal vatFromIncome = invoiceWithDateId.Item6;

                if (invoice.DeliveryProductProtocolId.HasValue)
                    protocol =
                        _supplyRepositoriesFactory.NewDeliveryProductProtocolRepository(connection)
                            .GetById(invoice.DeliveryProductProtocolId.Value);

                Currency currencyfromOrder = supplyOrderRepository.GetCurrencyByOrderNetId(invoice.SupplyOrder.NetUid);

                decimal govExchangeRateFromInvoiceAmount = 1;
                decimal govExchangeRateFromUahToEur = 1;

                if (isTransferFromStorageThree) {
                    govExchangeRateFromInvoiceAmount =
                        GetGovExchangeRateOnDateToUah(
                            currencyfromOrder,
                            dateForExchange,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    GovExchangeRate govExchangeRate =
                        govExchangeRateRepository
                            .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateForExchange);

                    govExchangeRateFromUahToEur = govExchangeRate.Amount;
                } else if (isIncomeFromStorageThree) {
                    govExchangeRateFromInvoiceAmount =
                        GetExchangeRateOnDateToUah(
                            currencyfromOrder,
                            dateForExchange,
                            exchangeRateRepository,
                            currencyRepository
                        );

                    ExchangeRate govExchangeRate =
                        exchangeRateRepository
                            .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateForExchange);

                    govExchangeRateFromUahToEur = govExchangeRate.Amount;
                } else {
                    govExchangeRateFromInvoiceAmount =
                        GetGovExchangeRateOnDateToUah(
                            currencyfromOrder,
                            dateCustomDeclaration ?? invoice.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    GovExchangeRate govExchangeRate =
                        govExchangeRateRepository
                            .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateCustomDeclaration ?? invoice.Created);

                    govExchangeRateFromUahToEur = govExchangeRate.Amount;
                }

                decimal totalNetPrice = 0m;
                decimal totalGrossPrice = 0m;
                decimal totalAccountingGrossPrice = 0m;
                decimal totalGeneralAccountingGrossPrice = 0m;

                if (!govExchangeRateFromInvoiceAmount.Equals(0))
                    totalNetPrice =
                        govExchangeRateFromInvoiceAmount > 0
                            ? totalNetValue * govExchangeRateFromInvoiceAmount
                            : Math.Abs(
                                totalNetValue / govExchangeRateFromInvoiceAmount);
                else
                    totalNetPrice = totalNetValue;

                foreach (PackingList packListFromDb in invoice.PackingLists)
                foreach (PackingListPackageOrderItem packingListItem in packListFromDb.PackingListPackageOrderItems) {
                    if (!govExchangeRateFromInvoiceAmount.Equals(0))
                        packingListItem.UnitPriceUah =
                            govExchangeRateFromInvoiceAmount > 0
                                ? packingListItem.UnitPrice * govExchangeRateFromInvoiceAmount
                                : Math.Abs(
                                    packingListItem.UnitPrice / govExchangeRateFromInvoiceAmount);
                    else
                        packingListItem.UnitPriceUah = packingListItem.UnitPrice;

                    packingListItem.ExchangeRateAmount = govExchangeRateFromInvoiceAmount;
                    packingListItem.ExchangeRateAmountUahToEur = govExchangeRateFromUahToEur;
                }

                List<SupplyInvoiceMergedService> supplyInvoiceMergedServices =
                    supplyInvoiceMergedServiceRepository.GetBySupplyInvoiceId(invoice.Id);

                foreach (SupplyInvoiceMergedService supplyInvoiceMergedService in supplyInvoiceMergedServices) {
                    Currency currency = currencyRepository.GetByMergedServiceId(supplyInvoiceMergedService.MergedServiceId);
                    Currency invoiceCurrency = invoice.SupplyOrder.ClientAgreement.Agreement.Currency;

                    string serviceName = supplyInvoiceMergedService.MergedService.ConsumableProduct.Name + " " +
                                         supplyInvoiceMergedService.MergedService.Number;

                    decimal govExchangeRateAmount;

                    if (isTransferFromStorageThree)
                        govExchangeRateAmount =
                            supplyInvoiceMergedService.MergedService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                currency,
                                dateForExchange,
                                govExchangeRateRepository,
                                currencyRepository
                            );
                    else if (isIncomeFromStorageThree)
                        govExchangeRateAmount =
                            supplyInvoiceMergedService.MergedService.ExchangeRate ?? GetExchangeRateOnDateToUah(
                                currency,
                                dateForExchange,
                                exchangeRateRepository,
                                currencyRepository
                            );
                    else
                        govExchangeRateAmount =
                            supplyInvoiceMergedService.MergedService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                currency,
                                dateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                                govExchangeRateRepository,
                                currencyRepository
                            );


                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + supplyInvoiceMergedService.Value / (0 - govExchangeRateAmount)
                            : totalGrossPrice + supplyInvoiceMergedService.Value * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount;

                    if (isTransferFromStorageThree)
                        govAccountingExchangeRateAmount =
                            supplyInvoiceMergedService.MergedService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                currency,
                                dateForExchange,
                                govExchangeRateRepository,
                                currencyRepository
                            );
                    else if (isIncomeFromStorageThree)
                        govAccountingExchangeRateAmount =
                            supplyInvoiceMergedService.MergedService.AccountingExchangeRate ?? GetExchangeRateOnDateToUah(
                                currency,
                                dateForExchange,
                                exchangeRateRepository,
                                currencyRepository
                            );
                    else
                        govAccountingExchangeRateAmount =
                            supplyInvoiceMergedService.MergedService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                currency,
                                dateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                                govExchangeRateRepository,
                                currencyRepository
                            );

                    if (supplyInvoiceMergedService.MergedService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice =
                            govAccountingExchangeRateAmount < 0
                                ? totalAccountingGrossPrice + supplyInvoiceMergedService.AccountingValue / (0 - govAccountingExchangeRateAmount)
                                : totalAccountingGrossPrice + supplyInvoiceMergedService.AccountingValue * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice =
                            govAccountingExchangeRateAmount < 0
                                ? totalGeneralAccountingGrossPrice + supplyInvoiceMergedService.AccountingValue / (0 - govAccountingExchangeRateAmount)
                                : totalGeneralAccountingGrossPrice + supplyInvoiceMergedService.AccountingValue * govAccountingExchangeRateAmount;

                    decimal accountingServicePrice = govAccountingExchangeRateAmount < 0
                        ? supplyInvoiceMergedService.AccountingValue / (0 - govAccountingExchangeRateAmount)
                        : supplyInvoiceMergedService.AccountingValue * govAccountingExchangeRateAmount;

                    decimal grossServicePrice = govAccountingExchangeRateAmount < 0
                        ? supplyInvoiceMergedService.Value / (0 - govAccountingExchangeRateAmount)
                        : supplyInvoiceMergedService.Value * govAccountingExchangeRateAmount;


                    decimal invoiceCurrencyExchangeRate =
                        GetGovExchangeRateOnDateToUah(
                            invoiceCurrency,
                            dateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal invoicePrice = invoiceCurrencyExchangeRate < 0
                        ? invoice.NetPrice / (0 - invoiceCurrencyExchangeRate)
                        : invoice.NetPrice * invoiceCurrencyExchangeRate;

                    decimal accountingGrossPercentCurrentService = accountingServicePrice * 100 / invoicePrice;

                    decimal grossPercentCurrentService = grossServicePrice * 100 / invoicePrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPriceUah * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            decimal.Round(
                                totalNetPricePackingListItem * grossPercentCurrentService / 100 / govExchangeRateFromUahToEur,
                                14,
                                MidpointRounding.AwayFromZero
                            );

                        decimal accountingValueOnCurrentPackListItem =
                            decimal.Round(
                                totalNetPricePackingListItem * accountingGrossPercentCurrentService / 100 / govExchangeRateFromUahToEur,
                                14,
                                MidpointRounding.AwayFromZero
                            );

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyInvoiceMergedService.MergedService.Id,
                                    TypeService.MergedService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = currency.Id,
                                    Name = serviceName,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    MergedServiceId = supplyInvoiceMergedService.MergedService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = dateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyInvoiceMergedService.MergedService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = dateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created;

                            existItem.CurrencyId = currency.Id;

                            if (supplyInvoiceMergedService.MergedService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                decimal grossPercent = totalGrossPrice * 100 / totalNetPrice;

                decimal accountingGrossPercent = totalAccountingGrossPrice * 100 / totalNetPrice;

                decimal generalAccountingGrossPercent = totalGeneralAccountingGrossPrice * 100 / totalNetPrice;

                foreach (PackingList packingList in invoice.PackingLists) {
                    IEnumerable<IGrouping<long, PackingListPackageOrderItem>> itemGroupByProducts =
                        packingList.PackingListPackageOrderItems
                            .GroupBy(x => x.SupplyInvoiceOrderItem.ProductId);

                    foreach (PackingListPackageOrderItem packingListItem in packingList.PackingListPackageOrderItems) {
                        double qtyTotalForSpecifications = itemGroupByProducts
                            .Where(x => x.Key == packingListItem.SupplyInvoiceOrderItem.ProductId)
                            .SelectMany(x => x)
                            .Sum(x => x.Qty);

                        ProductSpecification actuallyProductSpecification =
                            _productRepositoriesFactory.NewProductSpecificationRepository(connection)
                                .GetByProductAndSupplyInvoiceIdsIfExists(
                                    packingListItem.SupplyInvoiceOrderItem.Product.Id,
                                    invoice.Id);

                        // if (actuallyProductSpecification != null)
                        //     packingListItem.VatAmount = actuallyProductSpecification.VATValue;

                        decimal productSpecificationValues =
                            actuallyProductSpecification != null ? actuallyProductSpecification.Duty + packingListItem.VatAmount : 0;

                        decimal totalPriceForSpecification = packingListItem.UnitPriceUah * Convert.ToDecimal(qtyTotalForSpecifications);

                        decimal specificationValuesPerUnit = productSpecificationValues * 100 / totalPriceForSpecification;

                        decimal accountingGrossPercentPerItem = accountingGrossPercent + specificationValuesPerUnit;

                        if (!forAmg && !storageThreeNames.Contains(storageName)) {
                            packingListItem.GrossUnitPriceEur =
                                decimal.Round(
                                    packingListItem.UnitPriceUah * grossPercent / 100 / govExchangeRateFromUahToEur,
                                    14,
                                    MidpointRounding.AwayFromZero
                                );

                            packingListItem.AccountingGeneralGrossUnitPriceEur =
                                decimal.Round(
                                    packingListItem.UnitPriceUah * generalAccountingGrossPercent / 100 / govExchangeRateFromUahToEur,
                                    14,
                                    MidpointRounding.AwayFromZero
                                );

                            packingListItem.AccountingGrossUnitPriceEur =
                                decimal.Round(
                                    (packingListItem.UnitPriceUah +
                                     packingListItem.UnitPriceUah * accountingGrossPercentPerItem / 100) / govExchangeRateFromUahToEur,
                                    14,
                                    MidpointRounding.AwayFromZero
                                );

                            packingListItem.AccountingGrossUnitPriceEur += packingListItem.GrossUnitPriceEur;
                            packingListItem.GrossUnitPriceEur = 0;

                            if (isTransferFromStorageThree && vatFromIncome != 0)
                                packingListItem.AccountingGrossUnitPriceEur += packingListItem.AccountingGrossUnitPriceEur * (vatFromIncome / 100);
                        } else {
                            packingListItem.GrossUnitPriceEur =
                                decimal.Round(
                                    packingListItem.UnitPriceUah * grossPercent / 100 / govExchangeRateFromUahToEur,
                                    14,
                                    MidpointRounding.AwayFromZero
                                );

                            packingListItem.AccountingGeneralGrossUnitPriceEur =
                                decimal.Round(
                                    packingListItem.UnitPriceUah * generalAccountingGrossPercent / 100 / govExchangeRateFromUahToEur,
                                    14,
                                    MidpointRounding.AwayFromZero
                                );

                            packingListItem.AccountingGrossUnitPriceEur =
                                decimal.Round(
                                    (packingListItem.UnitPriceUah +
                                     packingListItem.UnitPriceUah * accountingGrossPercentPerItem / 100) / govExchangeRateFromUahToEur,
                                    14,
                                    MidpointRounding.AwayFromZero
                                );

                            if (vatFromIncome != 0) packingListItem.AccountingGrossUnitPriceEur += packingListItem.AccountingGrossUnitPriceEur * (vatFromIncome / 100);
                        }

                        if (!govExchangeRateFromUahToEur.Equals(1))
                            packingListItem.UnitPriceEur =
                                Math.Abs(govExchangeRateFromUahToEur > 0
                                    ? packingListItem.UnitPriceUah / govExchangeRateFromUahToEur
                                    : Math.Abs(packingListItem.UnitPriceUah * govExchangeRateFromUahToEur));
                        else
                            packingListItem.UnitPriceEur = packingListItem.UnitPrice;
                    }

                    packingListPackageOrderItemRepository.Update(packingList.PackingListPackageOrderItems);
                }

                if (!protocol.Id.Equals(0) && protocol.IsCompleted && user.UserRole.UserRoleType.Equals(UserRoleType.GBA))
                    supplyInvoicesWithCompletedProtocolIds.Add(invoice.Id);
            }

            if (supplyInvoicesWithCompletedProtocolIds.Any())
                ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR).Tell(new UpdateConsignmentItemGrossPriceMessage(
                    supplyInvoicesWithCompletedProtocolIds,
                    user.NetUid
                ));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private static decimal GetGovExchangeRateOnDateToUah(
        Currency from,
        DateTime onDate,
        IGovExchangeRateRepository govExchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        if (from.Id.Equals(uah.Id))
            return 1m;

        GovExchangeRate govExchangeRate =
            govExchangeRateRepository
                .GetByCurrencyIdAndCode(
                    uah.Id, from.Code, onDate);

        return govExchangeRate?.Amount ?? 1m;
    }

    private static decimal GetExchangeRateOnDateToUah(
        Currency from,
        DateTime onDate,
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        if (from.Id.Equals(uah.Id))
            return 1m;

        ExchangeRate exchangeRate =
            exchangeRateRepository
                .GetByCurrencyIdAndCode(
                    uah.Id, from.Code, onDate);

        return exchangeRate?.Amount ?? 1m;
    }
}