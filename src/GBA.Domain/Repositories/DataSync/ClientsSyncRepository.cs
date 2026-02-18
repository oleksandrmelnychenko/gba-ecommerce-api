using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync;

public sealed class ClientsSyncRepository : IClientsSyncRepository {
    private readonly IDbConnection _amgSyncConnection;
    private readonly IDbConnection _oneCConnection;

    private readonly IDbConnection _remoteSyncConnection;

    private readonly string[] ORGANIZATION_NAMES = new[] {
        "ТОВ «АМГ «КОНКОРД»",
        "Фенікс",
        "ФОП Самолюк Алла Дмитрівна",
        "ФОП Пархоменко Ганна Юріївна",
        "ФОП Самолюк Юрій Миколайович"
    };

    private readonly string[] ORGANIZATION_NAMES_FOR_PROVIDER = new[] {
        "Фенікс",
        "ФОП Самолюк Алла Дмитрівна",
        "ФОП Пархоменко Ганна Юріївна",
        "ФОП Самолюк Юрій Миколайович"
    };

    public ClientsSyncRepository(
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgSyncConnection) {
        _oneCConnection = oneCConnection;

        _remoteSyncConnection = remoteSyncConnection;

        _amgSyncConnection = amgSyncConnection;
    }

    public IEnumerable<SyncClient> GetAllSyncClients() {
        List<SyncClient> clients = new();

        _oneCConnection.Query<SyncClient, SyncClientAddress, SyncClient>(
            "SELECT " +
            "T1._IDRRef [SourceId], " +
            "CAST(T1._Code AS bigint) [ClientCode], " +
            "LTRIM(RTRIM(T1._Description)) [ClientName], " +
            "ISNULL(T6._EnumOrder, 1) [IsIndividual], " +
            "CAST(T1._Fld1112 AS bit) [Buyer], " +
            "CAST(T1._Fld1113 AS bit) [Supplier], " +
            "CAST(T1._Fld1128 AS bit) [IsNotResident], " +
            "T1._Fld1114 [TIN], " +
            "LTRIM(RTRIM(CAST( T1._Fld1115 as nvarchar(max)))) [FullName], " +
            "T1._Fld1125 [USREOU], " +
            "T1._Fld1126 [SROI], " +
            "T7._Code [RegionName], " +
            "T1._Fld1130 [RegionCode], " +
            "T8._Code [BankAccountCode], " +
            "T8._Fld832 [BankAccountNumber], " +
            "T18._Description [BankName], " +
            "T2.Fld12266_ [LastName], " +
            "T2.Fld12267_ [FirstName], " +
            "T2.Fld12268_ [MiddleName], " +
            "CAST(T9._Code AS bigint) [MainClientCode], " +
            "LTRIM(RTRIM(T9._Description)) [MainClientName], " +
            "T10._Code [MainContactPersonCode], " +
            "T10._Description [MainContactPersonName], " +
            "T10._Fld1101 [MainContactPersonPosition], " +
            "T11._Code [ActivityCode], " +
            "T11._Description [ActivityName], " +
            "CAST(T12._Code AS bigint) [MainRecipientCode], " +
            "T12._Description [MainRecipientName], " +
            "T1._Fld1117 [IdentityDocument], " +
            "T1._Fld1121 [SupplierDeadline], " +
            "T1._Fld1119 [Description], " +
            "T1._Fld1131 [QuantityDayDebt], " +
            "CAST(T1._Fld1132 AS bit) [IsControlDayDebt], " +
            "(CASE WHEN T17._Description IS NOT NULL THEN T17._Description WHEN T16._Description IS NOT NULL THEN T16._Description ELSE T14._Description END) [ClientGroupName], " +
            "T15._Description [ManagerName], " +
            "[AddressInfo].* " +
            "FROM dbo._Reference68 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN (SELECT " +
            "T5._Fld12265_TYPE AS Fld12265_TYPE, " +
            "T5._Fld12265_RTRef AS Fld12265_RTRef, " +
            "T5._Fld12265_RRRef AS Fld12265_RRRef, " +
            "T5._Fld12266 AS Fld12266_, " +
            "T5._Fld12268 AS Fld12268_, " +
            "T5._Fld12267 AS Fld12267_ " +
            "FROM (SELECT " +
            "T4._Fld12265_TYPE AS Fld12265_TYPE, " +
            "T4._Fld12265_RTRef AS Fld12265_RTRef, " +
            "T4._Fld12265_RRRef AS Fld12265_RRRef, " +
            "MAX(T4._Period) AS MAXPERIOD_ " +
            "FROM dbo._InfoRg12264 T4 WITH(NOLOCK) " +
            "GROUP BY T4._Fld12265_TYPE, " +
            "T4._Fld12265_RTRef, " +
            "T4._Fld12265_RRRef) T3 " +
            "INNER JOIN dbo._InfoRg12264 T5 WITH(NOLOCK) " +
            "ON T3.Fld12265_TYPE = T5._Fld12265_TYPE AND T3.Fld12265_RTRef = T5._Fld12265_RTRef AND T3.Fld12265_RRRef = T5._Fld12265_RRRef AND T3.MAXPERIOD_ = T5._Period) T2 " +
            "ON (0x08 = T2.Fld12265_TYPE AND 0x00000044 = T2.Fld12265_RTRef AND T1._IDRRef = T2.Fld12265_RRRef) " +
            "LEFT OUTER JOIN dbo._Enum602 T6 WITH(NOLOCK) " +
            "ON T1._Fld1111RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference110 T7 WITH(NOLOCK) " +
            "ON T1._Fld1129RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 T8 WITH(NOLOCK) " +
            "ON T1._Fld1116RRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference14 T18 WITH(NOLOCK) " +
            "ON T8._Fld833RRef = T18._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T9 WITH(NOLOCK) " +
            "ON T1._Fld1110RRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference67 T10 WITH(NOLOCK) " +
            "ON T1._Fld1123RRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference21 T11 WITH(NOLOCK) " +
            "ON T1._Fld1124RRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15417 T12 WITH(NOLOCK) " +
            "ON T1._Fld15484RRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference143 T13 WITH(NOLOCK) " +
            "ON T1._Fld1133RRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T14 WITH(NOLOCK) " +
            "ON T1._ParentIDRRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 T15 WITH(NOLOCK) " +
            "ON T1._Fld1118RRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T16 WITH(NOLOCK) " +
            "ON T14._ParentIDRRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T17 WITH(NOLOCK) " +
            "ON T16._ParentIDRRef = T17._IDRRef " +
            "LEFT JOIN ( " +
            "SELECT " +
            "CAST(T2._Code AS bigint) [ActualClientCode], " +
            "T3._EnumOrder [AddressType], " +
            "CAST(T1._Fld10975 AS nvarchar(max)) [Value], " +
            "CASE " +
            "WHEN T4._Code = N'000000031' " +
            "THEN 0 " +
            "WHEN T4._Code = N'000000008' " +
            "THEN 1 " +
            "WHEN T4._Code = N'000000010' " +
            "THEN 2 " +
            "WHEN T4._Code = N'000000034' " +
            "THEN 3 " +
            "WHEN T4._Code = N'000000033' AND T4._IsMetadata = 0x00 " +
            "THEN 4 " +
            "WHEN T4._Code = N'000000027' " +
            "THEN 5 " +
            "WHEN T4._Code = N'000000005' " +
            "THEN 6 " +
            "WHEN T4._Code = N'000000033' AND T4._IsMetadata = 0x01 " +
            "THEN 7 " +
            "WHEN T4._Code = N'000000025' " +
            "THEN 8 " +
            "WHEN T4._Code = N'000000035' " +
            "THEN 9 " +
            "ELSE 10 " +
            "END [AddressInfoType] " +
            "FROM dbo._InfoRg10971 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference68 T2 WITH(NOLOCK) " +
            "ON (T1._Fld10972_TYPE = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x08 END AND T1._Fld10972_RTRef = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x00000044 END AND T1._Fld10972_RRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Enum577 T3 WITH(NOLOCK) " +
            "ON T1._Fld10973RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference24 T4 WITH(NOLOCK) " +
            "ON T1._Fld10974_TYPE = 0x08 AND CASE WHEN T1._Fld10974_TYPE = 0x08 THEN 0x00000018 ELSE 0x00000000 END = 0x00000018 AND T1._Fld10974_RRRef = T4._IDRRef " +
            "WHERE (T1._Fld10972_TYPE = 0x08 AND T1._Fld10972_RTRef = 0x00000044) " +
            "AND CAST(T1._Fld10975 AS nvarchar(max)) IS NOT NULL " +
            "AND CAST(T1._Fld10975 AS nvarchar(max)) <> N'' " +
            "GROUP BY " +
            "T2._Code, " +
            "T3._EnumOrder, " +
            "CAST(T1._Fld10975 AS nvarchar(max)), " +
            "T4._Code, " +
            "T4._IsMetadata " +
            ") AS [AddressInfo] " +
            "ON [AddressInfo].ActualClientCode = CAST(T1._Code AS bigint) " +
            "WHERE (T1._Marked = 0x00) AND (T1._Folder) = 0x01 " +
            "ORDER BY (CASE WHEN T1._Code = T9._Code THEN 0 ELSE 1 END), T1._Code ",
            (client, address) => {
                if (clients.Any(c => c.ClientCode.Equals(client.ClientCode)))
                    client = clients.First(c => c.ClientCode.Equals(client.ClientCode));
                else
                    clients.Add(client);

                if (address == null) return client;

                client.SyncClientAddresses.Add(address);

                return client;
            },
            splitOn: "ActualClientCode"
        );

        return clients;
    }

