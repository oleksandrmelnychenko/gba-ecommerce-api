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
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Synchronizations;
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
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Domain.TranslationEntities;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace GBA.Services.Actors.DataSync;

public sealed class SyncIncomedOrdersWorkerActor : ReceiveActor {
    private const string DEFAULT_ORGANIZATION_AMG = "��� ���� �������Ļ";

    private const string DEFAULT_ORGANIZATION_FENIX = "������";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;

    private readonly IDataSyncRepositoriesFactory _dataSyncRepositoriesFactory;

    private readonly string _defaultComment = "��� ������� � 1�.";

    private readonly decimal _defaultVatPercent = 20;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;

    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    private readonly List<string> _vatOrganizationNames;

    private readonly string[] eurCodes;

    private readonly string[] organizationNames;

    private readonly string[] plnCodes;

    private readonly string[] storageOneNames;

    private readonly string[] usdCodes;

    public SyncIncomedOrdersWorkerActor(
        IStringLocalizer<SharedResource> localizer,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDataSyncRepositoriesFactory dataSyncRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _localizer = localizer;
        _localizer = localizer;

        _connectionFactory = connectionFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        _dataSyncRepositoriesFactory = dataSyncRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

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

        storageOneNames = new[] { "�����-1", "����� -1" };

        Receive<SynchronizeIncomedOrdersMessage>(ProcessSynchronizeIncomedOrdersMessage);
    }

    private void ProcessSynchronizeIncomedOrdersMessage(SynchronizeIncomedOrdersMessage message) {
        using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
        using IDbConnection remoteSyncConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection amgOneCConnection = _connectionFactory.NewAmgOneCSqlConnection();
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(remoteSyncConnection);

        IActorRef hubSenderActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

        User currentUser = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

        DateTime currentDate = DateTime.UtcNow;

        SynchronizeIncomedOrders(hubSenderActorRef, oneCConnection, remoteSyncConnection, amgOneCConnection, currentUser, message.ForAmg);

        ActorReferenceManager.Instance.Get(BaseActorNames.DATA_SYNC_MANAGEMENT_ACTOR).Tell(new IncomedOrdersSynchronizationFinishedMessage());

        _dataSyncRepositoriesFactory
            .NewDataSyncOperationRepository(remoteSyncConnection)
            .AddWithSpecificDates(new DataSyncOperation {
                UserId = currentUser.Id,
                OperationType = DataSyncOperationType.IncomedOrders,
                Created = currentDate,
                Updated = currentDate,
                ForAmg = message.ForAmg
            });
    }

