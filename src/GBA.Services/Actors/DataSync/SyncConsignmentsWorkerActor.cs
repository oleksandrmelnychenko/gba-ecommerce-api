using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Synchronizations;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.EntityHelpers.Supplies.PackingLists;
using GBA.Domain.Messages.Communications.Hubs;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.DataSync;
using GBA.Domain.Messages.Logging;
using GBA.Domain.Messages.Supplies.Invoices;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Domain.TranslationEntities;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using static GBA.Common.Helpers.DateTimeHelper;

namespace GBA.Services.Actors.DataSync;

public sealed class SyncConsignmentsWorkerActor : ReceiveActor {
    private const string DEFAULT_AGREEMENT_NAME = "�������� ������";

    private const string DEFAULT_ORGANIZATION_AMG = "��� ���� �������Ļ";

    private const string DEFAULT_ORGANIZATION_FENIX = "������";

    private const string SUPPLY_PRICING_NAME = "��";

    private const double DEFAULT_VAT_RATE_VALUE = 20;

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;

    private readonly IDataSyncRepositoriesFactory _dataSyncRepositoriesFactory;

    private readonly string _defaultComment = "��� ������� � 1�.";
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    private readonly List<string> _vatOrganizationNames;

    private readonly string[] eurCodes;

    private readonly string[] organizationNames;

    private readonly string[] plnCodes;

    private readonly string[] storageOneNames;

    private readonly string[] storageThreeNames;

    private readonly string[] usdCodes;

    public SyncConsignmentsWorkerActor(
        IStringLocalizer<SharedResource> localizer,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDataSyncRepositoriesFactory dataSyncRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _localizer = localizer;

        _connectionFactory = connectionFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        _dataSyncRepositoriesFactory = dataSyncRepositoriesFactory;

        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        _vatOrganizationNames = new List<string> {
            "��� ���� �������Ļ",
            "��� �����²˲ ����� ²��������"
        };

        organizationNames = new[] {
            DEFAULT_ORGANIZATION_AMG,
            DEFAULT_ORGANIZATION_FENIX,
            "��� ������� ���� ��������",
            "��� ���������� ����� �����",
            "��� ������� ��� �����������"
        };

        eurCodes = new[] { "978", "979", "555" };

        usdCodes = new[] { "840", "556" };

        plnCodes = new[] { "830", "831", "835" };

        storageThreeNames = new[] { "�����-3", "����� -3" };

        storageOneNames = new[] { "�����-1", "����� -1" };

        Receive<SynchronizeConsignmentsMessage>(ProcessSynchronizeConsignmentsMessage);
    }

    private void ProcessSynchronizeConsignmentsMessage(SynchronizeConsignmentsMessage message) {
        using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
        using IDbConnection remoteSyncConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection amgOneCConnection = _connectionFactory.NewAmgOneCSqlConnection();
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(remoteSyncConnection);

        IActorRef hubSenderActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

        User currentUser = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

        _dataSyncRepositoriesFactory
            .NewDataSyncOperationRepository(remoteSyncConnection)
            .Add(new DataSyncOperation {
                UserId = currentUser.Id,
                OperationType = DataSyncOperationType.Consignments,
                ForAmg = message.ForAmg
            });

        SyncExchangeRates(
            currentUser,
            message.ForAmg,
            amgOneCConnection,
            oneCConnection,
            remoteSyncConnection,
            hubSenderActorRef
        );

        SynchronizeConsignments(hubSenderActorRef, oneCConnection, remoteSyncConnection, amgOneCConnection, currentUser, message.ForAmg);

        ActorReferenceManager.Instance.Get(DataSyncActorNames.DATA_SYNC_WORKER_ACTOR)
            .Tell(new StartDataSyncWorkMessage(message.SyncEntityTypes, message.UserNetId, message.ForAmg));
    }