    public IEnumerable<SyncClient> GetAmgAllSyncClients() {
        List<SyncClient> clients = new();

        _amgSyncConnection.Query<SyncClient, SyncClientAddress, SyncClient>(
            "SELECT " +
            "T1._IDRRef [SourceId], " +
            "T1._Code [ClientCode], " +
            "LTRIM(RTRIM(T1._Description)) [ClientName], " +
            "T2._EnumOrder [IsIndividual], " +
            "CAST(T1._Fld1463 AS bit) [Buyer], " +
            "CAST(T1._Fld1464 AS bit) [Supplier], " +
            "CAST(T1._Fld1479 AS bit) [IsNotResident], " +
            "T1._Fld1465 [TIN], " +
            "LTRIM(RTRIM(CAST(T1._Fld1466 as nvarchar(max)))) [FullName], " +
            "T1._Fld1476 [USREOU], " +
            "T1._Fld1477 [SROI], " +
            "T3._Description [RegionName], " +
            "T1._Fld1481 [RegionCode], " +
            "T4._Code [BankAccountCode], " +
            "T4._Fld1156 [BankAccountNumber], " +
            "T6._Fld1433 [LastName], " +
            "T6._Fld1434 [FirstName], " +
            "T6._Fld1435 [MiddleName], " +
            "CAST(T7._Code AS bigint) [MainClientCode], " +
            "LTRIM(RTRIM(T7._Description)) [MainClientName], " +
            "T8._Code [MainContactPersonCode], " +
            "LTRIM(RTRIM(T8._Description)) [MainContactPersonName], " +
            "T8._Fld1452 [MainContactPersonPosition], " +
            "T9._Code [ActivityCode], " +
            "T9._Description [ActivityName], " +
            "T10._Code [MainRecipientCode], " +
            "T10._Description [MainRecipientName], " +
            "T1._Fld1468 [IdentityDocument], " +
            "T1._Fld1472 [SupplierDeadline], " +
            "T1._Fld1471 [Description], " +
            "T1._Fld1482 [QuantityDayDebt], " +
            "CAST(T1._Fld1483 AS bit) [IsControlDayDebt], " +
            "CASE WHEN T19.[_Description] IS NOT NULL THEN [T19].[_Description] ELSE [T11].[_Description] END [ClientGroupName], " +
            "T12._Description [ManagerName], " +
            "T17._Description [BankName], " +
            "T1._Code [ActualClientCode], " +
            "CASE WHEN T14._Fld13053_TYPE = 0x08 AND CASE WHEN T14._Fld13053_TYPE = 0x08 THEN 0x0000002B WHEN T14._Fld13053_TYPE <> 0x08 THEN 0x00000000 END = 0x0000002B THEN T16._EnumOrder ELSE CAST(NULL AS NUMERIC(10, 0)) END [AddressType], " +
            "T14._Fld13054 [Value], " +
            "CASE " +
            "WHEN T15._Code = N'000000031' " +
            "THEN 0 " +
            "WHEN T15._Code = N'000000008' " +
            "THEN 1 " +
            "WHEN T15._Code = N'000000010' " +
            "THEN 2 " +
            "WHEN T15._Code = N'000000037' " +
            "THEN 3 " +
            "WHEN T15._Code = N'000000033' AND T15._IsMetadata = 0x00 " +
            "THEN 4 " +
            "WHEN T15._Code = N'000000027' " +
            "THEN 5 " +
            "WHEN T15._Code = N'000000005' " +
            "THEN 6 " +
            "WHEN T15._Code = N'000000033' AND T15._IsMetadata = 0x01 " +
            "THEN 7 " +
            "WHEN T15._Code = N'000000025' " +
            "THEN 8 " +
            "WHEN T15._Code = N'000000035' " +
            "THEN 9 " +
            "ELSE 10 " +
            "END [AddressInfoType] " +
            "FROM dbo._Reference90 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Enum714 T2 WITH(NOLOCK) " +
            "ON T1._Fld1462RRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference137 T3 WITH(NOLOCK) " +
            "ON T1._Fld1480RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference33 T4 WITH(NOLOCK) " +
            "ON T1._Fld1467RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference89 T5 WITH(NOLOCK) " +
            "ON T1._Fld1474RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference88 T6 WITH(NOLOCK) " +
            "ON T5._Fld1454RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T7 WITH(NOLOCK) " +
            "ON T1._Fld1461RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference89 T8 WITH(NOLOCK) " +
            "ON T7._Fld1474RRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference39 T9 WITH(NOLOCK) " +
            "ON T1._Fld1475RRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference188 T10 WITH(NOLOCK) " +
            "ON T1._Fld1487RRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T11 WITH(NOLOCK) " +
            "ON T1._ParentIDRRef = T11._IDRRef " +
            "LEFT JOIN dbo._Reference90 T19 WITH(NOLOCK) " +
            "ON T11._ParentIDRRef = T19._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference126 T12 WITH(NOLOCK) " +
            "ON T1._Fld1469RRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._InfoRg13050 T14 WITH(NOLOCK) " +
            "ON T14._Fld13051_RRRef = T1._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference43 T15 WITH(NOLOCK) " +
            "ON T14._Fld13053_RRRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum670 T16 WITH(NOLOCK) " +
            "ON T15._Fld1203RRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference32 T17 WITH(NOLOCK) " +
            "ON T4._Fld1157RRef = T17._IDRRef " +
            "WHERE (T1._Marked = 0x00) AND (T1._Folder) = 0x01 " +
            "ORDER BY (CASE WHEN T1._Code = T9._Code THEN 0 ELSE 1 END), T1._Code ",
            (client, address) => {
                if (clients.Any(c => c.ClientCode.Equals(client.ClientCode)))
                    client = clients.First(c => c.ClientCode.Equals(client.ClientCode));
                else
                    clients.Add(client);

                if (address == null) return client;

                client.SyncClientAddresses.Add(address);

                return client;
            },
            splitOn: "ActualClientCode"
        );

        return clients;
    }

    public List<Client> GetAllClients() {
        List<Client> clients = new();

        Type[] types = {
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Pricing),
            typeof(Organization),
            typeof(ClientInRole),
            typeof(ClientType)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Currency currency = (Currency)objects[3];
            Pricing pricing = (Pricing)objects[4];
            Organization organization = (Organization)objects[5];
            ClientInRole clientInRole = (ClientInRole)objects[6];
            ClientType clientType = (ClientType)objects[7];

            if (clients.Any(c => c.Id.Equals(client.Id))) {
                client = clients.First(c => c.Id.Equals(client.Id));
            } else {
                if (clientInRole != null) clientInRole.ClientType = clientType;

                client.ClientInRole = clientInRole;

                clients.Add(client);
            }

            if (clientAgreement == null || agreement == null) return client;

            agreement.Currency = currency;
            agreement.Pricing = pricing;
            agreement.Organization = organization;

            clientAgreement.Agreement = agreement;

            client.ClientAgreements.Add(clientAgreement);

            return client;
        };

        _remoteSyncConnection.Query(
            "SELECT * " +
            "FROM [Client] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].[ID] = [ClientInRole].[ClientTypeID] ", types, mapper);

