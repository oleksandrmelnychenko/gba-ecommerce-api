using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using Akka.Util.Internal;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Synchronizations;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Messages.Communications.Hubs;
using GBA.Domain.Messages.DataSync;
using GBA.Domain.Messages.Logging;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Domain.TranslationEntities;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace GBA.Services.Actors.DataSync;

public sealed class SyncAccountingWorkerActor : ReceiveActor {
    private const string UaCulture = "uk";

    private const string PlCulture = "pl";

    private const string BUYER = "Buyer";

    private const string PROVIDER = "Provider";

    private const string DEBTS_FROM_ONE_C_UK_KEY = "��� ����� � 1�";

    private const string DEBTS_FROM_ONE_C_PL_KEY = "Zarzadzanie dlugiem z 1�";

    private const string DEFAULT_ORGANIZATION_AMG = "��� ���� �������Ļ";

    private const string DEFAULT_ORGANIZATION_FENIX = "������";

    private readonly IDbConnectionFactory _connectionFactory;

    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;

    private readonly IDataSyncRepositoriesFactory _dataSyncRepositoriesFactory;
    private readonly IStringLocalizer<SharedResource> _localizer;

    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    private readonly string[] eurCodes;

    private readonly string[] plnCodes;

    private readonly string[] usdCodes;

    public SyncAccountingWorkerActor(
        IStringLocalizer<SharedResource> localizer,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDataSyncRepositoriesFactory dataSyncRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory) {
        _localizer = localizer;

        _connectionFactory = connectionFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        _dataSyncRepositoriesFactory = dataSyncRepositoriesFactory;

        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        eurCodes = new[] { "978", "979", "555" };

        usdCodes = new[] { "840", "556" };

        plnCodes = new[] { "830", "831" };

        Receive<SynchronizeAccountingMessage>(ProcessSynchronizeAccountingMessage);
    }

    private void ProcessSynchronizeAccountingMessage(SynchronizeAccountingMessage message) {
        using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
        using IDbConnection amgCConnection = _connectionFactory.NewAmgOneCSqlConnection();
        using IDbConnection remoteSyncConnection = _connectionFactory.NewSqlConnection();
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(remoteSyncConnection);

        IActorRef hubSenderActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

        User currentUser = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

        _dataSyncRepositoriesFactory
            .NewDataSyncOperationRepository(remoteSyncConnection)
            .Add(new DataSyncOperation {
                UserId = currentUser.Id,
                OperationType = DataSyncOperationType.Accounting,
                ForAmg = message.ForAmg
            });

        SynchronizeAccounting(hubSenderActorRef, oneCConnection, remoteSyncConnection, amgCConnection, currentUser, message.ForAmg);

        ActorReferenceManager.Instance.Get(DataSyncActorNames.DATA_SYNC_WORKER_ACTOR)
            .Tell(new StartDataSyncWorkMessage(message.SyncEntityTypes, message.UserNetId, message.ForAmg));
    }

