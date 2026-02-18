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
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Synchronizations;
using GBA.Domain.Entities.VatRates;
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

public sealed class SyncPaymentRegistersWorkerActor : ReceiveActor {
    private const string DEFAULT_ORGANIZATION_FENIX = "������";

    private const string DEFAULT_ORGANIZATION_AMG = "��� ���� �������Ļ";

    private const double DEFAULT_VAT_RATE_VALUE = 20;

    private static readonly Regex _cashRegisterNameReplace = new(@"\(.+\)", RegexOptions.Compiled);

    private readonly IDbConnectionFactory _connectionFactory;

    private readonly IDataSyncRepositoriesFactory _dataSyncRepositoriesFactory;
    private readonly IStringLocalizer<SharedResource> _localizer;

    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    private readonly string[] fenixOrganizationName;

    private readonly string[] organizationNames;

    public SyncPaymentRegistersWorkerActor(
        IStringLocalizer<SharedResource> localizer,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDataSyncRepositoriesFactory dataSyncRepositoriesFactory) {
        _localizer = localizer;

        _connectionFactory = connectionFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        _dataSyncRepositoriesFactory = dataSyncRepositoriesFactory;

        organizationNames = new[] {
            DEFAULT_ORGANIZATION_AMG,
            DEFAULT_ORGANIZATION_FENIX,
            "��� ������� ���� ��������",
            "��� ���������� ����� �����",
            "��� ������� ��� �����������"
        };

        fenixOrganizationName = new[] {
            DEFAULT_ORGANIZATION_FENIX,
            "��� ������� ���� ��������",
            "��� ���������� ����� �����",
            "��� ������� ��� �����������"
        };

        Receive<SynchronizePaymentRegistersMessage>(ProcessSynchronizePaymentRegistersMessage);
    }