        return clients;
    }

    public IEnumerable<Client> GetAllClientsWithRegionCodes() {
        return _remoteSyncConnection.Query<Client, RegionCode, Region, Client>(
            "SELECT * " +
            "FROM [Client] " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [Region] " +
            "ON [Region].ID = [Client].RegionID " +
            "WHERE [Client].Deleted = 0 " +
            "AND [Region].ID IS NOT NULL " +
            "AND [RegionCode].ID IS NOT NULL " +
            "AND [RegionCode].[Value] <> N'' " +
            "AND [RegionCode].[Value] IS NOT NULL " +
            "AND PATINDEX(N'B%', [RegionCode].[Value]) <> 0",
            (client, regionCode, region) => {
                client.RegionCode = regionCode;
                client.Region = region;

                return client;
            }
        );
    }

    public IEnumerable<Client> GetAllBySearchPatterns(string pattern, string additionalPattern, long exceptClientId) {
        return _remoteSyncConnection.Query<Client, RegionCode, Client>(
            "SELECT * " +
            "FROM [Client] " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [Client].Deleted = 0 " +
            "AND [Client].ID <> @ExceptClientId " +
            "AND (" +
            "PATINDEX(@Pattern, [RegionCode].[Value]) <> 0 " +
            "OR " +
            "PATINDEX(@AdditionalPattern, [RegionCode].[Value]) <> 0" +
            ") " +
            "ORDER BY CASE WHEN [RegionCode].[Value] LIKE N'B%' OR [RegionCode].[Value] LIKE N'В%' THEN 0 ELSE 1 END",
            (client, regionCode) => {
                client.RegionCode = regionCode;

                return client;
            },
            new { Pattern = pattern, AdditionalPattern = additionalPattern, ExceptClientId = exceptClientId }
        );
    }

    public IEnumerable<Region> GetAllRegions() {
        return _remoteSyncConnection.Query<Region>(
            "SELECT * " +
            "FROM [Region] " +
            "WHERE Deleted = 0"
        ).ToList();
    }

    public long Add(RegionCode regionCode) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [RegionCode] " +
            "([RegionID], [Value], [City], [District], [Updated]) " +
            "VALUES " +
            "(@RegionId, @Value, @City, @District, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            regionCode
        ).Single();
    }

    public void Update(RegionCode regionCode) {
        _remoteSyncConnection.Execute(
            "UPDATE [RegionCode] " +
            "SET [RegionID] = @RegionId, [Value] = @Value, [City] = @City, [District] = @District, [Updated] = GETUTCDATE() " +
            "WHERE ID = @Id",
            regionCode
        );
    }

    public long Add(Client client) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Client] " +
            "(" +
            "[Comment] " +
            ",[TIN] " +
            ",[USREOU] " +
            ",[Updated] " +
            ",[AccountantNumber] " +
            ",[ActualAddress] " +
            ",[DeliveryAddress] " +
            ",[DirectorNumber] " +
            ",[EmailAddress] " +
            ",[FaxNumber] " +
            ",[ICQ] " +
            ",[LegalAddress] " +
            ",[MobileNumber] " +
            ",[RegionID] " +
            ",[SMSNumber] " +
            ",[ClientNumber] " +
            ",[SROI] " +
            ",[Name] " +
            ",[FullName] " +
            ",[IsIndividual] " +
            ",[RegionCodeID] " +
            ",[IsActive] " +
            ",[IsSubClient] " +
            ",[Abbreviation] " +
            ",[IsBlocked] " +
            ",[IsTradePoint] " +
            ",[Brand] " +
            ",[ClientBankDetailsID] " +
            ",[CountryID] " +
            ",[Manufacturer] " +
            ",[TermsOfDeliveryID] " +
            ",[SupplierContactName] " +
            ",[SupplierName] " +
            ",[PackingMarkingID] " +
            ",[PackingMarkingPaymentID] " +
            ",[IncotermsElse] " +
            ",[IsPayForDelivery] " +
            ",[IsIncotermsElse] " +
            ",[SupplierCode] " +
            ",[IsTemporaryClient] " +
            ",[FirstName] " +
            ",[LastName] " +
            ",[MiddleName] " +
            ",[SourceAmgCode] " +
            ",[SourceFenixCode] " +
            ",[SourceAmgId] " +
            ",[SourceFenixId] " +
            ",[HouseNumber] " +
            ",[Street] " +
            ",[ZipCode] " +
            ",[ClearCartAfterDays] " +
            ",[IsFromECommerce] " +
            ",[Manager] " +
            ",[OriginalRegionCode] " +
            ",[IsNotResident] " +
            ",[MainManagerID] " +
            ",[OrderExpireDays] " +
            ") " +
            "VALUES " +
            "(" +
            "@Comment " +
            ",@TIN " +
            ",@USREOU " +
            ",GETUTCDATE() " +
            ",@AccountantNumber " +
            ",@ActualAddress " +
            ",@DeliveryAddress " +
            ",@DirectorNumber " +
            ",@EmailAddress " +
            ",@FaxNumber " +
            ",@ICQ " +
            ",@LegalAddress " +
            ",@MobileNumber " +
            ",@RegionID " +
            ",@SMSNumber " +
            ",@ClientNumber " +
            ",@SROI " +
            ",@Name " +
            ",@FullName " +
            ",@IsIndividual " +
            ",@RegionCodeID " +
            ",@IsActive " +
            ",@IsSubClient " +
            ",@Abbreviation " +
            ",@IsBlocked " +
            ",@IsTradePoint " +
            ",@Brand " +
            ",@ClientBankDetailsID " +
            ",@CountryID " +
            ",@Manufacturer " +
            ",@TermsOfDeliveryID " +
            ",@SupplierContactName " +
            ",@SupplierName " +
            ",@PackingMarkingID " +
            ",@PackingMarkingPaymentID " +
            ",@IncotermsElse " +
            ",@IsPayForDelivery " +
            ",@IsIncotermsElse " +
            ",@SupplierCode " +
            ",@IsTemporaryClient " +
            ",@FirstName " +
            ",@LastName " +
            ",@MiddleName " +
            ",@SourceAmgCode " +
            ",@SourceFenixCode " +
            ",@SourceAmgId " +
            ",@SourceFenixId " +
            ",@HouseNumber " +
            ",@Street " +
            ",@ZipCode " +
            ",@ClearCartAfterDays " +
            ",@IsFromECommerce " +
            ",@Manager " +
            ",@OriginalRegionCode " +
            ",@IsNotResident " +
            ",@MainManagerId " +
            ",@OrderExpireDays " +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            client
        ).Single();
    }

    public void Update(Client client) {
        _remoteSyncConnection.Execute(
            "UPDATE [Client] " +
            "SET " +
            "[Comment] = @Comment " +
            ",[TIN] = @TIN " +
            ",[USREOU] = @USREOU " +
            ",[Updated] = GETUTCDATE() " +
            ",[AccountantNumber] = @AccountantNumber " +
            ",[ActualAddress] = @ActualAddress " +
            ",[DeliveryAddress] = @DeliveryAddress " +
            ",[DirectorNumber] = @DirectorNumber " +
            ",[EmailAddress] = @EmailAddress " +
            ",[FaxNumber] = @FaxNumber " +
            ",[ICQ] = @ICQ " +
            ",[LegalAddress] = @LegalAddress " +
            ",[MobileNumber] = @MobileNumber " +
            ",[RegionID] = @RegionId " +
            ",[SMSNumber] = @SMSNumber " +
            ",[ClientNumber] = @ClientNumber " +
            ",[SROI] = @SROI " +
            ",[Name] = @Name " +
            ",[FullName] = @FullName " +
            ",[IsIndividual] = @IsIndividual " +
            ",[RegionCodeID] = @RegionCodeId " +
            ",[IsActive] = @IsActive " +
            ",[IsSubClient] = @IsSubClient " +
            ",[Abbreviation] = @Abbreviation " +
            ",[IsBlocked] = @IsBlocked " +
            ",[IsTradePoint] = @IsTradePoint " +
            ",[Brand] = @Brand " +
            ",[ClientBankDetailsID] = @ClientBankDetailsId " +
            ",[CountryID] = @CountryID " +
            ",[Manufacturer] = @Manufacturer " +
            ",[TermsOfDeliveryID] = @TermsOfDeliveryId " +
            ",[SupplierContactName] = @SupplierContactName " +
            ",[SupplierName] = @SupplierName " +
            ",[PackingMarkingID] = @PackingMarkingId " +
            ",[PackingMarkingPaymentID] = @PackingMarkingPaymentId " +
            ",[IncotermsElse] = @IncotermsElse " +
            ",[IsPayForDelivery] = @IsPayForDelivery " +
            ",[IsIncotermsElse] = @IsIncotermsElse " +
            ",[SupplierCode] = @SupplierCode " +
            ",[IsTemporaryClient] = @IsTemporaryClient " +
            ",[FirstName] = @FirstName " +
            ",[LastName] = @LastName " +
            ",[MiddleName] = @MiddleName " +
            ",[SourceAmgCode] = @SourceAmgCode " +
            ",[SourceFenixCode] = @SourceFenixCode " +
            ",[SourceAmgID] = @SourceAmgId " +
            ",[SourceFenixID] = @SourceFenixId " +
            ",[HouseNumber] = @HouseNumber " +
            ",[Street] = @Street " +
            ",[ZipCode] = @ZipCode " +
            ",[ClearCartAfterDays] = @ClearCartAfterDays " +
            ",[IsFromECommerce] = @IsFromECommerce " +
            ",[Manager] = @Manager " +
            ",[Deleted] = @Deleted " +
            ",[IsNotResident] = @IsNotResident " +
            ",[MainManagerID] = @MainManagerId " +
            ",[OrderExpireDays] = @OrderExpireDays " +
            "WHERE ID = @Id",
            client
        );
    }

    public void Add(ClientInRole clientInRole) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ClientInRole] " +
            "([ClientID], [ClientTypeID], [ClientTypeRoleID], [Updated]) " +
            "VALUES " +
            "(@ClientId, @ClientTypeId, @ClientTypeRoleId, GETUTCDATE())",
            clientInRole
        );
    }

    public void Update(ClientInRole clientInRole) {
        _remoteSyncConnection.Execute(
            "UPDATE [ClientInRole] " +
            "SET [ClientID] = @ClientId, [ClientTypeID] = @ClientTypeId, [ClientTypeRoleID] = @ClientTypeRoleId, [Updated] = GETUTCDATE() " +
            "WHERE ID = @Id",
            clientInRole
        );
    }

    public void Add(ClientSubClient clientSubClient) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ClientSubClient] " +
            "([RootClientID], [SubClientID], [Updated]) " +
            "VALUES " +
            "(@RootClientId, @SubClientId, GETUTCDATE())",
            clientSubClient
        );
    }

    public long Add(ClientBankDetailAccountNumber accountNumber) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ClientBankDetailAccountNumber] " +
            "([AccountNumber], [CurrencyID], Updated) " +
            "VALUES " +
            "(@AccountNumber, @CurrencyId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            accountNumber
        ).Single();
    }

    public long Add(ClientBankDetailIbanNo ibanNo) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ClientBankDetailIbanNo] " +
            "([IBANNO], [CurrencyID], Updated) " +
            "VALUES " +
            "(@IBANNO, @CurrencyId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            ibanNo
        ).Single();
    }

    public long Add(ClientBankDetails bankDetails) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ClientBankDetails] " +
            "([BankAndBranch], [BankAddress], [Swift], [BranchCode], [AccountNumberID], [ClientBankDetailIbanNoID], Updated) " +
            "VALUES " +
            "(@BankAndBranch, @BankAddress, @Swift, @BranchCode, @AccountNumberId, @ClientBankDetailIbanNoId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            bankDetails
        ).Single();
    }

    public void RemoveSubClientsByRootClientId(long rootClientId) {
        _remoteSyncConnection.Execute(
            "DELETE FROM [ClientSubClient] WHERE RootClientID = @rootClientId",
            new { RootClientId = rootClientId }
        );
    }

    public IEnumerable<SyncOrganization> GetAllSyncOrganizations(
        string[] organizations) {
        return _oneCConnection.Query<SyncOrganization>(
            "SELECT " +
            "T5._Code [OrganizationCode], " +
            "T5._Description [OrganizationName], " +
            "( " +
            "SELECT TOP 1 T14._Description FROM dbo._InfoRg11249 T13 " +
            "LEFT OUTER JOIN dbo._Reference151 T14 WITH(NOLOCK) " +
            "ON T14._IDRRef = T13._Fld11252RRef " +
            "LEFT JOIN dbo._Reference50 T15 WITH(NOLOCK) " +
            "ON T15._IDRRef = T13._Fld11253RRef " +
            "WHERE T13._Fld11250_RRRef = T5._IDRRef " +
            "AND T15._Code = 000000006 " +
            "ORDER BY T13.[_Period] DESC " +
            ") [Manager], " +
            "T5._Fld1384 [OrganizationPrefix], " +
            "T5._Fld1385 [OrganizationFullName], " +
            "CAST(T6._EnumOrder AS bit) [IsIndividual], " +
            "T7._Description [MainBankAccountName], " +
            "T10._Code [MainCurrencyCode], " +
            "T1.Fld10936_ [EDRPOU], " +
            "T1.Fld10937_ [IPN], " +
            "(CASE WHEN T1.Fld10930_ = N'2001-01-01 00:00:00.000' THEN NULL ELSE DATEADD(YEAR, -2000, T1.Fld10930_) END) [DateRegistration], " +
            "T1.Fld10931_ [NumberRegistration], " +
            "T11._Description [TaxInspectionName], " +
            "T1.Fld10938_ [NumberCertification], " +
            "T1.Fld10941_ [CodeKVED], " +
            "T12._Description [StorageName] " +
            "FROM (SELECT " +
            "T4._Fld10931 AS Fld10931_, " +
            "T4._Fld10935RRef AS Fld10935RRef, " +
            "T4._Fld10930 AS Fld10930_, " +
            "T4._Fld10937  AS Fld10937_, " +
            "T4._Fld10938  AS Fld10938_, " +
            "T4._Fld10941  AS Fld10941_, " +
            "T4._Fld10929RRef AS Fld10929RRef, " +
            "T4._Fld10936  AS Fld10936_ " +
            "FROM ( " +
            "SELECT " +
            "T3._Fld10929RRef AS Fld10929RRef, " +
            "MAX(T3._Period) AS MAXPERIOD_ " +
            "FROM dbo._InfoRg10928 T3 WITH(NOLOCK) " +
            "GROUP BY T3._Fld10929RRef) T2 " +
            "INNER JOIN dbo._InfoRg10928 T4 WITH(NOLOCK) " +
            "ON T2.Fld10929RRef = T4._Fld10929RRef AND T2.MAXPERIOD_ = T4._Period) T1 " +
            "LEFT OUTER JOIN dbo._Reference90 T5 WITH(NOLOCK) " +
            "ON (T1.Fld10929RRef = T5._IDRRef) " +
            "LEFT OUTER JOIN dbo._Enum602 T6 WITH(NOLOCK) " +
            "ON T5._Fld1386RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 T7 WITH(NOLOCK) " +
            "ON T5._Fld1387RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T8 WITH(NOLOCK) " +
            "ON T5._Fld1388RRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference56 T9 WITH(NOLOCK) " +
            "ON T5._Fld1390RRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T10 WITH(NOLOCK) " +
            "ON T5._Fld15587RRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference78 T11 WITH(NOLOCK) " +
            "ON T1.Fld10935RRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T12 WITH(NOLOCK) " +
            "ON T5._Fld16181RRef = T12._IDRRef " +
            "WHERE (T5._Marked = 0x00) " +
            "AND T5._Description IN @Organizations ",
            new { Organizations = organizations }
        );
    }

    public IEnumerable<SyncOrganization> GetAmgAllSyncOrganizations() {
        return _amgSyncConnection.Query<SyncOrganization>(
            "SELECT " +
            "T5._Code [OrganizationCode], " +
            "T5._Description [OrganizationName], " +
            "( " +
            "SELECT TOP 1 T12._Description FROM dbo._InfoRg13397 T11 " +
            "LEFT OUTER JOIN dbo._Reference179 T12 WITH(NOLOCK) " +
            "ON T11._Fld13400RRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference69 T13 WITH(NOLOCK) " +
            "ON T11._Fld13401RRef = T13._IDRRef " +
            "WHERE T11._Fld13398_RRRef = T5._IDRRef " +
            "AND T11._Fld13398_TYPE = 0x08 AND T11._Fld13398_RTRef = 0x00000074 " +
            "AND T13._Code = 000000001 " +
            "ORDER BY T11.[_Period] DESC " +
            ") [Manager], " +
            "T5._Fld1886 [OrganizationPrefix], " +
            "T5._Fld1887 [OrganizationFullName], " +
            "T10._EnumOrder [IsIndividual], " +
            "T6._Description [MainBankAccountName], " +
            "T7._Code [MainCurrencyCode], " +
            "T1.Fld13004_ [EDRPOU], " +
            "T1.Fld13005_ [IPN], " +
            "(CASE WHEN T1.Fld12998_ = N'2001-01-01 00:00:00.000' THEN NULL ELSE DATEADD(YEAR, -2000, T1.Fld12998_) END)  [DateRegistration], " +
            "T1.Fld12999_ [NumberRegistration], " +
            "T8._Description [TaxInspectionName], " +
            "T1.Fld13006_ [NumberCertification], " +
            "T1.Fld13009_ [CodeKVED], " +
            "T9._Description [StorageName] " +
            "FROM (SELECT " +
            "T4._Fld12999  AS Fld12999_, " +
            "T4._Fld13006  AS Fld13006_, " +
            "T4._Fld13009  AS Fld13009_, " +
            "T4._Fld12997RRef AS Fld12997RRef, " +
            "T4._Fld13003RRef AS Fld13003RRef, " +
            "T4._Fld13004  AS Fld13004_, " +
            "T4._Fld12998 AS Fld12998_, " +
            "T4._Fld13005  AS Fld13005_ " +
            "FROM (SELECT " +
            "T3._Fld12997RRef AS Fld12997RRef, " +
            "MAX(T3._Period) AS MAXPERIOD_ " +
            "FROM dbo._InfoRg12996 T3 WITH(NOLOCK) " +
            "GROUP BY T3._Fld12997RRef) T2 " +
            "INNER JOIN dbo._InfoRg12996 T4 WITH(NOLOCK) " +
            "ON T2.Fld12997RRef = T4._Fld12997RRef AND T2.MAXPERIOD_ = T4._Period) T1 " +
            "LEFT OUTER JOIN dbo._Reference116 T5 WITH(NOLOCK) " +
            "ON (T1.Fld12997RRef = T5._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference76 T6 WITH(NOLOCK) " +
            "ON T5._Fld1892RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 T7 WITH(NOLOCK) " +
            "ON T6._Fld1400RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 T8 WITH(NOLOCK) " +
            "ON T1.Fld13003RRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference144 T9 WITH(NOLOCK) " +
            "ON T5._Fld15976RRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum714 T10 WITH(NOLOCK) " +
            "ON T5._Fld1888RRef = T10._IDRRef "
        );
    }

    public IEnumerable<SyncOrganizationAddress> GetOrganizationAddresses(string organizationCode) {
        return _oneCConnection.Query<SyncOrganizationAddress>(
            "SELECT " +
            "T14._EnumOrder [AddressType], " +
            "REPLACE(REPLACE(CAST(T6._Fld10975 AS nvarchar(max)), N'(', N''), N')', N'') [Value] " +
            "FROM (SELECT " +
            "T4._Fld10938  AS Fld10938_, " +
            "T4._Fld10936  AS Fld10936_, " +
            "T4._Fld10930 AS Fld10930_, " +
            "T4._Fld10935RRef AS Fld10935RRef, " +
            "T4._Fld10941  AS Fld10941_, " +
            "T4._Fld10937  AS Fld10937_, " +
            "T4._Fld10929RRef AS Fld10929RRef, " +
            "T4._Fld10931  AS Fld10931_ " +
            "FROM (SELECT " +
            "T3._Fld10929RRef AS Fld10929RRef, " +
            "MAX(T3._Period) AS MAXPERIOD_ " +
            "FROM dbo._InfoRg10928 T3 WITH(NOLOCK) " +
            "GROUP BY T3._Fld10929RRef) T2 " +
            "INNER JOIN dbo._InfoRg10928 T4 WITH(NOLOCK) " +
            "ON T2.Fld10929RRef = T4._Fld10929RRef AND T2.MAXPERIOD_ = T4._Period) T1 " +
            "LEFT OUTER JOIN dbo._Reference90 T5 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._InfoRg10971 T6 WITH(NOLOCK) " +
            "ON (CASE WHEN T5._IDRRef IS NOT NULL THEN 0x08 END = T6._Fld10972_TYPE AND CASE WHEN T5._IDRRef IS NOT NULL THEN 0x0000005A END = T6._Fld10972_RTRef AND T5._IDRRef = T6._Fld10972_RRRef) " +
            "ON (T1.Fld10929RRef = T5._IDRRef) " +
            "LEFT OUTER JOIN dbo._Enum602 T7 WITH(NOLOCK) " +
            "ON T5._Fld1386RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 T8 WITH(NOLOCK) " +
            "ON T5._Fld1387RRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T9 WITH(NOLOCK) " +
            "ON T5._Fld1388RRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference56 T10 WITH(NOLOCK) " +
            "ON T5._Fld1390RRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T11 WITH(NOLOCK) " +
            "ON T5._Fld15587RRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference78 T12 WITH(NOLOCK) " +
            "ON T1.Fld10935RRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T13 WITH(NOLOCK) " +
            "ON T5._Fld16181RRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum577 T14 WITH(NOLOCK) " +
            "ON T6._Fld10973RRef = T14._IDRRef " +
            "WHERE (T5._Marked = 0x00) " +
            "AND T14._EnumOrder IS NOT NULL " +
            "AND T5._Code = @OrganizationCode " +
            "GROUP BY T5._Code, " +
            "T14._EnumOrder, " +
            "REPLACE(REPLACE(CAST(T6._Fld10975 AS nvarchar(max)), N'(', N''), N')', N'')",
            new { OrganizationCode = organizationCode }
        );
    }

    public IEnumerable<SyncOrganizationAddress> GetAmgOrganizationAddresses(string organizationCode) {
        return _amgSyncConnection.Query<SyncOrganizationAddress>(
            "SELECT " +
            "T4._EnumOrder [AddressType], " +
            "T1._Fld13054 [Value] " +
            "FROM dbo._InfoRg13050 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference43 T2 WITH(NOLOCK) " +
            "ON (T1._Fld13053_TYPE = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x08 END AND T1._Fld13053_S = CASE WHEN T2._IDRRef IS NOT NULL THEN N'' END AND T1._Fld13053_RRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference116 T3 WITH(NOLOCK) " +
            "ON (T1._Fld13051_TYPE = CASE WHEN T3._IDRRef IS NOT NULL THEN 0x08 END AND T1._Fld13051_RTRef = CASE WHEN T3._IDRRef IS NOT NULL THEN 0x00000074 END AND T1._Fld13051_RRRef = T3._IDRRef) " +
            "LEFT OUTER JOIN dbo._Enum670 T4 WITH(NOLOCK) " +
            "ON T2._Fld1203RRef = T4._IDRRef " +
            "WHERE T3._Code = @OrganizationCode "
            ,
            new { OrganizationCode = organizationCode }
        );
    }

    public List<Organization> GetAllOrganizations() {
        List<Organization> organizations = new();

        _remoteSyncConnection.Query<Organization, OrganizationTranslation, Organization>(
            "SELECT [Organization].* " +
            ", ( " +
            "SELECT TOP(1) [OrganizationTranslation].[Name] " +
            "FROM [OrganizationTranslation] " +
            "WHERE [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = N'uk' " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            ") [NameUk] " +
            ", ( " +
            "SELECT TOP(1) [OrganizationTranslation].[Name] " +
            "FROM [OrganizationTranslation] " +
            "WHERE [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = N'pl' " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            ") [NamePl] " +
            ", [Translation].* " +
            "FROM [Organization] " +
            "LEFT JOIN [OrganizationTranslation] AS [Translation] " +
            "ON [Translation].OrganizationID = [Organization].ID " +
            "AND [Translation].Deleted = 0 " +
            "WHERE [Organization].[Deleted] = 0 ",
            (organization, translation) => {
                if (organizations.Any(o => o.Id.Equals(organization.Id)))
                    organization = organizations.First(o => o.Id.Equals(organization.Id));
                else
                    organizations.Add(organization);

                if (translation == null) return organization;

                organization.OrganizationTranslations.Add(translation);

                return organization;
            }
        );

        return organizations;
    }

    public long Add(Organization organization) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Organization] " +
            "(" +
            "[Name] " +
            ",[Updated] " +
            ",[Code] " +
            ",[Culture] " +
            ",[Address] " +
            ",[FullName] " +
            ",[IsIndividual] " +
            ",[PFURegistrationDate] " +
            ",[PFURegistrationNumber] " +
            ",[PhoneNumber] " +
            ",[RegistrationDate] " +
            ",[RegistrationNumber] " +
            ",[SROI] " +
            ",[TIN] " +
            ",[USREOU] " +
            ",[CurrencyID] " +
            ",[StorageID] " +
            ",[TaxInspectionID] " +
            ",[IsVatAgreements] " +
            ",[Manager] " +
            ",[VatRateID] " +
            ") " +
            "VALUES " +
            "(" +
            "@Name " +
            ",GETUTCDATE() " +
            ",@Code " +
            ",@Culture " +
            ",@Address " +
            ",@FullName " +
            ",@IsIndividual " +
            ",@PFURegistrationDate " +
            ",@PFURegistrationNumber " +
            ",@PhoneNumber " +
            ",@RegistrationDate " +
            ",@RegistrationNumber " +
            ",@SROI " +
            ",@TIN " +
            ",@USREOU " +
            ",@CurrencyID " +
            ",@StorageID " +
            ",@TaxInspectionID " +
            ",@IsVatAgreements " +
            ",@Manager " +
            ",@VatRateId " +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            organization
        ).Single();
    }

    public void Update(Organization organization) {
        _remoteSyncConnection.Execute(
            "UPDATE [Organization] " +
            "SET " +
            "[Name] = @Name " +
            ",[Updated] = GETUTCDATE() " +
            ",[Code] = @Code " +
            ",[Culture] = @Culture " +
            ",[Address] = @Address " +
            ",[FullName] = @FullName " +
            ",[IsIndividual] = @IsIndividual " +
            ",[PFURegistrationDate] = @PFURegistrationDate " +
            ",[PFURegistrationNumber] = @PFURegistrationNumber " +
            ",[PhoneNumber] = @PhoneNumber " +
            ",[RegistrationDate] = @RegistrationDate " +
            ",[RegistrationNumber] = @RegistrationNumber " +
            ",[SROI] = @SROI " +
            ",[TIN] = @TIN " +
            ",[USREOU] = @USREOU " +
            ",[CurrencyID] = @CurrencyId " +
            ",[StorageID] = @StorageId " +
            ",[TaxInspectionID] = @TaxInspectionId " +
            ",[Deleted] = @Deleted " +
            ",[IsVatAgreements] = @IsVatAgreements " +
            ",[Manager] = @Manager " +
            ",[VatRateID] = @VatRateId " +
            "WHERE ID = @Id ",
            organization
        );
    }

    public void Add(OrganizationTranslation translation) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [OrganizationTranslation] " +
            "(" +
            "[CultureCode] " +
            ",[Name] " +
            ",[OrganizationID] " +
            ",[Updated]" +
            ") " +
            "VALUES " +
            "(" +
            "@CultureCode, " +
            "@Name, " +
            "@OrganizationId, " +
            "GETUTCDATE()" +
            ")",
            translation
        );
    }

    public void Update(OrganizationTranslation translation) {
        _remoteSyncConnection.Execute(
            "UPDATE [OrganizationTranslation] " +
            "SET " +
            "[Name] = @Name " +
            ",[Updated] = GETUTCDATE() " +
            "WHERE ID = @Id",
            translation
        );
    }

    public IEnumerable<SyncTaxInspection> GetAllSyncTaxInspections() {
        return _oneCConnection.Query<SyncTaxInspection>(
            "SELECT " +
            "T1._Code [TaxInspectionCode], " +
            "T1._Fld1183 [TypeDPI], " +
            "T1._Description [TaxInspectionName], " +
            "T1._Fld1185 [NameAdminDistrict], " +
            "T1._Fld1184 [CodeAdminDistrict], " +
            "T1._Fld1186 [Address], " +
            "T1._Fld1187 [EDRPOU] " +
            "FROM dbo._Reference78 T1 WITH(NOLOCK) " +
            "WHERE (T1._Folder) = 0x01 AND (T1._Marked = 0x00) "
        );
    }

    public IEnumerable<SyncTaxInspection> GetAmgAllSyncTaxInspections() {
        return _amgSyncConnection.Query<SyncTaxInspection>(
            "SELECT " +
            "T1._Code [TaxInspectionCode], " +
            "T1._Fld1546 [TypeDPI], " +
            "T1._Description [TaxInspectionName], " +
            "T1._Fld1548 [NameAdminDistrict], " +
            "T1._Fld1547 [CodeAdminDistrict], " +
            "T1._Fld1549 [Address], " +
            "T1._Fld1550 [EDRPOU] " +
            "FROM dbo._Reference100 T1 WITH(NOLOCK) " +
            "WHERE (T1._Marked = 0x00) AND (T1._Folder) = 0x01 "
        );
    }

    public List<TaxInspection> GetAllTaxInspections() {
        return _remoteSyncConnection.Query<TaxInspection>(
            "SELECT * " +
            "FROM [TaxInspection] "
        ).ToList();
    }

    public long Add(TaxInspection taxInspection) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [TaxInspection] " +
            "(" +
            "[InspectionNumber], " +
            "[InspectionType], " +
            "[InspectionName], " +
            "[InspectionRegionName], " +
            "[InspectionRegionCode], " +
            "[InspectionAddress], " +
            "[InspectionUSREOU]," +
            "[Updated] " +
            ") " +
            "VALUES " +
            "(" +
            "@InspectionNumber, " +
            "@InspectionType, " +
            "@InspectionName, " +
            "@InspectionRegionName, " +
            "@InspectionRegionCode, " +
            "@InspectionAddress, " +
            "@InspectionUSREOU, " +
            "GETUTCDATE()" +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            taxInspection
        ).Single();
    }

    public void Update(TaxInspection taxInspection) {
        _remoteSyncConnection.Execute(
            "UPDATE [TaxInspection] " +
            "SET " +
            "[InspectionNumber] = @InspectionNumber, " +
            "[InspectionType] = @InspectionType, " +
            "[InspectionName] = @InspectionName, " +
            "[InspectionRegionName] = @InspectionRegionName, " +
            "[InspectionRegionCode] = @InspectionRegionCode, " +
            "[InspectionAddress] = @InspectionAddress, " +
            "[InspectionUSREOU] = @InspectionUSREOU, " +
            "[Updated] = GETUTCDATE(), " +
            "[Deleted] = @Deleted " +
            "WHERE ID = @Id",
            taxInspection
        );
    }

    public IEnumerable<Currency> GetAllCurrencies() {
        return _remoteSyncConnection.Query<Currency>(
            "SELECT * " +
            "FROM [Currency] " +
            "WHERE [Currency].[Deleted] = 0 "
        );
    }

    public IEnumerable<SyncCurrency> GetAmgAllSyncCurrencies() {
        return _amgSyncConnection.Query<SyncCurrency>(
            "SELECT " +
            "T1._Code [Code], " +
            "T1._Description [Name] " +
            "FROM dbo._Reference35 T1 WITH(NOLOCK) " +
            "WHERE (T1._Marked = 0x00) "
        );
    }

    public IEnumerable<SyncCurrency> GetAllSyncCurrencies() {
        return _oneCConnection.Query<SyncCurrency>(
            "SELECT " +
            "T1._Code [Code], " +
            "T1._Description [Name] " +
            "FROM dbo._Reference17 T1 WITH(NOLOCK) " +
            "WHERE (T1._Marked = 0x00) "
        );
    }

    public void Update(Currency currency) {
        _remoteSyncConnection.Execute(
            "UPDATE [Currency] " +
            "SET CodeOneC = @CodeOneC, Updated = GETUTCDATE(), [Deleted] = @Deleted " +
            "WHERE ID = @Id",
            currency
        );
    }

    public IEnumerable<SyncPricing> GetAllSyncPricings() {
        return _oneCConnection.Query<SyncPricing>(
            "SELECT " +
            "T1._Description [Name], " +
            "T3._Description [BaseName], " +
            "T1._Fld1752 [Discount], " +
            "(CASE WHEN T1._Description LIKE N'%(НДС)' THEN 1 ELSE 0 END) [ForVat] " +
            "FROM dbo._Reference143 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference143 T3 WITH(NOLOCK) " +
            "ON T1._Fld1750RRef = T3._IDRRef " +
            "WHERE (T1._Marked = 0x00) " +
            "AND T1._Description IN ( " +
            "SELECT " +
            "T7._Description " +
            "FROM dbo._Reference47 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference68 T2 WITH(NOLOCK) " +
            "ON (T1._OwnerIDRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Enum468 T3 WITH(NOLOCK) " +
            "ON (T1._Fld991RRef = T3._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference17 T4 WITH(NOLOCK) " +
            "ON T1._Fld988RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T5 WITH(NOLOCK) " +
            "ON T1._Fld999RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum468 T6 WITH(NOLOCK) " +
            "ON T1._Fld991RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference143 T7 WITH(NOLOCK) " +
            "ON T2._Fld1133RRef = T7._IDRRef " +
            "WHERE (T1._Marked = 0x00) AND (T1._Folder) = 0x01 " +
            "AND T7._Description NOT LIKE N'(до%' " +
            "GROUP BY T7._Description " +
            ") " +
            "ORDER BY CASE WHEN T3._Description IS NULL THEN 0 ELSE 1 END "
        );
    }

    public IEnumerable<SyncPricing> GetAmgAllSyncPricings() {
        return _amgSyncConnection.Query<SyncPricing>(
            "SELECT " +
            "T1._Description [Name], " +
            "T2._Description [BaseName], " +
            "T1._Fld2329 [Discount], " +
            "CAST(T1._Fld2330 AS bit) [ForVat] " +
            "FROM dbo._Reference171 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference171 T2 WITH(NOLOCK)  " +
            "ON T1._Fld2327RRef = T2._IDRRef " +
            "WHERE(T1._Marked = 0x00) "
        );
    }

    public List<Pricing> GetAllPricings() {
        return _remoteSyncConnection.Query<Pricing>(
            "SELECT * " +
            "FROM [Pricing] " +
            "WHERE [Pricing].Deleted = 0"
        ).ToList();
    }

    public long Add(Pricing pricing) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Pricing] " +
            "([Name], [ExtraCharge], [CalculatedExtraCharge], [BasePricingID], [CurrencyID], [PriceTypeID], [Culture], [ForShares], [ForVat], [Updated]) " +
            "VALUES " +
            "(@Name, @ExtraCharge, @CalculatedExtraCharge, @BasePricingId, @CurrencyId, @PriceTypeId, @Culture, @ForShares, @ForVat, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            pricing
        ).Single();
    }

    public void Update(Pricing pricing) {
        _remoteSyncConnection.Execute(
            "UPDATE [Pricing] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [BasePricingID] = @BasePricingId " +
            ", [Comment] = @Comment " +
            ", [CurrencyID] = @CurrencyId " +
            ", [Deleted] = @Deleted " +
            ", [ExtraCharge] = @ExtraCharge " +
            ", [PriceTypeID] = @PriceTypeId " +
            ", [Culture] = @Culture " +
            ", [CalculatedExtraCharge] = @CalculatedExtraCharge " +
            ", [ForShares] = @ForShares " +
            ", [ForVat] = @ForVat " +
            "WHERE [Pricing].[ID] = @ID ", pricing);
    }

    public void Add(PricingTranslation translation) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [PricingTranslation] " +
            "(" +
            "[CultureCode] " +
            ",[Name] " +
            ",[PricingID] " +
            ",[Updated]" +
            ") " +
            "VALUES " +
            "(" +
            "@CultureCode, " +
            "@Name, " +
            "@PricingId, " +
            "GETUTCDATE()" +
            ")",
            translation
        );
    }

    public void SetSharesPricings() {
        _remoteSyncConnection.Execute(
            "UPDATE [Pricing] SET ForShares = 0; " +
            "UPDATE [Pricing] SET ForShares = 1 WHERE ID IN ( " +
            "SELECT TOP(1) ID " +
            "FROM [Pricing] " +
            "WHERE [Pricing].Culture = N'uk' " +
            "AND [Pricing].ForVat = 0 " +
            "ORDER BY [Pricing].CalculatedExtraCharge " +
            "UNION ALL " +
            "SELECT TOP(1) ID " +
            "FROM [Pricing] " +
            "WHERE [Pricing].Culture = N'uk' " +
            "AND [Pricing].ForVat = 1 " +
            "ORDER BY [Pricing].CalculatedExtraCharge " +
            "UNION ALL " +
            "SELECT TOP(1) ID " +
            "FROM [Pricing] " +
            "WHERE [Pricing].Culture = N'pl' " +
            "AND [Pricing].ForVat = 0 " +
            "ORDER BY [Pricing].CalculatedExtraCharge " +
            "UNION ALL " +
            "SELECT TOP(1) ID " +
            "FROM [Pricing] " +
            "WHERE [Pricing].Culture = N'pl' " +
            "AND [Pricing].ForVat = 1 " +
            "ORDER BY [Pricing].CalculatedExtraCharge " +
            ") "
        );
    }

    public IEnumerable<SyncAgreement> GetAllSyncAgreementsByCode(
        long clientCode,
        bool? isProvider = null) {
        return _oneCConnection.Query<SyncAgreement>(
            "SELECT " +
            "T1._IDRRef [SourceId], " +
            "T1._Code [SourceCode], " +
            "T1._Description [Name], " +
            "T1._Fld987 [Number], " +
            "T4._Code [CurrencyCode], " +
            "T1._Fld993 [PermissibleDebtAmount], " +
            "T1._Fld994 [DebtDaysAllowedNumber], " +
            "T5._Description [OrganizationName], " +
            "CASE WHEN T6._Description IS NULL THEN T9._Description ELSE T6._Description END [TypePriceName], " +
            "T7._Description [PromotionalTypePriceName], " +
            "CONVERT(bit, T1._Fld1018) [IsManagementAccounting], " +
            "CONVERT(bit, T1._Fld1022) [IsAccounting], " +
            "CASE WHEN T1._Fld986 = '2001-1-1' THEN NULL ELSE DATEADD(YEAR, -2000, T1._Fld986) END [FromDate], " +
            "CASE WHEN T1._Fld1021 = '2001-1-1' THEN NULL ELSE DATEADD(YEAR, -2000, T1._Fld1021) END [ToDate], " +
            "T8._EnumOrder [Type] " +
            "FROM dbo._Reference47 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference68 T2 WITH(NOLOCK) " +
            "ON (T1._OwnerIDRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference17 T4 WITH(NOLOCK) " +
            "ON T1._Fld988RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T5 WITH(NOLOCK) " +
            "ON T1._Fld999RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference143 T6 WITH(NOLOCK) " +
            "ON T1._Fld1003_RRRef = T6._IDRRef " +
            "AND T6._Description NOT LIKE N'(до%' " +
            "LEFT OUTER JOIN dbo._Reference143 T7 WITH(NOLOCK) " +
            "ON T2._Fld16086RRef = T7._IDRRef " +
            "AND T7._Description NOT LIKE N'(до%' " +
            "LEFT OUTER JOIN dbo._Reference144 T9 WITH(NOLOCK) " +
            "ON T1._Fld1003_RRRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum362 T8 WITH(NOLOCK) " +
            "ON T1._Fld1005RRef = T8._IDRRef " +
            "WHERE (T1._Marked = 0x00) AND (T1._Folder) = 0x01 " +
            "AND CAST(T2._Code AS bigint) = @ClientCode " +
            "AND T5._Description IN @OrganizationNames ",
            new {
                ClientCode = clientCode,
                OrganizationNames = isProvider.HasValue && isProvider.Value ? ORGANIZATION_NAMES_FOR_PROVIDER : ORGANIZATION_NAMES
            }
        );
    }

    public IEnumerable<SyncAgreement> GetAmgAllSyncAgreementsByCode(
        long clientCode) {
        return _amgSyncConnection.Query<SyncAgreement>(
            "SELECT " +
            "T1._IDRRef [SourceId], " +
            "T1._Code [SourceCode], " +
            "T1._Description [Name], " +
            "T1._Fld1322 [Number], " +
            "T2._Code [CurrencyCode], " +
            "T1._Fld1328 [PermissibleDebtAmount], " +
            "T1._Fld1329 [DebtDaysAllowedNumber], " +
            "T3._Description [OrganizationName], " +
            "CASE WHEN T4._Description IS NULL THEN T8._Description ELSE T4._Description END [TypePriceName], " +
            "T5._Description [PromotionalTypePriceName], " +
            "CAST(T1._Fld1353 AS bit) [IsManagementAccounting], " +
            "CAST(T1._Fld1357 AS bit) [IsAccounting], " +
            "CASE WHEN T1._Fld1321 = '2001-1-1' THEN NULL ELSE DATEADD(YEAR, -2000, T1._Fld1321) END [FromDate], " +
            "CASE WHEN T1._Fld1356 = '2001-1-1' THEN NULL ELSE DATEADD(YEAR, -2000, T1._Fld1356) END [ToDate], " +
            "T7._EnumOrder [Type] " +
            "FROM dbo._Reference66 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference35 T2 WITH(NOLOCK) " +
            "ON T1._Fld1323RRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference116 T3 WITH(NOLOCK) " +
            "ON T1._Fld1334RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T6 WITH(NOLOCK) " +
            "ON T1._OwnerIDRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference171 T4 WITH(NOLOCK) " +
            "ON T1._Fld1338_RRRef = T4._IDRRef " +
            "AND T4._Description NOT LIKE N'(до%' " +
            "LEFT OUTER JOIN dbo._Reference171 T5 WITH(NOLOCK) " +
            "ON T6._Fld1491RRef = T5._IDRRef " +
            "AND T5._Description NOT LIKE N'(до%' " +
            "LEFT OUTER JOIN dbo._Enum433 T7 WITH(NOLOCK) " +
            "ON T1._Fld1340RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference172 T8 WITH(NOLOCK) " +
            "ON T1._Fld1338_RRRef = T8._IDRRef " +
            "WHERE (T1._Marked = 0x00) AND (T1._Folder) = 0x01 " +
            "AND CAST(T6._Code AS bigint) = @ClientCode " +
            "AND T3._Description IN @OrganizationNames ",
            new {
                ClientCode = clientCode,
                OrganizationNames = ORGANIZATION_NAMES
            }
        );
    }

    public void RemoveAllAgreementsByClientId(long clientId) {
        _remoteSyncConnection.Execute(
            "UPDATE [ClientAgreement] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ClientID = @ClientId",
            new { ClientId = clientId }
        );
    }

    public long Add(Agreement agreement) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Agreement] " +
            "(" +
            "[AmountDebt] " +
            ",[CurrencyID] " +
            ",[NumberDaysDebt] " +
            ",[Updated] " +
            ",[OrganizationID] " +
            ",[PricingID] " +
            ",[PromotionalPricingID] " +
            ",[IsAccounting] " +
            ",[IsActive] " +
            ",[IsControlAmountDebt] " +
            ",[IsControlNumberDaysDebt] " +
            ",[IsManagementAccounting] " +
            ",[WithVATAccounting] " +
            ",[Name] " +
            ",[ProviderPricingID] " +
            ",[DeferredPayment] " +
            ",[TermsOfPayment] " +
            ",[IsPrePaymentFull] " +
            ",[PrePaymentPercentages] " +
            ",[IsPrePayment] " +
            ",[IsDefault] " +
            ",[Number] " +
            ",[TaxAccountingSchemeID] " +
            ",[AgreementTypeCivilCodeID] " +
            ",[FromDate] " +
            ",[ToDate] " +
            ",[SourceAmgCode] " +
            ",[SourceFenixCode] " +
            ",[SourceAmgID] " +
            ",[SourceFenixID] " +
            ",[WithAgreementLine] " +
            ",[IsDefaultForSyncConsignment] " +
            ") " +
            "VALUES " +
            "(" +
            "@AmountDebt " +
            ",@CurrencyId " +
            ",@NumberDaysDebt " +
            ",GETUTCDATE() " +
            ",@OrganizationId " +
            ",@PricingId " +
            ",@PromotionalPricingId " +
            ",@IsAccounting " +
            ",@IsActive " +
            ",@IsControlAmountDebt " +
            ",@IsControlNumberDaysDebt " +
            ",@IsManagementAccounting " +
            ",@WithVATAccounting " +
            ",@Name " +
            ",@ProviderPricingId " +
            ",@DeferredPayment " +
            ",@TermsOfPayment " +
            ",@IsPrePaymentFull " +
            ",@PrePaymentPercentages " +
            ",@IsPrePayment " +
            ",@IsDefault " +
            ",@Number " +
            ",@TaxAccountingSchemeId " +
            ",@AgreementTypeCivilCodeId " +
            ",@FromDate " +
            ",@ToDate " +
            ",@SourceAmgCode " +
            ",@SourceFenixCode " +
            ",@SourceAmgId " +
            ",@SourceFenixId " +
            ",@WithAgreementLine " +
            ",@IsDefaultForSyncConsignment " +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            agreement
        ).Single();
    }

    public long Add(ClientAgreement clientAgreement) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ClientAgreement] " +
            "([ClientID], [AgreementID], [ProductReservationTerm], [CurrentAmount], [OriginalClientAmgCode], [OriginalClientFenixCode], [Updated]) " +
            "VALUES " +
            "(@ClientId, @AgreementId, @ProductReservationTerm, @CurrentAmount, @OriginalClientAmgCode, @OriginalClientFenixCode, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            clientAgreement
        ).Single();
    }

    public void Update(Agreement agreement) {
        _remoteSyncConnection.Execute(
            "UPDATE Agreement SET " +
            "Name = @Name, IsManagementAccounting = @IsManagementAccounting, IsAccounting = @IsAccounting, WithVATAccounting = @WithVATAccounting, " +
            "IsControlAmountDebt = @IsControlAmountDebt, IsControlNumberDaysDebt = @IsControlNumberDaysDebt, IsActive = @IsActive, AmountDebt = @AmountDebt, " +
            "NumberDaysDebt = @NumberDaysDebt, CurrencyId = @CurrencyId, OrganizationId = @OrganizationId, PricingId = @PricingId, " +
            "ProviderPricingId = @ProviderPricingId, DeferredPayment = @DeferredPayment, TermsOfPayment = @TermsOfPayment, " +
            "PrePaymentPercentages = @PrePaymentPercentages, IsPrePaymentFull = @IsPrePaymentFull, IsPrePayment = @IsPrePayment, IsDefault = @IsDefault, " +
            "Updated = getutcdate(), TaxAccountingSchemeID = @TaxAccountingSchemeId, Number = @Number, FromDate = @FromDate, ToDate = @ToDate, " +
            "[PromotionalPricingID] = @PromotionalPricingId, IsSelected = @IsSelected, [ForReSale] = @ForReSale, [WithAgreementLine] = @WithAgreementLine, " +
            "[Deleted] = @Deleted,[SourceAmgCode] = @SourceAmgCode,[SourceFenixCode] = @SourceFenixCode,[SourceAmgID] = @SourceAmgId,[SourceFenixID] = @SourceFenixId " +
            "WHERE NetUID = @NetUid ",
            agreement
        );
    }

    public void Update(ClientAgreement clientAgreement) {
        _remoteSyncConnection.Execute(
            "UPDATE ClientAgreement SET " +
            "ProductReservationTerm = @ProductReservationTerm" +
            ", ClientId = @ClientId" +
            ", AgreementId = @AgreementId" +
            ", Updated = getutcdate()" +
            ", [Deleted] = @Deleted " +
            ",[OriginalClientAmgCode] = @OriginalClientAmgCode " +
            ",[OriginalClientFenixCode] = @OriginalClientFenixCode " +
            "WHERE NetUID = @NetUid ",
            clientAgreement
        );
    }

    public long Add(ProviderPricing pricing) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProviderPricing] " +
            "([Name], [CurrencyID], [BasePricingID], [Updated]) " +
            "VALUES " +
            "(@Name, @CurrencyId, @BasePricingId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            pricing
        ).Single();
    }

    public Pricing GetPricingByCultureWithHighestExtraCharge(string culture) {
        return _remoteSyncConnection.Query<Pricing>(
            "SELECT TOP(1) * " +
            "FROM [Pricing] " +
            "WHERE [Pricing].Deleted = 0 " +
            "AND [Pricing].Culture = @Culture " +
            "ORDER BY [Pricing].CalculatedExtraCharge DESC",
            new { Culture = culture }
        ).SingleOrDefault();
    }

    public Organization GetOrganizationByCultureIfExists(string culture) {
        return _remoteSyncConnection.Query<Organization>(
            "SELECT TOP(1) * " +
            "FROM [Organization] " +
            "WHERE [Organization].Deleted = 0 " +
            "AND [Organization].Culture = @Culture",
            new { Culture = culture }
        ).FirstOrDefault();
    }

    public IEnumerable<SyncStorage> GetAllSyncStorages() {
        return _oneCConnection.Query<SyncStorage>(
            "SELECT " +
            "T1._Description [StorageName], " +
            "T2._Description [OrganizationName] " +
            "FROM dbo._Reference117 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference90 T2 WITH(NOLOCK) " +
            "ON (T1._IDRRef = T2._Fld16181RRef) " +
            "WHERE (T1._Folder) = 0x01 AND (T1._Marked = 0x00) "
        );
    }

    public IEnumerable<SyncStorage> GetAmgAllSyncStorages() {
        return _amgSyncConnection.Query<SyncStorage>(
            "SELECT " +
            "T1._Description [StorageName], " +
            "T2._Description [OrganizationName] " +
            "FROM dbo._Reference144 T1 " +
            "LEFT JOIN dbo._Reference116 T2 " +
            "ON (T2._Fld15976RRef = T1._IDRRef) " +
            "WHERE (T1._Folder) = 0x01 AND (T1._Marked = 0x00) "
        );
    }

    public List<Storage> GetAllStorages() {
        return _remoteSyncConnection.Query<Storage, Organization, Storage>(
            "SELECT * " +
            "FROM [Storage] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID ",
            (storage, organization) => {
                storage.Organization = organization;

                return storage;
            }
        ).ToList();
    }

    public long Add(Storage storage) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Storage] " +
            "([Name], [Locale], [ForDefective], [ForVatProducts], [OrganizationID], [Updated]) " +
            "VALUES " +
            "(@Name, @Locale, @ForDefective, @ForVatProducts, @OrganizationId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            storage
        ).Single();
    }

    public void Update(Storage storage) {
        _remoteSyncConnection.Execute(
            "UPDATE [Storage] " +
            "SET " +
            "[Locale] = @Locale, " +
            "[ForDefective] = @ForDefective, " +
            "[ForVatProducts] = @ForVatProducts, " +
            "[OrganizationID] = @OrganizationId, " +
            "[Updated] = GETUTCDATE(), " +
            "[Deleted] = @Deleted " +
            "WHERE ID = @Id",
            storage
        );
    }

    public void AddDefaultDiscountsForSpecificClientAgreement(long clientAgreementId) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ProductGroupDiscount] " +
            "([ClientAgreementID], [ProductGroupID], [IsActive], [DiscountRate], [Updated]) " +
            "SELECT @ClientAgreementId, [ProductGroup].ID, 1, 0.00, GETUTCDATE() " +
            "FROM [ProductGroup] " +
            "WHERE [ProductGroup].Deleted = 0",
            new { ClientAgreementId = clientAgreementId }
        );
    }

    public IEnumerable<ProductGroup> GetAllProductGroups() {
        return _remoteSyncConnection.Query<ProductGroup>(
            "SELECT * " +
            "FROM [ProductGroup] " +
            "WHERE [ProductGroup].Deleted = 0"
        );
    }

    public IEnumerable<SyncDiscount> GetAllDiscountsForSpecificClient(long clientCode, string pricingName) {
        return _oneCConnection.Query<SyncDiscount>(
            "SELECT " +
            "T2._Code [ClientCode], " +
            "CAST(T1._Active AS bit) [IsActive], " +
            "T3._IDRRef [ProductGroupSourceId], " +
            "T1._Fld11722_N [Discount] " +
            "FROM dbo._InfoRg11714 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference68 T2 WITH(NOLOCK) " +
            "ON (T1._Fld11720_TYPE = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x08 END AND T1._Fld11720_RTRef = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x00000044 " +
            "END AND T1._Fld11720_RRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference84 T3 WITH(NOLOCK) " +
            "ON T1._Fld11715RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum597 T4 WITH(NOLOCK) " +
            "ON T1._Fld11718RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference143 T5 WITH(NOLOCK) " +
            "ON T1._Fld11721RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference143 T6 WITH(NOLOCK) " +
            "ON T1._Fld11722_TYPE = 0x08 AND CASE WHEN T1._Fld11722_TYPE = 0x08 THEN 0x0000008F ELSE 0x00000000 END = 0x0000008F AND T1._Fld11722_RRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T7 WITH(NOLOCK) " +
            "ON T1._Fld11724RRef = T7._IDRRef " +
            "WHERE (T1._Fld11720_TYPE = 0x08 AND T1._Fld11720_RTRef = 0x00000044) " +
            "AND T2._Code = @ClientCode " +
            "AND T5._Description = @PricingName " +
            "ORDER BY T1._Period DESC, [ProductGroupSourceId]",
            new {
                ClientCode = clientCode,
                PricingName = pricingName
            }
        );
    }

    public IEnumerable<SyncDiscount> GetAmgAllDiscountsForSpecificClient(long clientCode, string pricingName) {
        return _amgSyncConnection.Query<SyncDiscount>(
            "SELECT " +
            "CAST(T1.Active_ AS bit) [IsActive], " +
            "T5._IDRRef [ProductGroupSourceId], " +
            "T1.Fld13998_N [Discount] " +
            "FROM (SELECT " +
            "T4._Fld13998_TYPE AS Fld13998_TYPE, " +
            "T4._Fld13998_N AS Fld13998_N, " +
            "T4._Fld13998_RRRef AS Fld13998_RRRef, " +
            "T4._Fld13996_TYPE AS Fld13996_TYPE, " +
            "T4._Fld13996_RTRef AS Fld13996_RTRef, " +
            "T4._Fld13996_RRRef AS Fld13996_RRRef, " +
            "T4._Fld13997RRef AS Fld13997RRef, " +
            "T4._Active AS Active_, " +
            "T4._Fld13991RRef AS Fld13991RRef " +
            "FROM (SELECT " +
            "T3._Fld13991RRef AS Fld13991RRef, " +
            "T3._Fld13992RRef AS Fld13992RRef, " +
            "T3._Fld13993RRef AS Fld13993RRef, " +
            "T3._Fld13994RRef AS Fld13994RRef, " +
            "T3._Fld13995_TYPE AS Fld13995_TYPE, " +
            "T3._Fld13995_N AS Fld13995_N, " +
            "T3._Fld13995_RTRef AS Fld13995_RTRef, " +
            "T3._Fld13995_RRRef AS Fld13995_RRRef, " +
            "T3._Fld13996_TYPE AS Fld13996_TYPE, " +
            "T3._Fld13996_RTRef AS Fld13996_RTRef, " +
            "T3._Fld13996_RRRef AS Fld13996_RRRef, " +
            "T3._Fld13997RRef AS Fld13997RRef, " +
            "MAX(T3._Period) AS MAXPERIOD_ " +
            "FROM dbo._InfoRg13990 T3 WITH(NOLOCK) " +
            "WHERE T3._Active = 0x01 " +
            "GROUP BY T3._Fld13991RRef, " +
            "T3._Fld13992RRef, " +
            "T3._Fld13993RRef, " +
            "T3._Fld13994RRef, " +
            "T3._Fld13995_TYPE, " +
            "T3._Fld13995_N, " +
            "T3._Fld13995_RTRef, " +
            "T3._Fld13995_RRRef, " +
            "T3._Fld13996_TYPE, " +
            "T3._Fld13996_RTRef, " +
            "T3._Fld13996_RRRef, " +
            "T3._Fld13997RRef) T2 " +
            "INNER JOIN dbo._InfoRg13990 T4 WITH(NOLOCK) " +
            "ON T2.Fld13991RRef = T4._Fld13991RRef AND T2.Fld13992RRef = T4._Fld13992RRef AND T2.Fld13993RRef = T4._Fld13993RRef AND " +
            "T2.Fld13994RRef = T4._Fld13994RRef AND T2.Fld13995_TYPE = T4._Fld13995_TYPE AND T2.Fld13995_N = T4._Fld13995_N " +
            "AND T2.Fld13995_RTRef = T4._Fld13995_RTRef AND T2.Fld13995_RRRef = T4._Fld13995_RRRef AND " +
            "T2.Fld13996_TYPE = T4._Fld13996_TYPE AND T2.Fld13996_RTRef = T4._Fld13996_RTRef AND " +
            "T2.Fld13996_RRRef = T4._Fld13996_RRRef AND T2.Fld13997RRef = T4._Fld13997RRef AND T2.MAXPERIOD_ = T4._Period) T1 " +
            "LEFT OUTER JOIN dbo._Reference108 T5 WITH(NOLOCK) " +
            "ON T1.Fld13991RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 T6 WITH(NOLOCK) " +
            "ON T1.Fld13996_TYPE = 0x08 AND T1.Fld13996_RTRef = 0x00000042 AND T1.Fld13996_RRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T7 WITH(NOLOCK) " +
            "ON T1.Fld13996_TYPE = 0x08 AND T1.Fld13996_RTRef = 0x0000005A AND T1.Fld13996_RRRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference144 T8 WITH(NOLOCK) " +
            "ON T1.Fld13996_TYPE = 0x08 AND T1.Fld13996_RTRef = 0x00000090 AND T1.Fld13996_RRRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference171 T9 WITH(NOLOCK) " +
            "ON T1.Fld13997RRef = T9._IDRRef " +
            "WHERE T9._Description = @PricingName " +
            "AND CASE WHEN T1.Fld13996_TYPE = 0x08 AND T1.Fld13996_RTRef = 0x00000042  " +
            "THEN T6._Code WHEN T1.Fld13996_TYPE = 0x08 AND T1.Fld13996_RTRef = 0x0000005A  " +
            "THEN T7._Code WHEN T1.Fld13996_TYPE = 0x08 AND T1.Fld13996_RTRef = 0x00000090  " +
            "THEN T8._Code ELSE CAST(NULL AS NVARCHAR(9)) END = @ClientCode "
            ,
            new {
                ClientCode = clientCode,
                PricingName = pricingName
            }
        );
    }

    public void ExecuteSql(string sqlExpression) {
        _remoteSyncConnection.Execute(
            sqlExpression
        );
    }

    public IEnumerable<SyncDeliveryRecipient> GetAllSyncDeliveryRecipientsByClientCode(long clientCode) {
        return _oneCConnection.Query<SyncDeliveryRecipient>(
            "SELECT " +
            "T1._Description [Name], " +
            "T1._Fld15420 [Address] " +
            "FROM dbo._Reference15417 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference68 T2 WITH(NOLOCK) " +
            "ON T1._OwnerIDRRef = T2._IDRRef " +
            "WHERE (T1._Marked = 0x00) " +
            "AND CAST(T2._Code AS bigint) = @ClientCode",
            new { ClientCode = clientCode }
        );
    }

    public IEnumerable<SyncDeliveryRecipient> GetAmgAllSyncDeliveryRecipientsByClientCode(long clientCode) {
        return _amgSyncConnection.Query<SyncDeliveryRecipient>(
            "SELECT " +
            "T1._Description [Name], " +
            "T1._Fld2487 [Address] " +
            "FROM dbo._Reference188 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference90 T2 WITH(NOLOCK) " +
            "ON T1._OwnerIDRRef = T2._IDRRef " +
            "WHERE T2._Code = @ClientCode "
            ,
            new { ClientCode = clientCode }
        );
    }

    public List<DeliveryRecipient> GetAllDeliveryRecipientsByClientId(long clientId) {
        List<DeliveryRecipient> recipients = new();

        _remoteSyncConnection.Query<DeliveryRecipient, DeliveryRecipientAddress, DeliveryRecipient>(
            "SELECT * " +
            "FROM [DeliveryRecipient] " +
            "LEFT JOIN [DeliveryRecipientAddress] " +
            "ON [DeliveryRecipientAddress].DeliveryRecipientID = [DeliveryRecipient].ID " +
            "AND [DeliveryRecipientAddress].Deleted = 0 " +
            "WHERE [DeliveryRecipient].ClientID = @ClientId ",
            (recipient, address) => {
                if (recipients.Any(r => r.Id.Equals(recipient.Id)))
                    recipient = recipients.First(r => r.Id.Equals(recipient.Id));
                else
                    recipients.Add(recipient);

                if (address == null) return recipient;

                recipient.DeliveryRecipientAddresses.Add(address);

                return recipient;
            },
            new { ClientId = clientId }
        );

        return recipients;
    }

    public long Add(DeliveryRecipient recipient) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [DeliveryRecipient] " +
            "([FullName], [ClientID], [Priority], [MobilePhone], [Updated]) " +
            "VALUES " +
            "(@FullName, @ClientId, @Priority, @MobilePhone, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            recipient
        ).Single();
    }

    public void Add(DeliveryRecipientAddress address) {
        _remoteSyncConnection.Query<long>(
            "INSERT INTO [DeliveryRecipientAddress] " +
            "([Value], [Department], [City], [Priority], [DeliveryRecipientID], [Updated]) " +
            "VALUES " +
            "(@Value, @Department, @City, @Priority, @DeliveryRecipientId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            address
        );
    }

    public void ReAssignClientAgreements(long fromClientId, long toClientId, bool forAmg) {
        _remoteSyncConnection.Execute(
            "UPDATE [Agreement] " +
            "SET IsActive = 0, Updated = GETUTCDATE() " +
            "WHERE ID IN (" +
            "SELECT [ClientAgreement].AgreementID " +
            "FROM [ClientAgreement] " +
            "WHERE [ClientAgreement].ClientID = @FromClientId" +
            "); " +
            "UPDATE [ClientAgreement] " +
            "SET ClientID = @ToClientId, Updated = GETUTCDATE() " +
            "WHERE ClientID = @FromClientId " +
            (forAmg
                ? "AND [ClientAgreement].[OriginalClientAmgCode] NOT IN ( " +
                  "SELECT CASE WHEN [ClientAgreement].[OriginalClientAmgCode] IS NOT NULL THEN [ClientAgreement].[OriginalClientAmgCode] ELSE 0 END " +
                  "FROM [ClientAgreement] " +
                  "WHERE [ClientID] = @ToClientId) "
                : "AND [ClientAgreement].[OriginalClientFenixCode] NOT IN ( " +
                  "SELECT CASE WHEN [ClientAgreement].[OriginalClientFenixCode] IS NOT NULL THEN [ClientAgreement].[OriginalClientFenixCode] ELSE 0 END " +
                  "FROM [ClientAgreement] " +
                  "WHERE [ClientID] = @ToClientId) "),
            //"AND " +
            // "( " +
            // "[ClientAgreement].[OriginalClientFenixCode] NOT IN ( " +
            // "SELECT CASE WHEN [ClientAgreement].[OriginalClientFenixCode] IS NOT NULL THEN [ClientAgreement].[OriginalClientFenixCode] ELSE 0 END " +
            // "FROM [ClientAgreement] " +
            // "WHERE [ClientID] = @ToClientId) " +
            // "OR [ClientAgreement].[OriginalClientFenixCode] IS NULL) "),
            new {
                FromClientId = fromClientId,
                ToClientId = toClientId
            }
        );
    }

    public void RemoveClient(long clientId, long mainClientId) {
        _remoteSyncConnection.Execute(
            "UPDATE [Client] " +
            "SET Deleted = 1, MainClientID = @MainClientId " +
            "WHERE ID = @ClientId",
            new { ClientId = clientId, MainClientId = mainClientId }
        );
    }

    public ClientSubClient GetClientSubClientIfExists(long rootClientId, long subClientId) {
        return _remoteSyncConnection.Query<ClientSubClient>(
            "SELECT TOP(1) * " +
            "FROM [ClientSubClient] " +
            "WHERE [ClientSubClient].RootClientID = @RootClientId " +
            "AND [ClientSubClient].SubClientID = @SubClientId " +
            "ORDER BY Deleted",
            new { RootClientId = rootClientId, SubClientId = subClientId }
        ).SingleOrDefault();
    }

    public void Update(ClientSubClient subClient) {
        _remoteSyncConnection.Execute(
            "UPDATE [ClientSubClient] " +
            "SET Deleted = @Deleted, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            subClient
        );
    }

    public int GetNameDistance(string name1, string name2) {
        return _remoteSyncConnection.Query<int>(
            "SELECT dbo.fn_LevenshteinDistance(@Name1, @Name2, 1)",
            new { Name1 = name1, Name2 = name2 }
        ).Single();
    }

    public ClientAgreement GetClientAgreementBySourceId(byte[] sourceId, bool forAmg) {
        string where = forAmg ? "WHERE [Agreement].[SourceAmgID] = @SourceId " : "WHERE [Agreement].[SourceFenixID] = @SourceId ";

        return _remoteSyncConnection.Query<ClientAgreement, Agreement, Currency, Pricing, Organization, ClientAgreement>(
            "SELECT * " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            where,
            (clientAgreement, agreement, currency, pricing, organization) => {
                agreement.Currency = currency;
                agreement.Pricing = pricing;
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            }, new { SourceId = sourceId }
        ).FirstOrDefault();
    }

    public ProductGroupDiscount GetProductGroupDiscountByClientAgreementIdAndProductGroupId(
        long clientAgreementId,
        long productGroupId) {
        return _remoteSyncConnection.Query<ProductGroupDiscount>(
            "SELECT * FROM [ProductGroupDiscount] " +
            "WHERE [ProductGroupDiscount].[ClientAgreementID] = @ClientAgreementId " +
            "AND [ProductGroupDiscount].[ProductGroupID] = @productGroupId; ",
            new {
                ClientAgreementId = clientAgreementId,
                ProductGroupId = productGroupId
            }).FirstOrDefault();
    }

    public List<SupplyOrganization> GetAllSupplyOrganization() {
        List<SupplyOrganization> supplyOrganizations = new();

        _remoteSyncConnection.Query<SupplyOrganization, SupplyOrganizationAgreement, Organization, Currency, SupplyOrganization>(
            "SELECT * " +
            "FROM [SupplyOrganization] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[SupplyOrganizationID] = [SupplyOrganization].[ID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrganizationAgreement].[OrganizationID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] ",
            (supplyOrganization, supplyOrganizationAgreement, organization, currency) => {
                if (supplyOrganizations.Any(c => c.Id.Equals(supplyOrganization.Id)))
                    supplyOrganization = supplyOrganizations.First(c => c.Id.Equals(supplyOrganization.Id));
                else
                    supplyOrganizations.Add(supplyOrganization);

                if (supplyOrganizationAgreement == null) return supplyOrganization;

                supplyOrganizationAgreement.Organization = organization;

                supplyOrganizationAgreement.Currency = currency;

                supplyOrganization.SupplyOrganizationAgreements.Add(supplyOrganizationAgreement);

                return supplyOrganization;
            }
        );

        return supplyOrganizations;
    }

    public long Add(SupplyOrganization supplyOrganization) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [SupplyOrganization] " +
                "(Name, Address, PhoneNumber, EmailAddress, Requisites, Swift, SwiftBic, IntermediaryBank, BeneficiaryBank, AccountNumber, " +
                "Beneficiary, Bank, BankAccount, NIP, BankAccountPLN, BankAccountEUR, ContactPersonName, ContactPersonPhone, ContactPersonEmail, ContactPersonViber, " +
                "ContactPersonSkype, ContactPersonComment, IsAgreementReceived, IsBillReceived, AgreementReceiveDate, BillReceiveDate, Updated, [OriginalRegionCode], " +
                "[SourceAmgCode], [SourceFenixCode], [SourceAmgID], [SourceFenixID], [IsNotResident], TIN, USREOU, SROI) " +
                "VALUES (@Name, @Address, @PhoneNumber, @EmailAddress, @Requisites, @Swift, @SwiftBic, @IntermediaryBank, @BeneficiaryBank, @AccountNumber, " +
                "@Beneficiary, @Bank, @BankAccount, @NIP, @BankAccountPLN, @BankAccountEUR, @ContactPersonName, @ContactPersonPhone, @ContactPersonEmail, " +
                "@ContactPersonViber, @ContactPersonSkype, @ContactPersonComment, @IsAgreementReceived, @IsBillReceived, @AgreementReceiveDate, " +
                "@BillReceiveDate, GETUTCDATE(), @OriginalRegionCode, @SourceAmgCode, @SourceFenixCode, @SourceAmgId, @SourceFenixId, @IsNotResident, " +
                "@TIN, @USREOU, @SROI ); " +
                "SELECT SCOPE_IDENTITY()",
                supplyOrganization
            )
            .Single();
    }

    public void Update(SupplyOrganization supplyOrganization) {
        _remoteSyncConnection.Execute(
            "UPDATE [SupplyOrganization] " +
            "SET Name = @Name, Address = @Address, PhoneNumber = @PhoneNumber, EmailAddress = @EmailAddress, Requisites = @Requisites, Swift = @Swift, " +
            "SwiftBic = @SwiftBic, IntermediaryBank = @IntermediaryBank, BeneficiaryBank = @BeneficiaryBank, AccountNumber = @AccountNumber, Beneficiary = @Beneficiary, " +
            "Bank = @Bank, BankAccount = @BankAccount, NIP = @NIP, BankAccountPLN = @BankAccountPLN, BankAccountEUR = @BankAccountEUR, " +
            "ContactPersonName = @ContactPersonName, ContactPersonPhone = @ContactPersonPhone, ContactPersonEmail = @ContactPersonEmail, " +
            "ContactPersonViber = @ContactPersonViber, ContactPersonSkype = @ContactPersonSkype, ContactPersonComment = @ContactPersonComment, " +
            "IsAgreementReceived = @IsAgreementReceived, IsBillReceived = @IsBillReceived, " +
            "AgreementReceiveDate = @AgreementReceiveDate, BillReceiveDate = @BillReceiveDate, Updated = GETUTCDATE(), " +
            "SourceAmgCode = @SourceAmgCode, [SourceFenixCode] = @SourceFenixCode, [SourceAmgID] = @SourceAmgId, [SourceFenixID] = @SourceFenixId, " +
            "[IsNotResident] = @IsNotResident, TIN = @TIN, USREOU = @USREOU, SROI = @SROI " +
            "WHERE [SupplyOrganization].ID = @Id",
            supplyOrganization
        );
    }

    public long Add(SupplyOrganizationAgreement agreement) {
        return _remoteSyncConnection.Execute(
            "INSERT INTO [SupplyOrganizationAgreement] (Name, CurrentAmount, SupplyOrganizationId, CurrencyId, Updated, [ExistTo], [ExistFrom], [OrganizationID], " +
            "[SourceAmgCode], [SourceFenixCode], [SourceAmgID], [SourceFenixID], [Number]) " +
            "VALUES (@Name, @CurrentAmount, @SupplyOrganizationId, @CurrencyId, GETUTCDATE(), @ExistTo, @ExistFrom, @OrganizationId, " +
            "@SourceAmgCode, @SourceFenixCode, @SourceAmgId, @SourceFenixId, @Number); " +
            "SELECT SCOPE_IDENTITY()",
            agreement
        );
    }

    public void Update(SupplyOrganizationAgreement agreement) {
        _remoteSyncConnection.Execute(
            "UPDATE [SupplyOrganizationAgreement] " +
            "SET [Name] = @Name" +
            ", Updated = GETUTCDATE()" +
            ",[SourceAmgCode] = @SourceAmgCode " +
            ",[SourceFenixCode] = @SourceFenixCode " +
            ",[SourceAmgID] = @SourceAmgId " +
            ",[SourceFenixID] = @SourceFenixId " +
            ", [ExistTo] = @ExistTo" +
            ", [ExistFrom] = @ExistFrom" +
            ", [OrganizationID] = @OrganizationId " +
            ", [Number] = @Number " +
            "WHERE [SupplyOrganizationAgreement].ID = @Id",
            agreement
        );
    }

    public void AddClientUserProfile(ClientUserProfile clientUserProfile) {
        _remoteSyncConnection.Execute(
            "INSERT INTO ClientUserProfile (ClientId, UserProfileId, Updated) " +
            "VALUES (@ClientId, @UserProfileId, getutcdate())",
            clientUserProfile
        );
    }

    public long Add(Currency currency) {
        return _remoteSyncConnection.Execute(
            "INSERT INTO [Currency] ([Code], [Name], [Updated], [Created], [CodeOneC]) " +
            "VALUES (@Code, @Name, GETUTCDATE(), GETUTCDATE(), @CodeOneC); " +
            "SELECT SCOPE_IDENTITY()",
            currency
        );
    }

    public Currency GetEURCurrencyIfExists() {
        return _remoteSyncConnection.Query<Currency>(
                "SELECT TOP(1) * " +
                "FROM [Currency] " +
                "WHERE [Currency].Deleted = 0 " +
                "AND [Currency].Code = 'eur'"
            )
            .SingleOrDefault();
    }

    public Currency GetPLNCurrencyIfExists() {
        return _remoteSyncConnection.Query<Currency>(
                "SELECT TOP(1) * " +
                "FROM [Currency] " +
                "WHERE [Currency].Deleted = 0 " +
                "AND [Currency].Code = 'pln'"
            )
            .SingleOrDefault();
    }

    public Currency GetUAHCurrencyIfExists() {
        return _remoteSyncConnection.Query<Currency>(
                "SELECT TOP(1) * " +
                "FROM [Currency] " +
                "WHERE [Currency].Deleted = 0 " +
                "AND [Currency].Code = 'uah'"
            )
            .SingleOrDefault();
    }

    public Currency GetUSDCurrencyIfExists() {
        return _remoteSyncConnection.Query<Currency>(
                "SELECT TOP(1) * " +
                "FROM [Currency] " +
                "WHERE [Currency].Deleted = 0 " +
                "AND [Currency].Code = 'usd'"
            )
            .SingleOrDefault();
    }

    public IEnumerable<ClientType> GetAllClientTypes() {
        return _remoteSyncConnection.Query<ClientType>(
            "SELECT * " +
            "FROM [ClientType] " +
            "WHERE [ClientType].Deleted = 0 "
        );
    }

    public IEnumerable<ClientTypeRole> GetAllClientTypeRoles() {
        return _remoteSyncConnection.Query<ClientTypeRole>(
            "SELECT * " +
            "FROM [ClientTypeRole] " +
            "WHERE [ClientTypeRole].Deleted = 0 "
        );
    }

    public IEnumerable<PriceType> GetAllPriceTypes() {
        return _remoteSyncConnection.Query<PriceType>(
            "SELECT * FROM [PriceType] " +
            "WHERE [PriceType].[Deleted] = 0;"
        ).ToList();
    }

    public void DeleteDefaultAgreementForSyncConsignments() {
        _remoteSyncConnection.Execute(
            "UPDATE [ClientAgreement] " +
            "SET [Deleted] = 1 " +
            "WHERE [ClientAgreement].[AgreementID] IN ( " +
            "SELECT [Agreement].[ID] FROM [Agreement] " +
            "WHERE [IsDefaultForSyncConsignment] = 1); " +
            "UPDATE [Agreement] SET [Deleted] = 1 " +
            "WHERE [IsDefaultForSyncConsignment] = 1; "
        );
    }

    public void DeleteClientByNetId(Guid netId) {
        _remoteSyncConnection.Execute("UPDATE [Client] Set [Deleted] = 1 WHERE NetUID = @NetId ", new { NetId = netId });
    }

    public void DeleteClientById(long id) {
        _remoteSyncConnection.Execute("UPDATE [Client] Set [Deleted] = 1 WHERE ID = @Id ", new { Id = id });
    }

    public IEnumerable<VatRate> GetAllVatRates() {
        return _remoteSyncConnection.Query<VatRate>("SELECT * FROM [VatRate] WHERE [VatRate].[Deleted] = 0 ");
    }

    public long AddVatRate(VatRate vatRate) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [VatRate]([Updated], [Value]) " +
            "VALUES (getutcdate(), @Value); " +
            "SELECT SCOPE_IDENTITY(); ",
            vatRate).FirstOrDefault();
    }
}