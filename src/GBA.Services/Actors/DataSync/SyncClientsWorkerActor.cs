using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Supplies;
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

public sealed class SyncClientsWorkerActor : ReceiveActor {
    private const string PL_CLIENTS_GROUP_NAME = "Polscy klienci";

    private const string PL_UK_CLIENTS_GROUP_NAME = "������� PL ������";

    private const string DEFAULT_ORGANIZATION_NAME = "������";

    private const string DEFAULT_ORGANIZATION_AMG = "��� ���� �������Ļ";

    private const string DEFAULT_ORGANIZATION_FENIX = "������";

    private const string SUPPLY_PRICING_NAME = "��";

    private const string STANDARD_PROMOTIONAL_PRICING_VAT = "��2 (���)";

    private const string STANDARD_PROMOTIONAL_PRICING = "��2";

    private const double DEFAULT_VAT_RATE_VALUE = 20;

    private readonly Dictionary<long, string> _clientGroupNames;

    private readonly IDbConnectionFactory _connectionFactory;

    private readonly IDataSyncRepositoriesFactory _dataSyncRepositoriesFactory;
    private readonly IStringLocalizer<SharedResource> _localizer;

    private readonly List<string> _supplierServiceGroupNames;

    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    private readonly List<string> _vatOrganizationNames;

    private readonly string[] eurCodes;

    private readonly string[] organizationNames;

    private readonly string[] plnCodes;

    private readonly string[] storageOneNames;

    private readonly string[] usdCodes;

    public SyncClientsWorkerActor(
        IStringLocalizer<SharedResource> localizer,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDataSyncRepositoriesFactory dataSyncRepositoriesFactory) {
        _localizer = localizer;

        _connectionFactory = connectionFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        _dataSyncRepositoriesFactory = dataSyncRepositoriesFactory;

        _clientGroupNames = new Dictionary<long, string> {
            { 10, "��������� ��������������" },
            { 1, "�������" },
            { 7, "Concord.PL" },
            { 8, "�������" },
            { 9, "�����" },
            { 11, "��������� ��������" },
            { 12, "������� �� ������" },
            { 13, "����" },
            { 14, "���� �������������" },
            { 15, "����� (������-����)" },
            { 16, "�������" },
            { 17, "������������� ��� ������� �.�." },
            { 18, "�������� ��������" },
            { 19, "������" },
            { 20, "�����" }
        };

        _supplierServiceGroupNames = new List<string> {
            "������������� ������"
        };

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

        Receive<SynchronizeClientsMessage>(ProcessSynchronizeClientsMessage);
    }

    private void ProcessSynchronizeClientsMessage(SynchronizeClientsMessage message) {
        using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
        using IDbConnection remoteSyncConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection amgSyncConnection = _connectionFactory.NewAmgOneCSqlConnection();
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(remoteSyncConnection);

        IActorRef hubSenderActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

        User currentUser = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);
        List<User> users = userRepository.GetAll();

        _dataSyncRepositoriesFactory
            .NewDataSyncOperationRepository(remoteSyncConnection)
            .Add(new DataSyncOperation {
                UserId = currentUser.Id,
                OperationType = DataSyncOperationType.Clients,
                ForAmg = message.ForAmg
            });

        SynchronizeRegions(hubSenderActorRef, oneCConnection, remoteSyncConnection, amgSyncConnection, currentUser, message.ForAmg);

        SynchronizeClients(hubSenderActorRef, oneCConnection, remoteSyncConnection, amgSyncConnection, currentUser, users, message.ForAmg);