    private void SyncExchangeRates(
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

    private void SynchronizeConsignments(
        IActorRef hubSenderActorRef,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgOneCConnection,
        User currentUser,
        bool forAmg) {
        try {
            IConsignmentsSyncRepository consignmentsSyncRepository =
                _dataSyncRepositoriesFactory.NewConsignmentsSyncRepository(oneCConnection, amgOneCConnection, remoteSyncConnection);
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
                    .NewClientsSyncRepository(oneCConnection, remoteSyncConnection, amgOneCConnection);

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.TAX_INSPECTIONS_SYNC_START]));

            IEnumerable<SyncTaxInspection> syncTaxInspections =
                forAmg ? consignmentsSyncRepository.GetAmgAllSyncTaxInspections() : consignmentsSyncRepository.GetAllSyncTaxInspections();

            List<TaxInspection> taxInspections =
                consignmentsSyncRepository.GetAllTaxInspections();

            foreach (SyncTaxInspection syncTaxInspection in syncTaxInspections) {
                TaxInspection taxInspection =
                    taxInspections
                        .FirstOrDefault(i => i.InspectionNumber == syncTaxInspection.TaxInspectionCode
                                             || i.InspectionName == syncTaxInspection.TaxInspectionName);

                if (taxInspection == null) {
                    taxInspection = new TaxInspection {
                        InspectionNumber = syncTaxInspection.TaxInspectionCode,
                        InspectionType = syncTaxInspection.TypeDPI,
                        InspectionName = syncTaxInspection.TaxInspectionName,
                        InspectionRegionName = syncTaxInspection.NameAdminDistrict,
                        InspectionRegionCode = syncTaxInspection.CodeAdminDistrict,
                        InspectionAddress = syncTaxInspection.Address,
                        InspectionUSREOU = syncTaxInspection.EDRPOU
                    };

                    taxInspections.Add(taxInspection);

                    taxInspection.Id = consignmentsSyncRepository.Add(taxInspection);
                } else {
                    taxInspection.InspectionNumber = syncTaxInspection.TaxInspectionCode;
                    taxInspection.InspectionType = syncTaxInspection.TypeDPI;
                    taxInspection.InspectionName = syncTaxInspection.TaxInspectionName;
                    taxInspection.InspectionRegionName = syncTaxInspection.NameAdminDistrict;
                    taxInspection.InspectionRegionCode = syncTaxInspection.CodeAdminDistrict;
                    taxInspection.InspectionAddress = syncTaxInspection.Address;
                    taxInspection.InspectionUSREOU = syncTaxInspection.EDRPOU;
                    taxInspection.Deleted = false;

                    consignmentsSyncRepository.Update(taxInspection);
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.TAX_INSPECTIONS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.ORGANIZATIONS_SYNC_START]));

            IEnumerable<SyncOrganization> syncOrganizations =
                forAmg ? consignmentsSyncRepository.GetAmgAllSyncOrganizations() : consignmentsSyncRepository.GetAllSyncOrganizations(organizationNames);

            List<Organization> organizations =
                consignmentsSyncRepository.GetAllOrganizations();

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

            IEnumerable<Currency> currencies =
                consignmentsSyncRepository.GetAllCurrencies();

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

                consignmentsSyncRepository.Update(currency);
            }

            IEnumerable<VatRate> vatRates = clientsSyncRepository.GetAllVatRates();

            if (!vatRates.Any(x => x.Value == 20)) {
                clientsSyncRepository.AddVatRate(new VatRate {
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                    Value = 20
                });

                vatRates = clientsSyncRepository.GetAllVatRates();
            }

            foreach (SyncOrganization syncOrganization in syncOrganizations) {
                Organization organization =
                    organizations.FirstOrDefault(o => !string.IsNullOrEmpty(o.Name) && o.Name.Equals(syncOrganization.OrganizationName));

                IEnumerable<SyncOrganizationAddress> syncOrganizationAddresses =
                    forAmg
                        ? consignmentsSyncRepository.GetAmgOrganizationAddresses(syncOrganization.OrganizationCode)
                        : consignmentsSyncRepository.GetOrganizationAddresses(syncOrganization.OrganizationCode);

                if (organization == null) {
                    organization = new Organization {
                        Name = syncOrganization.OrganizationName,
                        NameUk = syncOrganization.OrganizationName,
                        NamePl = syncOrganization.OrganizationName,
                        FullName = syncOrganization.OrganizationFullName,
                        Culture =
                            syncOrganization.MainCurrencyCode == "830"
                                ? "pl"
                                : "uk",
                        CurrencyId = currencies.FirstOrDefault(c => c.CodeOneC == syncOrganization.MainCurrencyCode)?.Id,
                        Code = syncOrganization.OrganizationPrefix,
                        Address = string.Empty,
                        IsIndividual = syncOrganization.IsIndividual,
                        RegistrationDate = syncOrganization.DateRegistration,
                        RegistrationNumber = syncOrganization.NumberCertification,
                        SROI = syncOrganization.CodeKVED,
                        TIN = syncOrganization.IPN,
                        USREOU = syncOrganization.EDRPOU,
                        Manager = syncOrganization.Manager,
                        TaxInspectionId = taxInspections.FirstOrDefault(i => i.InspectionName == syncOrganization.TaxInspectionName)?.Id
                    };

                    if (organization.Name == DEFAULT_ORGANIZATION_AMG) {
                        organization.VatRateId = vatRates.FirstOrDefault(x => x.Value == DEFAULT_VAT_RATE_VALUE)?.Id;
                        organization.IsVatAgreements = true;
                    } else {
                        organization.IsVatAgreements = false;
                    }

                    foreach (SyncOrganizationAddress address in syncOrganizationAddresses)
                        if (address.AddressType == SyncClientAddressType.Address) {
                            organization.Address = address.Value;
                        } else {
                            if (string.IsNullOrEmpty(organization.PhoneNumber))
                                organization.PhoneNumber = address.Value;
                            else
                                organization.PhoneNumber += $", {address.Value}";
                        }

                    organization.Id = consignmentsSyncRepository.Add(organization);

                    consignmentsSyncRepository.Add(new OrganizationTranslation {
                        OrganizationId = organization.Id,
                        Name = organization.NamePl,
                        CultureCode = "pl"
                    });

                    consignmentsSyncRepository.Add(new OrganizationTranslation {
                        OrganizationId = organization.Id,
                        Name = organization.NameUk,
                        CultureCode = "uk"
                    });

                    organizations.Add(organization);
                } else {
                    organization.Name = syncOrganization.OrganizationName;
                    organization.NameUk = syncOrganization.OrganizationName;
                    organization.NamePl = syncOrganization.OrganizationName;
                    organization.FullName = syncOrganization.OrganizationFullName;
                    organization.Culture =
                        syncOrganization.MainCurrencyCode == "830"
                            ? "pl"
                            : "uk";
                    organization.CurrencyId = currencies.FirstOrDefault(c => c.CodeOneC == syncOrganization.MainCurrencyCode)?.Id;
                    organization.Code = syncOrganization.OrganizationPrefix;
                    organization.Address = string.Empty;
                    organization.IsIndividual = syncOrganization.IsIndividual;
                    organization.RegistrationDate = syncOrganization.DateRegistration;
                    organization.RegistrationNumber = syncOrganization.NumberCertification;
                    organization.SROI = syncOrganization.CodeKVED;
                    organization.TIN = syncOrganization.IPN;
                    organization.USREOU = syncOrganization.EDRPOU;
                    organization.TaxInspectionId = taxInspections.FirstOrDefault(i => i.InspectionName == syncOrganization.TaxInspectionName)?.Id;
                    organization.PhoneNumber = string.Empty;
                    organization.Manager = !forAmg && syncOrganization.OrganizationName.Equals(DEFAULT_ORGANIZATION_AMG) ? organization.Manager : syncOrganization.Manager;
                    organization.Deleted = false;

                    if (organization.Name == DEFAULT_ORGANIZATION_AMG) {
                        organization.VatRateId = vatRates.FirstOrDefault(x => x.Value == DEFAULT_VAT_RATE_VALUE)?.Id;
                        organization.IsVatAgreements = true;
                    } else {
                        organization.IsVatAgreements = false;
                    }

                    foreach (SyncOrganizationAddress address in syncOrganizationAddresses)
                        if (address.AddressType == SyncClientAddressType.Address) {
                            organization.Address = address.Value;
                        } else {
                            if (string.IsNullOrEmpty(organization.PhoneNumber))
                                organization.PhoneNumber = address.Value;
                            else
                                organization.PhoneNumber += $", {address.Value}";
                        }

                    consignmentsSyncRepository.Update(organization);

                    OrganizationTranslation plTranslation = organization.OrganizationTranslations.FirstOrDefault(t => t.CultureCode == "pl");

                    if (plTranslation == null) {
                        consignmentsSyncRepository.Add(new OrganizationTranslation {
                            OrganizationId = organization.Id,
                            Name = organization.NamePl,
                            CultureCode = "pl"
                        });
                    } else {
                        plTranslation.Name = organization.NamePl;

                        consignmentsSyncRepository.Update(plTranslation);
                    }

                    OrganizationTranslation ukTranslation = organization.OrganizationTranslations.FirstOrDefault(t => t.CultureCode == "uk");

                    if (ukTranslation == null) {
                        consignmentsSyncRepository.Add(new OrganizationTranslation {
                            OrganizationId = organization.Id,
                            Name = organization.NameUk,
                            CultureCode = "uk"
                        });
                    } else {
                        ukTranslation.Name = organization.NamePl;

                        consignmentsSyncRepository.Update(ukTranslation);
                    }
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.ORGANIZATIONS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.STORAGES_SYNC_START]));

            IEnumerable<SyncStorage> syncStorages =
                forAmg ? consignmentsSyncRepository.GetAmgAllSyncStorages() : consignmentsSyncRepository.GetAllSyncStorages();

            List<Storage> storages =
                consignmentsSyncRepository.GetAllStorages();

            IEnumerable<Organization> organizationForStorages = organizations.Where(x => !x.Deleted && !string.IsNullOrEmpty(x.Name));

            foreach (SyncStorage syncStorage in syncStorages) {
                Organization organization = organizationForStorages.FirstOrDefault(o => o.Name == syncStorage.OrganizationName);

                if (storageOneNames.Contains(syncStorage.StorageName) && !forAmg) {
                    organization = organizationForStorages.First(o => o.Name == DEFAULT_ORGANIZATION_FENIX);
                } else {
                    if (organization == null || string.IsNullOrEmpty(syncStorage.OrganizationName)) {
                        if (forAmg)
                            organization = organizationForStorages.First(o => o.Name == DEFAULT_ORGANIZATION_AMG);
                        else
                            organization = organizationForStorages.First(o => o.Name == DEFAULT_ORGANIZATION_FENIX);
                    }
                }

                Storage storage =
                    storages
                        .FirstOrDefault(s => s.Name == syncStorage.StorageName);

                if (storage == null) {
                    storage = new Storage {
                        Name = syncStorage.StorageName,
                        Locale = organization.Culture,
                        ForDefective = false,
                        ForVatProducts = _vatOrganizationNames.Any(n => n.Equals(organization.Name)),
                        OrganizationId = organization.Id
                    };

                    storage.Id = consignmentsSyncRepository.Add(storage);

                    storages.Add(storage);
                } else {
                    storage.Locale = organization.Culture;
                    storage.ForDefective = false;
                    storage.ForVatProducts = _vatOrganizationNames.Any(n => n.Equals(organization.Name));
                    storage.OrganizationId = organization.Id;
                    storage.Deleted = false;

                    consignmentsSyncRepository.Update(storage);
                }

                organization.StorageId = storage.Id;

                consignmentsSyncRepository.Update(organization);
            }

            string[] existStorageNames = storages.Select(x => x.Name).ToArray();

            IEnumerable<string> storageNames =
                forAmg
                    ? consignmentsSyncRepository.GetAmgStorageNamesFromSyncConsignmentsExceptProvided(storages.Select(s => s.Name))
                    : consignmentsSyncRepository
                        .GetStorageNamesFromSyncConsignmentsExceptProvided(
                            storages.Select(s => s.Name)
                        );

            Organization defaultOrganization =
                organizations
                    .FirstOrDefault(o => o.Name.Equals("������"));

            storageNames = storageNames.Where(x => !existStorageNames.Contains(x));

            foreach (string storageName in storageNames) {
                Storage storage = new() {
                    Name = storageName,
                    ForDefective = true,
                    ForVatProducts = false,
                    Locale = "uk",
                    OrganizationId = defaultOrganization?.Id
                };

                storage.Id = consignmentsSyncRepository.Add(storage);

                storages.Add(storage);
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.STORAGES_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(GetCurrentDateInString() + _localizer[SharedResourceNames.CONSIGNMENTS_SYNC_START]));

            List<Pricing> pricings =
                clientsSyncRepository.GetAllPricings();

            IEnumerable<SyncConsignment> syncConsignments =
                forAmg
                    ? consignmentsSyncRepository.GetAmgAllSyncConsignments(storages.Select(s => s.Name))
                    : consignmentsSyncRepository.GetAllSyncConsignments(storages.Select(s => s.Name));

            //TODO first test
            // long[] productAmgCodes = new long[] {
            //     427101
            //  };
            //
            // long[] productFenixCodes = new long[] {
            //     427101
            //  };

            // syncConsignments = syncConsignments
            // .Where(x => x.DocumentArrivalNumber == "WOKA2023060301");
            // .Where(x => forAmg ? productAmgCodes.Contains(x.ProductCode) : productFenixCodes.Contains(x.ProductCode));

            IEnumerable<Client> clients =
                consignmentsSyncRepository.GetAllClients();

            StringBuilder builder = new();

            builder.Append("(-1");

            foreach (long productCode in syncConsignments.Select(p => p.ProductCode).Distinct()) builder.Append($",{productCode}");

            builder.Append(")");

            IEnumerable<Consignment> consignments =
                consignmentsSyncRepository.GetAllConsignmentsToDelete();

            if (forAmg) {
                consignmentsSyncRepository.CleanAllConsignmentsToDelete();

                foreach (Consignment consignmentToDelete in consignments)
                foreach (ConsignmentItem consignmentItem in consignmentToDelete.ConsignmentItems) {
                    if (!consignmentItem.Product.ProductAvailabilities.Any()) continue;

                    ProductAvailability availability = consignmentItem.Product.ProductAvailabilities.First();

                    availability = consignmentsSyncRepository.GetProductAvailabilityById(availability.Id);

                    availability.Amount -= consignmentItem.RemainingQty;

                    if (availability.Amount < 0) availability.Amount = 0;

                    consignmentsSyncRepository.Update(availability);
                }
            }

            IEnumerable<Product> products =
                consignmentsSyncRepository.GetAllProductsByProductCodes(builder.ToString(), forAmg);

            IEnumerable<IGrouping<Tuple<string, string>, SyncConsignment>> groupedSyncConsignments =
                syncConsignments.GroupBy(x => new Tuple<string, string>(x.DocumentIdInString, x.StorageName));

            IEnumerable<SupplyOrganization> supplyOrganizations = consignmentsSyncRepository.GetAllSupplyOrganizations(_defaultComment);

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

            ConsumableProductCategory consumableProductCategory = consignmentsSyncRepository.GetSupplyServiceConsumablesProductCategory();

            if (consumableProductCategory == null) {
                consumableProductCategory = new ConsumableProductCategory {
                    Created = DateTime.Now,
                    Description = _defaultComment,
                    Name = _defaultComment,
                    Updated = DateTime.Now,
                    IsSupplyServiceCategory = true
                };

                consumableProductCategory.Id = consignmentsSyncRepository.Add(consumableProductCategory);
            }

            ConsumableProduct consumableProduct = consignmentsSyncRepository.GetConsumablesProductByKey(_defaultComment);

            if (consumableProduct == null) {
                consumableProduct = new ConsumableProduct {
                    ConsumableProductCategoryId = consumableProductCategory.Id,
                    Created = DateTime.Now,
                    Name = _defaultComment,
                    Updated = DateTime.Now
                };

                consumableProduct.Id = consignmentsSyncRepository.Add(consumableProduct);
            }

            List<Tuple<long, DateTime?, bool, bool, DateTime, decimal>> invoiceWithDatesIds = new();

            Dictionary<long, Tuple<decimal, string>> documentValues = new();

            IEnumerable<SupplyInvoice> existInvoices = consignmentsSyncRepository.GetExistSupplyInvoices();

            IEnumerable<PaymentMovement> paymentMovements = consignmentsSyncRepository.GetAllPaymentMovementOperations();

            string[] paymentMovementsFromGba = paymentMovements.Select(x => x.OperationName).ToArray();

            IEnumerable<string> paymentMovementsFromOneC = forAmg ? consignmentsSyncRepository.GetAmgPaymentMovements() : consignmentsSyncRepository.GetFenixPaymentMovements();

            IEnumerable<string> newPaymentMovements = paymentMovementsFromOneC.Distinct().Where(name => !paymentMovementsFromGba.Contains(name));

            if (newPaymentMovements.Any()) {
                foreach (string newPaymentMovement in newPaymentMovements) {
                    PaymentMovement paymentMovement = new() {
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow,
                        OperationName = newPaymentMovement
                    };

                    paymentMovement.Id = consignmentsSyncRepository.AddPaymentMovement(paymentMovement);

                    PaymentMovementTranslation paymentMovementTranslation = new() {
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow,
                        CultureCode = "uk",
                        Name = newPaymentMovement,
                        PaymentMovementId = paymentMovement.Id
                    };

                    consignmentsSyncRepository.AddPaymentMovementTranslation(paymentMovementTranslation);
                }

                paymentMovements = consignmentsSyncRepository.GetAllPaymentMovementOperations();
            }

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
                                                                               x.Number == firstSyncConsignment.DocumentArrivalNumber &&
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

                if (existInvoice == null || fromInvoice) {
                    IEnumerable<IGrouping<long, SyncConsignment>> groupedProducts =
                        currentSyncConsignments
                            .GroupBy(x => x.ProductCode)
                            .Where(x => x.Select(y => y).Count() > 1);

                    if (groupedProducts.Any())
                        foreach (IGrouping<long, SyncConsignment> groupedProduct in groupedProducts) {
                            IEnumerable<SyncConsignment> existProductConsignments = groupedProduct.Select(x => x);

                            SyncConsignment lastExistConsignments = existProductConsignments.Last();

                            double totalQty = lastExistConsignments.Qty;

                            foreach (SyncConsignment existProductConsignment in existProductConsignments.Where(x => x.IncomeQty.HasValue)) {
                                if (totalQty == 0) {
                                    existProductConsignment.Qty = 0;

                                    continue;
                                }

                                if (totalQty >= existProductConsignment.IncomeQty.Value) {
                                    existProductConsignment.Qty = existProductConsignment.IncomeQty.Value;

                                    totalQty -= existProductConsignment.Qty;
                                } else {
                                    existProductConsignment.Qty = totalQty;

                                    totalQty = 0;
                                }
                            }
                        }
                }

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
                        c.ClientInRole.ClientType.Type == ClientTypeType.Provider &&
                        (forAmg ? c.SourceAmgCode == firstSyncConsignment.ClientCode : c.SourceFenixCode == firstSyncConsignment.ClientCode));

                    if (client == null) continue;

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
                            IsHide = true,
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

                                    vatAmount = decimal.Round(totalValue * (vatFromIncome / (100 + vatFromIncome)), 14, MidpointRounding.AwayFromZero);

                                    totalPriceWithoutVat -= vatAmount;
                                } else {
                                    vatAmount = decimal.Round(specification != null ? vatFromSpecification : syncConsignment.TotalVat, 14, MidpointRounding.AwayFromZero);

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
                                        vatAmount = decimal.Round(totalValue * (vatFromIncome / (100 + vatFromIncome)), 14, MidpointRounding.AwayFromZero);

                                        totalPriceWithoutVat -= vatAmount;
                                    }
                                } else {
                                    vatAmount = decimal.Round(specification != null ? vatFromSpecification : syncConsignment.TotalVat, 14, MidpointRounding.AwayFromZero);

                                    decimal customAndDuty = syncConsignment.CustomsValue + dutyFromSpecification;

                                    if (customAndDuty > 0)
                                        vatFromIncome = decimal.Round(vatAmount * 100 / customAndDuty, 2, MidpointRounding.AwayFromZero);
                                }
                            }

                            decimal netPricePerItem = decimal.Round(totalPriceWithoutVat / Convert.ToDecimal(incomeQty), 14, MidpointRounding.AwayFromZero);

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
                                        //decimal documentValueInEur = firstSyncConsignment.DocumentValue / documentValueExchangeRateAmount;
                                        //decimal totalSpendInEur = consignmentSpend.TotalSpend / serviceExchangeRateAmount;

                                        //valueSpend *= documentValueInEur / totalSpendInEur;

                                        //totalValueSpend += decimal.Round(valueSpend / serviceExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

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

                    IEnumerable<ProductIncome> existIncomes = packigListItems.Select(x => x.ProductIncomeItem.ProductIncome);

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
                            //decimal documentValueInEur = firstSyncConsignment.DocumentValue / documentValueExchangeRateAmount;
                            //decimal totalSpendInEur = consignmentSpend.TotalSpend / serviceExchangeRateAmount;

                            //valueSpend *= documentValueInEur / totalSpendInEur;

                            //totalValueSpend += decimal.Round(valueSpend / serviceExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                            decimal documentValueInEur = firstSyncConsignment.DocumentValue / documentValueExchangeRateAmount;
                            decimal totalInvoicesInEur = consignmentSpend.TotalSpend / serviceExchangeRateAmount;

                            decimal valueSpendInEur = valueSpend / serviceExchangeRateAmount;

                            decimal spendOnCurrentInvoice = documentValueInEur * valueSpendInEur / totalInvoicesInEur;

                            totalValueSpend += decimal.Round(spendOnCurrentInvoice, 14, MidpointRounding.AwayFromZero);
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

                bool isResident = !invoice.SupplyOrder.Client.IsNotResident;

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

                if (!govExchangeRateFromInvoiceAmount.Equals(0)) {
                    if (usdCodes.Contains(currencyfromOrder.CodeOneC)) {
                        GovExchangeRate govExchangeRateUahToEur =
                            govExchangeRateRepository
                                .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateForExchange);

                        decimal cross = govExchangeRateUahToEur.Amount / govExchangeRateFromInvoiceAmount;

                        decimal inEur = totalNetValue / cross;

                        totalNetPrice = inEur * govExchangeRateUahToEur.Amount;
                    } else {
                        totalNetPrice =
                            govExchangeRateFromInvoiceAmount > 0
                                ? totalNetValue * govExchangeRateFromInvoiceAmount
                                : Math.Abs(
                                    totalNetValue / govExchangeRateFromInvoiceAmount);
                    }
                } else {
                    totalNetPrice = totalNetValue;
                }

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

                    decimal invoicePrice = govAccountingExchangeRateAmount < 0
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

                        decimal productSpecificationValues =
                            actuallyProductSpecification != null ? actuallyProductSpecification.Duty + packingListItem.VatAmount : 0;

                        decimal productSpecificationValuesPetItem = productSpecificationValues / Convert.ToDecimal(packingListItem.Qty);

                        //decimal totalPriceForSpecification = packingListItem.UnitPriceUah * Convert.ToDecimal(qtyTotalForSpecifications);

                        //decimal specificationValuesPerUnit = decimal.Round(productSpecificationValues * 100 / totalPriceForSpecification, 14, MidpointRounding.AwayFromZero);

                        //decimal accountingGrossPercentPerItem = decimal.Round(accountingGrossPercent, 14, MidpointRounding.AwayFromZero);

                        //decimal priceWithVat = vatFromIncome > 0 ? packingListItem.UnitPriceUah * (1 + vatFromIncome / 100) : packingListItem.UnitPriceUah;

                        //if (vatFromIncome > 0)
                        //    accountingGrossPercentPerItem -= specificationValuesPerUnit;

                        //if (actuallyProductSpecification.Duty > 0) {
                        //    decimal dutysPerUnit = decimal.Round(actuallyProductSpecification.Duty * 100 / totalPriceForSpecification, 14, MidpointRounding.AwayFromZero);

                        //    accountingGrossPercentPerItem += dutysPerUnit;
                        //}

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
                                     packingListItem.UnitPriceUah * accountingGrossPercent / 100) / govExchangeRateFromUahToEur,
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

                            decimal accountingGrossUnitPriceEur = 0;

                            if (isResident)
                                accountingGrossUnitPriceEur = decimal.Round(
                                    (packingListItem.UnitPriceUah + productSpecificationValuesPetItem +
                                     (packingListItem.UnitPriceUah + productSpecificationValuesPetItem) * accountingGrossPercent / 100) / govExchangeRateFromUahToEur,
                                    14,
                                    MidpointRounding.AwayFromZero
                                );
                            else
                                accountingGrossUnitPriceEur = decimal.Round(
                                    (packingListItem.UnitPriceUah + productSpecificationValuesPetItem + packingListItem.UnitPriceUah * accountingGrossPercent / 100) /
                                    govExchangeRateFromUahToEur,
                                    14,
                                    MidpointRounding.AwayFromZero
                                );

                            packingListItem.AccountingGrossUnitPriceEur = accountingGrossUnitPriceEur;

                            //if (vatFromIncome != 0) {
                            //packingListItem.AccountingGrossUnitPriceEur += packingListItem.AccountingGrossUnitPriceEur * (vatFromIncome / 100);
                            //}
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
                if (currencyFromInvoice.Code == uah.Code)
                    totalNetValueInEur =
                        documentValueExchangeRateAmount > 0
                            ? totalNetValue / documentValueExchangeRateAmount
                            : Math.Abs(
                                totalNetValue * documentValueExchangeRateAmount);
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

                    //decimal vatEur = packingListItem.UnitPriceEurWithVat - packingListItem.UnitPriceEur;

                    decimal accountingValue = decimal.Round(packingListItem.AccountingGrossUnitPriceEur - specificationValue - packingListItem.UnitPriceEur, 4,
                        MidpointRounding.AwayFromZero);

                    decimal syncManageValue = decimal.Round(
                        packingListItem.UnitPriceEurWithVat * grossPercent / 100,
                        4,
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

    private static decimal GetGovExchangeRateUk(
        Currency from,
        Currency to,
        IGovExchangeRateRepository govExchangeRateRepository,
        IGovCrossExchangeRateRepository govCrossExchangeRateRepository) {
        if (from.Id.Equals(to.Id))
            return 1m;

        GovExchangeRate exchangeRate =
            govExchangeRateRepository.GetByCurrencyIdAndCode(to.Id, from.Code);

        if (exchangeRate != null) return exchangeRate.Amount;

        exchangeRate =
            govExchangeRateRepository.GetByCurrencyIdAndCode(from.Id, to.Code);

        if (exchangeRate != null)
            return exchangeRate.Amount;

        GovCrossExchangeRate crossExchangeRate =
            govCrossExchangeRateRepository.GetByCurrenciesIds(to.Id, from.Id);

        if (crossExchangeRate != null) return decimal.Zero - crossExchangeRate.Amount;

        crossExchangeRate =
            govCrossExchangeRateRepository.GetByCurrenciesIds(from.Id, to.Id);

        return crossExchangeRate?.Amount ?? 1m;
    }
}