    private void SynchronizeAccounting(
        IActorRef hubSenderActorRef,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgCConnection,
        User currentUser,
        bool forAmg) {
        try {
            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.EXCHANGE_RATE_HISTORY_SYNC_START]));

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
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.EXCHANGE_RATE_HISTORY_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ACCOUNTING_SYNC_START]));

            if (forAmg)
                accountingSyncRepository.CleanDebtsAndBalances();

            IEnumerable<Client> clients = accountingSyncRepository.GetAllClients();

            Product productDev =
                accountingSyncRepository.GetDevProduct();

            if (productDev == null) {
                MeasureUnit measureUnit =
                    accountingSyncRepository.GetMeasureUnit();

                if (measureUnit == null) {
                    measureUnit = new MeasureUnit {
                        Name = " ",
                        Deleted = true
                    };

                    measureUnit.Id = accountingSyncRepository.Add(measureUnit);

                    accountingSyncRepository.Add(new MeasureUnitTranslation {
                        MeasureUnitId = measureUnit.Id,
                        CultureCode = UaCulture,
                        Name = " "
                    });
                    accountingSyncRepository.Add(new MeasureUnitTranslation {
                        MeasureUnitId = measureUnit.Id,
                        CultureCode = "pl",
                        Name = " "
                    });
                }

                productDev = new Product {
                    VendorCode = "����",
                    Name = "��� �����",
                    NamePL = "��� �����",
                    NameUA = "��� �����",
                    Description = "��� �����",
                    DescriptionPL = "��� �����",
                    DescriptionUA = "��� �����",
                    Deleted = true,
                    MeasureUnitId = measureUnit.Id,
                    Top = "���"
                };

                productDev.Id = accountingSyncRepository.Add(productDev);
            }

            SupplyOrderPaymentDeliveryProtocolKey protocolPaymentKey = accountingSyncRepository.GetProtocolPaymentByKey(DEBTS_FROM_ONE_C_UK_KEY);

            if (protocolPaymentKey == null) {
                protocolPaymentKey = new SupplyOrderPaymentDeliveryProtocolKey {
                    Created = DateTime.Now,
                    Updated = DateTime.Now,
                    Key = DEBTS_FROM_ONE_C_UK_KEY
                };

                protocolPaymentKey.Id = accountingSyncRepository.Add(protocolPaymentKey);
            }

            SupplyInformationDeliveryProtocolKey informationProtocolKey = accountingSyncRepository.GetInformationProtocolByKey(DEBTS_FROM_ONE_C_UK_KEY);

            if (informationProtocolKey == null) {
                informationProtocolKey = new SupplyInformationDeliveryProtocolKey {
                    Created = DateTime.Now,
                    Key = DEBTS_FROM_ONE_C_UK_KEY,
                    Updated = DateTime.Now
                };

                informationProtocolKey.Id = accountingSyncRepository.Add(informationProtocolKey);

                accountingSyncRepository.Add(new SupplyInformationDeliveryProtocolKeyTranslation {
                    Created = DateTime.Now,
                    CultureCode = UaCulture,
                    Key = DEBTS_FROM_ONE_C_UK_KEY,
                    SupplyInformationDeliveryProtocolKeyId = informationProtocolKey.Id,
                    Updated = DateTime.Now
                });

                accountingSyncRepository.Add(new SupplyInformationDeliveryProtocolKeyTranslation {
                    Created = DateTime.Now,
                    CultureCode = PlCulture,
                    Key = DEBTS_FROM_ONE_C_PL_KEY,
                    SupplyInformationDeliveryProtocolKeyId = informationProtocolKey.Id,
                    Updated = DateTime.Now
                });
            }

            Storage storageDev = accountingSyncRepository.GetDevStorage(DEBTS_FROM_ONE_C_UK_KEY);

            if (storageDev == null) {
                storageDev = new Storage {
                    Created = DateTime.Now,
                    Deleted = true,
                    Locale = UaCulture,
                    Name = DEBTS_FROM_ONE_C_UK_KEY,
                    Updated = DateTime.Now
                };
                storageDev.Id = accountingSyncRepository.Add(storageDev);
            }

            PaymentRegister paymentRegisterDev = accountingSyncRepository.GetDevPaymentRegister(DEBTS_FROM_ONE_C_UK_KEY);

            PaymentCurrencyRegister paymentCurrencyRegisterEurDev;
            PaymentCurrencyRegister paymentCurrencyRegisterUsdDev;
            PaymentCurrencyRegister paymentCurrencyRegisterPlnDev;
            PaymentCurrencyRegister paymentCurrencyRegisterUahDev;

            if (paymentRegisterDev == null) {
                Organization organizationAmg = accountingSyncRepository.GetByName(DEFAULT_ORGANIZATION_AMG);

                paymentRegisterDev = new PaymentRegister {
                    Created = DateTime.Now,
                    Name = DEBTS_FROM_ONE_C_UK_KEY,
                    Updated = DateTime.Now,
                    Type = PaymentRegisterType.Cash,
                    OrganizationId = organizationAmg.Id
                };

                paymentRegisterDev.Id = accountingSyncRepository.Add(paymentRegisterDev);

                paymentCurrencyRegisterEurDev = new PaymentCurrencyRegister {
                    Created = DateTime.Now,
                    CurrencyId = eur.Id,
                    PaymentRegisterId = paymentRegisterDev.Id,
                    Updated = DateTime.Now
                };

                paymentCurrencyRegisterEurDev.Id = accountingSyncRepository.Add(paymentCurrencyRegisterEurDev);

                paymentCurrencyRegisterUsdDev = new PaymentCurrencyRegister {
                    Created = DateTime.Now,
                    CurrencyId = usd.Id,
                    PaymentRegisterId = paymentRegisterDev.Id,
                    Updated = DateTime.Now
                };

                paymentCurrencyRegisterUsdDev.Id = accountingSyncRepository.Add(paymentCurrencyRegisterUsdDev);

                paymentCurrencyRegisterPlnDev = new PaymentCurrencyRegister {
                    Created = DateTime.Now,
                    CurrencyId = pln.Id,
                    PaymentRegisterId = paymentRegisterDev.Id,
                    Updated = DateTime.Now
                };

                paymentCurrencyRegisterPlnDev.Id = accountingSyncRepository.Add(paymentCurrencyRegisterPlnDev);

                paymentCurrencyRegisterUahDev = new PaymentCurrencyRegister {
                    Created = DateTime.Now,
                    CurrencyId = uah.Id,
                    PaymentRegisterId = paymentRegisterDev.Id,
                    Updated = DateTime.Now
                };

                paymentCurrencyRegisterUahDev.Id = accountingSyncRepository.Add(paymentCurrencyRegisterUahDev);
            } else {
                paymentCurrencyRegisterEurDev = paymentRegisterDev.PaymentCurrencyRegisters.First(x => x.Currency.Id == eur.Id);
                paymentCurrencyRegisterUsdDev = paymentRegisterDev.PaymentCurrencyRegisters.First(x => x.Currency.Id == usd.Id);
                paymentCurrencyRegisterPlnDev = paymentRegisterDev.PaymentCurrencyRegisters.First(x => x.Currency.Id == pln.Id);
                paymentCurrencyRegisterUahDev = paymentRegisterDev.PaymentCurrencyRegisters.First(x => x.Currency.Id == uah.Id);
            }

            PaymentMovement paymentMovementDev = accountingSyncRepository.GetDevPaymentMovement(DEBTS_FROM_ONE_C_UK_KEY);

            if (paymentMovementDev == null) {
                paymentMovementDev = new PaymentMovement {
                    Created = DateTime.Now,
                    Updated = DateTime.Now,
                    OperationName = DEBTS_FROM_ONE_C_UK_KEY
                };

                paymentMovementDev.Id = accountingSyncRepository.Add(paymentMovementDev);
            }

            GovExchangeRate govExchangeRate =
                accountingSyncRepository
                    .GetGovByCurrencyIdAndCode(uah.Id, eur.Code, DateTime.Now);

            foreach (Client client in clients.Where(x => forAmg ? x.SourceAmgCode.HasValue : x.SourceFenixCode.HasValue)) {
                IEnumerable<ClientAgreement> clientAgreements = client.ClientAgreements
                    .Where(x => forAmg ? x.OriginalClientAmgCode.HasValue : x.OriginalClientFenixCode.HasValue);

                if (forAmg)
                    clientAgreements = clientAgreements.Where(x => x.Agreement.Organization.Name == DEFAULT_ORGANIZATION_AMG);
                else
                    clientAgreements = clientAgreements.Where(x => x.Agreement.Organization.Name == DEFAULT_ORGANIZATION_FENIX);

                if (!clientAgreements.Any()) continue;

                foreach (ClientAgreement clientAgreement in clientAgreements) {
                    IEnumerable<SyncAccounting> accounting =
                        forAmg
                            ? accountingSyncRepository.GetAmgSyncAccountingFiltered(
                                clientAgreement.OriginalClientAmgCode.Value,
                                clientAgreement.Agreement.Name,
                                clientAgreement.Agreement.Organization.Name,
                                clientAgreement.Agreement.Currency.CodeOneC
                            )
                            : accountingSyncRepository
                                .GetSyncAccountingFiltered(
                                    clientAgreement.OriginalClientFenixCode.Value,
                                    clientAgreement.Agreement.Name,
                                    clientAgreement.Agreement.Organization.Name,
                                    clientAgreement.Agreement.Currency.CodeOneC
                                );

                    if (!accounting.Any()) continue;

                    SyncAccounting syncAccounting = accounting.First();

                    syncAccounting.Value = accounting.Sum(a => a.Value);

                    if (syncAccounting.Value == 0)
                        continue;

                    decimal originExchangeRateAmount =
                        accountingSyncRepository
                            .GetExchangeRateAmountToEuroByDate(
                                clientAgreement.Agreement.Currency.Id,
                                syncAccounting.Date
                            );

                    decimal exchangeRateAmount = originExchangeRateAmount;

                    if (exchangeRateAmount < 0) exchangeRateAmount = Math.Abs(exchangeRateAmount);

                    decimal amount = exchangeRateAmount > 1
                        ? decimal.Round(syncAccounting.Value / exchangeRateAmount, 14, MidpointRounding.AwayFromZero)
                        : decimal.Round(syncAccounting.Value * exchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                    PaymentCurrencyRegister paymentCurrencyRegisterDev;

                    if (clientAgreement.Agreement.Currency.Code == eur.Code)
                        paymentCurrencyRegisterDev = paymentCurrencyRegisterEurDev;
                    else if (clientAgreement.Agreement.Currency.Code == usd.Code)
                        paymentCurrencyRegisterDev = paymentCurrencyRegisterUsdDev;
                    else if (clientAgreement.Agreement.Currency.Code == pln.Code)
                        paymentCurrencyRegisterDev = paymentCurrencyRegisterPlnDev;
                    else
                        paymentCurrencyRegisterDev = paymentCurrencyRegisterUahDev;

                    if (client.ClientInRole.ClientType.Name == BUYER) {
                        if (amount < decimal.Zero) {
                            amount = Math.Abs(amount);

                            clientAgreement.CurrentAmount = amount;

                            accountingSyncRepository.Update(clientAgreement);

                            decimal value = Math.Abs(syncAccounting.Value);
                            string number = string.Empty;

                            IncomePaymentOrder lastIncomePaymentOrder = accountingSyncRepository.GetLastIncomePaymentOrder();

                            if (lastIncomePaymentOrder == null || !lastIncomePaymentOrder.Created.Year.Equals(DateTime.Now.Year))
                                number = clientAgreement.Agreement.Organization.Code + string.Format("{0:D10}", 1);
                            else
                                number =
                                    clientAgreement.Agreement.Organization.Code +
                                    string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastIncomePaymentOrder.Number, @"(\d+)").Value) + 1);

                            bool isAccounting = clientAgreement.Agreement.Organization.Name.Equals(DEFAULT_ORGANIZATION_AMG);

                            IncomePaymentOrder incomePaymentOrder = new() {
                                ClientId = client.Id,
                                Created = DateTime.Now,
                                CurrencyId = paymentCurrencyRegisterDev.CurrencyId,
                                FromDate = DateTime.Now,
                                IsAccounting = isAccounting,
                                IsManagementAccounting = !isAccounting,
                                Number = number,
                                OrganizationId = clientAgreement.Agreement.Organization.Id,
                                PaymentRegisterId = paymentRegisterDev.Id,
                                Updated = DateTime.Now,
                                UserId = currentUser.Id,
                                ClientAgreementId = clientAgreement.Id,
                                EuroAmount = amount,
                                Amount = value,
                                Comment = DEBTS_FROM_ONE_C_UK_KEY
                            };

                            ExchangeRate exchangeRate =
                                accountingSyncRepository
                                    .GetByCurrencyIdAndCode(
                                        clientAgreement.Agreement.CurrencyId.Value,
                                        eur.Code,
                                        TimeZoneInfo.ConvertTimeToUtc(syncAccounting.Date)
                                    );

                            if (clientAgreement?.Agreement?.CurrencyId != null) {
                                long agreementCurrencyId = clientAgreement.Agreement.CurrencyId.Value;

                                if (eur != null) {
                                    if (agreementCurrencyId.Equals(eur.Id)) {
                                        incomePaymentOrder.AgreementEuroExchangeRate = 1m;
                                    } else {
                                        if (exchangeRate != null) {
                                            incomePaymentOrder.AgreementEuroExchangeRate = exchangeRate.Amount;
                                        } else {
                                            CrossExchangeRate crossExchangeRate =
                                                accountingSyncRepository
                                                    .GetByCurrenciesIds(
                                                        agreementCurrencyId,
                                                        eur.Id,
                                                        TimeZoneInfo.ConvertTimeToUtc(syncAccounting.Date)
                                                    );

                                            if (crossExchangeRate != null) {
                                                incomePaymentOrder.AgreementEuroExchangeRate = decimal.Zero - crossExchangeRate.Amount;
                                            } else {
                                                crossExchangeRate =
                                                    accountingSyncRepository
                                                        .GetByCurrenciesIds(
                                                            eur.Id,
                                                            agreementCurrencyId,
                                                            TimeZoneInfo.ConvertTimeToUtc(syncAccounting.Date)
                                                        );

                                                incomePaymentOrder.AgreementEuroExchangeRate =
                                                    crossExchangeRate?.Amount ?? 1m;
                                            }
                                        }
                                    }
                                }
                            }

                            incomePaymentOrder.ExchangeRate = exchangeRate?.Amount ?? 0;

                            if (incomePaymentOrder.ExchangeRate <= 0) {
                                CrossExchangeRate crossExchangeRate =
                                    accountingSyncRepository
                                        .GetByCurrenciesIds(
                                            eur.Id,
                                            clientAgreement.Agreement.Currency.Id
                                        );

                                if (crossExchangeRate != null) {
                                    incomePaymentOrder.ExchangeRate = crossExchangeRate.Amount;
                                } else {
                                    crossExchangeRate =
                                        accountingSyncRepository
                                            .GetByCurrenciesIds(
                                                clientAgreement.Agreement.Currency.Id,
                                                eur.Id
                                            );

                                    incomePaymentOrder.ExchangeRate = crossExchangeRate?.Amount ?? 1;
                                }
                            }

                            incomePaymentOrder.Id = accountingSyncRepository.Add(incomePaymentOrder);

                            accountingSyncRepository.Add(new PaymentMovementOperation {
                                Created = DateTime.Now,
                                Updated = DateTime.Now,
                                IncomePaymentOrderId = incomePaymentOrder.Id,
                                PaymentMovementId = paymentMovementDev.Id
                            });

                            accountingSyncRepository.Add(new ClientBalanceMovement {
                                Created = DateTime.Now,
                                Updated = DateTime.Now,
                                Amount = amount,
                                ClientAgreementId = clientAgreement.Id,
                                ExchangeRateAmount = amount
                            });
                        } else {
                            Order order = new() {
                                UserId = currentUser.Id,
                                ClientAgreementId = clientAgreement.Id,
                                OrderSource = OrderSource.Local,
                                OrderStatus = OrderStatus.Sale,
                                Created = syncAccounting.Date,
                                Updated = syncAccounting.Date
                            };

                            order.Id = accountingSyncRepository.Add(order);

                            accountingSyncRepository.Add(new ClientInDebt {
                                DebtId = accountingSyncRepository.Add(new Debt {
                                    Total = syncAccounting.Value,
                                    Created = syncAccounting.Date,
                                    Updated = syncAccounting.Date
                                }),
                                ClientId = client.Id,
                                AgreementId = clientAgreement.AgreementId,
                                SaleId = accountingSyncRepository.Add(new Sale {
                                    OrderId = order.Id,
                                    SaleNumberId = accountingSyncRepository.Add(new SaleNumber {
                                        Value = syncAccounting.Number,
                                        OrganizationId = clientAgreement.Agreement.Organization.Id
                                    }),
                                    BaseLifeCycleStatusId = accountingSyncRepository.Add(new ReceivedSaleLifeCycleStatus()),
                                    BaseSalePaymentStatusId = accountingSyncRepository.Add(new NotPaidSalePaymentStatus()),
                                    TransporterId = 57,
                                    Comment = DEBTS_FROM_ONE_C_UK_KEY,
                                    ChangedToInvoice = syncAccounting.Date,
                                    Created = syncAccounting.Date,
                                    Updated = syncAccounting.Date,
                                    ChangedToInvoiceById = currentUser.Id,
                                    IsVatSale = true,
                                    IsLocked = true,
                                    IsPaymentBillDownloaded = false,
                                    ClientAgreementId = clientAgreement.Id,
                                    UserId = currentUser.Id
                                }),
                                Created = syncAccounting.Date,
                                Updated = syncAccounting.Date
                            });

                            accountingSyncRepository.Add(new OrderItem {
                                ProductId = productDev.Id,
                                OrderId = order.Id,
                                Qty = 1,
                                UserId = currentUser.Id,
                                Comment = DEBTS_FROM_ONE_C_UK_KEY,
                                IsValidForCurrentSale = true,
                                PricePerItem = amount,
                                ExchangeRateAmount = exchangeRateAmount,
                                PricePerItemWithoutVat = amount
                            });
                        }
                    } else {
                        if (amount < decimal.Zero) {
                            syncAccounting.Value = Math.Abs(syncAccounting.Value);

                            string number = string.Empty;

                            OutcomePaymentOrder lastOutcomePaymentOrder = accountingSyncRepository.GetLastOutcomePaymentOrder();

                            if (lastOutcomePaymentOrder == null || !lastOutcomePaymentOrder.Created.Year.Equals(DateTime.Now.Year))
                                number = clientAgreement.Agreement.Organization.Code + string.Format("{0:D10}", 1);
                            else
                                number =
                                    clientAgreement.Agreement.Organization.Code +
                                    string.Format("{0:D10}", Convert.ToInt64(Regex.Match(lastOutcomePaymentOrder.Number, @"(\d+)").Value) + 1);

                            bool isAccounting = clientAgreement.Agreement.Organization.Name.Equals(DEFAULT_ORGANIZATION_AMG);

                            OutcomePaymentOrder outcomePaymentOrder = new() {
                                Comment = DEBTS_FROM_ONE_C_UK_KEY,
                                Created = DateTime.Now,
                                FromDate = DateTime.Now,
                                Number = number,
                                OrganizationId = clientAgreement.Agreement.Organization.Id,
                                PaymentCurrencyRegisterId = paymentCurrencyRegisterDev.Id,
                                Updated = DateTime.Now,
                                UserId = currentUser.Id,
                                ClientAgreementId = clientAgreement.Id,
                                AfterExchangeAmount = syncAccounting.Value,
                                IsAccounting = isAccounting,
                                IsManagementAccounting = !isAccounting
                            };

                            ExchangeRate exchangeRate =
                                accountingSyncRepository
                                    .GetByCurrencyIdAndCode(
                                        clientAgreement.Agreement.CurrencyId.Value,
                                        eur.Code,
                                        TimeZoneInfo.ConvertTimeToUtc(outcomePaymentOrder.FromDate)
                                    );

                            outcomePaymentOrder.ExchangeRate = exchangeRate?.Amount ?? 0;

                            if (outcomePaymentOrder.ExchangeRate > 0) {
                                if (clientAgreement.Agreement.Currency.Code.ToLower().Equals("uah") ||
                                    clientAgreement.Agreement.Currency.Code.ToLower().Equals("pln"))
                                    outcomePaymentOrder.Amount =
                                        Math.Round(outcomePaymentOrder.AfterExchangeAmount / outcomePaymentOrder.ExchangeRate, 14);
                                else
                                    outcomePaymentOrder.Amount =
                                        Math.Round(outcomePaymentOrder.AfterExchangeAmount * outcomePaymentOrder.ExchangeRate, 14);
                            } else {
                                CrossExchangeRate crossExchangeRate =
                                    accountingSyncRepository
                                        .GetByCurrenciesIds(
                                            eur.Id,
                                            clientAgreement.Agreement.Currency.Id
                                        );

                                if (crossExchangeRate != null) {
                                    outcomePaymentOrder.ExchangeRate = crossExchangeRate.Amount;

                                    outcomePaymentOrder.Amount =
                                        Math.Round(outcomePaymentOrder.AfterExchangeAmount / outcomePaymentOrder.ExchangeRate, 14);
                                } else {
                                    crossExchangeRate =
                                        accountingSyncRepository
                                            .GetByCurrenciesIds(
                                                clientAgreement.Agreement.Currency.Id,
                                                eur.Id
                                            );

                                    outcomePaymentOrder.ExchangeRate = crossExchangeRate?.Amount ?? 1;

                                    outcomePaymentOrder.Amount =
                                        Math.Round(outcomePaymentOrder.AfterExchangeAmount / outcomePaymentOrder.ExchangeRate, 14);
                                }
                            }

                            outcomePaymentOrder.Id = accountingSyncRepository.Add(outcomePaymentOrder);

                            accountingSyncRepository.Add(new PaymentMovementOperation {
                                Created = DateTime.Now,
                                Updated = DateTime.Now,
                                OutcomePaymentOrderId = outcomePaymentOrder.Id,
                                PaymentMovementId = paymentMovementDev.Id
                            });

                            SupplyPaymentTask task = new() {
                                Comment = DEBTS_FROM_ONE_C_UK_KEY,
                                Created = DateTime.Now,
                                Updated = DateTime.Now,
                                UserId = currentUser.Id,
                                TaskAssignedTo = 0,
                                TaskStatus = 0,
                                GrossPrice = amount,
                                NetPrice = amount,
                                IsAccounting = forAmg,
                                IsImportedFromOneC = true
                            };

                            task.Id = accountingSyncRepository.Add(task);

                            accountingSyncRepository.Add(new SupplyOrderPaymentDeliveryProtocol {
                                Value = amount,
                                Created = DateTime.Now,
                                SupplyPaymentTaskId = task.Id,
                                Updated = DateTime.Now,
                                UserId = currentUser.Id,
                                SupplyOrderPaymentDeliveryProtocolKeyId = protocolPaymentKey.Id,
                                Discount = 100,
                                IsAccounting = forAmg
                            });

                            accountingSyncRepository.Add(new SupplyInformationDeliveryProtocol {
                                Created = DateTime.Now,
                                Updated = DateTime.Now,
                                UserId = currentUser.Id,
                                SupplyInformationDeliveryProtocolKeyId = informationProtocolKey.Id,
                                IsDefault = true
                            });

                            amount = Math.Abs(amount);

                            accountingSyncRepository.Add(new OutcomePaymentOrderSupplyPaymentTask {
                                Amount = amount,
                                Created = DateTime.Now,
                                Updated = DateTime.Now,
                                OutcomePaymentOrderId = outcomePaymentOrder.Id,
                                SupplyPaymentTaskId = task.Id
                            });

                            clientAgreement.CurrentAmount = amount;

                            accountingSyncRepository.Update(clientAgreement);
                        } else {
                            decimal govExchangeRateFromInvoiceAmount =
                                GetGovExchangeRateOnDateToUah(
                                    clientAgreement.Agreement.Currency,
                                    DateTime.Now,
                                    accountingSyncRepository,
                                    currencyRepository
                                );

                            SupplyOrderNumber number = new() {
                                Created = DateTime.Now,
                                Number = DEBTS_FROM_ONE_C_UK_KEY,
                                Updated = DateTime.Now
                            };

                            number.Id = accountingSyncRepository.Add(number);

                            SupplyProForm proForm = new() {
                                Created = DateTime.Now,
                                NetPrice = syncAccounting.Value,
                                Number = DEBTS_FROM_ONE_C_UK_KEY,
                                Updated = DateTime.Now,
                                DateFrom = DateTime.Now
                            };

                            proForm.Id = accountingSyncRepository.Add(proForm);

                            SupplyOrder order = new() {
                                NetPrice = syncAccounting.Value,
                                ClientId = client.Id,
                                Created = DateTime.Now,
                                OrganizationId = clientAgreement.Agreement.OrganizationId.Value,
                                Qty = 1,
                                SupplyOrderNumberId = number.Id,
                                SupplyProFormId = proForm.Id,
                                Updated = DateTime.Now,
                                DateFrom = DateTime.Now,
                                GrossPrice = amount,
                                IsGrossPricesCalculated = true,
                                Comment = DEBTS_FROM_ONE_C_UK_KEY,
                                ClientAgreementId = clientAgreement.Id,
                                IsFullyPlaced = true,
                                IsPartiallyPlaced = true,
                                SupplyOrderItems = new List<SupplyOrderItem> {
                                    new() {
                                        Created = DateTime.Now,
                                        Description = DEBTS_FROM_ONE_C_UK_KEY,
                                        ProductId = productDev.Id,
                                        Qty = 1,
                                        TotalAmount = syncAccounting.Value,
                                        UnitPrice = syncAccounting.Value,
                                        Updated = DateTime.Now,
                                        GrossWeight = 1,
                                        NetWeight = 1
                                    }
                                },
                                SupplyInvoices = new List<SupplyInvoice> {
                                    new() {
                                        NetPrice = syncAccounting.Value,
                                        Created = DateTime.Now,
                                        Number = DEBTS_FROM_ONE_C_UK_KEY,
                                        Updated = DateTime.Now,
                                        DateFrom = DateTime.Now,
                                        ServiceNumber = DEBTS_FROM_ONE_C_UK_KEY,
                                        Comment = DEBTS_FROM_ONE_C_UK_KEY,
                                        IsFullyPlaced = true,
                                        IsPartiallyPlaced = true,
                                        SupplyInvoiceOrderItems = new List<SupplyInvoiceOrderItem> {
                                            new() {
                                                Created = DateTime.Now,
                                                Qty = 1,
                                                Updated = DateTime.Now,
                                                UnitPrice = syncAccounting.Value,
                                                RowNumber = 1,
                                                ProductIsImported = true,
                                                ProductId = productDev.Id
                                            }
                                        },
                                        PackingLists = new List<PackingList> {
                                            new() {
                                                Created = DateTime.Now,
                                                Updated = DateTime.Now,
                                                FromDate = DateTime.Now,
                                                InvNo = DEBTS_FROM_ONE_C_UK_KEY,
                                                No = DEBTS_FROM_ONE_C_UK_KEY,
                                                Comment = DEBTS_FROM_ONE_C_UK_KEY,
                                                IsPlaced = true,
                                                PackingListPackageOrderItems = new List<PackingListPackageOrderItem> {
                                                    new() {
                                                        Created = DateTime.Now,
                                                        Qty = 1,
                                                        Updated = DateTime.Now,
                                                        GrossWeight = 1,
                                                        IsPlaced = true,
                                                        NetWeight = 1,
                                                        UnitPrice = syncAccounting.Value,
                                                        UnitPriceEur = amount,
                                                        ExchangeRateAmount = govExchangeRateFromInvoiceAmount,
                                                        PlacedQty = 1,
                                                        AccountingGrossUnitPriceEur = amount,
                                                        ExchangeRateAmountUahToEur = govExchangeRate.Amount,
                                                        ProductIsImported = true
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            };

                            order.SupplyInvoices.ForEach(x => {
                                x.SupplyInvoiceOrderItems.ForEach(invoiceItem => {
                                    if (order.SupplyOrderItems.Any(x => x.ProductId == invoiceItem.ProductId))
                                        invoiceItem.SupplyOrderItemId = order.SupplyOrderItems.FirstOrDefault(x => x.ProductId == invoiceItem.ProductId)?.Id;
                                });
                            });

                            order.Id = accountingSyncRepository.Add(order);

                            SupplyOrderItem supplyOrderItem = order.SupplyOrderItems.First();

                            supplyOrderItem.SupplyOrderId = order.Id;
                            supplyOrderItem.Id = accountingSyncRepository.Add(supplyOrderItem);

                            SupplyInvoice supplyInvoice = order.SupplyInvoices.First();

                            supplyInvoice.SupplyOrderId = order.Id;
                            supplyInvoice.Id = accountingSyncRepository.Add(supplyInvoice);

                            SupplyInvoiceOrderItem supplyInvoiceOrderItem = supplyInvoice.SupplyInvoiceOrderItems.First();

                            supplyInvoiceOrderItem.SupplyInvoiceId = supplyInvoice.Id;
                            supplyInvoiceOrderItem.SupplyOrderItemId = order.SupplyOrderItems.First().Id;
                            supplyInvoiceOrderItem.Id = accountingSyncRepository.Add(supplyInvoiceOrderItem);

                            PackingList packingList = supplyInvoice.PackingLists.First();

                            packingList.SupplyInvoiceId = supplyInvoice.Id;
                            packingList.Id = accountingSyncRepository.Add(packingList);

                            PackingListPackageOrderItem packingListPackageOrderItem = packingList.PackingListPackageOrderItems.First();

                            packingListPackageOrderItem.PackingListId = packingList.Id;
                            packingListPackageOrderItem.SupplyInvoiceOrderItemId = supplyInvoice.SupplyInvoiceOrderItems.First().Id;

                            packingListPackageOrderItem.Id = accountingSyncRepository.Add(packingListPackageOrderItem);

                            SupplyPaymentTask task = new() {
                                Comment = DEBTS_FROM_ONE_C_UK_KEY,
                                Created = DateTime.Now,
                                Updated = DateTime.Now,
                                UserId = currentUser.Id,
                                TaskAssignedTo = 0,
                                TaskStatus = 0,
                                GrossPrice = amount,
                                NetPrice = amount,
                                IsAccounting = forAmg,
                                IsImportedFromOneC = true
                            };

                            task.Id = accountingSyncRepository.Add(task);

                            accountingSyncRepository.Add(new SupplyOrderPaymentDeliveryProtocol {
                                Value = amount,
                                Created = DateTime.Now,
                                SupplyPaymentTaskId = task.Id,
                                Updated = DateTime.Now,
                                UserId = currentUser.Id,
                                SupplyInvoiceId = supplyInvoice.Id,
                                SupplyOrderPaymentDeliveryProtocolKeyId = protocolPaymentKey.Id,
                                Discount = 100,
                                IsAccounting = forAmg
                            });

                            accountingSyncRepository.Add(new SupplyInformationDeliveryProtocol {
                                Created = DateTime.Now,
                                SupplyOrderId = order.Id,
                                Updated = DateTime.Now,
                                UserId = currentUser.Id,
                                SupplyInvoiceId = supplyInvoice.Id,
                                SupplyInformationDeliveryProtocolKeyId = informationProtocolKey.Id,
                                IsDefault = true
                            });

                            Consignment consignment = new() {
                                OrganizationId = clientAgreement.Agreement.OrganizationId.Value,
                                StorageId = storageDev.Id,
                                FromDate = DateTime.Now,
                                ProductIncome = new ProductIncome {
                                    Comment = DEBTS_FROM_ONE_C_UK_KEY,
                                    Number = "00000000000",
                                    FromDate = DateTime.Now,
                                    UserId = currentUser.Id,
                                    StorageId = storageDev.Id
                                },
                                IsImportedFromOneC = true
                            };

                            consignment.ProductIncomeId = accountingSyncRepository.Add(consignment.ProductIncome);

                            consignment.Id = accountingSyncRepository.Add(consignment);

                            ConsignmentItem consignmentItem =
                                new() {
                                    ProductId = productDev.Id,
                                    ProductIncomeItem = new ProductIncomeItem {
                                        ProductIncomeId = consignment.ProductIncomeId,
                                        Qty = 1,
                                        RemainingQty = 0,
                                        PackingListPackageOrderItemId = packingListPackageOrderItem.Id
                                    },
                                    NetPrice = 1,
                                    Price = 1,
                                    Weight = 1,
                                    RemainingQty = 0,
                                    Qty = 1,
                                    DutyPercent = 0,
                                    ProductSpecification = new ProductSpecification {
                                        ProductId = productDev.Id,
                                        AddedById = currentUser.Id,
                                        Locale = UaCulture,
                                        SpecificationCode = DEBTS_FROM_ONE_C_UK_KEY,
                                        Name = DEBTS_FROM_ONE_C_UK_KEY,
                                        DutyPercent = 0
                                    },
                                    ConsignmentId = consignment.Id,
                                    ExchangeRate = 1
                                };

                            consignmentItem.ProductIncomeItemId =
                                accountingSyncRepository.Add(consignmentItem.ProductIncomeItem);

                            consignmentItem.ProductSpecificationId =
                                accountingSyncRepository.Add(consignmentItem.ProductSpecification);

                            consignmentItem.Id = accountingSyncRepository.Add(consignmentItem);

                            accountingSyncRepository.Add(new ConsignmentItemMovement {
                                Qty = 1,
                                IsIncomeMovement = true,
                                ProductIncomeItemId = consignmentItem.ProductIncomeItemId,
                                ConsignmentItemId = consignmentItem.Id,
                                MovementType = ConsignmentItemMovementType.Income
                            });
                        }
                    }
                }
            }

            IEnumerable<SupplyOrganization> supplyOrganizations = accountingSyncRepository.GetAllSupplyOrganizations();

            ConsumablesStorage consumablesStorage = accountingSyncRepository.GetConsumablesStorageByKey(DEBTS_FROM_ONE_C_UK_KEY);

            if (consumablesStorage == null) {
                Organization organizationAmg = accountingSyncRepository.GetByName(DEFAULT_ORGANIZATION_AMG);

                consumablesStorage = new ConsumablesStorage {
                    Created = DateTime.Now,
                    Description = DEBTS_FROM_ONE_C_UK_KEY,
                    Name = DEBTS_FROM_ONE_C_UK_KEY,
                    OrganizationId = organizationAmg.Id,
                    ResponsibleUserId = currentUser.Id,
                    Updated = DateTime.Now
                };

                consumablesStorage.Id = accountingSyncRepository.Add(consumablesStorage);
            }

            ConsumableProductCategory consumableProductCategory = accountingSyncRepository.GetSupplyServiceConsumablesProductCategory();

            if (consumableProductCategory == null) {
                consumableProductCategory = new ConsumableProductCategory {
                    Created = DateTime.Now,
                    Description = DEBTS_FROM_ONE_C_UK_KEY,
                    Name = DEBTS_FROM_ONE_C_UK_KEY,
                    Updated = DateTime.Now,
                    IsSupplyServiceCategory = true
                };

                consumableProductCategory.Id = accountingSyncRepository.Add(consumableProductCategory);
            }

            ConsumableProduct consumableProduct = accountingSyncRepository.GetConsumablesProductByKey(DEBTS_FROM_ONE_C_UK_KEY);

            if (consumableProduct == null) {
                consumableProduct = new ConsumableProduct {
                    ConsumableProductCategoryId = consumableProductCategory.Id,
                    Created = DateTime.Now,
                    Name = DEBTS_FROM_ONE_C_UK_KEY,
                    Updated = DateTime.Now
                };

                consumableProduct.Id = accountingSyncRepository.Add(consumableProduct);
            }

            PaymentCostMovement paymentCostMovement = accountingSyncRepository.GetPaymentCostMovementByKey(DEBTS_FROM_ONE_C_UK_KEY);

            if (paymentCostMovement == null) {
                paymentCostMovement = new PaymentCostMovement {
                    Created = DateTime.Now,
                    OperationName = DEBTS_FROM_ONE_C_UK_KEY,
                    Updated = DateTime.Now
                };

                paymentCostMovement.Id = accountingSyncRepository.Add(paymentCostMovement);
            }

            if (forAmg)
                accountingSyncRepository.CleanDebtsAndBalancesForSupplyOrganizations();

            foreach (SupplyOrganization supplyOrganization in supplyOrganizations.Where(x => forAmg ? x.SourceAmgCode.HasValue : x.SourceFenixCode.HasValue)) {
                IEnumerable<SupplyOrganizationAgreement> agreements = supplyOrganization.SupplyOrganizationAgreements
                    .Where(x => forAmg ? x.SourceAmgCode.HasValue : x.SourceFenixCode.HasValue);

                if (forAmg)
                    agreements = agreements.Where(x => x.Organization.Name == DEFAULT_ORGANIZATION_AMG);
                else
                    agreements = agreements.Where(x => x.Organization.Name == DEFAULT_ORGANIZATION_FENIX);

                if (!agreements.Any()) continue;

                foreach (SupplyOrganizationAgreement agreement in agreements) {
                    IEnumerable<SyncAccounting> accounting =
                        forAmg
                            ? accountingSyncRepository.GetAmgSyncAccountingFiltered(
                                supplyOrganization.SourceAmgCode.Value,
                                agreement.Name,
                                agreement.Organization.Name,
                                agreement.Currency.CodeOneC
                            )
                            : accountingSyncRepository
                                .GetSyncAccountingFiltered(
                                    supplyOrganization.SourceFenixCode.Value,
                                    agreement.Name,
                                    agreement.Organization.Name,
                                    agreement.Currency.CodeOneC
                                );

                    if (!accounting.Any()) continue;

                    SyncAccounting syncAccounting = accounting.First();

                    syncAccounting.Value = accounting.Sum(a => a.Value);

                    if (syncAccounting.Value == 0)
                        continue;

                    PaymentCurrencyRegister paymentCurrencyRegisterDev;

                    if (agreement.Currency.Code == eur.Code)
                        paymentCurrencyRegisterDev = paymentCurrencyRegisterEurDev;
                    else if (agreement.Currency.Code == usd.Code)
                        paymentCurrencyRegisterDev = paymentCurrencyRegisterUsdDev;
                    else if (agreement.Currency.Code == pln.Code)
                        paymentCurrencyRegisterDev = paymentCurrencyRegisterPlnDev;
                    else
                        paymentCurrencyRegisterDev = paymentCurrencyRegisterUahDev;

                    if (syncAccounting.Value < decimal.Zero) {
                        syncAccounting.Value = Math.Abs(syncAccounting.Value);

                        bool isAccounting = agreement.Organization.Name.Equals(DEFAULT_ORGANIZATION_AMG);

                        OutcomePaymentOrder outcomePaymentOrder = new() {
                            Comment = DEBTS_FROM_ONE_C_UK_KEY,
                            Created = DateTime.Now,
                            FromDate = DateTime.Now,
                            Number = DEBTS_FROM_ONE_C_UK_KEY,
                            OrganizationId = agreement.Organization.Id,
                            PaymentCurrencyRegisterId = paymentCurrencyRegisterDev.Id,
                            Updated = DateTime.Now,
                            UserId = currentUser.Id,
                            ConsumableProductOrganizationId = supplyOrganization.Id,
                            AfterExchangeAmount = syncAccounting.Value,
                            SupplyOrganizationAgreementId = agreement.Id,
                            IsAccounting = isAccounting,
                            IsManagementAccounting = !isAccounting
                        };

                        ExchangeRate exchangeRate =
                            accountingSyncRepository
                                .GetByCurrencyIdAndCode(
                                    agreement.CurrencyId,
                                    eur.Code,
                                    TimeZoneInfo.ConvertTimeToUtc(outcomePaymentOrder.FromDate)
                                );

                        outcomePaymentOrder.ExchangeRate = exchangeRate?.Amount ?? 0;

                        if (outcomePaymentOrder.ExchangeRate > 0) {
                            if (agreement.Currency.Code.ToLower().Equals("uah") ||
                                agreement.Currency.Code.ToLower().Equals("pln"))
                                outcomePaymentOrder.Amount =
                                    Math.Round(outcomePaymentOrder.AfterExchangeAmount / outcomePaymentOrder.ExchangeRate, 14);
                            else
                                outcomePaymentOrder.Amount =
                                    Math.Round(outcomePaymentOrder.AfterExchangeAmount * outcomePaymentOrder.ExchangeRate, 14);
                        } else {
                            CrossExchangeRate crossExchangeRate =
                                accountingSyncRepository
                                    .GetByCurrenciesIds(
                                        eur.Id,
                                        agreement.Currency.Id
                                    );

                            if (crossExchangeRate != null) {
                                outcomePaymentOrder.ExchangeRate = crossExchangeRate.Amount;

                                outcomePaymentOrder.Amount =
                                    Math.Round(outcomePaymentOrder.AfterExchangeAmount / outcomePaymentOrder.ExchangeRate, 14);
                            } else {
                                crossExchangeRate =
                                    accountingSyncRepository
                                        .GetByCurrenciesIds(
                                            agreement.Currency.Id,
                                            eur.Id
                                        );

                                outcomePaymentOrder.ExchangeRate = crossExchangeRate?.Amount ?? 1;

                                outcomePaymentOrder.Amount =
                                    Math.Round(outcomePaymentOrder.AfterExchangeAmount / outcomePaymentOrder.ExchangeRate, 14);
                            }
                        }

                        outcomePaymentOrder.Id = accountingSyncRepository.Add(outcomePaymentOrder);

                        accountingSyncRepository.Add(new PaymentMovementOperation {
                            Created = DateTime.Now,
                            Updated = DateTime.Now,
                            OutcomePaymentOrderId = outcomePaymentOrder.Id,
                            PaymentMovementId = paymentMovementDev.Id
                        });

                        if (forAmg)
                            agreement.AccountingCurrentAmount =
                                Math.Round(
                                    outcomePaymentOrder.AfterExchangeAmount,
                                    2
                                );
                        else
                            agreement.CurrentAmount =
                                Math.Round(
                                    outcomePaymentOrder.AfterExchangeAmount,
                                    2
                                );

                        accountingSyncRepository.Update(agreement);
                    } else {
                        SupplyPaymentTask task = new() {
                            Created = DateTime.Now,
                            Updated = DateTime.Now,
                            Comment = DEBTS_FROM_ONE_C_UK_KEY,
                            UserId = currentUser.Id,
                            PayToDate = DateTime.Now,
                            TaskAssignedTo = TaskAssignedTo.ConsumablesOrder,
                            GrossPrice = syncAccounting.Value,
                            NetPrice = syncAccounting.Value,
                            IsImportedFromOneC = true
                        };

                        task.Id = accountingSyncRepository.Add(task);

                        ConsumablesOrder order = new() {
                            Comment = DEBTS_FROM_ONE_C_UK_KEY,
                            Created = DateTime.Now,
                            Number = DEBTS_FROM_ONE_C_UK_KEY,
                            Updated = DateTime.Now,
                            OrganizationFromDate = DateTime.Now,
                            OrganizationNumber = DEBTS_FROM_ONE_C_UK_KEY,
                            UserId = currentUser.Id,
                            ConsumablesStorageId = consumablesStorage.Id,
                            SupplyPaymentTaskId = task.Id
                        };

                        order.Id = accountingSyncRepository.Add(order);

                        ConsumablesOrderItem orderItem = new() {
                            Created = DateTime.Now,
                            ConsumableProductCategoryId = consumableProductCategory.Id,
                            ConsumableProductId = consumableProduct.Id,
                            ConsumableProductOrganizationId = supplyOrganization.Id,
                            ConsumablesOrderId = order.Id,
                            TotalPrice = syncAccounting.Value,
                            Qty = 1,
                            Updated = DateTime.Now,
                            PricePerItem = syncAccounting.Value,
                            SupplyOrganizationAgreementId = agreement.Id
                        };

                        orderItem.Id = accountingSyncRepository.Add(orderItem);

                        PaymentCostMovementOperation costMovementOperation = new() {
                            Created = DateTime.Now,
                            ConsumablesOrderItemId = orderItem.Id,
                            PaymentCostMovementId = paymentCostMovement.Id,
                            Updated = DateTime.Now
                        };

                        accountingSyncRepository.Add(costMovementOperation);
                    }
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ACCOUNTING_SYNC_END], true));
        } catch (Exception exc) {
            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_ERROR], true, true));

            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddDataSyncLogMessage(
                        "SYNC_ERROR Accounting",
                        $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                        JsonConvert.SerializeObject(new {
                            exc.Message,
                            exc.StackTrace
                        })
                    )
                );
        }
    }

    private static decimal GetGovExchangeRateOnDateToUah(
        Currency from,
        DateTime onDate,
        IAccountingSyncRepository accountingSyncRepository,
        ICurrencyRepository currencyRepository) {
        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        if (from.Id.Equals(uah.Id))
            return 1m;

        GovExchangeRate govExchangeRate =
            accountingSyncRepository
                .GetGovByCurrencyIdAndCode(
                    uah.Id, from.Code, onDate);

        return govExchangeRate?.Amount ?? 1m;
    }
}