    private void SynchronizeIncomedOrders(
        IActorRef hubSenderActorRef,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgOneCConnection,
        User currentUser,
        bool forAmg) {
        try {
            DataSyncOperation operation =
                _dataSyncRepositoriesFactory
                    .NewDataSyncOperationRepository(remoteSyncConnection)
                    .GetLastRecordByOperationType(
                        DataSyncOperationType.Consignments,
                        DataSyncOperationType.IncomedOrders
                    );

            ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(remoteSyncConnection);

            IIncomedOrdersSyncRepository incomedOrdersSyncRepository =
                _dataSyncRepositoriesFactory.NewIncomedOrdersSyncRepository(oneCConnection, amgOneCConnection, remoteSyncConnection);

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.TAX_INSPECTIONS_SYNC_START]));

            IEnumerable<SyncTaxInspection> syncTaxInspections =
                forAmg ? incomedOrdersSyncRepository.GetAmgAllSyncTaxInspections() : incomedOrdersSyncRepository.GetAllSyncTaxInspections();

            List<TaxInspection> taxInspections =
                incomedOrdersSyncRepository.GetAllTaxInspections();

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

                    taxInspection.Id = incomedOrdersSyncRepository.Add(taxInspection);
                } else {
                    taxInspection.Deleted = false;
                    taxInspection.InspectionNumber = syncTaxInspection.TaxInspectionCode;
                    taxInspection.InspectionType = syncTaxInspection.TypeDPI;
                    taxInspection.InspectionName = syncTaxInspection.TaxInspectionName;
                    taxInspection.InspectionRegionName = syncTaxInspection.NameAdminDistrict;
                    taxInspection.InspectionRegionCode = syncTaxInspection.CodeAdminDistrict;
                    taxInspection.InspectionAddress = syncTaxInspection.Address;
                    taxInspection.InspectionUSREOU = syncTaxInspection.EDRPOU;

                    incomedOrdersSyncRepository.Update(taxInspection);
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.TAX_INSPECTIONS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ORGANIZATIONS_SYNC_START]));

            IEnumerable<SyncOrganization> syncOrganizations =
                forAmg ? incomedOrdersSyncRepository.GetAmgAllSyncOrganizations() : incomedOrdersSyncRepository.GetAllSyncOrganizations(organizationNames);

            List<Organization> organizations =
                incomedOrdersSyncRepository.GetAllOrganizations();

            IEnumerable<Currency> currencies =
                incomedOrdersSyncRepository.GetAllCurrencies();

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

                incomedOrdersSyncRepository.Update(currency);
            }

            foreach (SyncOrganization syncOrganization in syncOrganizations) {
                Organization organization =
                    organizations.FirstOrDefault(o => o.Name.Equals(syncOrganization.OrganizationName));

                IEnumerable<SyncOrganizationAddress> syncOrganizationAddresses =
                    incomedOrdersSyncRepository.GetOrganizationAddresses(syncOrganization.OrganizationCode);

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

                    foreach (SyncOrganizationAddress address in syncOrganizationAddresses)
                        if (address.AddressType == SyncClientAddressType.Address) {
                            organization.Address = address.Value;
                        } else {
                            if (string.IsNullOrEmpty(organization.PhoneNumber))
                                organization.PhoneNumber = address.Value;
                            else
                                organization.PhoneNumber += $", {address.Value}";
                        }

                    organization.Id = incomedOrdersSyncRepository.Add(organization);

                    incomedOrdersSyncRepository.Add(new OrganizationTranslation {
                        OrganizationId = organization.Id,
                        Name = organization.NamePl,
                        CultureCode = "pl"
                    });

                    incomedOrdersSyncRepository.Add(new OrganizationTranslation {
                        OrganizationId = organization.Id,
                        Name = organization.NameUk,
                        CultureCode = "uk"
                    });

                    organizations.Add(organization);
                } else {
                    organization.Deleted = false;
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
                    organization.Manager = !forAmg && syncOrganization.OrganizationName.Equals(DEFAULT_ORGANIZATION_AMG) ? organization.Manager : syncOrganization.Manager;
                    organization.TaxInspectionId = taxInspections.FirstOrDefault(i => i.InspectionName == syncOrganization.TaxInspectionName)?.Id;
                    organization.PhoneNumber = string.Empty;

                    foreach (SyncOrganizationAddress address in syncOrganizationAddresses)
                        if (address.AddressType == SyncClientAddressType.Address) {
                            organization.Address = address.Value;
                        } else {
                            if (string.IsNullOrEmpty(organization.PhoneNumber))
                                organization.PhoneNumber = address.Value;
                            else
                                organization.PhoneNumber += $", {address.Value}";
                        }

                    incomedOrdersSyncRepository.Update(organization);

                    OrganizationTranslation plTranslation = organization.OrganizationTranslations.FirstOrDefault(t => t.CultureCode == "pl");

                    if (plTranslation == null) {
                        incomedOrdersSyncRepository.Add(new OrganizationTranslation {
                            OrganizationId = organization.Id,
                            Name = organization.NamePl,
                            CultureCode = "pl"
                        });
                    } else {
                        plTranslation.Name = organization.NamePl;

                        incomedOrdersSyncRepository.Update(plTranslation);
                    }

                    OrganizationTranslation ukTranslation = organization.OrganizationTranslations.FirstOrDefault(t => t.CultureCode == "uk");

                    if (ukTranslation == null) {
                        incomedOrdersSyncRepository.Add(new OrganizationTranslation {
                            OrganizationId = organization.Id,
                            Name = organization.NameUk,
                            CultureCode = "uk"
                        });
                    } else {
                        ukTranslation.Name = organization.NamePl;

                        incomedOrdersSyncRepository.Update(ukTranslation);
                    }
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ORGANIZATIONS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.STORAGES_SYNC_START]));

            IEnumerable<SyncStorage> syncStorages =
                forAmg ? incomedOrdersSyncRepository.GetAmgAllSyncStorages() : incomedOrdersSyncRepository.GetAllSyncStorages();

            List<Storage> storages =
                incomedOrdersSyncRepository.GetAllStorages();

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

                    storage.Id = incomedOrdersSyncRepository.Add(storage);

                    storages.Add(storage);
                } else {
                    storage.Deleted = false;
                    storage.Locale = organization.Culture;
                    storage.ForDefective = false;
                    storage.ForVatProducts = _vatOrganizationNames.Any(n => n.Equals(organization.Name));
                    storage.OrganizationId = organization.Id;

                    incomedOrdersSyncRepository.Update(storage);
                }

                organization.StorageId = storage.Id;

                incomedOrdersSyncRepository.Update(organization);
            }

            string[] existStorageNames = storages.Select(x => x.Name).ToArray();

            IEnumerable<string> storageNames =
                incomedOrdersSyncRepository
                    .GetStorageNamesFromSyncConsignmentsExceptProvided(
                        storages.Select(s => s.Name),
                        operation?.Created ?? DateTime.MinValue
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

                storage.Id = incomedOrdersSyncRepository.Add(storage);

                storages.Add(storage);
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.STORAGES_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.CONSIGNMENTS_SYNC_START]));

            IEnumerable<SyncConsignment> syncConsignments =
                forAmg
                    ? incomedOrdersSyncRepository.GetAmgAllSyncConsignments(storages.Select(s => s.Name), operation?.Created ?? DateTime.MinValue)
                    : incomedOrdersSyncRepository
                        .GetAllSyncConsignments(
                            storages.Select(s => s.Name),
                            operation?.Created ?? DateTime.MinValue
                        );

            IEnumerable<Client> clients =
                incomedOrdersSyncRepository.GetAllClients();

            StringBuilder builder = new();

            builder.Append("(-1");

            foreach (long productCode in syncConsignments.Select(p => p.ProductCode).Distinct()) builder.Append($",{productCode}");

            builder.Append(")");

            IEnumerable<Consignment> consignments =
                incomedOrdersSyncRepository.GetAllConsignmentsToDelete();

            if (forAmg) {
                incomedOrdersSyncRepository.CleanAllConsignmentsToDelete();

                foreach (Consignment consignmentToDelete in consignments)
                foreach (ConsignmentItem consignmentItem in consignmentToDelete.ConsignmentItems) {
                    if (!consignmentItem.Product.ProductAvailabilities.Any()) continue;

                    ProductAvailability availability = consignmentItem.Product.ProductAvailabilities.First();

                    availability = incomedOrdersSyncRepository.GetProductAvailabilityById(availability.Id);

                    availability.Amount -= consignmentItem.RemainingQty;

                    if (availability.Amount < 0) availability.Amount = 0;

                    incomedOrdersSyncRepository.Update(availability);
                }
            }

            IEnumerable<Product> products =
                incomedOrdersSyncRepository.GetAllProductsByProductCodes(builder.ToString(), forAmg);

            SyncConsignmentType lastDocumentType = SyncConsignmentType.None;
            string lastDocumentNumber = string.Empty;
            DateTime lastDocumentDate = DateTime.MinValue;

            IEnumerable<IGrouping<byte[], SyncConsignment>> groupedSyncConsignments = syncConsignments.GroupBy(x => x.DocumentId);

            IEnumerable<SupplyOrganization> supplyOrganizations = incomedOrdersSyncRepository.GetAllSupplyOrganizations(_defaultComment);

            SupplyOrganization devSupplyOrganization = incomedOrdersSyncRepository.GetDevSupplyOrganization(_defaultComment);

            if (devSupplyOrganization == null) {
                devSupplyOrganization = new SupplyOrganization {
                    Created = DateTime.Now,
                    Updated = DateTime.Now,
                    Name = _defaultComment
                };

                devSupplyOrganization.Id = incomedOrdersSyncRepository.Add(devSupplyOrganization);

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

                devSupplyOrganizationAgreement.Id = incomedOrdersSyncRepository.Add(devSupplyOrganizationAgreement);

                devSupplyOrganization.SupplyOrganizationAgreements.Add(devSupplyOrganizationAgreement);
            }

            List<long> invoiceIds = new();

            string comment = "��� ������� � 1�.";

            foreach (IGrouping<byte[], SyncConsignment> groupedSyncConsignment in groupedSyncConsignments) {
                IEnumerable<SyncConsignment> currentSyncConsignments = groupedSyncConsignment.Select(x => x);

                if (!currentSyncConsignments.Any()) continue;

                IEnumerable<SyncConsignmentSpecification> specifications = forAmg
                    ? incomedOrdersSyncRepository.GetAmgAllSyncConsignmentSpecifications(groupedSyncConsignment.Key)
                    : incomedOrdersSyncRepository.GetFenixAllSyncConsignmentSpecifications(groupedSyncConsignment.Key);

                IEnumerable<SyncConsignmentSpend> consignmentSpends = forAmg
                    ? incomedOrdersSyncRepository.GetAmgConsignmentSpendsByDocumentId(groupedSyncConsignment.Key)
                    : incomedOrdersSyncRepository.GetFenixConsignmentSpendsByDocumentId(groupedSyncConsignment.Key);

                if (forAmg) {
                    if (consignmentSpends.Any())
                        foreach (SyncConsignmentSpend consignmentSpend in consignmentSpends) {
                            SupplyOrganization supplyOrganization = supplyOrganizations.FirstOrDefault(x =>
                                x.SupplyOrganizationAgreements.Any(y =>
                                    forAmg ? y.SourceAmgCode == consignmentSpend.AgreementCode : y.SourceFenixCode == consignmentSpend.AgreementCode));

                            if (supplyOrganization == null) supplyOrganization = devSupplyOrganization;
                        }

                    SyncConsignment firstSyncConsignment = currentSyncConsignments.First();

                    Storage storage = storages.FirstOrDefault(s => s.Name == firstSyncConsignment.StorageName);

                    if (storage == null) continue;

                    Organization organization = organizations.FirstOrDefault(o => o.Id == storage.OrganizationId);

                    if (organization == null) continue;

                    Client client = clients.FirstOrDefault(c =>
                        forAmg ? c.SourceAmgCode == firstSyncConsignment.ClientCode : c.SourceFenixCode == firstSyncConsignment.ClientCode);

                    if (client == null) continue;

                    ClientAgreement clientAgreement = client.ClientAgreements.FirstOrDefault(x =>
                        forAmg ? x.Agreement.SourceAmgCode == firstSyncConsignment.AgreementCode : x.Agreement.SourceFenixCode == firstSyncConsignment.AgreementCode);

                    if (clientAgreement == null) continue;

                    Consignment consignment = new() {
                        OrganizationId = organization.Id,
                        StorageId = storage.Id,
                        FromDate = firstSyncConsignment.DocumentDate.AddYears(-2000),
                        ProductIncome = new ProductIncome {
                            Comment = comment,
                            Number = firstSyncConsignment.DocumentNumber,
                            FromDate = firstSyncConsignment.DocumentDate.AddYears(-2000),
                            UserId = currentUser.Id,
                            StorageId = storage.Id,
                            IsHide = true
                        },
                        IsImportedFromOneC = true
                    };

                    consignment.ProductIncomeId = incomedOrdersSyncRepository.Add(consignment.ProductIncome);

                    consignment.Id = incomedOrdersSyncRepository.Add(consignment);

                    foreach (SyncConsignment syncConsignment in currentSyncConsignments.OrderBy(x => x.DocumentDate)) {
                        if (!products.Any(p => p.VendorCode.Equals(syncConsignment.VendorCode))) continue;

                        Product product = products.First(p => p.VendorCode.Equals(syncConsignment.VendorCode));

                        decimal qty = Convert.ToDecimal(syncConsignment.Qty);

                        decimal totalValue = syncConsignment.NetValue * qty;

                        decimal vatPercent = !client.IsNotResident ? _defaultVatPercent : syncConsignment.Vat;
                        decimal vatAmount = !client.IsNotResident ? totalValue * (vatPercent / (100 + vatPercent)) : 0;

                        decimal totalPriceWithoutVat = totalValue - vatAmount;

                        decimal netPricePerItem = decimal.Round(totalPriceWithoutVat / qty, 6, MidpointRounding.AwayFromZero);

                        SyncConsignmentSpecification specification =
                            string.IsNullOrEmpty(syncConsignment.DocumentArrivalNumber)
                                ? null
                                : specifications
                                    .FirstOrDefault(s => s.ProductVendorCode == product.VendorCode);

                        DateTime? dateCustomDeclaration = specification?.CustomsDeclarationDate != null
                            ? specification.CustomsDeclarationDate?.AddYears(-2000)
                            : syncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000);

                        string numberCustomDeclaration = specification?.NumberDeclarationDate;

                        GovExchangeRate govExchangeRate =
                            incomedOrdersSyncRepository
                                .GetByCurrencyIdAndCode(clientAgreement.Agreement.CurrencyId.Value, eur.Code, dateCustomDeclaration ?? DateTime.Now);

                        GovExchangeRate govExchangeRateUahToEur =
                            incomedOrdersSyncRepository
                                .GetByCurrencyIdAndCode(uah.Id, eur.Code, dateCustomDeclaration ?? DateTime.Now);

                        decimal exchangeRateAmount = govExchangeRate?.Amount ?? 1;

                        decimal netPricePerItemEur =
                            decimal.Round(syncConsignment.NetValue / exchangeRateAmount, 6, MidpointRounding.AwayFromZero);

                        decimal pricePerItem = decimal.Round(syncConsignment.Value / Convert.ToDecimal(syncConsignment.Qty), 6, MidpointRounding.AwayFromZero);

                        ConsignmentItem consignmentItem;

                        if (syncConsignment.TypeDocument == SyncConsignmentType.Capitalization || syncConsignment.TypeDocument == SyncConsignmentType.ClientReturn) {
                            if (client != null)
                                comment += $" ���������� �� \"{client.FullName}\"";
                            else
                                comment += " �������������";

                            string specCode = string.Empty;
                            string specName = string.Empty;
                            decimal dutyPercent = syncConsignment.CustomsRate;

                            if (specification != null) {
                                specCode = syncConsignment.UKTVEDCode;
                                specName = syncConsignment.UKTVEDName;
                                dutyPercent = syncConsignment.Rate;
                            } else {
                                dutyPercent = syncConsignment.Vat > 0m
                                    ? decimal.Round(syncConsignment.Vat * 100m / syncConsignment.Value, 2, MidpointRounding.AwayFromZero)
                                    : decimal.Zero;
                            }

                            consignmentItem =
                                new ConsignmentItem {
                                    ProductId = product.Id,
                                    ProductIncomeItem = new ProductIncomeItem {
                                        ProductIncomeId = consignment.ProductIncomeId,
                                        Qty = syncConsignment.Qty,
                                        RemainingQty = syncConsignment.Qty,
                                        ProductCapitalizationItem = new ProductCapitalizationItem {
                                            ProductCapitalization = new ProductCapitalization {
                                                Comment = comment,
                                                OrganizationId = organization.Id,
                                                FromDate = specification?.CustomsDeclarationDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000),
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
                                            UnitPrice = pricePerItem,
                                            Weight = syncConsignment.WeightBruttoPer
                                        }
                                    },
                                    NetPrice = pricePerItem,
                                    Price = pricePerItem,
                                    Weight = syncConsignment.WeightBruttoPer,
                                    AccountingPrice = pricePerItem,
                                    RemainingQty = syncConsignment.Qty,
                                    Qty = syncConsignment.Qty,
                                    DutyPercent = dutyPercent,
                                    ProductSpecification = new ProductSpecification {
                                        ProductId = product.Id,
                                        AddedById = currentUser.Id,
                                        Locale = organization.Culture,
                                        SpecificationCode = specCode,
                                        Name = specName,
                                        DutyPercent = dutyPercent,
                                        CustomsValue = syncConsignment.CustomsValue
                                    },
                                    ConsignmentId = consignment.Id,
                                    ExchangeRate = exchangeRateAmount
                                };

                            consignmentItem.ProductIncomeItem.ProductCapitalizationItem.ProductCapitalizationId =
                                consignment.ConsignmentItems.FirstOrDefault()?.ProductIncomeItem?.ProductCapitalizationItem?.ProductCapitalizationId
                                ??
                                incomedOrdersSyncRepository.Add(
                                    consignmentItem.ProductIncomeItem.ProductCapitalizationItem.ProductCapitalization
                                );

                            consignmentItem.ProductIncomeItem.ProductCapitalizationItemId =
                                incomedOrdersSyncRepository.Add(
                                    consignmentItem.ProductIncomeItem.ProductCapitalizationItem
                                );

                            consignmentItem.ProductIncomeItemId =
                                incomedOrdersSyncRepository.Add(consignmentItem.ProductIncomeItem);

                            consignmentItem.ProductSpecificationId =
                                incomedOrdersSyncRepository.Add(consignmentItem.ProductSpecification);

                            consignmentItem.Id = incomedOrdersSyncRepository.Add(consignmentItem);

                            incomedOrdersSyncRepository.Add(new ConsignmentItemMovement {
                                Qty = syncConsignment.Qty,
                                IsIncomeMovement = true,
                                ProductIncomeItemId = consignmentItem.ProductIncomeItemId,
                                ConsignmentItemId = consignmentItem.Id,
                                MovementType = ConsignmentItemMovementType.Capitalization
                            });

                            consignment.ConsignmentItems.Add(consignmentItem);

                            incomedOrdersSyncRepository.Add(new ProductPlacement {
                                Qty = syncConsignment.Qty,
                                ProductId = product.Id,
                                StorageId = storage.Id,
                                ConsignmentItemId = consignmentItem.Id,
                                RowNumber = "N",
                                CellNumber = "N",
                                StorageNumber = "N"
                            });
                        } else {
                            if (client != null)
                                comment += $" ���������� �� \"{client.FullName}\"";
                            else
                                continue;

                            string specCode = string.Empty;
                            string specName = string.Empty;
                            decimal dutyPercent = syncConsignment.CustomsRate;

                            if (specification != null) {
                                specCode = syncConsignment.UKTVEDCode;
                                specName = syncConsignment.UKTVEDName;
                                dutyPercent = syncConsignment.Rate;
                            }

                            if (consignment.ConsignmentItems.Any()) {
                                ConsignmentItem existingItem = consignment.ConsignmentItems.First();

                                consignmentItem =
                                    new ConsignmentItem {
                                        ProductId = product.Id,
                                        ProductIncomeItem = new ProductIncomeItem {
                                            ProductIncomeId = consignment.ProductIncomeId,
                                            Qty = syncConsignment.Qty,
                                            RemainingQty = syncConsignment.Qty,
                                            PackingListPackageOrderItem = new PackingListPackageOrderItem {
                                                PackingListId = existingItem.ProductIncomeItem.PackingListPackageOrderItem.PackingListId,
                                                Qty = syncConsignment.Qty,
                                                PlacedQty = syncConsignment.Qty,
                                                RemainingQty = syncConsignment.Qty,
                                                UploadedQty = syncConsignment.Qty,
                                                NetWeight = syncConsignment.WeightPer,
                                                GrossWeight = syncConsignment.WeightBruttoPer,
                                                VatAmount = vatAmount,
                                                VatPercent = vatPercent,
                                                UnitPrice = netPricePerItem,
                                                UnitPriceEur = netPricePerItemEur,
                                                GrossUnitPriceEur = pricePerItem,
                                                ExchangeRateAmount = syncConsignment.RateExchange,
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
                                                        UnitPrice = netPricePerItem
                                                    },
                                                    ProductId = product.Id
                                                },
                                                ProductIsImported = syncConsignment?.IsImported ?? false
                                            }
                                        },
                                        NetPrice = netPricePerItem,
                                        Price = pricePerItem,
                                        AccountingPrice = netPricePerItem,
                                        Weight = syncConsignment.WeightBruttoPer,
                                        RemainingQty = syncConsignment.Qty,
                                        Qty = syncConsignment.Qty,
                                        DutyPercent =
                                            syncConsignment.Vat > 0m
                                                ? decimal.Round(syncConsignment.Vat * 100m / syncConsignment.Value, 2, MidpointRounding.AwayFromZero)
                                                : decimal.Zero,
                                        ProductSpecification = new ProductSpecification {
                                            ProductId = product.Id,
                                            AddedById = currentUser.Id,
                                            Locale = organization.Culture,
                                            SpecificationCode = specCode,
                                            Name = specName,
                                            DutyPercent = dutyPercent,
                                            CustomsValue = syncConsignment.CustomsValue
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
                                            Qty = syncConsignment.Qty,
                                            RemainingQty = syncConsignment.Qty,
                                            PackingListPackageOrderItem = new PackingListPackageOrderItem {
                                                PackingList = new PackingList {
                                                    SupplyInvoice = new SupplyInvoice {
                                                        SupplyOrder = new SupplyOrder {
                                                            Comment = comment,
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
                                                            DateFrom = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000),
                                                            OrderShippedDate = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                                               syncConsignment.DocumentDate.AddYears(-2000),
                                                            CompleteDate = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000),
                                                            ShipArrived = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000),
                                                            VechicalArrived = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                                              syncConsignment.DocumentDate.AddYears(-2000),
                                                            PlaneArrived = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000),
                                                            OrderArrivedDate = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ??
                                                                               syncConsignment.DocumentDate.AddYears(-2000),
                                                            TransportationType = SupplyTransportationType.Vehicle,
                                                            SupplyProForm = new SupplyProForm {
                                                                Number = "0",
                                                                NetPrice = 0m,
                                                                ServiceNumber = "0"
                                                            },
                                                            AdditionalPaymentFromDate = null
                                                        },
                                                        DateFrom = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000),
                                                        Number = syncConsignment.DocumentArrivalNumber,
                                                        ServiceNumber = "0",
                                                        Comment = comment,
                                                        IsShipped = true,
                                                        IsFullyPlaced = true,
                                                        IsPartiallyPlaced = true,
                                                        DateCustomDeclaration = dateCustomDeclaration,
                                                        NumberCustomDeclaration = numberCustomDeclaration,
                                                        NetPrice = totalValue
                                                    },
                                                    MarkNumber = "0",
                                                    InvNo = syncConsignment.DocumentArrivalNumber,
                                                    PlNo = syncConsignment.DocumentArrivalNumber,
                                                    Comment = comment,
                                                    FromDate = syncConsignment.DocumentArrivalDate?.AddYears(-2000) ?? syncConsignment.DocumentDate.AddYears(-2000),
                                                    IsPlaced = true
                                                },
                                                Qty = syncConsignment.Qty,
                                                PlacedQty = syncConsignment.Qty,
                                                RemainingQty = syncConsignment.Qty,
                                                UploadedQty = syncConsignment.Qty,
                                                NetWeight = syncConsignment.WeightPer,
                                                GrossWeight = syncConsignment.WeightBruttoPer,
                                                VatAmount = vatAmount,
                                                VatPercent = vatPercent,
                                                UnitPrice = netPricePerItem,
                                                UnitPriceEur = netPricePerItemEur,
                                                GrossUnitPriceEur = pricePerItem,
                                                ExchangeRateAmount = syncConsignment.RateExchange,
                                                IsPlaced = true,
                                                ExchangeRateAmountUahToEur = govExchangeRateUahToEur.Amount,
                                                SupplyInvoiceOrderItem = new SupplyInvoiceOrderItem {
                                                    Qty = syncConsignment.Qty,
                                                    Weight = syncConsignment.WeightBruttoPer,
                                                    UnitPrice = netPricePerItem,
                                                    SupplyOrderItem = new SupplyOrderItem {
                                                        ProductId = product.Id,
                                                        Qty = syncConsignment.Qty,
                                                        GrossWeight = syncConsignment.WeightBruttoPer,
                                                        NetWeight = syncConsignment.WeightPer,
                                                        UnitPrice = netPricePerItem
                                                    },
                                                    ProductId = product.Id
                                                },
                                                ProductIsImported = syncConsignment?.IsImported ?? false
                                            }
                                        },
                                        Price = pricePerItem,
                                        NetPrice = netPricePerItem,
                                        AccountingPrice = netPricePerItem,
                                        Weight = syncConsignment.WeightBruttoPer,
                                        RemainingQty = syncConsignment.Qty,
                                        Qty = syncConsignment.Qty,
                                        DutyPercent =
                                            syncConsignment.Vat > 0m
                                                ? decimal.Round(syncConsignment.Vat * 100m / syncConsignment.Value, 2, MidpointRounding.AwayFromZero)
                                                : decimal.Zero,
                                        ProductSpecification = new ProductSpecification {
                                            ProductId = product.Id,
                                            AddedById = currentUser.Id,
                                            Locale = organization.Culture,
                                            SpecificationCode = specCode,
                                            Name = specName,
                                            DutyPercent = dutyPercent,
                                            CustomsValue = syncConsignment.CustomsValue
                                        },
                                        ConsignmentId = consignment.Id,
                                        ExchangeRate = syncConsignment.RateExchange
                                    };

                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrder.SupplyOrderNumberId =
                                    incomedOrdersSyncRepository.Add(
                                        consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrder.SupplyOrderNumber);

                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrder.SupplyProFormId =
                                    incomedOrdersSyncRepository.Add(
                                        consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrder.SupplyProForm);

                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem.SupplyOrderId =
                                    consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrderId =
                                        incomedOrdersSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.SupplyOrder);

                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyInvoiceId =
                                    consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoiceId =
                                        incomedOrdersSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice);

                                consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingListId =
                                    incomedOrdersSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.PackingList);

                                invoiceIds.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyInvoiceId);
                            }

                            consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItemId =
                                incomedOrdersSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem.SupplyOrderItem);

                            consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItemId =
                                incomedOrdersSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.SupplyInvoiceOrderItem);

                            consignmentItem.ProductIncomeItem.PackingListPackageOrderItemId =
                                incomedOrdersSyncRepository.Add(consignmentItem.ProductIncomeItem.PackingListPackageOrderItem);

                            consignmentItem.ProductIncomeItemId =
                                incomedOrdersSyncRepository.Add(consignmentItem.ProductIncomeItem);

                            consignmentItem.ProductSpecificationId =
                                incomedOrdersSyncRepository.Add(consignmentItem.ProductSpecification);

                            consignmentItem.Id = incomedOrdersSyncRepository.Add(consignmentItem);

                            incomedOrdersSyncRepository.Add(new ConsignmentItemMovement {
                                Qty = syncConsignment.Qty,
                                IsIncomeMovement = true,
                                ProductIncomeItemId = consignmentItem.ProductIncomeItemId,
                                ConsignmentItemId = consignmentItem.Id,
                                MovementType = ConsignmentItemMovementType.Income
                            });

                            consignment.ConsignmentItems.Add(consignmentItem);

                            incomedOrdersSyncRepository.Add(new ProductPlacement {
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

                            incomedOrdersSyncRepository.Update(availability);
                        } else {
                            ProductAvailability availability = new() {
                                Amount = syncConsignment.Qty,
                                ProductId = product.Id,
                                StorageId = storage.Id
                            };

                            availability.Id = incomedOrdersSyncRepository.Add(availability);

                            product.ProductAvailabilities.Add(availability);
                        }

                        lastDocumentDate = syncConsignment.DocumentDate;
                        lastDocumentType = syncConsignment.TypeDocument;
                        lastDocumentNumber = syncConsignment.DocumentNumber;
                    }
                }
                // SyncConsignment firstSyncConsignment = currentSyncConsignments.First();
                //
                // Client client = clients.FirstOrDefault(c =>
                //     (forAmg ? c.SourceAmgCode == firstSyncConsignment.ClientCode : c.SourceFenixCode == firstSyncConsignment.ClientCode));
                //
                // Consignment consignment = consignments.FirstOrDefault(x =>
                //     x.ConsignmentItems.Any(y =>
                //         y.ProductIncomeItem.PackingListPackageOrderItem.PackingList.SupplyInvoice.Number == firstSyncConsignment.DocumentArrivalNumber));
                //
                // if (client.IsNotResident) { } else { }
            }

            incomedOrdersSyncRepository.UpdateActiveSpecification();

            if (invoiceIds.Any()) {
                IEnumerable<SupplyInvoice> allSupplyInvoices = incomedOrdersSyncRepository.GetSupplyInvoiceByIds(invoiceIds);

                IEnumerable<IGrouping<long, SupplyInvoice>> groupedSupplyInvoices = allSupplyInvoices.GroupBy(x => x.SupplyOrderId);

                foreach (IGrouping<long, SupplyInvoice> groupedSupplyInvoice in groupedSupplyInvoices) {
                    IEnumerable<SupplyInvoice> supplyInvoices = groupedSupplyInvoice.Select(x => x);

                    long[] currentInvoiceIds = supplyInvoices.Select(x => x.Id).ToArray();

                    ProcessUpdateSupplyInvoiceGrossPrice(
                        new UpdateSupplyInvoiceItemGrossPriceMessage(
                            currentInvoiceIds,
                            currentUser.NetUid
                        ));
                }

                IEnumerable<ConsignmentItem> consignmentItems = incomedOrdersSyncRepository.GetConsignmentItemsByInvoiceIds(invoiceIds);

                if (consignmentItems.Any()) {
                    foreach (ConsignmentItem consignmentItem in consignmentItems) {
                        consignmentItem.NetPrice = consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.UnitPriceEur;
                        consignmentItem.AccountingPrice = consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.AccountingGrossUnitPriceEur;
                        consignmentItem.Price = consignmentItem.ProductIncomeItem.PackingListPackageOrderItem.AccountingGrossUnitPriceEur;
                    }

                    incomedOrdersSyncRepository.UpdatePrices(consignmentItems);
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.CONSIGNMENTS_SYNC_END]));
        } catch (Exception exc) {
            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddDataSyncLogMessage(
                        "SYNC_ERROR Incomed Orders",
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
        UpdateSupplyInvoiceItemGrossPriceMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderRepository supplyOrderRepository =
                _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            ICurrencyRepository currencyRepository =
                _currencyRepositoriesFactory.NewCurrencyRepository(connection);
            IGovExchangeRateRepository govExchangeRateRepository =
                _exchangeRateRepositoriesFactory.NewGovExchangeRateRepository(connection);
            IPackingListPackageOrderItemRepository packingListPackageOrderItemRepository =
                _supplyRepositoriesFactory.NewPackingListPackageOrderItemRepository(connection);
            IGovCrossExchangeRateRepository govCrossExchangeRateRepository = _exchangeRateRepositoriesFactory.NewGovCrossExchangeRateRepository(connection);
            ISupplyInvoiceBillOfLadingServiceRepository supplyInvoiceBillOfLadingServiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceBillOfLadingServiceRepository(connection);
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

                if (invoice.DeliveryProductProtocolId.HasValue)
                    protocol =
                        _supplyRepositoriesFactory.NewDeliveryProductProtocolRepository(connection)
                            .GetById(invoice.DeliveryProductProtocolId.Value);

                SupplyOrder supplyOrder = supplyOrderRepository.GetByNetId(invoice.SupplyOrder.NetUid);

                decimal govExchangeRateFromInvoiceAmount =
                    GetGovExchangeRateOnDateToUah(
                        supplyOrder.ClientAgreement.Agreement.Currency,
                        invoice.DateCustomDeclaration ?? invoice.Created,
                        govExchangeRateRepository,
                        currencyRepository
                    );

                GovExchangeRate govExchangeRate =
                    govExchangeRateRepository
                        .GetByCurrencyIdAndCode(uah.Id, eur.Code, invoice.DateCustomDeclaration ?? invoice.Created);

                decimal govExchangeRateFromUahToEur = govExchangeRate.Amount;

                double qtySupplyInvoiceInSupplyOrder = supplyOrderRepository.GetQtySupplyInvoiceById(supplyOrder.Id);

                decimal totalNetPrice = 0m;
                decimal totalGrossPrice = 0m;
                decimal totalAccountingGrossPrice = 0m;
                decimal totalGeneralAccountingGrossPrice = 0m;

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

                    totalNetPrice += packingListItem.UnitPriceUah * Convert.ToDecimal(packingListItem.Qty);
                }

                foreach (CustomService service in supplyOrder.CustomServices) {
                    decimal govExchangeRateAmount =
                        service.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            service.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? service.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = service.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        service.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            service.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? service.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = service.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (service.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, service.Id, TypeService.CustomService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = service.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����������� ����� " + service.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    CustomServiceId = service.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? service.Created,
                                    Updated = DateTime.Now
                                };

                            if (service.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? service.Created;

                            existItem.CurrencyId = service.SupplyOrganizationAgreement.Currency.Id;

                            if (service.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                foreach (MergedService service in supplyOrder.MergedServices) {
                    decimal govExchangeRateAmount =
                        service.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            service.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? service.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = service.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        service.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            service.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? service.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = service.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (service.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, service.Id, TypeService.MergedService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = service.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "��'������� ����� " + service.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    MergedServiceId = service.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? service.Created,
                                    Updated = DateTime.Now
                                };

                            if (service.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? service.Created;

                            existItem.CurrencyId = service.SupplyOrganizationAgreement.Currency.Id;

                            if (service.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.PortWorkService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.PortWorkService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PortWorkService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PortWorkService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );
                    decimal servicePricePerItem = supplyOrder.PortWorkService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.PortWorkService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PortWorkService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PortWorkService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.PortWorkService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.PortWorkService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.PortWorkService.Id, TypeService.PortWorkService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.PortWorkService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����� �������� ���� " + supplyOrder.PortWorkService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    PortWorkServiceId = supplyOrder.PortWorkService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PortWorkService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.PortWorkService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PortWorkService.Created;

                            existItem.CurrencyId = supplyOrder.PortWorkService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.PortWorkService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.TransportationService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.TransportationService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.TransportationService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.TransportationService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = supplyOrder.TransportationService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.TransportationService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.TransportationService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.TransportationService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.TransportationService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.TransportationService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.TransportationService.Id,
                                    TypeService.TransportationService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.TransportationService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "������������ ����� " + supplyOrder.TransportationService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    TransportationServiceId = supplyOrder.TransportationService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.TransportationService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.TransportationService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.TransportationService.Created;

                            existItem.CurrencyId = supplyOrder.TransportationService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.TransportationService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.CustomAgencyService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.CustomAgencyService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.CustomAgencyService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = supplyOrder.CustomAgencyService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.CustomAgencyService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.CustomAgencyService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.CustomAgencyService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.CustomAgencyService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.CustomAgencyService.Id,
                                    TypeService.CustomAgencyService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����� ������� ��������� " + supplyOrder.CustomAgencyService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    CustomAgencyServiceId = supplyOrder.CustomAgencyService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.CustomAgencyService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.CustomAgencyService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.CustomAgencyService.Created;

                            existItem.CurrencyId = supplyOrder.CustomAgencyService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.CustomAgencyService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.PortCustomAgencyService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.PortCustomAgencyService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PortCustomAgencyService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = supplyOrder.PortCustomAgencyService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.PortCustomAgencyService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PortCustomAgencyService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.PortCustomAgencyService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.PortCustomAgencyService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.PortCustomAgencyService.Id,
                                    TypeService.PortCustomAgencyService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����� ��������� �������� ��������� " + supplyOrder.PortCustomAgencyService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    PortCustomAgencyServiceId = supplyOrder.PortCustomAgencyService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PortCustomAgencyService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.PortCustomAgencyService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PortCustomAgencyService.Created;

                            existItem.CurrencyId = supplyOrder.PortCustomAgencyService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.PortCustomAgencyService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.PlaneDeliveryService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.PlaneDeliveryService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PlaneDeliveryService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = supplyOrder.PlaneDeliveryService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.PlaneDeliveryService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.PlaneDeliveryService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.PlaneDeliveryService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.PlaneDeliveryService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.PlaneDeliveryService.Id,
                                    TypeService.PlaneDeliveryService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����� �������� ������ " + supplyOrder.PlaneDeliveryService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    PlaneDeliveryServiceId = supplyOrder.PlaneDeliveryService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PlaneDeliveryService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.PlaneDeliveryService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PlaneDeliveryService.Created;

                            existItem.CurrencyId = supplyOrder.PlaneDeliveryService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.PlaneDeliveryService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.VehicleDeliveryService != null) {
                    decimal govExchangeRateAmount =
                        supplyOrder.VehicleDeliveryService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.VehicleDeliveryService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal servicePricePerItem = supplyOrder.VehicleDeliveryService.GrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice + servicePricePerItem * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyOrder.VehicleDeliveryService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            supplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Currency,
                            invoice.DateCustomDeclaration ?? supplyOrder.VehicleDeliveryService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    decimal serviceAccountingPricePerItem = supplyOrder.VehicleDeliveryService.AccountingGrossPrice / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    if (supplyOrder.VehicleDeliveryService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;
                    else
                        totalGeneralAccountingGrossPrice = govAccountingExchangeRateAmount < 0
                            ? totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem / (0 - govAccountingExchangeRateAmount)
                            : totalGeneralAccountingGrossPrice + serviceAccountingPricePerItem * govAccountingExchangeRateAmount;

                    decimal grossPercentCurrentService =
                        servicePricePerItem / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        serviceAccountingPricePerItem / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyOrder.VehicleDeliveryService.Id,
                                    TypeService.VehicleDeliveryService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = supplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Currency.Id,
                                    Name = "����� ��������� ���������� " + supplyOrder.VehicleDeliveryService.Number,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    VehicleDeliveryServiceId = supplyOrder.VehicleDeliveryService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.VehicleDeliveryService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyOrder.VehicleDeliveryService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyOrder.PlaneDeliveryService.Created;

                            existItem.CurrencyId = supplyOrder.VehicleDeliveryService.SupplyOrganizationAgreement.Currency.Id;

                            if (supplyOrder.VehicleDeliveryService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                if (supplyOrder.AdditionalPaymentCurrency != null && supplyOrder.AdditionalAmount > 0) {
                    decimal govExchangeRateAmount =
                        GetGovExchangeRateUk(
                            supplyOrder.AdditionalPaymentCurrency,
                            eur,
                            govExchangeRateRepository,
                            govCrossExchangeRateRepository
                        );

                    decimal servicePricePerItem = supplyOrder.AdditionalAmount / Convert.ToDecimal(qtySupplyInvoiceInSupplyOrder);

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice +
                              servicePricePerItem / (0 - govExchangeRateAmount)
                            : totalGrossPrice +
                              servicePricePerItem * govExchangeRateAmount;
                }

                List<SupplyInvoiceBillOfLadingService> supplyInvoiceBillOfLadingServices =
                    supplyInvoiceBillOfLadingServiceRepository.GetBySupplyInvoiceId(invoice.Id);

                List<SupplyInvoiceMergedService> supplyInvoiceMergedServices =
                    supplyInvoiceMergedServiceRepository.GetBySupplyInvoiceId(invoice.Id);

                foreach (SupplyInvoiceMergedService supplyInvoiceMergedService in supplyInvoiceMergedServices) {
                    Currency currency = currencyRepository.GetByMergedServiceId(supplyInvoiceMergedService.MergedServiceId);

                    string serviceName = supplyInvoiceMergedService.MergedService.ConsumableProduct.Name + " " +
                                         supplyInvoiceMergedService.MergedService.Number;

                    decimal govExchangeRateAmount =
                        supplyInvoiceMergedService.MergedService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            currency,
                            invoice.DateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? totalGrossPrice + supplyInvoiceMergedService.Value / (0 - govExchangeRateAmount)
                            : totalGrossPrice + supplyInvoiceMergedService.Value * govExchangeRateAmount;

                    decimal govAccountingExchangeRateAmount =
                        supplyInvoiceMergedService.MergedService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            currency,
                            invoice.DateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
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

                    decimal grossPercentCurrentService =
                        supplyInvoiceMergedService.Value / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        supplyInvoiceMergedService.AccountingValue / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

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
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created,
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

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyInvoiceMergedService.MergedService.Created;

                            existItem.CurrencyId = currency.Id;

                            if (supplyInvoiceMergedService.MergedService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                foreach (SupplyInvoiceBillOfLadingService supplyInvoice in supplyInvoiceBillOfLadingServices) {
                    Currency currency = currencyRepository.GetByBillOfLadingServiceId(supplyInvoice.BillOfLadingServiceId);

                    string serviceName = supplyInvoice.BillOfLadingService.TypeBillOfLadingService.Equals(TypeBillOfLadingService.Container)
                        ? "��������� " + supplyInvoice.BillOfLadingService.Number
                        : "��������� " + supplyInvoice.BillOfLadingService.Number;

                    decimal govExchangeRateAmount =
                        supplyInvoice.BillOfLadingService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            currency,
                            invoice.DateCustomDeclaration ?? supplyInvoice.BillOfLadingService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? decimal.Round(
                                totalGrossPrice + supplyInvoice.Value / (0 - govExchangeRateAmount),
                                2,
                                MidpointRounding.AwayFromZero
                            )
                            : decimal.Round(
                                totalGrossPrice + supplyInvoice.Value * govExchangeRateAmount,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                    decimal govAccountingExchangeRateAmount =
                        supplyInvoice.BillOfLadingService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                            currency,
                            invoice.DateCustomDeclaration ?? supplyInvoice.BillOfLadingService.Created,
                            govExchangeRateRepository,
                            currencyRepository
                        );

                    if (supplyInvoice.BillOfLadingService.IsIncludeAccountingValue)
                        totalAccountingGrossPrice = totalAccountingGrossPrice +
                            govAccountingExchangeRateAmount < 0
                                ? decimal.Round(
                                    totalAccountingGrossPrice + supplyInvoice.AccountingValue / (0 - govAccountingExchangeRateAmount),
                                    2,
                                    MidpointRounding.AwayFromZero
                                )
                                : decimal.Round(
                                    totalAccountingGrossPrice + supplyInvoice.AccountingValue * govAccountingExchangeRateAmount,
                                    2,
                                    MidpointRounding.AwayFromZero
                                );
                    else
                        totalGeneralAccountingGrossPrice = totalAccountingGrossPrice +
                            govAccountingExchangeRateAmount < 0
                                ? decimal.Round(
                                    totalGeneralAccountingGrossPrice + supplyInvoice.AccountingValue / (0 - govAccountingExchangeRateAmount),
                                    2,
                                    MidpointRounding.AwayFromZero
                                )
                                : decimal.Round(
                                    totalGeneralAccountingGrossPrice + supplyInvoice.AccountingValue * govAccountingExchangeRateAmount,
                                    2,
                                    MidpointRounding.AwayFromZero
                                );

                    decimal grossPercentCurrentService =
                        supplyInvoice.Value / invoice.NetPrice;

                    decimal accountingGrossPercentCurrentService =
                        supplyInvoice.AccountingValue / invoice.NetPrice;

                    foreach (PackingList packingList in invoice.PackingLists)
                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, supplyInvoice.BillOfLadingService.Id,
                                    TypeService.BillOfLadingService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = currency.Id,
                                    Name = serviceName,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    BillOfLadingServiceId = supplyInvoice.BillOfLadingService.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyInvoice.BillOfLadingService.Created,
                                    Updated = DateTime.Now
                                };

                            if (supplyInvoice.BillOfLadingService.IsIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? supplyInvoice.BillOfLadingService.Created;

                            existItem.CurrencyId = currency.Id;

                            if (supplyInvoice.BillOfLadingService.IsIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                foreach (PackingList packingList in invoice.PackingLists) {
                    if (!packingList.ContainerServiceId.HasValue && !packingList.VehicleServiceId.HasValue) continue;
                    decimal govExchangeRateAmount;
                    decimal accountingGovExchangeRateAmount;
                    TypeService typeService;
                    bool isIncludeAccountingValue;
                    DateTime createdService;
                    long serviceId;
                    Currency fromContainerOrVehicleService;
                    string serviceName;
                    if (packingList.ContainerServiceId.HasValue) {
                        fromContainerOrVehicleService = currencyRepository.GetByContainerServiceId(packingList.ContainerServiceId.Value);
                        ContainerService containerService =
                            _supplyRepositoriesFactory
                                .NewContainerServiceRepository(connection)
                                .GetById(packingList.ContainerServiceId.Value);

                        govExchangeRateAmount =
                            containerService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                fromContainerOrVehicleService,
                                invoice.DateCustomDeclaration ?? containerService.Created,
                                govExchangeRateRepository,
                                currencyRepository
                            );

                        accountingGovExchangeRateAmount =
                            containerService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                fromContainerOrVehicleService,
                                invoice.DateCustomDeclaration ?? containerService.Created,
                                govExchangeRateRepository,
                                currencyRepository
                            );

                        serviceId = packingList.ContainerServiceId.Value;
                        createdService = containerService.Created;
                        isIncludeAccountingValue = containerService.IsIncludeAccountingValue;
                        typeService = TypeService.ContainerService;
                        serviceName = "��������� " + containerService.Number;
                    } else {
                        fromContainerOrVehicleService = currencyRepository.GetByVehicleServiceId(packingList.VehicleServiceId.Value);

                        VehicleService vehicleService =
                            _supplyRepositoriesFactory
                                .NewVehicleServiceRepository(connection)
                                .GetById(packingList.VehicleServiceId.Value);

                        govExchangeRateAmount =
                            vehicleService.ExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                fromContainerOrVehicleService,
                                invoice.DateCustomDeclaration ?? vehicleService.Created,
                                govExchangeRateRepository,
                                currencyRepository
                            );

                        accountingGovExchangeRateAmount =
                            vehicleService.AccountingExchangeRate ?? GetGovExchangeRateOnDateToUah(
                                fromContainerOrVehicleService,
                                invoice.DateCustomDeclaration ?? vehicleService.Created,
                                govExchangeRateRepository,
                                currencyRepository
                            );

                        serviceId = packingList.VehicleServiceId.Value;
                        createdService = vehicleService.Created;
                        isIncludeAccountingValue = vehicleService.IsIncludeAccountingValue;
                        typeService = TypeService.VehicleService;
                        serviceName = "��������� " + vehicleService.Number;
                    }

                    totalGrossPrice =
                        govExchangeRateAmount < 0
                            ? decimal.Round(
                                totalGrossPrice + packingList.ExtraCharge / (0 - govExchangeRateAmount),
                                2,
                                MidpointRounding.AwayFromZero
                            )
                            : decimal.Round(
                                totalGrossPrice + packingList.ExtraCharge * govExchangeRateAmount,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                    totalAccountingGrossPrice =
                        accountingGovExchangeRateAmount < 0
                            ? decimal.Round(
                                totalAccountingGrossPrice + packingList.AccountingExtraCharge / (0 - accountingGovExchangeRateAmount),
                                2,
                                MidpointRounding.AwayFromZero
                            )
                            : decimal.Round(
                                totalAccountingGrossPrice + packingList.AccountingExtraCharge * accountingGovExchangeRateAmount,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                    decimal totalNetPricePackingList =
                        packingList.PackingListPackageOrderItems.Sum(packingListItem => packingListItem.UnitPrice * Convert.ToDecimal(packingListItem.Qty));

                    decimal grossPercentCurrentService =
                        packingList.ExtraCharge / totalNetPricePackingList;

                    decimal accountingGrossPercentCurrentService =
                        packingList.AccountingExtraCharge / totalNetPricePackingList;

                    foreach (PackingListPackageOrderItem packingListPackageOrderItem in packingList.PackingListPackageOrderItems) {
                        decimal totalNetPricePackingListItem = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty);

                        decimal valueOnCurrentPackListItem =
                            totalNetPricePackingListItem * grossPercentCurrentService;
                        decimal accountingValueOnCurrentPackListItem =
                            totalNetPricePackingListItem * accountingGrossPercentCurrentService;

                        PackingListPackageOrderItemSupplyService existItem =
                            packingListPackageOrderItemSupplyServiceRepository
                                .GetByPackingListItemAndServiceId(packingListPackageOrderItem.Id, serviceId,
                                    typeService);

                        if (existItem == null) {
                            PackingListPackageOrderItemSupplyService newItem =
                                new() {
                                    CurrencyId = fromContainerOrVehicleService.Id,
                                    PackingListPackageOrderItemId = packingListPackageOrderItem.Id,
                                    ManagementValue = valueOnCurrentPackListItem,
                                    Name = serviceName,
                                    ExchangeRateDate = invoice.DateCustomDeclaration ?? createdService,
                                    Updated = DateTime.Now
                                };

                            if (typeService.Equals(TypeService.ContainerService))
                                newItem.ContainerServiceId = serviceId;
                            else
                                newItem.VehicleServiceId = serviceId;

                            if (isIncludeAccountingValue)
                                newItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                newItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository
                                .New(newItem);
                        } else {
                            existItem.ManagementValue = valueOnCurrentPackListItem;

                            if (existItem.Deleted)
                                existItem.Deleted = false;

                            existItem.ExchangeRateDate = invoice.DateCustomDeclaration ?? createdService;

                            existItem.CurrencyId = fromContainerOrVehicleService.Id;

                            if (isIncludeAccountingValue)
                                existItem.NetValue = accountingValueOnCurrentPackListItem;
                            else
                                existItem.GeneralValue = accountingValueOnCurrentPackListItem;

                            packingListPackageOrderItemSupplyServiceRepository.Update(existItem);
                        }
                    }
                }

                decimal deliveryTotal = govExchangeRateFromInvoiceAmount > 0
                    ? invoice.DeliveryAmount * govExchangeRateFromInvoiceAmount
                    : Math.Abs(
                        invoice.DeliveryAmount / govExchangeRateFromInvoiceAmount);

                decimal deliveryPercent = deliveryTotal * 100 / totalNetPrice;

                decimal grossPercent = totalGrossPrice * 100 / totalNetPrice;

                decimal accountingGrossPercent = totalAccountingGrossPrice * 100 / totalNetPrice;

                decimal generalAccountingGrossPercent = totalGeneralAccountingGrossPrice * 100 / totalNetPrice;

                if (invoice.SupplyOrder.AdditionalPercent > 0)
                    accountingGrossPercent += accountingGrossPercent * (Convert.ToDecimal(invoice.SupplyOrder.AdditionalPercent) / 100);

                foreach (PackingList packingList in invoice.PackingLists) {
                    decimal containerPricePerItem = 0m;
                    decimal accountingContainerPricePerItem = 0m;

                    double qtyItems = packingList.PackingListPackageOrderItems.Sum(x => x.Qty);

                    if (!packingList.ExtraCharge.Equals(0))
                        containerPricePerItem = packingList.ExtraCharge / Convert.ToDecimal(qtyItems);

                    if (!packingList.AccountingExtraCharge.Equals(0))
                        accountingContainerPricePerItem = packingList.AccountingExtraCharge / Convert.ToDecimal(qtyItems);

                    foreach (PackingListPackageOrderItem packingListItem in packingList.PackingListPackageOrderItems) {
                        ProductSpecification actuallyProductSpecification =
                            _productRepositoriesFactory.NewProductSpecificationRepository(connection)
                                .GetByProductAndSupplyInvoiceIdsIfExists(
                                    packingListItem.SupplyInvoiceOrderItem.Product.Id,
                                    invoice.Id);

                        if (actuallyProductSpecification != null)
                            packingListItem.VatAmount = actuallyProductSpecification.VATValue;

                        decimal productSpecificationValues =
                            actuallyProductSpecification != null ? actuallyProductSpecification.Duty + actuallyProductSpecification.VATValue : 0;

                        decimal totalNetPriceItem = packingListItem.UnitPriceUah * Convert.ToDecimal(packingListItem.Qty);

                        decimal specificationValuesPerUnit = productSpecificationValues * 100 / totalNetPriceItem;

                        decimal accountingGrossPercentPerItem = accountingGrossPercent + specificationValuesPerUnit + deliveryPercent;

                        packingListItem.DeliveryPerItem =
                            decimal.Round(
                                packingListItem.UnitPriceUah * deliveryPercent / 100 / govExchangeRateFromInvoiceAmount,
                                14,
                                MidpointRounding.AwayFromZero
                            );

                        packingListItem.ContainerUnitPriceEur = containerPricePerItem;
                        packingListItem.AccountingContainerUnitPriceEur = accountingContainerPricePerItem;

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