    private void ProcessSynchronizePaymentRegistersMessage(SynchronizePaymentRegistersMessage message) {
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
                OperationType = DataSyncOperationType.PaymentRegisters,
                ForAmg = message.ForAmg
            });

        SynchronizeRegisters(hubSenderActorRef, oneCConnection, remoteSyncConnection, currentUser, amgCConnection, message.ForAmg);

        ActorReferenceManager.Instance.Get(DataSyncActorNames.DATA_SYNC_WORKER_ACTOR)
            .Tell(new StartDataSyncWorkMessage(message.SyncEntityTypes, message.UserNetId, message.ForAmg));
    }

    private void SynchronizeRegisters(
        IActorRef hubSenderActorRef,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        User currentUser,
        IDbConnection amgCConnection,
        bool forAmg) {
        try {
            IPaymentRegistersSyncRepository paymentRegistersSyncRepository =
                _dataSyncRepositoriesFactory.NewPaymentRegistersSyncRepository(oneCConnection, remoteSyncConnection, amgCConnection);

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.BANKS_SYNC_START]));

            IEnumerable<SyncBank> syncBanks =
                forAmg ? paymentRegistersSyncRepository.GetAmgAllSyncBanks() : paymentRegistersSyncRepository.GetAllSyncBanks();

            List<Bank> banks =
                paymentRegistersSyncRepository.GetAllBanks();

            foreach (SyncBank syncBank in syncBanks) {
                Bank bank =
                    banks
                        .FirstOrDefault(i => i.MfoCode == syncBank.MfoCode
                                             || i.Name == syncBank.Name);

                if (bank == null) {
                    bank = new Bank {
                        Address = syncBank.Address,
                        City = syncBank.City,
                        Name = syncBank.Name,
                        Phones = syncBank.Phones,
                        EdrpouCode = syncBank.EdrpouCode,
                        MfoCode = syncBank.MfoCode
                    };

                    paymentRegistersSyncRepository.Add(bank);
                } else {
                    bank.Address = syncBank.Address;
                    bank.City = syncBank.City;
                    bank.Name = syncBank.Name;
                    bank.Phones = syncBank.Phones;
                    bank.EdrpouCode = syncBank.EdrpouCode;
                    bank.MfoCode = syncBank.MfoCode;
                    bank.Deleted = false;

                    paymentRegistersSyncRepository.Update(bank);
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.BANKS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.TAX_INSPECTIONS_SYNC_START]));

            IEnumerable<SyncTaxInspection> syncTaxInspections =
                forAmg ? paymentRegistersSyncRepository.GetAmgAllSyncTaxInspections() : paymentRegistersSyncRepository.GetAllSyncTaxInspections();

            List<TaxInspection> taxInspections =
                paymentRegistersSyncRepository.GetAllTaxInspections();

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

                    taxInspection.Id = paymentRegistersSyncRepository.Add(taxInspection);
                } else {
                    taxInspection.InspectionNumber = syncTaxInspection.TaxInspectionCode;
                    taxInspection.InspectionType = syncTaxInspection.TypeDPI;
                    taxInspection.InspectionName = syncTaxInspection.TaxInspectionName;
                    taxInspection.InspectionRegionName = syncTaxInspection.NameAdminDistrict;
                    taxInspection.InspectionRegionCode = syncTaxInspection.CodeAdminDistrict;
                    taxInspection.InspectionAddress = syncTaxInspection.Address;
                    taxInspection.InspectionUSREOU = syncTaxInspection.EDRPOU;
                    taxInspection.Deleted = false;

                    paymentRegistersSyncRepository.Update(taxInspection);
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.TAX_INSPECTIONS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ORGANIZATIONS_SYNC_START]));

            IEnumerable<SyncOrganization> syncOrganizations =
                forAmg ? paymentRegistersSyncRepository.GetAmgAllSyncOrganizations() : paymentRegistersSyncRepository.GetAllSyncOrganizations(organizationNames);

            List<Organization> organizations =
                paymentRegistersSyncRepository.GetAllOrganizations();

            IEnumerable<Currency> currencies =
                paymentRegistersSyncRepository.GetAllCurrencies();

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

                paymentRegistersSyncRepository.Update(currency);
            }

            List<Storage> storages =
                paymentRegistersSyncRepository.GetAllStorages();

            IEnumerable<VatRate> vatRates = paymentRegistersSyncRepository.GetAllVatRates();

            if (!vatRates.Any(x => x.Value == 20)) {
                paymentRegistersSyncRepository.AddVatRate(new VatRate {
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                    Value = 20
                });

                vatRates = paymentRegistersSyncRepository.GetAllVatRates();
            }

            foreach (SyncOrganization syncOrganization in syncOrganizations) {
                Organization organization =
                    organizations.FirstOrDefault(o => o.Name.Equals(syncOrganization.OrganizationName));

                IEnumerable<SyncOrganizationAddress> syncOrganizationAddresses =
                    forAmg
                        ? paymentRegistersSyncRepository.GetAmgOrganizationAddresses(syncOrganization.OrganizationCode)
                        : paymentRegistersSyncRepository.GetOrganizationAddresses(syncOrganization.OrganizationCode);

                Storage storage = storages.FirstOrDefault(x => x.Name == syncOrganization.StorageName);

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
                        RegistrationNumber = syncOrganization.NumberRegistration,
                        SROI = syncOrganization.CodeKVED,
                        TIN = syncOrganization.IPN,
                        USREOU = syncOrganization.EDRPOU,
                        TaxInspectionId = taxInspections.FirstOrDefault(i => i.InspectionName == syncOrganization.TaxInspectionName)?.Id,
                        Manager = syncOrganization.Manager,
                        PFURegistrationNumber = syncOrganization.PFURegistrationNumber
                    };

                    if (organization.Name == DEFAULT_ORGANIZATION_AMG) {
                        organization.VatRateId = vatRates.FirstOrDefault(x => x.Value == DEFAULT_VAT_RATE_VALUE)?.Id;
                        organization.IsVatAgreements = true;
                    } else {
                        organization.IsVatAgreements = false;
                    }

                    if (storage != null)
                        organization.StorageId = storage.Id;

                    foreach (SyncOrganizationAddress address in syncOrganizationAddresses)
                        if (address.AddressType == SyncClientAddressType.Address) {
                            organization.Address = address.Value;
                        } else {
                            if (string.IsNullOrEmpty(organization.PhoneNumber))
                                organization.PhoneNumber = address.Value;
                            else
                                organization.PhoneNumber += $", {address.Value}";
                        }

                    organization.Id = paymentRegistersSyncRepository.Add(organization);

                    paymentRegistersSyncRepository.Add(new OrganizationTranslation {
                        OrganizationId = organization.Id,
                        Name = organization.NamePl,
                        CultureCode = "pl"
                    });

                    paymentRegistersSyncRepository.Add(new OrganizationTranslation {
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
                    organization.RegistrationNumber = syncOrganization.NumberRegistration;
                    organization.SROI = syncOrganization.CodeKVED;
                    organization.TIN = syncOrganization.IPN;
                    organization.USREOU = syncOrganization.EDRPOU;
                    organization.TaxInspectionId = taxInspections.FirstOrDefault(i => i.InspectionName == syncOrganization.TaxInspectionName)?.Id;
                    organization.PhoneNumber = string.Empty;
                    organization.Manager = !forAmg && syncOrganization.OrganizationName.Equals(DEFAULT_ORGANIZATION_AMG) ? organization.Manager : syncOrganization.Manager;
                    organization.PFURegistrationNumber = syncOrganization.PFURegistrationNumber;

                    if (organization.Name == DEFAULT_ORGANIZATION_AMG) {
                        organization.VatRateId = vatRates.FirstOrDefault(x => x.Value == DEFAULT_VAT_RATE_VALUE)?.Id;
                        organization.IsVatAgreements = true;
                    } else {
                        organization.IsVatAgreements = false;
                    }

                    if (storage != null)
                        organization.StorageId = storage.Id;

                    foreach (SyncOrganizationAddress address in syncOrganizationAddresses)
                        if (address.AddressType == SyncClientAddressType.Address) {
                            organization.Address = address.Value;
                        } else {
                            if (string.IsNullOrEmpty(organization.PhoneNumber))
                                organization.PhoneNumber = address.Value;
                            else
                                organization.PhoneNumber += $", {address.Value}";
                        }

                    paymentRegistersSyncRepository.Update(organization);

                    OrganizationTranslation plTranslation = organization.OrganizationTranslations.FirstOrDefault(t => t.CultureCode == "pl");

                    if (plTranslation == null) {
                        paymentRegistersSyncRepository.Add(new OrganizationTranslation {
                            OrganizationId = organization.Id,
                            Name = organization.NamePl,
                            CultureCode = "pl"
                        });
                    } else {
                        plTranslation.Name = organization.NamePl;

                        paymentRegistersSyncRepository.Update(plTranslation);
                    }

                    OrganizationTranslation ukTranslation = organization.OrganizationTranslations.FirstOrDefault(t => t.CultureCode == "uk");

                    if (ukTranslation == null) {
                        paymentRegistersSyncRepository.Add(new OrganizationTranslation {
                            OrganizationId = organization.Id,
                            Name = organization.NameUk,
                            CultureCode = "uk"
                        });
                    } else {
                        ukTranslation.Name = organization.NamePl;

                        paymentRegistersSyncRepository.Update(ukTranslation);
                    }
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ORGANIZATIONS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.CASH_REGISTERS_SYNC_START]));

            IEnumerable<SyncCashRegister> syncCashRegisters =
                forAmg
                    ? paymentRegistersSyncRepository.GetAmgAllSyncCashRegisters(DEFAULT_ORGANIZATION_AMG)
                    : paymentRegistersSyncRepository.GetAllSyncCashRegisters(fenixOrganizationName);

            List<PaymentRegister> paymentRegisters =
                paymentRegistersSyncRepository.GetAllPaymentRegisters();

            foreach (SyncCashRegister syncCashRegister in syncCashRegisters) {
                if (!string.IsNullOrEmpty(syncCashRegister.CashRegisterName))
                    syncCashRegister.CashRegisterName =
                        _cashRegisterNameReplace.Replace(syncCashRegister.CashRegisterName, string.Empty).Trim();

                PaymentRegister register =
                    paymentRegisters.FirstOrDefault(r => r.Name == syncCashRegister.CashRegisterName && r.Type == PaymentRegisterType.Cash);

                Organization organization =
                    organizations.First(o => o.Name == syncCashRegister.OrganizationName);

                Currency currency = currencies.First(c => c.CodeOneC.Equals(syncCashRegister.CurrencyCode));

                if (register == null) {
                    register = new PaymentRegister {
                        Name = syncCashRegister.CashRegisterName,
                        OrganizationId = organization.Id,
                        Type = PaymentRegisterType.Cash,
                        IsActive = false
                    };

                    register.Id = paymentRegistersSyncRepository.Add(register);

                    paymentRegisters.Add(register);
                }

                PaymentCurrencyRegister currencyRegister =
                    register.PaymentCurrencyRegisters.FirstOrDefault(r => r.CurrencyId.Equals(currency.Id));

                if (currencyRegister == null) {
                    currencyRegister = new PaymentCurrencyRegister {
                        Amount = syncCashRegister.Value,
                        InitialAmount = syncCashRegister.Value,
                        CurrencyId = currency.Id,
                        PaymentRegisterId = register.Id
                    };

                    currencyRegister.Id = paymentRegistersSyncRepository.Add(currencyRegister);

                    register.PaymentCurrencyRegisters.Add(currencyRegister);
                } else {
                    currencyRegister.Amount = syncCashRegister.Value;
                    currencyRegister.InitialAmount = syncCashRegister.Value;
                    currencyRegister.Deleted = false;

                    paymentRegistersSyncRepository.Update(currencyRegister);
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.CASH_REGISTERS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.BANK_REGISTERS_SYNC_START]));

            IEnumerable<SyncBankRegister> syncBankRegisters =
                forAmg
                    ? paymentRegistersSyncRepository.GetAmgAllSyncBankRegisters(DEFAULT_ORGANIZATION_AMG)
                    : paymentRegistersSyncRepository.GetAllSyncBankRegisters(fenixOrganizationName);

            foreach (SyncBankRegister syncBankRegister in syncBankRegisters) {
                PaymentRegister register =
                    paymentRegisters.FirstOrDefault(r => r.Name == syncBankRegister.BankAccountName &&
                                                         r.Type == PaymentRegisterType.Bank &&
                                                         r.AccountNumber == syncBankRegister.BankAccountNumber);

                Organization organization =
                    organizations.First(o => o.Name == syncBankRegister.OrganizationName);

                Currency currency = currencies.First(c => c.CodeOneC.Equals(syncBankRegister.CurrencyCode));

                if (register == null) {
                    register = new PaymentRegister {
                        Name = syncBankRegister.BankAccountName,
                        OrganizationId = organization.Id,
                        Type = PaymentRegisterType.Bank,
                        IsActive = false,
                        AccountNumber = syncBankRegister.BankAccountNumber,
                        FromDate = syncBankRegister.DateOpening,
                        ToDate = syncBankRegister.DateClosing,
                        SortCode = syncBankRegister.BankCode,
                        BankName = syncBankRegister.BankName,
                        IBAN = syncBankRegister.BankNumber,
                        City = $"{syncBankRegister.City}, {syncBankRegister.Address}"
                    };

                    register.Id = paymentRegistersSyncRepository.Add(register);

                    paymentRegisters.Add(register);
                } else {
                    register.Name = syncBankRegister.BankAccountName;
                    register.OrganizationId = organization.Id;
                    register.Type = PaymentRegisterType.Bank;
                    register.IsActive = false;
                    register.AccountNumber = syncBankRegister.BankAccountNumber;
                    register.FromDate = syncBankRegister.DateOpening;
                    register.ToDate = syncBankRegister.DateClosing;
                    register.SortCode = syncBankRegister.BankCode;
                    register.BankName = syncBankRegister.BankName;
                    register.IBAN = syncBankRegister.BankNumber;
                    register.City = $"{syncBankRegister.City}, {syncBankRegister.Address}";
                    register.Deleted = false;

                    paymentRegistersSyncRepository.Update(register);
                }

                PaymentCurrencyRegister currencyRegister =
                    register.PaymentCurrencyRegisters.FirstOrDefault(r => r.CurrencyId.Equals(currency.Id));

                if (currencyRegister == null) {
                    currencyRegister = new PaymentCurrencyRegister {
                        Amount = syncBankRegister.Value,
                        InitialAmount = syncBankRegister.Value,
                        CurrencyId = currency.Id,
                        PaymentRegisterId = register.Id
                    };

                    currencyRegister.Id = paymentRegistersSyncRepository.Add(currencyRegister);

                    register.PaymentCurrencyRegisters.Add(currencyRegister);
                } else {
                    currencyRegister.Amount = syncBankRegister.Value;
                    currencyRegister.InitialAmount = syncBankRegister.Value;

                    paymentRegistersSyncRepository.Update(currencyRegister);
                }
            }

            foreach (SyncOrganization syncOrganization in syncOrganizations
                         .Where(o => !string.IsNullOrEmpty(o.MainBankAccountName) && !string.IsNullOrEmpty(o.MainCurrencyCode))) {
                Currency currency = currencies.First(c => c.CodeOneC.Equals(syncOrganization.MainCurrencyCode));

                PaymentRegister register =
                    paymentRegisters.FirstOrDefault(r => r.Type == PaymentRegisterType.Bank &&
                                                         r.Name == syncOrganization.MainBankAccountName &&
                                                         r.PaymentCurrencyRegisters.Any(c => c.CurrencyId.Equals(currency.Id)));

                if (register == null) continue;

                Organization organization =
                    organizations.FirstOrDefault(o => o.Name.Equals(syncOrganization.OrganizationName));

                register.OrganizationId = organization.Id;
                register.IsActive = true;
                register.IsMain = true;

                paymentRegistersSyncRepository.Update(register);
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.BANK_REGISTERS_SYNC_END], true));
        } catch (Exception exc) {
            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_ERROR], true, true));

            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddDataSyncLogMessage(
                        "SYNC_ERROR Payment registers",
                        $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                        JsonConvert.SerializeObject(new {
                            exc.Message,
                            exc.StackTrace
                        })
                    )
                );
        }
    }
}