        ActorReferenceManager.Instance.Get(DataSyncActorNames.DATA_SYNC_WORKER_ACTOR)
            .Tell(new StartDataSyncWorkMessage(message.SyncEntityTypes, message.UserNetId, message.ForAmg));
    }

    private void SynchronizeRegions(
        IActorRef hubSenderActorRef,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgSyncConnection,
        User currentUser,
        bool forAmg) {
        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.REGIONS_SYNC_START]));

        try {
            IRegionsSyncRepository regionsSyncRepository =
                _dataSyncRepositoriesFactory
                    .NewRegionsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);

            IEnumerable<string> regionsForSync =
                forAmg ? regionsSyncRepository.GetAllAmgRegionsForSync() : regionsSyncRepository.GetAllRegionsForSync();

            IEnumerable<Region> regions =
                regionsSyncRepository.GetAllExistingRegions();

            foreach (string syncRegion in regionsForSync) {
                Region region =
                    regions.FirstOrDefault(r => r.Name.Equals(string.IsNullOrEmpty(syncRegion) ? syncRegion : syncRegion.Trim()));

                if (region == null) {
                    regionsSyncRepository.Add(new Region {
                        Name = string.IsNullOrEmpty(syncRegion) ? syncRegion : syncRegion.Trim()
                    });
                } else {
                    region.Deleted = false;

                    regionsSyncRepository.Update(region);
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.REGIONS_SYNC_END]));
        } catch (Exception exc) {
            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddDataSyncLogMessage(
                        "SYNC_ERROR Regions",
                        $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                        JsonConvert.SerializeObject(new {
                            exc.Message,
                            exc.StackTrace
                        })
                    )
                );
        }
    }

    private void SynchronizeClients(
        IActorRef hubSenderActorRef,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgSyncConnection,
        User currentUser,
        List<User> users,
        bool forAmg) {
        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.CLIENTS_AND_SUPPLIERS_SYNC_START]));

        try {
            IClientsSyncRepository clientsSyncRepository =
                _dataSyncRepositoriesFactory
                    .NewClientsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);

            IEnumerable<SyncClient> syncClients =
                forAmg ? clientsSyncRepository.GetAmgAllSyncClients() : clientsSyncRepository.GetAllSyncClients();

            List<Client> clients =
                clientsSyncRepository.GetAllClients();

            List<Organization> organizations =
                clientsSyncRepository.GetAllOrganizations();

            List<SupplyOrganization> supplyOrganizations =
                clientsSyncRepository.GetAllSupplyOrganization();

            IEnumerable<Region> regions =
                clientsSyncRepository.GetAllRegions();

            IEnumerable<ClientType> clientTypes = clientsSyncRepository.GetAllClientTypes();
            IEnumerable<ClientTypeRole> clientTypeRoles = clientsSyncRepository.GetAllClientTypeRoles();

            foreach (SyncClient syncClient in syncClients) {
                Client client;

                if (forAmg) {
                    client = clients.FirstOrDefault(c => c.ClientInRole.ClientType.Type == ClientTypeType.Buyer &&
                                                         c.SourceAmgCode.HasValue &&
                                                         c.SourceAmgCode.Equals(syncClient.ClientCode));

                    if (client == null)
                        client = clients.FirstOrDefault(c => c.ClientInRole.ClientType.Type == ClientTypeType.Buyer &&
                                                             !c.SourceAmgCode.HasValue &&
                                                             (!string.IsNullOrEmpty(c.OriginalRegionCode) && c.OriginalRegionCode == syncClient.RegionCode ||
                                                              c.Name == syncClient.ClientName ||
                                                              c.FullName == syncClient.FullName));
                } else {
                    client = clients.FirstOrDefault(c => c.ClientInRole.ClientType.Type == ClientTypeType.Buyer &&
                                                         c.SourceFenixCode.HasValue &&
                                                         c.SourceFenixCode.Equals(syncClient.ClientCode));

                    if (client == null)
                        client = clients.FirstOrDefault(c => c.ClientInRole.ClientType.Type == ClientTypeType.Buyer &&
                                                             !c.SourceFenixCode.HasValue &&
                                                             (!string.IsNullOrEmpty(c.OriginalRegionCode) && c.OriginalRegionCode == syncClient.RegionCode ||
                                                              c.Name == syncClient.ClientName ||
                                                              c.FullName == syncClient.FullName));
                }

                Client supplier;

                if (forAmg) {
                    supplier = clients.FirstOrDefault(c => c.ClientInRole.ClientType.Type == ClientTypeType.Provider &&
                                                           c.SourceAmgCode.HasValue &&
                                                           c.SourceAmgCode.Equals(syncClient.ClientCode));

                    if (supplier == null)
                        supplier = clients.FirstOrDefault(c => c.ClientInRole.ClientType.Type == ClientTypeType.Provider &&
                                                               !c.SourceAmgCode.HasValue &&
                                                               (c.Name == syncClient.ClientName ||
                                                                c.FullName == syncClient.FullName));
                } else {
                    supplier = clients.FirstOrDefault(c => c.ClientInRole.ClientType.Type == ClientTypeType.Provider &&
                                                           c.SourceFenixCode.HasValue &&
                                                           c.SourceFenixCode.Equals(syncClient.ClientCode));

                    if (supplier == null)
                        supplier = clients.FirstOrDefault(c => c.ClientInRole.ClientType.Type == ClientTypeType.Provider &&
                                                               !c.SourceFenixCode.HasValue &&
                                                               (c.Name == syncClient.ClientName ||
                                                                c.FullName == syncClient.FullName));
                }

                Region region =
                    string.IsNullOrEmpty(syncClient.RegionName)
                        ? null
                        : regions.FirstOrDefault(r => r.Name == syncClient.RegionName.Trim());

                if (syncClient.Buyer) {
                    if (client == null) {
                        client =
                            new Client {
                                TIN = syncClient.TIN,
                                USREOU = syncClient.USREOU,
                                RegionId = region?.Id,
                                RegionCode =
                                    region == null || string.IsNullOrEmpty(syncClient.RegionCode)
                                        ? null
                                        : new RegionCode {
                                            RegionId = region.Id,
                                            Value = syncClient.RegionCode
                                        },
                                SROI = syncClient.SROI,
                                Name = syncClient.ClientName,
                                FullName = syncClient.FullName,
                                IsIndividual = syncClient.IsIndividual,
                                IsActive = true,
                                FirstName = syncClient.FirstName,
                                LastName = syncClient.LastName,
                                MiddleName = syncClient.MiddleName,
                                Manager = syncClient.MainContactPersonName,
                                OriginalRegionCode = syncClient.RegionCode,
                                Brand = syncClient.ClientName,
                                Manufacturer = syncClient.ClientName,
                                SupplierName = syncClient.ClientName,
                                OrderExpireDays = 3,
                                ClearCartAfterDays = 3
                            };

                        if (forAmg) {
                            client.IsNotResident = syncClient.IsNotResident;
                            client.SourceAmgId = syncClient.SourceId;
                            client.SourceAmgCode = syncClient.ClientCode;
                        } else {
                            client.SourceFenixId = syncClient.SourceId;
                            client.SourceFenixCode = syncClient.ClientCode;
                        }

                        if (syncClient.IsSubClient) {
                            if (client.RegionCode != null)
                                client.IsTradePoint = true;
                            else
                                client.IsSubClient = true;
                        }

                        if (!string.IsNullOrEmpty(syncClient.ManagerName)) {
                            string[] userName = syncClient.ManagerName.Split(' ');

                            User mainManager = users
                                .OrderBy(user => user.Deleted)
                                .FirstOrDefault(user =>
                                    user.LastName.Trim().ToLower().Equals(userName[0].Trim().ToLower()) &&
                                    user.FirstName.Trim().ToLower().Equals(userName[1].Trim().ToLower()) &&
                                    user.MiddleName.Trim().ToLower().Equals(userName[2].Trim().ToLower()));

                            if (mainManager != null)
                                client.MainManagerId = mainManager.Id;
                        }

                        clients.Add(client);
                    } else {
                        client.Deleted = false;

                        client.TIN = syncClient.TIN;
                        client.USREOU = syncClient.USREOU;
                        client.RegionId = region?.Id;
                        client.RegionCode =
                            region == null || string.IsNullOrEmpty(syncClient.RegionCode)
                                ? null
                                : new RegionCode {
                                    RegionId = region.Id,
                                    Value = syncClient.RegionCode
                                };
                        client.SROI = syncClient.SROI;
                        client.Name = syncClient.ClientName;
                        client.FullName = syncClient.FullName;
                        client.IsIndividual = syncClient.IsIndividual;
                        client.IsActive = true;
                        client.FirstName = syncClient.FirstName;
                        client.LastName = syncClient.LastName;
                        client.MiddleName = syncClient.MiddleName;
                        client.Manager = syncClient.MainContactPersonName;
                        client.OrderExpireDays = 3;
                        client.ClearCartAfterDays = 3;

                        if (forAmg) {
                            client.IsNotResident = syncClient.IsNotResident;
                            client.SourceAmgId = syncClient.SourceId;
                            client.SourceAmgCode = syncClient.ClientCode;
                        } else {
                            client.SourceFenixId = syncClient.SourceId;
                            client.SourceFenixCode = syncClient.ClientCode;
                        }

                        if (syncClient.IsSubClient) {
                            if (client.RegionCode != null)
                                client.IsTradePoint = true;
                            else
                                client.IsSubClient = true;
                        }

                        if (!string.IsNullOrEmpty(syncClient.ManagerName)) {
                            string[] userName = syncClient.ManagerName.Split(' ');

                            User mainManager = users
                                .OrderBy(user => user.Deleted)
                                .FirstOrDefault(user =>
                                    user.LastName.Trim().ToLower().Equals(userName[0].Trim().ToLower()) &&
                                    user.FirstName.Trim().ToLower().Equals(userName[1].Trim().ToLower()) &&
                                    user.MiddleName.Trim().ToLower().Equals(userName[2].Trim().ToLower()));

                            if (mainManager != null)
                                client.MainManagerId = mainManager.Id;
                        }
                    }

                    client.ClientGroupName = syncClient.ClientGroupName;
                }

                if (_supplierServiceGroupNames.Any(n => n.Equals(syncClient.ClientGroupName))) {
                    SupplyOrganization supplyOrganization;

                    if (forAmg) {
                        supplyOrganization = supplyOrganizations.FirstOrDefault(c => c.SourceAmgCode.HasValue &&
                                                                                     c.SourceAmgCode.Equals(syncClient.ClientCode));

                        if (supplyOrganization == null)
                            supplyOrganization = supplyOrganizations.FirstOrDefault(c => !c.SourceAmgCode.HasValue &&
                                                                                         c.Name == syncClient.ClientName);
                    } else {
                        supplyOrganization = supplyOrganizations.FirstOrDefault(c => c.SourceFenixCode.HasValue &&
                                                                                     c.SourceFenixCode.Equals(syncClient.ClientCode));

                        if (supplyOrganization == null)
                            supplyOrganization = supplyOrganizations.FirstOrDefault(c => !c.SourceFenixCode.HasValue &&
                                                                                         c.Name == syncClient.ClientName);
                    }

                    SyncClientAddress actualAddress = syncClient.SyncClientAddresses
                        .FirstOrDefault(x => x.AddressType == SyncClientAddressType.Address &&
                                             x.AddressInfoType == SyncClientAddressInfoType.ActualAddress);

                    SyncClientAddress emailAddress = syncClient.SyncClientAddresses
                        .FirstOrDefault(x => x.AddressType == SyncClientAddressType.Email);

                    SyncClientAddress syncClientAddress = syncClient.SyncClientAddresses
                        .FirstOrDefault(x => x.AddressType == SyncClientAddressType.Phone);

                    if (supplyOrganization == null) {
                        supplyOrganization = new SupplyOrganization {
                            Address = actualAddress?.Value ?? string.Empty,
                            Bank = syncClient.BankName,
                            Name = syncClient.ClientName,
                            AccountNumber = syncClient.BankAccountNumber,
                            BankAccount = syncClient.BankAccountCode,
                            Created = DateTime.Now,
                            Updated = DateTime.Now,
                            EmailAddress = emailAddress?.Value ?? string.Empty,
                            PhoneNumber = syncClientAddress?.Value ?? string.Empty,
                            ContactPersonName = syncClient.MainContactPersonName,
                            OriginalRegionCode = syncClient.RegionCode,
                            SROI = syncClient.SROI,
                            TIN = syncClient.TIN,
                            USREOU = syncClient.USREOU
                        };

                        if (forAmg) {
                            supplyOrganization.IsNotResident = syncClient.IsNotResident;
                            supplyOrganization.SourceAmgId = syncClient.SourceId;
                            supplyOrganization.SourceAmgCode = syncClient.ClientCode;
                        } else {
                            supplyOrganization.SourceFenixId = syncClient.SourceId;
                            supplyOrganization.SourceFenixCode = syncClient.ClientCode;
                        }

                        supplyOrganization.Id = clientsSyncRepository.Add(supplyOrganization);

                        supplyOrganizations.Add(supplyOrganization);
                    } else {
                        supplyOrganization.Address = actualAddress?.Value ?? string.Empty;
                        supplyOrganization.Bank = syncClient.BankName;
                        supplyOrganization.Name = syncClient.ClientName;
                        supplyOrganization.AccountNumber = syncClient.BankAccountNumber;
                        supplyOrganization.BankAccount = syncClient.BankAccountCode;
                        supplyOrganization.EmailAddress = emailAddress?.Value ?? string.Empty;
                        supplyOrganization.PhoneNumber = syncClientAddress?.Value ?? string.Empty;
                        supplyOrganization.ContactPersonName = syncClient.MainContactPersonName;
                        supplyOrganization.SROI = syncClient.SROI;
                        supplyOrganization.TIN = syncClient.TIN;
                        supplyOrganization.USREOU = syncClient.USREOU;

                        if (forAmg) supplyOrganization.IsNotResident = syncClient.IsNotResident;

                        clientsSyncRepository.Update(supplyOrganization);
                    }
                } else if (syncClient.Supplier) {
                    if (supplier == null) {
                        supplier =
                            new Client {
                                TIN = syncClient.TIN,
                                USREOU = syncClient.USREOU,
                                SROI = syncClient.SROI,
                                Brand = syncClient.ClientName,
                                Manufacturer = syncClient.ClientName,
                                SupplierName = syncClient.ClientName,
                                ClientBankDetails = new ClientBankDetails {
                                    AccountNumber = new ClientBankDetailAccountNumber {
                                        CurrencyId = 2
                                    },
                                    ClientBankDetailIbanNo = new ClientBankDetailIbanNo {
                                        CurrencyId = 2
                                    }
                                },
                                FullName = syncClient.FullName,
                                IsIndividual = syncClient.IsIndividual,
                                IsActive = true,
                                Manager = syncClient.MainContactPersonName,
                                OriginalRegionCode = syncClient.RegionCode,
                                IsNotResident = syncClient.IsNotResident,
                                Name = syncClient.ClientName,
                                IsSupplier = true,
                                OrderExpireDays = 3,
                                ClearCartAfterDays = 3
                            };

                        if (forAmg) {
                            supplier.IsNotResident = syncClient.IsNotResident;
                            supplier.SourceAmgId = syncClient.SourceId;
                            supplier.SourceAmgCode = syncClient.ClientCode;
                        } else {
                            supplier.SourceFenixId = syncClient.SourceId;
                            supplier.SourceFenixCode = syncClient.ClientCode;
                        }

                        if (supplier.ClientBankDetails != null) {
                            supplier.ClientBankDetails.AccountNumberId = clientsSyncRepository.Add(supplier.ClientBankDetails.AccountNumber);
                            supplier.ClientBankDetails.ClientBankDetailIbanNoId = clientsSyncRepository.Add(supplier.ClientBankDetails.ClientBankDetailIbanNo);
                            supplier.ClientBankDetailsId = clientsSyncRepository.Add(supplier.ClientBankDetails);
                        }

                        clients.Add(supplier);
                    } else {
                        supplier.Deleted = false;
                        supplier.TIN = syncClient.TIN;
                        supplier.USREOU = syncClient.USREOU;
                        supplier.SROI = syncClient.SROI;
                        supplier.Brand = syncClient.ClientName;
                        supplier.Manufacturer = syncClient.ClientName;
                        supplier.SupplierName = syncClient.ClientName;
                        supplier.FullName = syncClient.FullName;
                        supplier.IsIndividual = syncClient.IsIndividual;
                        supplier.IsActive = true;
                        supplier.Manager = syncClient.MainContactPersonName;
                        supplier.Name = syncClient.ClientName;
                        supplier.IsSupplier = true;

                        if (forAmg) {
                            supplier.IsNotResident = syncClient.IsNotResident;
                            supplier.SourceAmgId = syncClient.SourceId;
                            supplier.SourceAmgCode = syncClient.ClientCode;
                        } else {
                            supplier.SourceFenixId = syncClient.SourceId;
                            supplier.SourceFenixCode = syncClient.ClientCode;
                        }
                    }

                    supplier.IsSupplier = true;
                    supplier.ClientGroupName = syncClient.ClientGroupName;
                }

                foreach (Client foundClient in clients.Where(x => forAmg ? x.SourceAmgCode == syncClient.ClientCode : x.SourceFenixCode == syncClient.ClientCode)) {
                    foreach (SyncClientAddress clientAddress in syncClient.SyncClientAddresses)
                        switch (clientAddress.AddressType) {
                            case SyncClientAddressType.Address:
                                switch (clientAddress.AddressInfoType) {
                                    case SyncClientAddressInfoType.DeliveryAddress:
                                        foundClient.DeliveryAddress = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.ActualAddress:
                                        foundClient.ActualAddress = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.LegalAddress:
                                        foundClient.LegalAddress = clientAddress.Value;

                                        break;
                                }

                                break;
                            case SyncClientAddressType.Phone:
                                switch (clientAddress.AddressInfoType) {
                                    case SyncClientAddressInfoType.AccountingNumber:
                                        foundClient.AccountantNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.MobileNumber:
                                        foundClient.MobileNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.FaxNumber:
                                        foundClient.FaxNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.PhoneNumber:
                                        foundClient.ClientNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.DeliveryNumber:
                                        foundClient.SMSNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.ManagerNumber:
                                        foundClient.DirectorNumber = clientAddress.Value;
                                        break;
                                }

                                break;
                            case SyncClientAddressType.Email:
                                foundClient.EmailAddress = clientAddress.Value;

                                break;
                            case SyncClientAddressType.None:
                                switch (clientAddress.AddressInfoType) {
                                    case SyncClientAddressInfoType.AccountingNumber:
                                        foundClient.AccountantNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.MobileNumber:
                                        foundClient.MobileNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.FaxNumber:
                                        foundClient.FaxNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.PhoneNumber:
                                        foundClient.ClientNumber = clientAddress.Value;
                                        foundClient.MobileNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.DeliveryNumber:
                                        foundClient.SMSNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.ManagerNumber:
                                        foundClient.DirectorNumber = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.DeliveryAddress:
                                        foundClient.DeliveryAddress = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.ActualAddress:
                                        foundClient.ActualAddress = clientAddress.Value;

                                        break;
                                    case SyncClientAddressInfoType.LegalAddress:
                                        foundClient.LegalAddress = clientAddress.Value;

                                        break;
                                }

                                break;
                        }

                    if (foundClient.RegionCode != null)
                        if (foundClient.RegionCode.IsNew()) {
                            foundClient.RegionCodeId = clientsSyncRepository.Add(foundClient.RegionCode);
                        } else {
                            clientsSyncRepository.Update(foundClient.RegionCode);

                            foundClient.RegionCodeId = foundClient.RegionCode.Id;
                        }
                    else
                        foundClient.RegionCodeId = null;

                    if (foundClient.IsNew())
                        foundClient.Id = clientsSyncRepository.Add(foundClient);
                    else
                        clientsSyncRepository.Update(foundClient);

                    if (foundClient.ClientInRole == null) {
                        foundClient.ClientInRole = new ClientInRole {
                            ClientId = foundClient.Id
                        };

                        if (foundClient.IsSupplier) {
                            foundClient.ClientInRole.ClientTypeId = 3;
                            foundClient.ClientInRole.ClientTypeRoleId = 4;
                        } else {
                            if (!string.IsNullOrEmpty(syncClient.ClientGroupName) && syncClient.ClientGroupName.Equals(PL_CLIENTS_GROUP_NAME)) {
                                foundClient.ClientInRole.ClientTypeId = 2;
                                foundClient.ClientInRole.ClientTypeRoleId = 2;
                            } else if (!string.IsNullOrEmpty(syncClient.ClientGroupName) && syncClient.ClientGroupName.Equals(PL_UK_CLIENTS_GROUP_NAME)) {
                                foundClient.ClientInRole.ClientTypeId = 2;
                                foundClient.ClientInRole.ClientTypeRoleId = 3;
                            } else {
                                foundClient.ClientInRole.ClientTypeId = 2;

                                KeyValuePair<long, string> result = _clientGroupNames.FirstOrDefault(c => c.Value == syncClient.ClientGroupName);

                                foundClient.ClientInRole.ClientTypeRoleId = result.Value is not null ? result.Key : 1;
                            }
                        }

                        foundClient.ClientInRole.ClientType = clientTypes.FirstOrDefault(x => x.Id == foundClient.ClientInRole.ClientTypeId);
                        foundClient.ClientInRole.ClientTypeRole = clientTypeRoles.FirstOrDefault(x => x.Id == foundClient.ClientInRole.ClientTypeRoleId);

                        clientsSyncRepository.Add(foundClient.ClientInRole);
                    } else {
                        if (foundClient.IsSupplier) {
                            foundClient.ClientInRole.ClientTypeId = 3;
                            foundClient.ClientInRole.ClientTypeRoleId = 4;
                        } else {
                            if (_clientGroupNames.Any(c => c.Value.Equals(syncClient.ClientGroupName))) {
                                foundClient.ClientInRole.ClientTypeId = 2;

                                KeyValuePair<long, string> result = _clientGroupNames.FirstOrDefault(c => c.Value == syncClient.ClientGroupName);

                                foundClient.ClientInRole.ClientTypeRoleId = result.Value is not null ? result.Key : 1;
                            } else if (!string.IsNullOrEmpty(syncClient.ClientGroupName) && syncClient.ClientGroupName.Equals(PL_CLIENTS_GROUP_NAME)) {
                                foundClient.ClientInRole.ClientTypeId = 2;
                                foundClient.ClientInRole.ClientTypeRoleId = 2;
                            } else if (!string.IsNullOrEmpty(syncClient.ClientGroupName) && syncClient.ClientGroupName.Equals(PL_UK_CLIENTS_GROUP_NAME)) {
                                foundClient.ClientInRole.ClientTypeId = 2;
                                foundClient.ClientInRole.ClientTypeRoleId = 3;
                            } else {
                                foundClient.ClientInRole.ClientTypeId = 2;
                                foundClient.ClientInRole.ClientTypeRoleId = 1;
                            }
                        }

                        clientsSyncRepository.Update(foundClient.ClientInRole);
                    }

                    if (!syncClient.IsSubClient) {
                        clientsSyncRepository.RemoveSubClientsByRootClientId(foundClient.Id);

                        continue;
                    }

                    Client rootClient;

                    if (forAmg)
                        rootClient = clients.FirstOrDefault(c => c.SourceAmgCode.Equals(syncClient.MainClientCode));
                    else
                        rootClient = clients.FirstOrDefault(c => c.SourceFenixCode.Equals(syncClient.MainClientCode));

                    if (rootClient == null) continue;

                    clientsSyncRepository.Add(new ClientSubClient {
                        RootClientId = rootClient.Id,
                        SubClientId = foundClient.Id
                    });
                }
            }

            foreach (Client client in clients.Where(c => c.MainManagerId.HasValue))
                clientsSyncRepository.AddClientUserProfile(new ClientUserProfile {
                    ClientId = client.Id,
                    UserProfileId = client.MainManagerId.Value
                });

            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.CLIENTS_AND_SUPPLIERS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.TAX_INSPECTIONS_SYNC_START]));

            IEnumerable<SyncTaxInspection> syncTaxInspections =
                forAmg ? clientsSyncRepository.GetAmgAllSyncTaxInspections() : clientsSyncRepository.GetAllSyncTaxInspections();

            List<TaxInspection> taxInspections =
                clientsSyncRepository.GetAllTaxInspections();

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

                    taxInspection.Id = clientsSyncRepository.Add(taxInspection);
                } else {
                    taxInspection.Deleted = false;
                    taxInspection.InspectionNumber = syncTaxInspection.TaxInspectionCode;
                    taxInspection.InspectionType = syncTaxInspection.TypeDPI;
                    taxInspection.InspectionName = syncTaxInspection.TaxInspectionName;
                    taxInspection.InspectionRegionName = syncTaxInspection.NameAdminDistrict;
                    taxInspection.InspectionRegionCode = syncTaxInspection.CodeAdminDistrict;
                    taxInspection.InspectionAddress = syncTaxInspection.Address;
                    taxInspection.InspectionUSREOU = syncTaxInspection.EDRPOU;

                    clientsSyncRepository.Update(taxInspection);
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.TAX_INSPECTIONS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ORGANIZATIONS_SYNC_START]));

            IEnumerable<SyncOrganization> syncOrganizations =
                forAmg ? clientsSyncRepository.GetAmgAllSyncOrganizations() : clientsSyncRepository.GetAllSyncOrganizations(organizationNames);

            IEnumerable<Currency> currencies =
                clientsSyncRepository.GetAllCurrencies();

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

                clientsSyncRepository.Update(currency);
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
                    organizations.FirstOrDefault(o => o.Name.Equals(syncOrganization.OrganizationName));

                IEnumerable<SyncOrganizationAddress> syncOrganizationAddresses =
                    forAmg
                        ? clientsSyncRepository.GetAmgOrganizationAddresses(syncOrganization.OrganizationCode)
                        : clientsSyncRepository.GetOrganizationAddresses(syncOrganization.OrganizationCode);

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

                    organization.Id = clientsSyncRepository.Add(organization);

                    clientsSyncRepository.Add(new OrganizationTranslation {
                        OrganizationId = organization.Id,
                        Name = organization.NamePl,
                        CultureCode = "pl"
                    });

                    clientsSyncRepository.Add(new OrganizationTranslation {
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

                    clientsSyncRepository.Update(organization);

                    OrganizationTranslation plTranslation = organization.OrganizationTranslations.FirstOrDefault(t => t.CultureCode == "pl");

                    if (plTranslation == null) {
                        clientsSyncRepository.Add(new OrganizationTranslation {
                            OrganizationId = organization.Id,
                            Name = organization.NamePl,
                            CultureCode = "pl"
                        });
                    } else {
                        plTranslation.Name = organization.NamePl;

                        clientsSyncRepository.Update(plTranslation);
                    }

                    OrganizationTranslation ukTranslation = organization.OrganizationTranslations.FirstOrDefault(t => t.CultureCode == "uk");

                    if (ukTranslation == null) {
                        clientsSyncRepository.Add(new OrganizationTranslation {
                            OrganizationId = organization.Id,
                            Name = organization.NameUk,
                            CultureCode = "uk"
                        });
                    } else {
                        ukTranslation.Name = organization.NamePl;

                        clientsSyncRepository.Update(ukTranslation);
                    }
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ORGANIZATIONS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.STORAGES_SYNC_START]));

            IEnumerable<SyncStorage> syncStorages =
                forAmg ? clientsSyncRepository.GetAmgAllSyncStorages() : clientsSyncRepository.GetAllSyncStorages();

            List<Storage> storages =
                clientsSyncRepository.GetAllStorages();

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

                    storage.Id = clientsSyncRepository.Add(storage);

                    storages.Add(storage);
                } else {
                    storage.Deleted = false;
                    storage.Locale = organization.Culture;
                    storage.ForDefective = false;
                    storage.ForVatProducts = _vatOrganizationNames.Any(n => n.Equals(organization.Name));
                    storage.OrganizationId = organization.Id;

                    clientsSyncRepository.Update(storage);
                }

                organization.StorageId = storage.Id;

                clientsSyncRepository.Update(organization);
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.STORAGES_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.PRICINGS_SYNC_START]));

            IEnumerable<PriceType> priceTypes =
                clientsSyncRepository.GetAllPriceTypes();

            IEnumerable<SyncPricing> syncPricings =
                forAmg ? clientsSyncRepository.GetAmgAllSyncPricings() : clientsSyncRepository.GetAllSyncPricings();

            PriceType assigned = priceTypes.First(x => x.Name == PriceTypeConst.ASSIGNED);

            PriceType calculated = priceTypes.First(x => x.Name == PriceTypeConst.CALCULATED);

            List<Pricing> pricings =
                clientsSyncRepository.GetAllPricings();

            foreach (SyncPricing syncPricing in syncPricings
                         .OrderByDescending(s => string.IsNullOrEmpty(s.BaseName))) {
                Pricing pricing = pricings.FirstOrDefault(x => x.Name == syncPricing.Name);

                if (pricing == null)
                    pricing = new Pricing {
                        Name = syncPricing.Name,
                        CurrencyId = currencies.First(c => c.Code.Equals("EUR")).Id,
                        ForVat = syncPricing.ForVat,
                        Culture = syncPricing.Name.StartsWith("�L") || syncPricing.Name.StartsWith("PL") ? "pl" : "uk",
                        ExtraCharge = Convert.ToDouble(syncPricing.Discount)
                    };

                if (!string.IsNullOrEmpty(syncPricing.BaseName)) {
                    if (pricings.All(p => p.Name != syncPricing.BaseName)) continue;

                    Pricing basePricing = pricings.First(p => p.Name == syncPricing.BaseName);
                    pricing.PriceTypeId = calculated.Id;
                    pricing.BasePricingId = basePricing.Id;

                    if (syncPricing.Discount > 0)
                        if (basePricing.CalculatedExtraCharge.Equals(decimal.Zero))
                            pricing.CalculatedExtraCharge = syncPricing.Discount;
                        else
                            pricing.CalculatedExtraCharge =
                                decimal.Round(
                                    basePricing.CalculatedExtraCharge + syncPricing.Discount +
                                    basePricing.CalculatedExtraCharge * syncPricing.Discount / 100,
                                    4
                                );
                    else
                        pricing.CalculatedExtraCharge = decimal.Zero;
                } else {
                    pricing.PriceTypeId = assigned.Id;
                    pricing.CalculatedExtraCharge = syncPricing.Discount;
                }

                if (pricing.IsNew()) {
                    pricing.Id = clientsSyncRepository.Add(pricing);

                    clientsSyncRepository.Add(new PricingTranslation {
                        Name = syncPricing.Name,
                        PricingId = pricing.Id,
                        CultureCode = "uk"
                    });

                    clientsSyncRepository.Add(new PricingTranslation {
                        Name = syncPricing.Name,
                        PricingId = pricing.Id,
                        CultureCode = "pl"
                    });
                } else {
                    pricing.Deleted = false;

                    clientsSyncRepository.Update(pricing);
                }

                if (pricings.All(x => x.Name != pricing.Name))
                    pricings.Add(pricing);
            }

            if (pricings.All(p => p.Name != SUPPLY_PRICING_NAME)) {
                Pricing pricing = new() {
                    Name = SUPPLY_PRICING_NAME,
                    CurrencyId = currencies.First(c => c.Code.Equals("EUR")).Id,
                    ForVat = false,
                    ExtraCharge = 0
                };

                pricing.Id = clientsSyncRepository.Add(pricing);

                clientsSyncRepository.Add(new PricingTranslation {
                    Name = SUPPLY_PRICING_NAME,
                    PricingId = pricing.Id,
                    CultureCode = "uk"
                });

                clientsSyncRepository.Add(new PricingTranslation {
                    Name = SUPPLY_PRICING_NAME,
                    PricingId = pricing.Id,
                    CultureCode = "pl"
                });

                pricings.Add(pricing);
            }

            clientsSyncRepository.SetSharesPricings();

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.PRICINGS_SYNC_END]));

            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.AGREEMENTS_AND_DISCOUNTS_SYNC_START]));

            IEnumerable<ProductGroup> productGroups =
                clientsSyncRepository.GetAllProductGroups();

            Currency eur = clientsSyncRepository.GetEURCurrencyIfExists();
            if (eur == null) {
                clientsSyncRepository.Add(new Currency {
                    Code = "EUR",
                    Name = "����",
                    CodeOneC = "978"
                });

                eur = clientsSyncRepository.GetEURCurrencyIfExists();
            }

            Currency usd = clientsSyncRepository.GetUSDCurrencyIfExists();
            if (usd == null) {
                clientsSyncRepository.Add(new Currency {
                    Code = "USD",
                    Name = "������ ���",
                    CodeOneC = "840"
                });

                usd = clientsSyncRepository.GetUSDCurrencyIfExists();
            }

            Currency pln = clientsSyncRepository.GetPLNCurrencyIfExists();
            if (pln == null) {
                clientsSyncRepository.Add(new Currency {
                    Code = "PLN",
                    Name = "Polish zloty",
                    CodeOneC = "830"
                });

                pln = clientsSyncRepository.GetPLNCurrencyIfExists();
            }

            Currency uah = clientsSyncRepository.GetUAHCurrencyIfExists();
            if (uah == null) {
                clientsSyncRepository.Add(new Currency {
                    Code = "UAH",
                    Name = "UAH",
                    CodeOneC = "980"
                });

                uah = clientsSyncRepository.GetUAHCurrencyIfExists();
            }

            if (!forAmg) clientsSyncRepository.DeleteDefaultAgreementForSyncConsignments();

            foreach (Client client in clients.Where(c =>
                         syncClients.Any(s => forAmg ? s.ClientCode.Equals(c.SourceAmgCode) : s.ClientCode.Equals(c.SourceFenixCode)))) {
                SyncClient syncClient = forAmg
                    ? syncClients.First(c => c.ClientCode.Equals(client.SourceAmgCode))
                    : syncClients.First(c => c.ClientCode.Equals(client.SourceFenixCode));

                if (syncClient == null) continue;

                bool isProvider = client.ClientInRole.ClientType.Type == ClientTypeType.Provider;

                IEnumerable<SyncAgreement> syncAgreements =
                    forAmg
                        ? clientsSyncRepository.GetAmgAllSyncAgreementsByCode(client.SourceAmgCode.Value)
                        : clientsSyncRepository.GetAllSyncAgreementsByCode(client.SourceFenixCode.Value, isProvider);

                if (!syncAgreements.Any() && !client.ClientAgreements.Any()) clientsSyncRepository.DeleteClientById(client.Id);

                if (!isProvider) {
                    bool first = true;

                    foreach (SyncAgreement syncAgreement in syncAgreements.Where(x => x.Type == TypeSyncAgreement.WithBuyer)) {
                        ClientAgreement existClientAgreement =
                            client.ClientAgreements.FirstOrDefault(x => x.Agreement.SourceIdsEqual(syncAgreement.SourceId));

                        Currency agreementCurrency = currencies.FirstOrDefault(c => c.CodeOneC.Equals(syncAgreement.CurrencyCode));

                        if (agreementCurrency == null) {
                            if (eurCodes.Contains(syncAgreement.CurrencyCode))
                                agreementCurrency = eur;
                            else if (usdCodes.Contains(syncAgreement.CurrencyCode))
                                agreementCurrency = usd;
                            else if (plnCodes.Contains(syncAgreement.CurrencyCode))
                                agreementCurrency = pln;
                            else
                                agreementCurrency = uah;
                        }

                        if (existClientAgreement == null) {
                            if (!string.IsNullOrEmpty(syncAgreement.Number))
                                existClientAgreement = client.ClientAgreements.FirstOrDefault(x => x.Agreement.Number == syncAgreement.Number &&
                                                                                                   x.Agreement.Currency.Id == agreementCurrency.Id);

                            if (existClientAgreement == null)
                                existClientAgreement = client.ClientAgreements.FirstOrDefault(x => x.Agreement.Name == syncAgreement.Name &&
                                                                                                   x.Agreement.Currency.Id == agreementCurrency.Id);
                        }

                        Organization organization = organizations.First(o => o.Name.Equals(syncAgreement.OrganizationName));

                        Pricing pricing = null;
                        Pricing promotionalPrice = null;

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

                        if (string.IsNullOrEmpty(syncAgreement.TypePriceName)) {
                            if (organization.IsVatAgreements)
                                pricing = pricings.First(p => p.Name.Equals(STANDARD_PROMOTIONAL_PRICING_VAT));
                            else
                                pricing = pricings.First(p => p.Name.Equals(STANDARD_PROMOTIONAL_PRICING));
                        } else {
                            pricing = pricings.First(p => p.Name.Equals(syncAgreement.TypePriceName));
                        }

                        if (!string.IsNullOrEmpty(syncAgreement.PromotionalTypePriceName))
                            promotionalPrice = pricings.First(p => p.Name.Equals(syncAgreement.PromotionalTypePriceName));

                        if (existClientAgreement != null && existClientAgreement.Agreement != null) {
                            if (forAmg || syncAgreement.OrganizationName != DEFAULT_ORGANIZATION_AMG) existClientAgreement.Agreement.Name = syncAgreement.Name;

                            existClientAgreement.Agreement.CurrencyId = agreementCurrency.Id;
                            existClientAgreement.Agreement.OrganizationId = organization.Id;
                            existClientAgreement.Agreement.PricingId = pricing.Id;
                            existClientAgreement.Agreement.Number = syncAgreement.Number;
                            existClientAgreement.Agreement.IsAccounting = isAccounting;
                            existClientAgreement.Agreement.IsActive = first;
                            existClientAgreement.Agreement.IsControlAmountDebt = true;
                            existClientAgreement.Agreement.IsControlNumberDaysDebt = true;
                            existClientAgreement.Agreement.IsManagementAccounting = isManagement;
                            existClientAgreement.Agreement.WithVATAccounting = withVat;
                            existClientAgreement.Agreement.IsPrePaymentFull = true;
                            existClientAgreement.Agreement.IsPrePayment = false;
                            existClientAgreement.Agreement.PrePaymentPercentages = 0;
                            existClientAgreement.Agreement.NumberDaysDebt = syncAgreement.DebtDaysAllowedNumber;
                            existClientAgreement.Agreement.AmountDebt = syncAgreement.PermissibleDebtAmount;
                            existClientAgreement.Agreement.PromotionalPricingId = promotionalPrice?.Id ?? null;
                            existClientAgreement.Deleted = false;
                            existClientAgreement.Agreement.Deleted = false;
                            existClientAgreement.Agreement.WithAgreementLine = true;
                            existClientAgreement.Agreement.FromDate = syncAgreement.FromDate;
                            existClientAgreement.Agreement.ToDate = syncAgreement.ToDate;
                            existClientAgreement.OriginalClientAmgCode = client.SourceAmgCode;
                            existClientAgreement.OriginalClientFenixCode = client.SourceFenixCode;

                            if (forAmg) {
                                existClientAgreement.Agreement.SourceAmgId = syncAgreement.SourceId;
                                existClientAgreement.Agreement.SourceAmgCode = syncAgreement.SourceCode;
                            } else {
                                existClientAgreement.Agreement.SourceFenixId = syncAgreement.SourceId;
                                existClientAgreement.Agreement.SourceFenixCode = syncAgreement.SourceCode;
                            }

                            clientsSyncRepository.Update(existClientAgreement.Agreement);
                            clientsSyncRepository.Update(existClientAgreement);
                        } else {
                            ClientAgreement newClientAgreement = new() {
                                Agreement = new Agreement {
                                    Name = syncAgreement.Name,
                                    CurrencyId = agreementCurrency.Id,
                                    OrganizationId = organization.Id,
                                    PricingId = pricing.Id,
                                    Number = syncAgreement.Number,
                                    IsAccounting = isAccounting,
                                    IsActive = first,
                                    IsControlAmountDebt = true,
                                    IsControlNumberDaysDebt = true,
                                    IsManagementAccounting = isManagement,
                                    WithVATAccounting = withVat,
                                    IsPrePaymentFull = true,
                                    IsPrePayment = false,
                                    PrePaymentPercentages = 0,
                                    NumberDaysDebt = syncAgreement.DebtDaysAllowedNumber,
                                    AmountDebt = syncAgreement.PermissibleDebtAmount,
                                    PromotionalPricingId = promotionalPrice?.Id ?? null,
                                    WithAgreementLine = true,
                                    FromDate = syncAgreement.FromDate,
                                    ToDate = syncAgreement.ToDate
                                },
                                ProductReservationTerm = 3,
                                ClientId = client.Id,
                                OriginalClientAmgCode = client.SourceAmgCode,
                                OriginalClientFenixCode = client.SourceFenixCode
                            };

                            if (forAmg) {
                                newClientAgreement.Agreement.SourceAmgId = syncAgreement.SourceId;
                                newClientAgreement.Agreement.SourceAmgCode = syncAgreement.SourceCode;
                            } else {
                                newClientAgreement.Agreement.SourceFenixId = syncAgreement.SourceId;
                                newClientAgreement.Agreement.SourceFenixCode = syncAgreement.SourceCode;
                            }

                            newClientAgreement.AgreementId = clientsSyncRepository.Add(newClientAgreement.Agreement);

                            newClientAgreement.Id = clientsSyncRepository.Add(newClientAgreement);
                        }

                        first = false;

                        if (!productGroups.Any()) continue;

                        ClientAgreement clientAgreement = clientsSyncRepository.GetClientAgreementBySourceId(syncAgreement.SourceId, forAmg);

                        IEnumerable<SyncDiscount> syncDiscounts =
                            forAmg
                                ? clientsSyncRepository.GetAmgAllDiscountsForSpecificClient(client.SourceAmgCode.Value, pricing.Name)
                                : clientsSyncRepository
                                    .GetAllDiscountsForSpecificClient(
                                        client.SourceFenixCode.Value,
                                        pricing.Name
                                    );

                        StringBuilder builder = new();

                        builder.Append("INSERT INTO [ProductGroupDiscount] ");
                        builder.Append("([ClientAgreementID], [ProductGroupID], [IsActive], [DiscountRate], [Updated])");
                        builder.Append("VALUES ");

                        bool firstInsert = true;

                        StringBuilder buildAdded = new();

                        foreach (ProductGroup productGroup in productGroups) {
                            ProductGroupDiscount productGroupDiscount =
                                clientsSyncRepository.GetProductGroupDiscountByClientAgreementIdAndProductGroupId(clientAgreement.Id, productGroup.Id);

                            SyncDiscount discount = syncDiscounts.FirstOrDefault(s => s.SourceIdsEqual(productGroup.SourceAmgId));

                            if (discount == null)
                                discount = syncDiscounts.FirstOrDefault(s => s.SourceIdsEqual(productGroup.SourceFenixId));

                            if (productGroupDiscount != null) {
                                buildAdded.Append("UPDATE [ProductGroupDiscount] ");

                                if (discount == null) {
                                    buildAdded.Append("SET [Updated] = GETUTCDATE(), [DiscountRate] = 0.00, [IsActive] = 1 ");
                                } else {
                                    if (discount.IsActive)
                                        buildAdded.Append(
                                            $"SET [Updated] = GETUTCDATE(), [DiscountRate] = {discount.Discount.ToString(CultureInfo.InvariantCulture)}, [IsActive] = 1 ");
                                    else
                                        buildAdded.Append(
                                            $"SET [Updated] = GETUTCDATE(), [DiscountRate] = {discount.Discount.ToString(CultureInfo.InvariantCulture)}, [IsActive] = 0 ");
                                }

                                buildAdded.Append(
                                    $"WHERE [ClientAgreementID] = {productGroupDiscount.ClientAgreementId} AND [ProductGroupID] = {productGroupDiscount.ProductGroupId}; ");
                            } else {
                                if (!firstInsert)
                                    builder.Append(", ");

                                builder.Append(
                                    discount == null
                                        ? $"({clientAgreement.Id}, {productGroup.Id}, 1, 0.00, GETUTCDATE())"
                                        : discount.IsActive
                                            ? $"({clientAgreement.Id}, {productGroup.Id}, 1, {discount.Discount.ToString(CultureInfo.InvariantCulture)}, GETUTCDATE())"
                                            : $"({clientAgreement.Id}, {productGroup.Id}, 0, {discount.Discount.ToString(CultureInfo.InvariantCulture)}, GETUTCDATE())"
                                );

                                firstInsert = false;
                            }
                        }

                        if (!firstInsert)
                            clientsSyncRepository.ExecuteSql(builder.ToString());

                        string buildAddedInString = buildAdded.ToString();

                        if (!string.IsNullOrEmpty(buildAddedInString))
                            clientsSyncRepository.ExecuteSql(buildAddedInString);
                    }
                } else if (isProvider) {
                    bool first = true;

                    foreach (SyncAgreement syncAgreement in syncAgreements.Where(x => x.Type == TypeSyncAgreement.WithSupplier)) {
                        Organization organization = organizations.First(o => o.Name.Equals(syncAgreement.OrganizationName));

                        Currency agreementCurrency = currencies.FirstOrDefault(c => c.CodeOneC.Equals(syncAgreement.CurrencyCode));

                        if (agreementCurrency == null) {
                            if (eurCodes.Contains(syncAgreement.CurrencyCode))
                                agreementCurrency = eur;
                            else if (usdCodes.Contains(syncAgreement.CurrencyCode))
                                agreementCurrency = usd;
                            else if (plnCodes.Contains(syncAgreement.CurrencyCode))
                                agreementCurrency = pln;
                            else
                                agreementCurrency = uah;
                        }

                        ClientAgreement existClientAgreement =
                            client.ClientAgreements.FirstOrDefault(x => x.Agreement.SourceIdsEqual(syncAgreement.SourceId));

                        if (existClientAgreement == null) {
                            if (!string.IsNullOrEmpty(syncAgreement.Number))
                                existClientAgreement = client.ClientAgreements.FirstOrDefault(x => x.Agreement.Number == syncAgreement.Number &&
                                                                                                   x.Agreement.CurrencyId == agreementCurrency.Id);

                            if (existClientAgreement == null)
                                existClientAgreement = client.ClientAgreements.FirstOrDefault(x => x.Agreement.Name == syncAgreement.Name &&
                                                                                                   x.Agreement.CurrencyId == agreementCurrency.Id);
                        }

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

                        if (existClientAgreement != null && existClientAgreement.Agreement != null) {
                            existClientAgreement.Agreement.Name = syncAgreement.Name;
                            existClientAgreement.Agreement.CurrencyId = agreementCurrency.Id;
                            existClientAgreement.Agreement.Number = syncAgreement.Number;
                            existClientAgreement.Agreement.IsAccounting = isAccounting;
                            existClientAgreement.Agreement.IsActive = first;
                            existClientAgreement.Agreement.IsControlAmountDebt = true;
                            existClientAgreement.Agreement.IsControlNumberDaysDebt = true;
                            existClientAgreement.Agreement.IsManagementAccounting = isManagement;
                            existClientAgreement.Agreement.WithVATAccounting = withVat;
                            existClientAgreement.Agreement.DeferredPayment = string.Empty;
                            existClientAgreement.Agreement.TermsOfPayment = string.Empty;
                            existClientAgreement.Agreement.IsPrePaymentFull = true;
                            existClientAgreement.Agreement.IsPrePayment = false;
                            existClientAgreement.Agreement.PrePaymentPercentages = 0;
                            existClientAgreement.Agreement.NumberDaysDebt = syncAgreement.DebtDaysAllowedNumber;
                            existClientAgreement.Agreement.FromDate = syncAgreement.FromDate;
                            existClientAgreement.Agreement.ToDate = syncAgreement.ToDate;
                            existClientAgreement.Deleted = false;
                            existClientAgreement.Agreement.Deleted = false;
                            existClientAgreement.Agreement.WithAgreementLine = true;
                            existClientAgreement.OriginalClientAmgCode = client.SourceAmgCode;
                            existClientAgreement.OriginalClientFenixCode = client.SourceFenixCode;

                            existClientAgreement.Agreement.ProviderPricing = new ProviderPricing {
                                Name = string.IsNullOrEmpty(syncAgreement.TypePriceName) ? SUPPLY_PRICING_NAME : syncAgreement.TypePriceName,
                                BasePricingId = pricings.First(p => p.Name.Equals(SUPPLY_PRICING_NAME)).Id
                            };

                            existClientAgreement.Agreement.ProviderPricingId = clientsSyncRepository.Add(existClientAgreement.Agreement.ProviderPricing);

                            if (forAmg) {
                                existClientAgreement.Agreement.SourceAmgId = syncAgreement.SourceId;
                                existClientAgreement.Agreement.SourceAmgCode = syncAgreement.SourceCode;
                            } else {
                                existClientAgreement.Agreement.SourceFenixId = syncAgreement.SourceId;
                                existClientAgreement.Agreement.SourceFenixCode = syncAgreement.SourceCode;
                            }

                            clientsSyncRepository.Update(existClientAgreement.Agreement);
                            clientsSyncRepository.Update(existClientAgreement);
                        } else {
                            ClientAgreement clientAgreement = new() {
                                Agreement = new Agreement {
                                    Name = syncAgreement.Name,
                                    CurrencyId = agreementCurrency.Id,
                                    OrganizationId = organization.Id,
                                    ProviderPricing = new ProviderPricing {
                                        Name = string.IsNullOrEmpty(syncAgreement.TypePriceName) ? SUPPLY_PRICING_NAME : syncAgreement.TypePriceName,
                                        BasePricingId = pricings.First(p => p.Name.Equals(SUPPLY_PRICING_NAME)).Id
                                    },
                                    Number = syncAgreement.Number,
                                    IsAccounting = isAccounting,
                                    IsActive = first,
                                    IsControlAmountDebt = true,
                                    IsControlNumberDaysDebt = true,
                                    IsManagementAccounting = isManagement,
                                    WithVATAccounting = withVat,
                                    DeferredPayment = string.Empty,
                                    TermsOfPayment = string.Empty,
                                    IsPrePaymentFull = true,
                                    IsPrePayment = false,
                                    PrePaymentPercentages = 0,
                                    NumberDaysDebt = syncAgreement.DebtDaysAllowedNumber,
                                    WithAgreementLine = true,
                                    FromDate = syncAgreement.FromDate,
                                    ToDate = syncAgreement.ToDate
                                },
                                ProductReservationTerm = 3,
                                ClientId = client.Id,
                                OriginalClientAmgCode = client.SourceAmgCode,
                                OriginalClientFenixCode = client.SourceFenixCode
                            };

                            if (forAmg) {
                                clientAgreement.Agreement.SourceAmgId = syncAgreement.SourceId;
                                clientAgreement.Agreement.SourceAmgCode = syncAgreement.SourceCode;
                            } else {
                                clientAgreement.Agreement.SourceFenixId = syncAgreement.SourceId;
                                clientAgreement.Agreement.SourceFenixCode = syncAgreement.SourceCode;
                            }

                            clientAgreement.Agreement.ProviderPricingId = clientsSyncRepository.Add(clientAgreement.Agreement.ProviderPricing);

                            clientAgreement.AgreementId = clientsSyncRepository.Add(clientAgreement.Agreement);

                            clientsSyncRepository.Add(clientAgreement);
                        }

                        first = false;
                    }
                }
            }

            foreach (SupplyOrganization supplyOrganization in supplyOrganizations.Where(c =>
                         syncClients.Any(s => (forAmg ? s.SourceId.Equals(c.SourceAmgId) : s.SourceId.Equals(c.SourceFenixId)) &&
                                              _supplierServiceGroupNames.Any(n => n.Equals(s.ClientGroupName))))) {
                if (!supplyOrganization.SourceAmgCode.HasValue && !supplyOrganization.SourceFenixCode.HasValue) continue;

                IEnumerable<SyncAgreement> syncAgreements =
                    forAmg
                        ? clientsSyncRepository.GetAmgAllSyncAgreementsByCode(supplyOrganization.SourceAmgCode.Value)
                        : clientsSyncRepository.GetAllSyncAgreementsByCode(supplyOrganization.SourceFenixCode.Value);

                if (!syncAgreements.Any()) continue;

                foreach (SyncAgreement syncAgreement in syncAgreements) {
                    SupplyOrganizationAgreement existAgreement =
                        supplyOrganization.SupplyOrganizationAgreements
                            .FirstOrDefault(x => x.SourceIdsEqual(syncAgreement.SourceId));

                    if (existAgreement == null) {
                        if (!string.IsNullOrEmpty(syncAgreement.Number))
                            existAgreement = supplyOrganization.SupplyOrganizationAgreements.FirstOrDefault(x => x.Number == syncAgreement.Number);

                        if (existAgreement == null)
                            existAgreement = supplyOrganization.SupplyOrganizationAgreements.FirstOrDefault(x => x.Name == syncAgreement.Name);
                    }

                    Currency agreementCurrency = currencies.First(x => x.CodeOneC == syncAgreement.CurrencyCode);

                    if (agreementCurrency == null) {
                        if (eurCodes.Contains(syncAgreement.CurrencyCode))
                            agreementCurrency = eur;
                        else if (usdCodes.Contains(syncAgreement.CurrencyCode))
                            agreementCurrency = usd;
                        else if (plnCodes.Contains(syncAgreement.CurrencyCode))
                            agreementCurrency = pln;
                        else
                            agreementCurrency = uah;
                    }

                    Organization organization = organizations.First(o => o.Name.Equals(syncAgreement.OrganizationName));

                    if (existAgreement != null) {
                        existAgreement.CurrencyId = agreementCurrency.Id;
                        existAgreement.Name = syncAgreement.Name;
                        existAgreement.OrganizationId = organization.Id;
                        existAgreement.ExistFrom = syncAgreement.FromDate ?? DateTime.Now;
                        existAgreement.ExistTo = syncAgreement.ToDate ?? DateTime.Now;
                        existAgreement.Number = syncAgreement.Number;
                        existAgreement.Deleted = false;

                        if (forAmg) {
                            existAgreement.SourceAmgCode = syncAgreement.SourceCode;
                            existAgreement.SourceAmgId = syncAgreement.SourceId;
                        } else {
                            existAgreement.SourceFenixCode = syncAgreement.SourceCode;
                            existAgreement.SourceFenixId = syncAgreement.SourceId;
                        }

                        clientsSyncRepository.Update(existAgreement);
                    } else {
                        SupplyOrganizationAgreement newAgreement = new() {
                            CurrencyId = agreementCurrency.Id,
                            Name = syncAgreement.Name,
                            OrganizationId = organization.Id,
                            ExistFrom = syncAgreement.FromDate ?? DateTime.Now,
                            ExistTo = syncAgreement.ToDate ?? DateTime.Now,
                            Updated = DateTime.Now,
                            Created = DateTime.Now,
                            SupplyOrganizationId = supplyOrganization.Id,
                            Number = syncAgreement.Number
                        };

                        if (forAmg) {
                            newAgreement.SourceAmgCode = syncAgreement.SourceCode;
                            newAgreement.SourceAmgId = syncAgreement.SourceId;
                        } else {
                            newAgreement.SourceFenixCode = syncAgreement.SourceCode;
                            newAgreement.SourceFenixId = syncAgreement.SourceId;
                        }

                        clientsSyncRepository.Add(newAgreement);
                    }
                }
            }

            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.AGREEMENTS_AND_DISCOUNTS_SYNC_END]));

            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.DELIVERY_RECIPIENTS_SYNC_START]));

            foreach (Client client in clients.Where(c =>
                         syncClients.Any(s => (forAmg ? s.ClientCode.Equals(c.SourceAmgCode) : s.ClientCode.Equals(c.SourceFenixCode)) &&
                                              !_supplierServiceGroupNames.Any(n => n.Equals(s.ClientGroupName))))) {
                IEnumerable<SyncDeliveryRecipient> syncDeliveryRecipients =
                    forAmg
                        ? clientsSyncRepository.GetAmgAllSyncDeliveryRecipientsByClientCode(client.SourceAmgCode.Value)
                        : clientsSyncRepository.GetAllSyncDeliveryRecipientsByClientCode(client.SourceFenixCode.Value);

                List<DeliveryRecipient> deliveryRecipients =
                    clientsSyncRepository.GetAllDeliveryRecipientsByClientId(client.Id);

                if (syncDeliveryRecipients.Any()) {
                    foreach (SyncDeliveryRecipient syncDeliveryRecipient in syncDeliveryRecipients) {
                        DeliveryRecipient deliveryRecipient = deliveryRecipients.FirstOrDefault(r => r.FullName == syncDeliveryRecipient.Name);

                        if (deliveryRecipient == null) {
                            deliveryRecipient = new DeliveryRecipient {
                                FullName = syncDeliveryRecipient.Name,
                                ClientId = client.Id,
                                Priority = 0,
                                MobilePhone = string.Empty
                            };

                            deliveryRecipient.Id = clientsSyncRepository.Add(deliveryRecipient);
                        } else {
                            deliveryRecipient.Deleted = false;
                        }

                        if (deliveryRecipient.DeliveryRecipientAddresses.Any(a => a.Value == syncDeliveryRecipient.Address)) continue;

                        clientsSyncRepository.Add(new DeliveryRecipientAddress {
                            Value = syncDeliveryRecipient.Address,
                            Department = string.Empty,
                            City = string.Empty,
                            Priority = 0,
                            DeliveryRecipientId = deliveryRecipient.Id
                        });
                    }
                } else if (!deliveryRecipients.Any()) {
                    DeliveryRecipient deliveryRecipient =
                        new() {
                            FullName = client.FullName,
                            ClientId = client.Id,
                            Priority = 0,
                            MobilePhone = string.Empty
                        };

                    deliveryRecipient.Id = clientsSyncRepository.Add(deliveryRecipient);

                    clientsSyncRepository.Add(new DeliveryRecipientAddress {
                        Value = string.Empty,
                        Department = string.Empty,
                        City = string.Empty,
                        Priority = 0,
                        DeliveryRecipientId = deliveryRecipient.Id
                    });
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.DELIVERY_RECIPIENTS_SYNC_END]));

            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.REORGANIZING_CLIENT_STRUCTURE_START]));

            IEnumerable<Client> existingClients = clientsSyncRepository.GetAllClientsWithRegionCodes();

            List<Client> deletedClients = new();

            foreach (Client client in existingClients) {
                if (client.RegionCode.Value.StartsWith(client.Region.Name)) continue;
                if (deletedClients.Any(c => c.Id.Equals(client.Id))) continue;

                try {
                    IEnumerable<Client> matchingClients =
                        clientsSyncRepository
                            .GetAllBySearchPatterns(
                                client.RegionCode.Value.Substring(1, client.RegionCode.Value.Length - 3) + "%",
                                client.RegionCode.Value.Substring(0, client.RegionCode.Value.Length - 2) + "%",
                                client.Id
                            );

                    if (!matchingClients.Any()) continue;

                    List<Client> tradePoints = new();

                    foreach (Client matchingClient in matchingClients)
                        if (matchingClient.RegionCode.Value.StartsWith("B") || matchingClient.RegionCode.Value.StartsWith("�")) {
                            matchingClient.IsTradePoint = true;

                            clientsSyncRepository.Update(matchingClient);

                            matchingClient.RegionCode.Value =
                                string.Format(
                                    "{0}{1}",
                                    matchingClient.RegionCode.Value.Substring(1, matchingClient.RegionCode.Value.Length - 3),
                                    string.Format("{0:D2}", tradePoints.Count + 1)
                                );

                            clientsSyncRepository.Update(matchingClient.RegionCode);

                            ClientSubClient subClient =
                                clientsSyncRepository
                                    .GetClientSubClientIfExists(
                                        client.Id,
                                        matchingClient.Id
                                    );

                            if (subClient == null) {
                                clientsSyncRepository.Add(new ClientSubClient {
                                    RootClientId = client.Id,
                                    SubClientId = matchingClient.Id
                                });
                            } else {
                                subClient.Deleted = false;

                                clientsSyncRepository.Update(subClient);
                            }

                            deletedClients.Add(matchingClient);

                            tradePoints.Add(matchingClient);
                        } else {
                            if (tradePoints.Any()) {
                                client.NameDistance =
                                    clientsSyncRepository
                                        .GetNameDistance(
                                            client.FullName,
                                            matchingClient.FullName
                                        );

                                Client baseClient = client;

                                foreach (Client tradePoint in tradePoints) {
                                    tradePoint.NameDistance =
                                        clientsSyncRepository
                                            .GetNameDistance(
                                                tradePoint.FullName,
                                                matchingClient.FullName
                                            );

                                    if (baseClient.NameDistance > tradePoint.NameDistance)
                                        baseClient = tradePoint;
                                }

                                clientsSyncRepository.ReAssignClientAgreements(matchingClient.Id, baseClient.Id, forAmg);

                                clientsSyncRepository.RemoveClient(matchingClient.Id, baseClient.Id);
                            } else {
                                clientsSyncRepository.ReAssignClientAgreements(matchingClient.Id, client.Id, forAmg);

                                clientsSyncRepository.RemoveClient(matchingClient.Id, client.Id);
                            }
                        }

                    client.RegionCode.Value = $"{client.RegionCode.Value.Substring(1, client.RegionCode.Value.Length - 3)}00";

                    clientsSyncRepository.Update(client.RegionCode);
                } catch (Exception) {
                    //Ignored
                }
            }

            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.REORGANIZING_CLIENT_STRUCTURE_END], true));
        } catch (Exception exc) {
            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_ERROR], true, true));

            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddDataSyncLogMessage(
                        "SYNC_ERROR Clients and suppliers, agreements, etc",
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