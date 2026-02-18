using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync;

public sealed class OutcomeOrdersSyncRepository : IOutcomeOrdersSyncRepository {
    private readonly IDbConnection _amgSyncConnection;
    private readonly IDbConnection _oneCConnection;

    private readonly IDbConnection _remoteSyncConnection;

    public OutcomeOrdersSyncRepository(
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgSyncConnection) {
        _oneCConnection = oneCConnection;

        _remoteSyncConnection = remoteSyncConnection;

        _amgSyncConnection = amgSyncConnection;
    }

    public IEnumerable<SyncOrganization> GetAllSyncOrganizations() {
        return _oneCConnection.Query<SyncOrganization>(
            "SELECT " +
            "T5._Code [OrganizationCode], " +
            "T5._Description [OrganizationName], " +
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
            "T4._Fld10931  AS Fld10931_, " +
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
            "WHERE (T5._Marked = 0x00) "
        );
    }

    public IEnumerable<SyncOrganization> GetAmgAllSyncOrganizations() {
        return _amgSyncConnection.Query<SyncOrganization>(
            "SELECT " +
            "T5._Code [OrganizationCode], " +
            "T5._Description [OrganizationName], " +
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
            "ON T5._Fld1888RRef = T10._IDRRef ");
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
            "WHERE [Organization].Deleted = 0",
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
            ",[TaxInspectionID]" +
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

    public Organization GetOrganizationByName(string name) {
        return _remoteSyncConnection.Query<Organization>(
            "SELECT * " +
            "FROM [Organization] " +
            "WHERE [Organization].Deleted = 0 " +
            "AND [Organization].[Name] = @Name",
            new { Name = name }
        ).SingleOrDefault();
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
            "WHERE (T1._Marked = 0x00) AND (T1._Folder) = 0x01 ");
    }

    public List<TaxInspection> GetAllTaxInspections() {
        return _remoteSyncConnection.Query<TaxInspection>(
            "SELECT * " +
            "FROM [TaxInspection] " +
            "WHERE [TaxInspection].Deleted = 0"
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
            "[Updated] = GETUTCDATE() " +
            "WHERE ID = @Id",
            taxInspection
        );
    }

    public IEnumerable<Currency> GetAllCurrencies() {
        return _remoteSyncConnection.Query<Currency>(
            "SELECT * " +
            "FROM [Currency] " +
            "WHERE [Currency].Deleted = 0"
        );
    }

    public void Update(Currency currency) {
        _remoteSyncConnection.Execute(
            "UPDATE [Currency] " +
            "SET CodeOneC = @CodeOneC, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            currency
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
            "ON (CASE WHEN T5._IDRRef IS NOT NULL THEN 0x08 END = T6._Fld10972_TYPE AND CASE WHEN T5._IDRRef IS NOT NULL " +
            "THEN 0x0000005A END = T6._Fld10972_RTRef AND T5._IDRRef = T6._Fld10972_RRRef) " +
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
            "ON (T1._Fld13053_TYPE = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x08 END " +
            "AND T1._Fld13053_S = CASE WHEN T2._IDRRef IS NOT NULL THEN N'' END AND T1._Fld13053_RRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference116 T3 WITH(NOLOCK) " +
            "ON (T1._Fld13051_TYPE = CASE WHEN T3._IDRRef IS NOT NULL THEN 0x08 END " +
            "AND T1._Fld13051_RTRef = CASE WHEN T3._IDRRef IS NOT NULL THEN 0x00000074 END " +
            "AND T1._Fld13051_RRRef = T3._IDRRef) " +
            "LEFT OUTER JOIN dbo._Enum670 T4 WITH(NOLOCK) " +
            "ON T2._Fld1203RRef = T4._IDRRef " +
            "WHERE T3._Code = @OrganizationCode ",
            new { OrganizationCode = organizationCode }
        );
    }

    public IEnumerable<ClientAgreement> GetAllClientAgreementsToSync() {
        return _remoteSyncConnection.Query<ClientAgreement, Agreement, Organization, Currency, Pricing, ClientAgreement>(
            "SELECT * " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "WHERE [ClientAgreement].Deleted = 0 " +
            "AND [Agreement].CurrencyID IS NOT NULL " +
            "AND [Agreement].OrganizationID IS NOT NULL " +
            "AND [Agreement].PricingID IS NOT NULL " +
            "AND ([ClientAgreement].[OriginalClientAmgCode] IS NOT NULL OR " +
            "[ClientAgreement].[OriginalClientFenixCode] IS NOT NULL) ",
            (clientAgreement, agreement, organization, currency, pricing) => {
                agreement.Organization = organization;
                agreement.Currency = currency;
                agreement.Pricing = pricing;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            }
        );
    }

    public IEnumerable<SyncSettlement> GetSyncSettlements(
        DateTime fromDate,
        DateTime toDate,
        long clientCode,
        string organizationName,
        string agreementName,
        string currencyCode,
        string typePriceName) {
        return _oneCConnection.Query<SyncSettlement>(
            "SELECT " +
            "T1._Period [FromDate], " +
            "( " +
            "CASE " +
            "WHEN T1._RecorderTRef = 0x000000AF " +
            "THEN 0 " +
            "WHEN T1._RecorderTRef = 0x0000010C " +
            "THEN 1 " +
            "WHEN T1._RecorderTRef = 0x0000010D " +
            "THEN 2 " +
            "WHEN T1._RecorderTRef = 0x0000011B " +
            "THEN 3 " +
            "WHEN T1._RecorderTRef = 0x00000122 " +
            "THEN 4 " +
            "ELSE 9 " +
            "END " +
            ") [SettlementType], " +
            "T1._RecorderRRef [DocumentRef], " +
            "T30._EnumOrder [TypeSettlement], " +
            "T1._Fld12436 [Value] " +
            "FROM dbo._AccumRg12429 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document175 T2 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000AF AND T1._RecorderRRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Document255 T3 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000FF AND T1._RecorderRRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Document291 T4 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000123 AND T1._RecorderRRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Document283 T5 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000011B AND T1._RecorderRRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Document269 T6 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000010D AND T1._RecorderRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Document287 T7 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000011F AND T1._RecorderRRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Document275 T8 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000113 AND T1._RecorderRRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Document278 T9 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000116 AND T1._RecorderRRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Document253 T10 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000FD AND T1._RecorderRRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Document247 T11 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000F7 AND T1._RecorderRRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Document271 T12 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000010F AND T1._RecorderRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Document163 T13 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000A3 AND T1._RecorderRRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Document276 T14 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000114 AND T1._RecorderRRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Document15602 T15 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00003CF2 AND T1._RecorderRRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Document222 T16 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000DE AND T1._RecorderRRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Document176 T17 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000B0 AND T1._RecorderRRef = T17._IDRRef " +
            "LEFT OUTER JOIN dbo._Document225 T18 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000E1 AND T1._RecorderRRef = T18._IDRRef " +
            "LEFT OUTER JOIN dbo._Document274 T19 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000112 AND T1._RecorderRRef = T19._IDRRef " +
            "LEFT OUTER JOIN dbo._Document268 T20 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000010C AND T1._RecorderRRef = T20._IDRRef " +
            "LEFT OUTER JOIN dbo._Document239 T21 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000EF AND T1._RecorderRRef = T21._IDRRef " +
            "LEFT OUTER JOIN dbo._Document277 T22 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000115 AND T1._RecorderRRef = T22._IDRRef " +
            "LEFT OUTER JOIN dbo._Document270 T23 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000010E AND T1._RecorderRRef = T23._IDRRef " +
            "LEFT OUTER JOIN dbo._Document246 T24 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000F6 AND T1._RecorderRRef = T24._IDRRef " +
            "LEFT OUTER JOIN dbo._Document177 T25 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000B1 AND T1._RecorderRRef = T25._IDRRef " +
            "LEFT OUTER JOIN dbo._Document290 T26 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000122 AND T1._RecorderRRef = T26._IDRRef " +
            "LEFT OUTER JOIN dbo._Document162 T27 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000A2 AND T1._RecorderRRef = T27._IDRRef " +
            "LEFT OUTER JOIN dbo._Document221 T28 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000DD AND T1._RecorderRRef = T28._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T29 WITH(NOLOCK) " +
            "ON T1._Fld12430RRef = T29._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum451 T30 WITH(NOLOCK) " +
            "ON T1._Fld12433RRef = T30._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T31 WITH(NOLOCK) " +
            "ON T1._Fld12434RRef = T31._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T32 WITH(NOLOCK) " +
            "ON T1._Fld12435RRef = T32._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T33 WITH(NOLOCK) " +
            "ON (T29._OwnerIDRRef = T33._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference17 T34 WITH(NOLOCK) " +
            "ON T29._Fld988RRef = T34._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference143 T36 WITH(NOLOCK) " +
            "ON T33._Fld1133RRef = T36._IDRRef " +
            "WHERE T1._Period >= CAST(@FromDate AS datetime2(3)) " +
            "AND T1._Period < CAST(@ToDate AS datetime2(3)) " +
            "AND T32._Code = @ClientCode " +
            "AND T31._Description = @OrganizationName " +
            "AND T29._Description = @AgreementName " +
            "AND T34._Code = @CurrencyCode " +
            "AND T36._Description = @TypePriceName " +
            "AND T36._Description IS NOT NULL " +
            "AND ( " +
            "CASE " +
            "WHEN T1._RecorderTRef = 0x000000AF " +
            "THEN 0 " +
            "WHEN T1._RecorderTRef = 0x0000010C " +
            "THEN 1 " +
            "WHEN T1._RecorderTRef = 0x0000010D " +
            "THEN 2 " +
            "WHEN T1._RecorderTRef = 0x0000011B " +
            "THEN 3 " +
            "WHEN T1._RecorderTRef = 0x00000122 " +
            "THEN 4 " +
            "ELSE 9 " +
            "END " +
            ") <> 9 " +
            "ORDER BY [FromDate] ",
            new {
                FromDate = fromDate.AddYears(2000),
                ToDate = toDate.AddYears(2000),
                ClientCode = clientCode,
                OrganizationName = organizationName,
                AgreementName = agreementName,
                CurrencyCode = currencyCode,
                TypePriceName = typePriceName
            }
        );
    }

    public IEnumerable<SyncSettlement> GetAmgSyncSettlements(
        DateTime fromDate,
        DateTime toDate,
        long clientCode,
        string organizationName,
        string agreementName,
        string currencyCode,
        string typePriceName) {
        return _amgSyncConnection.Query<SyncSettlement>(
            "SELECT " +
            "T1._Period [FromDate], " +
            "( " +
            "CASE " +
            "WHEN T1._RecorderTRef = 0x000000DB " +
            "THEN 0 " +
            "WHEN T1._RecorderTRef = 0x0000013F " +
            "THEN 1 " +
            "WHEN T1._RecorderTRef = 0x00000140 " +
            "THEN 2 " +
            "WHEN T1._RecorderTRef = 0x0000014C " +
            "THEN 3 " +
            "WHEN T1._RecorderTRef = 0x00000122 " +
            "THEN 4 " +
            "ELSE 9 " +
            "END " +
            ") [SettlementType], " +
            "T1._RecorderRRef [DocumentRef], " +
            "T30._EnumOrder [TypeSettlement], " +
            "T1._Fld14902 [Value] " +
            "FROM dbo._AccumRg14895 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document219 T2 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000DB AND T1._RecorderRRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Document304 T3 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000130 AND T1._RecorderRRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Document346 T4 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000015A AND T1._RecorderRRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Document332 T5 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000014C AND T1._RecorderRRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Document320 T6 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000140 AND T1._RecorderRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Document340 T7 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000154 AND T1._RecorderRRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Document328 T8 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000148 AND T1._RecorderRRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Document331 T9 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000014B AND T1._RecorderRRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Document302 T10 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000012E AND T1._RecorderRRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Document296 T11 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000128 AND T1._RecorderRRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Document321 T12 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000141 AND T1._RecorderRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Document205 T13 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000CD AND T1._RecorderRRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Document329 T14 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000149 AND T1._RecorderRRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Document396 T15 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000018C AND T1._RecorderRRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Document270 T16 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000010E AND T1._RecorderRRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Document220 T17 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000DC AND T1._RecorderRRef = T17._IDRRef " +
            "LEFT OUTER JOIN dbo._Document265 T18 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000109 AND T1._RecorderRRef = T18._IDRRef " +
            "LEFT OUTER JOIN dbo._Document327 T19 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000147 AND T1._RecorderRRef = T19._IDRRef " +
            "LEFT OUTER JOIN dbo._Document319 T20 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000013F AND T1._RecorderRRef = T20._IDRRef " +
            "LEFT OUTER JOIN dbo._Document288 T21 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000120 AND T1._RecorderRRef = T21._IDRRef " +
            "LEFT OUTER JOIN dbo._Document330 T22 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000014A AND T1._RecorderRRef = T22._IDRRef " +
            "LEFT OUTER JOIN dbo._Document322 T23 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000142 AND T1._RecorderRRef = T23._IDRRef " +
            "LEFT OUTER JOIN dbo._Document295 T24 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000127 AND T1._RecorderRRef = T24._IDRRef " +
            "LEFT OUTER JOIN dbo._Document221 T25 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000DD AND T1._RecorderRRef = T25._IDRRef " +
            "LEFT OUTER JOIN dbo._Document345 T26 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x00000159 AND T1._RecorderRRef = T26._IDRRef " +
            "LEFT OUTER JOIN dbo._Document203 T27 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x000000CB AND T1._RecorderRRef = T27._IDRRef " +
            "LEFT OUTER JOIN dbo._Document269 T28 WITH(NOLOCK) " +
            "ON T1._RecorderTRef = 0x0000010D AND T1._RecorderRRef = T28._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 T29 WITH(NOLOCK) " +
            "ON T1._Fld14896RRef = T29._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum528 T30 WITH(NOLOCK) " +
            "ON T1._Fld14899RRef = T30._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference116 T31 WITH(NOLOCK) " +
            "ON T1._Fld14900RRef = T31._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T32 WITH(NOLOCK) " +
            "ON T1._Fld14901RRef = T32._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T33 WITH(NOLOCK) " +
            "ON (T29._OwnerIDRRef = T33._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference35 T34 WITH(NOLOCK) " +
            "ON T29._Fld1323RRef = T34._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference171 T36 WITH(NOLOCK) " +
            "ON T33._Fld1484RRef = T36._IDRRef " +
            "WHERE T1._Period >= CAST(@FromDate AS datetime2(3)) " +
            "AND T1._Period < CAST(@ToDate AS datetime2(3)) " +
            "AND T32._Code = @ClientCode " +
            "AND T31._Description = @OrganizationName " +
            "AND T29._Description = @AgreementName " +
            "AND T34._Code = @CurrencyCode " +
            "AND T36._Description = @TypePriceName " +
            "AND T36._Description IS NOT NULL " +
            "AND ( " +
            "CASE " +
            "WHEN T1._RecorderTRef = 0x000000DB " +
            "THEN 0 " +
            "WHEN T1._RecorderTRef = 0x0000013F " +
            "THEN 1 " +
            "WHEN T1._RecorderTRef = 0x00000140 " +
            "THEN 2 " +
            "WHEN T1._RecorderTRef = 0x0000014C " +
            "THEN 3 " +
            "WHEN T1._RecorderTRef = 0x00000122 " +
            "THEN 4 " +
            "ELSE 9 " +
            "END " +
            ") <> 9 " +
            "ORDER BY [FromDate] ",
            new {
                FromDate = fromDate.AddYears(2000),
                ToDate = toDate.AddYears(2000),
                ClientCode = clientCode,
                OrganizationName = organizationName,
                AgreementName = agreementName,
                CurrencyCode = currencyCode,
                TypePriceName = typePriceName
            }
        );
    }

    public IEnumerable<SyncIncomePaymentOrderSale> GetAllIncomePaymentOrderSalesBySourceId(byte[] sourceId, bool cashOrder) {
        return cashOrder
            ? _oneCConnection.Query<SyncIncomePaymentOrderSale>(
                "SELECT " +
                "T3._Number [SaleNumber], " +
                "T1._Fld8262 [Amount] " +
                "FROM dbo._Document283_VT8257 T1 WITH(NOLOCK) " +
                "LEFT OUTER JOIN dbo._Document283 T2 WITH(NOLOCK) " +
                "ON (T1._Document283_IDRRef = T2._IDRRef) " +
                "LEFT JOIN dbo._Document290 T3 " +
                "ON T3._IDRRef = T1._Fld8282_RRRef " +
                "WHERE T1._Fld8282_RTRef = 0x00000122 " +
                "AND T2._IDRRef = @SourceId ",
                new { SourceId = sourceId })
            : _oneCConnection.Query<SyncIncomePaymentOrderSale>(
                "SELECT " +
                "T3._Number [SaleNumber], " +
                "T3._Fld8572 [Amount] " +
                "FROM dbo._Document268 T1 WITH(NOLOCK) " +
                "LEFT OUTER JOIN dbo._Document268_VT6922 T2 WITH(NOLOCK) " +
                "ON T1._IDRRef = T2._Document268_IDRRef " +
                "LEFT JOIN dbo._Document290 T3 " +
                "ON T3._IDRRef = T2._Fld6947_RRRef " +
                "WHERE T3._Number IS NOT NULL " +
                "AND T1._IDRRef = @SourceId ",
                new { SourceId = sourceId });
    }

    public IEnumerable<SyncIncomePaymentOrderSale> GetAllAmgIncomePaymentOrderSalesBySourceId(byte[] sourceId) {
        return new List<SyncIncomePaymentOrderSale>();
    }

    public IEnumerable<SyncProductTransferItem> GetAllAmgProductTransferItems(DateTime from, DateTime to) {
        return _amgSyncConnection.Query<SyncProductTransferItem>(
            "SELECT " +
            "[ProductTransfer]._IDRRef [DocumentId], " +
            "[ProductTransfer]._Number [Number], " +
            "[ProductTransfer]._Date_Time [DocumentDate], " +
            "[ProductTransfer]._Fld8013 [Comment], " +
            "CASE WHEN [ProductTransfer]._Fld8016 = 0x01 THEN 1 ELSE 0 END [Accounting], " +
            "CASE WHEN [ProductTransfer]._Fld8018 = 0x01 THEN 1 ELSE 0 END [ManagementAccounting], " +
            "[Organization]._Description [OrganizationName], " +
            "[StorageFrom]._Description [StorageFrom], " +
            "[StorageTo]._Description [StorageTo], " +
            "[ProductTransferProducts]._Fld8031 [Qty], " +
            "[Product]._Code [SourceProductCode] " +
            "FROM dbo._Document309 [ProductTransfer] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference116 [Organization] WITH(NOLOCK) " +
            "ON [ProductTransfer]._Fld8014RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference144 [StorageFrom] WITH(NOLOCK) " +
            "ON [ProductTransfer]._Fld8020RRef = [StorageFrom]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference144 [StorageTo] WITH(NOLOCK) " +
            "ON [ProductTransfer]._Fld8021RRef = [StorageTo]._IDRRef " +
            "LEFT OUTER JOIN dbo._Document309_VT8024 [ProductTransferProducts] WITH(NOLOCK) " +
            "ON [ProductTransferProducts]._Document309_IDRRef = [ProductTransfer]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference108 [Product] WITH(NOLOCK) " +
            "ON [Product]._IDRRef = [ProductTransferProducts]._Fld8026RRef " +
            "WHERE [ProductTransfer]._Date_Time >= @From " +
            "AND [ProductTransfer]._Date_Time <= @To " +
            "AND [ProductTransfer]._Posted = 0x01 " +
            "ORDER BY [Number], [DocumentDate] ",
            new {
                From = from.AddYears(2000),
                To = to.AddYears(2000)
            }
        );
    }

    public IEnumerable<SyncProductTransferItem> GetAllProductTransferItems(DateTime from, DateTime to) {
        return _oneCConnection.Query<SyncProductTransferItem>(
            "SELECT " +
            "[ProductTransfer]._IDRRef [DocumentId], " +
            "[ProductTransfer]._Number [Number], " +
            "[ProductTransfer]._Date_Time [DocumentDate], " +
            "[ProductTransfer]._Fld6600 [Comment], " +
            "CASE WHEN [ProductTransfer]._Fld6603 = 0x01 THEN 1 ELSE 0 END [Accounting], " +
            "CASE WHEN [ProductTransfer]._Fld6605 = 0x01 THEN 1 ELSE 0 END [ManagementAccounting], " +
            "[Organization]._Description [OrganizationName], " +
            "[StorageFrom]._Description [StorageFrom], " +
            "[StorageTo]._Description [StorageTo], " +
            "[ProductTransferProducts]._Fld6617 [Qty], " +
            "[Product]._Code [SourceProductCode] " +
            "FROM dbo._Document260 [ProductTransfer] " +
            "LEFT OUTER JOIN dbo._Reference90 [Organization] WITH(NOLOCK) " +
            "ON [Organization]._IDRRef = [ProductTransfer]._Fld6601RRef " +
            "LEFT OUTER JOIN dbo._Reference117 [StorageFrom] " +
            "ON [StorageFrom]._IDRRef = [ProductTransfer]._Fld6607RRef " +
            "LEFT OUTER JOIN dbo._Reference117 [StorageTo] " +
            "ON [StorageTo]._IDRRef = [ProductTransfer]._Fld6608RRef " +
            "LEFT OUTER JOIN dbo._Document260_VT6610 [ProductTransferProducts] " +
            "ON [ProductTransferProducts]._Document260_IDRRef = [ProductTransfer]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 [Product] WITH(NOLOCK) " +
            "ON [Product]._IDRRef = [ProductTransferProducts]._Fld6612RRef " +
            // "WHERE [ProductTransfer]._IDRRef IN (0x912BFE1137C5B1E311E7EF94D6CE57B0, 0x8456AA2B31EF4AD711EC37BF6B681F67) " +
            "WHERE [ProductTransfer]._Date_Time >= @From " +
            "AND [ProductTransfer]._Date_Time <= @To " +
            "AND [ProductTransfer]._Posted = 0x01 " +
            "ORDER BY [Number], DocumentDate ",
            new {
                From = from.AddYears(2000),
                To = to.AddYears(2000)
            });
    }

    public IEnumerable<SyncProductTransferItem> GetAllActProductTransferItems(DateTime from, DateTime to) {
        return _oneCConnection.Query<SyncProductTransferItem>(
            "SELECT " +
            "[Act]._IDRRef [DocumentId], " +
            "[Act]._Number [Number], " +
            "[Act]._Date_Time [DocumentDate], " +
            "[StorageFrom]._Description [StorageFrom], " +
            "[StorageTo]._Description [StorageTo], " +
            "[Organization]._Description [OrganizationName], " +
            "[Product]._Code [SourceProductCode], " +
            "[ActProduct]._Fld16209 [Qty] " +
            "FROM dbo._Document16189 [Act] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference117 [StorageFrom] WITH(NOLOCK) " +
            "ON [Act]._Fld16194RRef = [StorageFrom]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 [StorageTo] WITH(NOLOCK) " +
            "ON [Act]._Fld16200RRef = [StorageTo]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 [Organization] WITH(NOLOCK) " +
            "ON [Act]._Fld16190RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Document16189_VT16201 [ActProduct] WITH(NOLOCK) " +
            "ON [ActProduct]._Document16189_IDRRef = [Act]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 [Product] WITH(NOLOCK) " +
            "ON [ActProduct]._Fld16203RRef = [Product]._IDRRef " +
            "WHERE [Act]._Marked = 0x00 " +
            "AND [Product]._IDRRef IS NOT NULL " +
            "AND [Act]._Date_Time >= @From " +
            "AND [Act]._Date_Time <= @To " +
            "AND [Act]._Posted = 0x01 " +
            //"AND [Act]._IDRRef = 0x8103BB7F4244B8A611EC75F6F1EBB325 " +
            "ORDER BY [Number], [DocumentDate] ",
            new {
                From = from.AddYears(2000),
                To = to.AddYears(2000)
            });
    }

    public IEnumerable<SyncDepreciatedOrderItem> GetAllDepreciatedOrderItems(DateTime from, DateTime to) {
        return _oneCConnection.Query<SyncDepreciatedOrderItem>(
            "SELECT " +
            "[DepreciatedOrder]._IDRRef [DocumentId], " +
            "[DepreciatedOrder]._Number [Number], " +
            "[DepreciatedOrder]._Fld9147 [Comment], " +
            "[DepreciatedOrder]._Date_Time [DocumentDate], " +
            "CASE WHEN [DepreciatedOrder]._Fld9142 = 0x01 THEN 1 ELSE 0 END [ManagementAccounting], " +
            "CASE WHEN [DepreciatedOrder]._Fld9143 = 0x01 THEN 1 ELSE 0 END [Accounting], " +
            "[Storage]._Description [Storage], " +
            "[Organization]._Description [OrganizationName], " +
            "[ProductDepreciatedOrder]._Fld9169 [Qty], " +
            "[Product]._Code [SourceProductCode] " +
            "FROM dbo._Document302 [DepreciatedOrder] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference90 [Organization] WITH(NOLOCK) " +
            "ON [Organization]._IDRRef = [DepreciatedOrder]._Fld9139RRef " +
            "LEFT OUTER JOIN dbo._Reference117 [Storage] WITH(NOLOCK) " +
            "ON [Storage]._IDRRef = [DepreciatedOrder]._Fld9141RRef " +
            "LEFT OUTER JOIN dbo._Document302_VT9163 [ProductDepreciatedOrder] WITH(NOLOCK) " +
            "ON [ProductDepreciatedOrder]._Document302_IDRRef = [DepreciatedOrder]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 [Product] WITH(NOLOCK) " +
            "ON [Product]._IDRRef = [ProductDepreciatedOrder]._Fld9172RRef " +
            "WHERE [DepreciatedOrder]._Date_Time >= @From " +
            "AND [DepreciatedOrder]._Date_Time <= @To " +
            "AND [DepreciatedOrder]._Posted = 0x01 " +
            //"WHERE [DepreciatedOrder]._IDRRef = 0x8103BB7F4244B8A611EC73941D208DD7 " +
            "ORDER BY [Number], [DocumentDate] ",
            new {
                From = from.AddYears(2000),
                To = to.AddYears(2000)
            });
    }


    public IEnumerable<SyncSaleReturnItem> GetSaleReturnItemsBySourceId(byte[] sourceId) {
        return _oneCConnection.Query<SyncSaleReturnItem>(
            "SELECT " +
            "T1._Number [DocumentNumber], " +
            "T1._Date_Time [DocumentDate], " +
            "(CASE WHEN T1._Fld2604 IS NULL OR T1._Fld2604 = 0 THEN 1 ELSE T1._Fld2604 END) / " +
            "(CASE WHEN T1._Fld2607 IS NULL OR T1._Fld2607 = 0 THEN 1 ELSE T1._Fld2607 END) [RateExchange], " +
            "T1._Fld2606 [Comment], " +
            "T8._Description [ResponsibleName], " +
            "CAST(T9._Code AS bigint) [ProductCode], " +
            "T2._Fld2626 [Quantity], " +
            "T2._Fld2630 [Price], " +
            "T12._Number [SaleNumber], " +
            "CASE WHEN T1._Fld2595_TYPE = 0x08 AND T1._Fld2595_RTRef = 0x00000075 THEN T13._Description END [Storage] " +
            "FROM dbo._Document175 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document175_VT2623 T2 WITH(NOLOCK) " +
            "ON (T1._IDRRef = T2._Document175_IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference90 T3 WITH(NOLOCK) " +
            "ON T1._Fld2594RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference143 T4 WITH(NOLOCK) " +
            "ON T1._Fld2596RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T5 WITH(NOLOCK) " +
            "ON T1._Fld2597RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T6 WITH(NOLOCK) " +
            "ON T1._Fld2601RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T7 WITH(NOLOCK) " +
            "ON T1._Fld2602RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 T8 WITH(NOLOCK) " +
            "ON T1._Fld2608RRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 T9 WITH(NOLOCK) " +
            "ON T2._Fld2625RRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 T10 WITH(NOLOCK) " +
            "ON T2._Fld13534RRef = T10._IDRRef " +
            "LEFT JOIN dbo._Document175_VT2623 T11 WITH(NOLOCK) " +
            "ON T1._IDRRef = T11._Document175_IDRRef " +
            "LEFT JOIN dbo._Document290 T12 WITH(NOLOCK) " +
            "ON T11._Fld2637_RRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T13 WITH(NOLOCK) " +
            "ON T1._Fld2595_TYPE = 0x08 AND T1._Fld2595_RTRef = 0x00000075 AND T1._Fld2595_RRRef = T13._IDRRef " +
            "WHERE T11._Fld2637_TYPE = 0x08 AND T11._Fld2637_RTRef = 0x00000122 " +
            "AND [T1]._Posted = 0x01 " +
            "AND T1._IDRRef = @SourceId ",
            new { SourceId = sourceId }
        );
    }

    public IEnumerable<SyncSaleReturnItem> GetAmgSaleReturnItemsBySourceId(byte[] sourceId) {
        return _amgSyncConnection.Query<SyncSaleReturnItem>(
            "SELECT " +
            "T1._Number [DocumentNumber], " +
            "T1._Date_Time [DocumentDate], " +
            "(CASE WHEN T1._Fld3386 IS NULL OR T1._Fld3386 = 0 THEN 1 ELSE T1._Fld3386 END) / " +
            "(CASE WHEN T1._Fld3389 IS NULL OR T1._Fld3389 = 0 THEN 1 ELSE T1._Fld3389 END) [RateExchange], " +
            "T1._Fld3388 [Comment], " +
            "T3._Description [ResponsibleName], " +
            "CAST(T4._Code AS bigint) [ProductCode], " +
            "T2._Fld3414 [Quantity], " +
            "T2._Fld3418 [Price], " +
            "T5._Number [SaleNumber], " +
            "CASE WHEN T1._Fld3377_TYPE = 0x08 AND T1._Fld3377_RTRef = 0x00000090 THEN T6._Description END [Storage] " +
            "FROM dbo._Document219 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document219_VT3411 T2 WITH(NOLOCK) " +
            "ON T1._IDRRef = T2._Document219_IDRRef " +
            "LEFT OUTER JOIN dbo._Reference126 T3 WITH(NOLOCK) " +
            "ON T1._Fld3390RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference108 T4 WITH(NOLOCK) " +
            "ON T2._Fld3413RRef = T4._IDRRef " +
            "LEFT JOIN dbo._Document345 T5 WITH(NOLOCK) " +
            "ON T5._IDRRef = T2._Fld3425_RRRef " +
            "LEFT OUTER JOIN dbo._Reference144 T6 WITH(NOLOCK) " +
            "ON T1._Fld3377_TYPE = 0x08 AND T1._Fld3377_RTRef = 0x00000090 AND T1._Fld3377_RRRef = T6._IDRRef " +
            "WHERE (T1._Marked = 0x00) " +
            "AND [T1]._Posted = 0x01 " +
            "AND T1._IDRRef = @SourceId ",
            new { SourceId = sourceId }
        );
    }

    public IEnumerable<SyncSaleItem> GetSaleItemsBySourceId(byte[] sourceId) {
        return _oneCConnection.Query<SyncSaleItem>(
            "SELECT " +
            "T1._Number  [DocumentNumber], " +
            "T1._Date_Time  [DocumentDate], " +
            "CAST(T1._Fld8565 AS bit) [IsVatSale], " +
            "(CASE WHEN T1._Fld8566 IS NULL OR T1._Fld8566 = 0 THEN 1 ELSE T1._Fld8566 END) / " +
            "(CASE WHEN T1._Fld8573 IS NULL OR T1._Fld8573 = 0 THEN 1 ELSE T1._Fld8573 END) [RateExchange], " +
            "T6._Description [DocumentResponsibleName], " +
            "CAST(T7._Code AS bigint) [ProductCode], " +
            "T2._Fld8600 [Quantity], " +
            "T2._Fld8608 [Price] " +
            "FROM dbo._Document290 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document290_VT8595 T2 WITH(NOLOCK) " +
            "ON (T1._IDRRef = T2._Document290_IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference90 T3 WITH(NOLOCK) " +
            "ON T1._Fld8551RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T4 WITH(NOLOCK) " +
            "ON T1._Fld8560RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T5 WITH(NOLOCK) " +
            "ON T1._Fld8563RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 T6 WITH(NOLOCK) " +
            "ON T1._Fld8574RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 T7 WITH(NOLOCK) " +
            "ON T2._Fld8603RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 T8 WITH(NOLOCK) " +
            "ON T2._Fld8625RRef = T8._IDRRef " +
            "WHERE T1._IDRRef = @SourceId AND [T1]._Posted = 0x01 ",
            new { SourceId = sourceId }
        );
    }

    public IEnumerable<SyncSaleItem> GetAmgSaleItemsBySourceId(byte[] sourceId) {
        return _amgSyncConnection.Query<SyncSaleItem>(
            "SELECT " +
            "T2._Number [DocumentNumber], " +
            "T2._Date_Time [DocumentDate], " +
            "(CASE WHEN T2._Fld10348 IS NULL OR T2._Fld10348 = 0 THEN 1 ELSE T2._Fld10348 END) / " +
            "(CASE WHEN T2._Fld10355 IS NULL OR T2._Fld10355 = 0 THEN 1 ELSE T2._Fld10355 END) [RateExchange], " +
            "T3._Code [ProductCode], " +
            "T1._Fld10404 [Quantity], " +
            "T1._Fld10415 [Price] " +
            "FROM dbo._Document345 T2 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document345_VT10399 T1 WITH(NOLOCK) " +
            "ON (T1._Document345_IDRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference108 T3 WITH(NOLOCK) " +
            "ON T1._Fld10407RRef = T3._IDRRef " +
            "WHERE (T2._Marked = 0x00) " +
            "AND T2._IDRRef = @SourceId AND [T2]._Posted = 0x01 ",
            new { SourceId = sourceId }
        );
    }

    public SyncIncomePaymentOrder GetIncomePaymentOrderBySourceId(byte[] sourceId) {
        return _oneCConnection.Query<SyncIncomePaymentOrder>(
            "SELECT TOP(1) " +
            "T1._IDRRef [DocumentRef], " +
            "T1._Number [DocumentNumber], " +
            "T1._Date_Time [DocumentDate], " +
            "T1._Fld6900 [Comment], " +
            "T1._Fld6893 [DocumentValue], " +
            "T3._Code [CurrencyCode], " +
            "T4._Description [OrganizationAccountName], " +
            "T2._Fld6926 [RateExchange], " +
            "T35._Description [ArticleCashSpendingName] " +
            "FROM dbo._Document268 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document268_VT6922 T2 WITH(NOLOCK) " +
            "ON (T1._IDRRef = T2._Document268_IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference17 T3 WITH(NOLOCK) " +
            "ON T1._Fld6894RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 T4 WITH(NOLOCK) " +
            "ON T1._Fld6888RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T5 WITH(NOLOCK) " +
            "ON T1._Fld6887RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T6 WITH(NOLOCK) " +
            "ON T2._Fld6924RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Document175 T7 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000AF AND T2._Fld6947_RRRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Document255 T8 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000FF AND T2._Fld6947_RRRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Document291 T9 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x00000123 AND T2._Fld6947_RRRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Document283 T10 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x0000011B AND T2._Fld6947_RRRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Document269 T11 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x0000010D AND T2._Fld6947_RRRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Document287 T12 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x0000011F AND T2._Fld6947_RRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Document275 T13 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x00000113 AND T2._Fld6947_RRRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Document185 T14 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000B9 AND T2._Fld6947_RRRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Document278 T15 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x00000116 AND T2._Fld6947_RRRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Document253 T16 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000FD AND T2._Fld6947_RRRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Document247 T17 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000F7 AND T2._Fld6947_RRRef = T17._IDRRef " +
            "LEFT OUTER JOIN dbo._Document271 T18 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x0000010F AND T2._Fld6947_RRRef = T18._IDRRef " +
            "LEFT OUTER JOIN dbo._Document256 T19 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x00000100 AND T2._Fld6947_RRRef = T19._IDRRef " +
            "LEFT OUTER JOIN dbo._Document163 T20 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000A3 AND T2._Fld6947_RRRef = T20._IDRRef " +
            "LEFT OUTER JOIN dbo._Document276 T21 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x00000114 AND T2._Fld6947_RRRef = T21._IDRRef " +
            "LEFT OUTER JOIN dbo._Document222 T22 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000DE AND T2._Fld6947_RRRef = T22._IDRRef " +
            "LEFT OUTER JOIN dbo._Document176 T23 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000B0 AND T2._Fld6947_RRRef = T23._IDRRef " +
            "LEFT OUTER JOIN dbo._Document274 T24 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x00000112 AND T2._Fld6947_RRRef = T24._IDRRef " +
            "LEFT OUTER JOIN dbo._Document268 T25 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x0000010C AND T2._Fld6947_RRRef = T25._IDRRef " +
            "LEFT OUTER JOIN dbo._Document239 T26 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000EF AND T2._Fld6947_RRRef = T26._IDRRef " +
            "LEFT OUTER JOIN dbo._Document277 T27 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x00000115 AND T2._Fld6947_RRRef = T27._IDRRef " +
            "LEFT OUTER JOIN dbo._Document270 T28 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x0000010E AND T2._Fld6947_RRRef = T28._IDRRef " +
            "LEFT OUTER JOIN dbo._Document246 T29 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000F6 AND T2._Fld6947_RRRef = T29._IDRRef " +
            "LEFT OUTER JOIN dbo._Document177 T30 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000B1 AND T2._Fld6947_RRRef = T30._IDRRef " +
            "LEFT OUTER JOIN dbo._Document290 T31 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x00000122 AND T2._Fld6947_RRRef = T31._IDRRef " +
            "LEFT OUTER JOIN dbo._Document162 T32 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000A2 AND T2._Fld6947_RRRef = T32._IDRRef " +
            "LEFT OUTER JOIN dbo._Document221 T33 WITH(NOLOCK) " +
            "ON T2._Fld6947_TYPE = 0x08 AND T2._Fld6947_RTRef = 0x000000DD AND T2._Fld6947_RRRef = T33._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum563 T34 WITH(NOLOCK) " +
            "ON T2._Fld6930RRef = T34._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference127 T35 WITH(NOLOCK) " +
            "ON T2._Fld6932RRef = T35._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T36 WITH(NOLOCK) " +
            "ON (T6._OwnerIDRRef = T36._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference17 T37 WITH(NOLOCK) " +
            "ON T6._Fld988RRef = T37._IDRRef " +
            "WHERE T1._IDRRef = @SourceId AND [T1]._Posted = 0x01 ",
            new { SourceId = sourceId }
        ).SingleOrDefault();
    }

    public SyncIncomePaymentOrder GetAmgIncomePaymentOrderBySourceId(byte[] sourceId) {
        return _amgSyncConnection.Query<SyncIncomePaymentOrder>(
            "SELECT TOP (1) " +
            "T2._IDRRef [DocumentRef], " +
            "T2._Number [DocumentNumber], " +
            "T2._Date_Time [DocumentDate], " +
            "T2._Fld8453 [Comment], " +
            "T2._Fld8446 [DocumentValue], " +
            "T3._Code [CurrencyCode], " +
            "T4._Description [OrganizationAccountName], " +
            "(CASE WHEN T1._Fld8479 IS NULL OR T1._Fld8479 = 0 THEN 1 ELSE T1._Fld8479 END) / " +
            "(CASE WHEN T1._Fld8481 IS NULL OR T1._Fld8481 = 0 THEN 1 ELSE T1._Fld8481 END) [RateExchange], " +
            "T5._Description [ArticleCashSpendingName] " +
            "FROM dbo._Document319 T2 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document319_VT8475 T1 WITH(NOLOCK) " +
            "ON T1._Document319_IDRRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 T3 WITH(NOLOCK) " +
            "ON T2._Fld8447RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference33 T4 WITH(NOLOCK) " +
            "ON T2._Fld8441RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference155 T5 WITH(NOLOCK) " +
            "ON T1._Fld8485RRef = T5._IDRRef " +
            "WHERE (T2._Marked = 0x00) " +
            "AND T2._IDRRef = @SourceId AND [T2]._Posted = 0x01 ",
            new { SourceId = sourceId }
        ).SingleOrDefault();
    }

    public SyncIncomeCashOrder GetIncomeCashOrderBySourceId(byte[] sourceId) {
        return _oneCConnection.Query<SyncIncomeCashOrder>(
            "SELECT TOP(1) " +
            "T1._IDRRef [DocumentRef], " +
            "T1._Number [DocumentNumber], " +
            "T1._Date_Time [DocumentDate], " +
            "T4._Description [OrganizationName], " +
            "T6._Code [CurrencyCode], " +
            "T1._Fld8213 [DocumentValue], " +
            "T1._Fld8223 [Comment], " +
            "T2._Fld8261 [RateExchange], " +
            "T7._Description [ArticleCashExpendingName], " +
            "T8._Description [PaymentRegisterName] " +
            "FROM dbo._Document283 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document283_VT8257 T2 WITH(NOLOCK) " +
            "ON (T1._IDRRef = T2._Document283_IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference68 T3 WITH(NOLOCK) " +
            "ON (T1._Fld8209_TYPE = CASE WHEN T3._IDRRef IS NOT NULL THEN 0x08 END " +
            "AND T1._Fld8209_RTRef = CASE WHEN T3._IDRRef IS NOT NULL THEN 0x00000044 END AND T1._Fld8209_RRRef = T3._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference90 T4 WITH(NOLOCK) " +
            "ON T1._Fld8205RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T5 WITH(NOLOCK) " +
            "ON T1._Fld8210RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T6 WITH(NOLOCK) " +
            "ON T1._Fld8212RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference127 T7 WITH(NOLOCK) " +
            "ON T2._Fld8267RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference56 T8 WITH(NOLOCK) " +
            "ON T1._Fld8206RRef = T8._IDRRef " +
            "WHERE T3._Code IS NOT NULL " +
            "AND T1._IDRRef = @SourceId AND [T1]._Posted = 0x01 ",
            new { SourceId = sourceId }
        ).SingleOrDefault();
    }

    public SyncIncomeCashOrder GetAmgIncomeCashOrderBySourceId(byte[] sourceId) {
        return _amgSyncConnection.Query<SyncIncomeCashOrder>(
            "SELECT " +
            "TOP (1) " +
            "T2._Number [DocumentNumber], " +
            "T2._Date_Time [DocumentDate], " +
            "T3._Code [CurrencyCode], " +
            "T2._Fld9642 [DocumentValue], " +
            "T2._Fld9652 [Comment], " +
            "(CASE WHEN T1._Fld9691 IS NULL OR T1._Fld9691 = 0 THEN 1 ELSE T1._Fld9691 END) / " +
            "(CASE WHEN T1._Fld9693 IS NULL OR T1._Fld9693 = 0 THEN 1 ELSE T1._Fld9693 END) [RateExchange], " +
            "T4._Description [ArticleCashExpendingName], " +
            "T5._Description [PaymentRegisterName] " +
            "FROM dbo._Document332 T2 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document332_VT9687 T1 WITH(NOLOCK) " +
            "ON (((T1._Document332_IDRRef = T2._IDRRef) AND (T1._Fld9690_TYPE = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x08 END AND T1._Fld9690_RTRef = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x0000014C END AND T1._Fld9690_RRRef = T2._IDRRef)) AND (T1._Fld9711_TYPE = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x08 END AND T1._Fld9711_RTRef = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x0000014C END AND T1._Fld9711_RRRef = T2._IDRRef)) " +
            "LEFT OUTER JOIN dbo._Reference35 T3 WITH(NOLOCK) " +
            "ON T2._Fld9641RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference155 T4 WITH(NOLOCK) " +
            "ON T1._Fld9697RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference76 T5 WITH(NOLOCK) " +
            "ON T2._Fld9635RRef = T5._IDRRef " +
            "WHERE (T2._Marked = 0x00) " +
            "AND T2._IDRRef = @SourceId AND [T2]._Posted = 0x01 ",
            new { SourceId = sourceId }
        ).SingleOrDefault();
    }

    public SyncOutcomePaymentOrder GetOutcomePaymentOrderBySourceId(byte[] sourceId) {
        return _oneCConnection.Query<SyncOutcomePaymentOrder>(
            "SELECT TOP(1) " +
            "T1._Number [DocumentNumber], " +
            "T1._Date_Time [DocumentDate], " +
            "T4._Description [OrganizationAccountName], " +
            "T1._Fld6979 [DocumentValue], " +
            "T7._Code [CurrencyCode], " +
            "T1._Fld6969 [Comment], " +
            "T2._Fld7007 [RateExchange], " +
            "T8._Description [ArticleCashExpendingName] " +
            "FROM dbo._Document269 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document269_VT7003 T2 WITH(NOLOCK) " +
            "ON (T1._IDRRef = T2._Document269_IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference90 T3 WITH(NOLOCK) " +
            "ON T1._Fld6973RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 T4 WITH(NOLOCK) " +
            "ON T1._Fld6981RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T5 WITH(NOLOCK) " +
            "ON T1._Fld6970RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 T6 WITH(NOLOCK) " +
            "ON T1._Fld6980RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T7 WITH(NOLOCK) " +
            "ON T1._Fld6963RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference127 T8 WITH(NOLOCK) " +
            "ON T2._Fld7013RRef = T8._IDRRef " +
            "WHERE T1._IDRRef = @SourceId AND [T1]._Posted = 0x01 ",
            new { SourceId = sourceId }
        ).SingleOrDefault();
    }

    public SyncOutcomePaymentOrder GetAmgOutcomePaymentOrderBySourceId(byte[] sourceId) {
        return _amgSyncConnection.Query<SyncOutcomePaymentOrder>(
            "SELECT " +
            "TOP (1) " +
            "T2._Number [DocumentNumber], " +
            "T2._Date_Time [DocumentDate], " +
            "T3._Description [OrganizationAccountName], " +
            "T2._Fld8533 [DocumentValue], " +
            "T4._Code [CurrencyCode], " +
            "T2._Fld8540 [Comment], " +
            "(CASE WHEN T1._Fld8568 IS NULL OR T1._Fld8568 = 0 THEN 1 ELSE T1._Fld8568 END) / " +
            "(CASE WHEN T1._Fld8570 IS NULL OR T1._Fld8570 = 0 THEN 1 ELSE T1._Fld8570 END) [RateExchange], " +
            "T5._Description [ArticleCashExpendingName] " +
            "FROM dbo._Document320 T2 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document320_VT8564 T1 WITH(NOLOCK) " +
            "ON T1._Document320_IDRRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference33 T3 WITH(NOLOCK) " +
            "ON T2._Fld8528RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 T4 WITH(NOLOCK) " +
            "ON T2._Fld8534RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference155 T5 WITH(NOLOCK) " +
            "ON T1._Fld8574RRef = T5._IDRRef " +
            "WHERE (T2._Marked = 0x00) " +
            "AND T2._IDRRef = @SourceId AND [T2]._Posted = 0x01 ",
            new { SourceId = sourceId }
        ).SingleOrDefault();
    }

    public long Add(Order entity) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Order] " +
            "( " +
            "[Created] " +
            ",[OrderSource] " +
            ",[Updated] " +
            ",[UserID] " +
            ",[ClientAgreementID] " +
            ",[OrderStatus] " +
            ",[IsMerged] " +
            ",[ClientShoppingCartID] " +
            ") " +
            "VALUES " +
            "(" +
            "@Created " +
            ",@OrderSource " +
            ",@Updated " +
            ",@UserID " +
            ",@ClientAgreementId " +
            ",@OrderStatus " +
            ",@IsMerged " +
            ",@ClientShoppingCartId " +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            entity
        ).Single();
    }

    public long Add(BaseLifeCycleStatus status) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [BaseLifeCycleStatus] " +
            "([SaleLifeCycleType], [Updated]) " +
            "VALUES " +
            "(@SaleLifeCycleType, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            status
        ).Single();
    }

    public long Add(BaseSalePaymentStatus status) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [BaseSalePaymentStatus] " +
            "([SalePaymentStatusType], [Amount], [Updated]) " +
            "VALUES " +
            "(@SalePaymentStatusType, @Amount, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            status
        ).Single();
    }

    public long Add(SaleNumber number) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [SaleNumber] " +
            "([Value], [OrganizationID], [Updated]) " +
            "VALUES " +
            "(@Value, @OrganizationId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            number
        ).Single();
    }

    public long Add(DeliveryRecipient deliveryRecipient) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [DeliveryRecipient] " +
            "([FullName], [Priority], [ClientID], [MobilePhone], [Updated]) " +
            "VALUES " +
            "(@FullName, @Priority, @ClientId, @MobilePhone, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            deliveryRecipient
        ).Single();
    }

    public long Add(DeliveryRecipientAddress address) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [DeliveryRecipientAddress] " +
            "([Value], [Department], [City], [Priority], [DeliveryRecipientID], [Updated]) " +
            "VALUES " +
            "(@Value, @Department, @City, @Priority, @DeliveryRecipientId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            address
        ).Single();
    }

    public long Add(Debt entity) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Debt] " +
            "([Total], [Days], [Created], [Updated]) " +
            "VALUES " +
            "(@Total, @Days, @Created, @Updated); " +
            "SELECT SCOPE_IDENTITY()",
            entity
        ).Single();
    }

    public void Add(ClientInDebt entity) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ClientInDebt] " +
            "([ClientID], [AgreementID], [DebtID], [SaleID], [Created], [Updated]) " +
            "VALUES " +
            "(@ClientId, @AgreementId, @DebtId, @SaleId, @Created, @Updated)",
            entity
        );
    }

    public long Add(Sale sale) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Sale] " +
            "( " +
            "[ClientAgreementID] " +
            ",[Created] " +
            ",[Deleted] " +
            ",[OrderID] " +
            ",[Updated] " +
            ",[UserID] " +
            ",[BaseLifeCycleStatusID] " +
            ",[BaseSalePaymentStatusID] " +
            ",[Comment] " +
            ",[SaleNumberID] " +
            ",[DeliveryRecipientID] " +
            ",[DeliveryRecipientAddressID] " +
            ",[TransporterID] " +
            ",[ShiftStatusID] " +
            ",[ParentNetId] " +
            ",[IsMerged] " +
            ",[SaleInvoiceDocumentID] " +
            ",[SaleInvoiceNumberID] " +
            ",[ChangedToInvoice] " +
            ",[OneTimeDiscountComment] " +
            ",[ChangedToInvoiceByID] " +
            ",[ShipmentDate] " +
            ",[CashOnDeliveryAmount] " +
            ",[HasDocuments] " +
            ",[IsCashOnDelivery] " +
            ",[IsPrinted] " +
            ",[TTN] " +
            ",[ShippingAmount] " +
            ",[TaxFreePackListID] " +
            ",[SadID] " +
            ",[IsVatSale] " +
            ",[ShippingAmountEur] " +
            ",[ExpiredDays] " +
            ",[IsLocked] " +
            ",[IsPaymentBillDownloaded] " +
            ",[IsImported] " +
            ",[IsPrintedPaymentInvoice]" +
            ") " +
            "VALUES " +
            "(" +
            "@ClientAgreementId " +
            ",@Created " +
            ",@Deleted " +
            ",@OrderID " +
            ",@Updated " +
            ",@UserID " +
            ",@BaseLifeCycleStatusId " +
            ",@BaseSalePaymentStatusId " +
            ",@Comment " +
            ",@SaleNumberId " +
            ",@DeliveryRecipientId " +
            ",@DeliveryRecipientAddressId " +
            ",@TransporterId " +
            ",@ShiftStatusId " +
            ",@ParentNetId " +
            ",@IsMerged " +
            ",@SaleInvoiceDocumentId " +
            ",@SaleInvoiceNumberId " +
            ",@ChangedToInvoice " +
            ",@OneTimeDiscountComment " +
            ",@ChangedToInvoiceById " +
            ",@ShipmentDate " +
            ",@CashOnDeliveryAmount " +
            ",@HasDocuments " +
            ",@IsCashOnDelivery " +
            ",@IsPrinted " +
            ",@TTN " +
            ",@ShippingAmount " +
            ",@TaxFreePackListId " +
            ",@SadID " +
            ",@IsVatSale " +
            ",@ShippingAmountEur " +
            ",@ExpiredDays " +
            ",@IsLocked " +
            ",@IsPaymentBillDownloaded " +
            ",1 " +
            ",@IsPrintedPaymentInvoice" +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            sale
        ).Single();
    }

    public long AddDeliveryRecipient(DeliveryRecipient deliveryRecipient) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO DeliveryRecipient (ClientID, Updated) " +
            "VALUES (@ClientId, GETUTCDATE()) " +
            "SELECT SCOPE_IDENTITY() ",
            deliveryRecipient
        ).FirstOrDefault();
    }

    public long AddDeliverRecipientAddress(DeliveryRecipientAddress deliveryRecipientAddress) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO DeliveryRecipientAddress (DeliveryRecipientID, Updated)" +
            "VALUES (@DeliveryRecipientId, GETUTCDATE()) " +
            "SELECT SCOPE_IDENTITY() ",
            deliveryRecipientAddress
        ).FirstOrDefault();
    }


    public void Add(OrderItem orderItem) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [OrderItem] (ClientShoppingCartId, OrderId, UserId, ProductId, Qty, Comment, IsValidForCurrentSale, PricePerItem, OrderedQty, " +
            "FromOfferQty, IsFromOffer, ExchangeRateAmount, OneTimeDiscount, DiscountAmount, PricePerItemWithoutVat, ReturnedQty, AssignedSpecificationId, Updated, Vat) " +
            "VALUES(@ClientShoppingCartId, @OrderId, @UserId, @ProductId, @Qty, @Comment, @IsValidForCurrentSale, @PricePerItem, @OrderedQty, " +
            "@FromOfferQty, @IsFromOffer, @ExchangeRateAmount, 0.00, 0.00, 0.00, 0, @AssignedSpecificationId, getutcdate(), @Vat)",
            orderItem
        );
    }

    public long AddWithId(OrderItem orderItem) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [OrderItem] (ClientShoppingCartId, OrderId, UserId, ProductId, Qty, Comment, IsValidForCurrentSale, PricePerItem, OrderedQty, " +
            "FromOfferQty, IsFromOffer, ExchangeRateAmount, OneTimeDiscount, DiscountAmount, PricePerItemWithoutVat, ReturnedQty, AssignedSpecificationId, Updated, Vat) " +
            "VALUES(@ClientShoppingCartId, @OrderId, @UserId, @ProductId, @Qty, @Comment, @IsValidForCurrentSale, @PricePerItem, @OrderedQty, " +
            "@FromOfferQty, @IsFromOffer, @ExchangeRateAmount, 0.00, @DiscountAmount, @PricePerItemWithoutVat, 0, @AssignedSpecificationId, getutcdate(), @Vat); " +
            "SELECT SCOPE_IDENTITY()",
            orderItem
        ).Single();
    }

    public Product GetProductBySourceCodeWithIncludes(long sourceCode, bool forAmg) {
        string query =
            "SELECT TOP(1) * " +
            "FROM [Product] " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Product].ID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [Product].Deleted = 0 " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [ProductPricing].Deleted = 0 " +
            "AND [Storage].Deleted = 0 ";
        if (forAmg)
            query += "AND [Product].SourceAmgCode = @SourceCode ";
        else
            query += "AND [Product].SourceFenixCode = @SourceCode ";

        return _remoteSyncConnection.Query<Product, MeasureUnit, ProductPricing, ProductAvailability, Storage, Product>(
            query,
            (product, measureUnit, productPricing, productAvailability, storage) => {
                product.MeasureUnit = measureUnit;
                product.ProductPricings.Add(productPricing);

                productAvailability.Storage = storage;

                product.ProductAvailabilities.Add(productAvailability);

                return product;
            },
            new { SourceCode = sourceCode }
        ).SingleOrDefault();
    }

    public Product GetProductBySourceCode(long sourceCode, bool forAmg) {
        string query =
            "SELECT * " +
            "FROM [Product] ";
        if (forAmg)
            query += "WHERE [Product].SourceAmgCode = @SourceCode ";
        else
            query += "WHERE [Product].SourceFenixCode = @SourceCode ";

        return _remoteSyncConnection.Query<Product>(
            query,
            new { SourceCode = sourceCode }
        ).SingleOrDefault();
    }

    public OrderItem GetOrderItemBySaleNumberAndProductCode(string saleNumber, long productCode) {
        return _remoteSyncConnection.Query<OrderItem, Product, OrderItem>(
            "SELECT " +
            "OrderItem.*, " +
            "Product.* " +
            "FROM OrderItem " +
            "LEFT JOIN Product " +
            "ON Product.ID = OrderItem.ProductID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = OrderItem.OrderID " +
            "LEFT JOIN Sale " +
            "ON Sale.OrderID = [Order].ID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = Sale.SaleNumberID " +
            "WHERE SaleNumber.Value = @SaleNumber " +
            "AND Product.OldEcommerceID = @ProductCode " +
            "AND OrderItem.Deleted = 0 ",
            (orderItem, product) => {
                orderItem.Product = product;

                return orderItem;
            },
            new { SaleNumber = saleNumber, ProductCode = productCode }
        ).Single();
    }

    public Sale GetSaleByOrderItemId(long orderItemId) {
        return _remoteSyncConnection.Query<Sale>(
            "SELECT Sale.* FROM Sale " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = Sale.OrderID " +
            "LEFT JOIN OrderItem " +
            "ON OrderItem.OrderID = [Order].ID " +
            "WHERE OrderItem.ID = @OrderItemId ",
            new { OrderItemId = orderItemId }
        ).FirstOrDefault();
    }

    public long Add(SaleReturn saleReturn) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [SaleReturn] (FromDate, ClientID, CreatedById, UpdatedById, Number, Updated) " +
            "VALUES (@FromDate, @ClientId, @CreatedById, @UpdatedById, @Number, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            saleReturn
        ).Single();
    }

    public void Add(SaleReturnItem saleReturnItem) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [SaleReturnItem] (Qty, SaleReturnItemStatus, IsMoneyReturned, OrderItemId, SaleReturnId, CreatedById, UpdatedById, MoneyReturnedById, " +
            "MoneyReturnedAt, StorageId, Updated) " +
            "VALUES (@Qty, @SaleReturnItemStatus, @IsMoneyReturned, @OrderItemId, @SaleReturnId, @CreatedById, @UpdatedById, @MoneyReturnedById, @MoneyReturnedAt, " +
            "@StorageId, GETUTCDATE())",
            saleReturnItem
        );
    }

    public OrderItem GetLastOrderItemByClientAgreementAndProductIdsIfExists(long clientAgreementId, long productId) {
        return _remoteSyncConnection.Query<OrderItem>(
            "SELECT TOP(1) [OrderItem].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "WHERE [Order].ClientAgreementID = @ClientAgreementId " +
            "AND [OrderItem].ProductID = @ProductId " +
            "ORDER BY [OrderItem].ID DESC",
            new { ClientAgreementId = clientAgreementId, ProductId = productId }
        ).SingleOrDefault();
    }

    public Order GetLastOrderByClientAgreementId(long clientAgreementId) {
        return _remoteSyncConnection.Query<Order>(
            "SELECT TOP(1) * " +
            "FROM [Order] " +
            "WHERE [Order].ClientAgreementID = @ClientAgreementId " +
            "ORDER BY [Order].ID DESC",
            new { ClientAgreementId = clientAgreementId }
        ).SingleOrDefault();
    }

    public Storage GetStorageIfExists() {
        return _remoteSyncConnection.Query<Storage>(
            "SELECT TOP(1) * " +
            "FROM [Storage] " +
            "WHERE [Storage].Locale = N'uk' " +
            "ORDER BY [Storage].Deleted, [Storage].ForDefective DESC"
        ).SingleOrDefault();
    }

    public Storage GetStorageByName(string name) {
        return _remoteSyncConnection.Query<Storage>(
            "SELECT * FROM Storage " +
            "WHERE Name = @Name " +
            "AND Deleted = 0",
            new { Name = name }).LastOrDefault();
    }

    public long Add(Storage storage) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Storage] " +
            "([Name], [Locale], [ForDefective], [ForVatProducts], [OrganizationID], [Updated], Deleted) " +
            "VALUES " +
            "(@Name, @Locale, @ForDefective, @ForVatProducts, @OrganizationId, GETUTCDATE(), @Deleted); " +
            "SELECT SCOPE_IDENTITY()",
            storage
        ).Single();
    }

    public PaymentRegister GetPaymentRegister(string name, string currencyOneCCode) {
        return _remoteSyncConnection.Query<PaymentRegister, PaymentCurrencyRegister, PaymentRegister>(
            "SELECT [PaymentRegister].* " +
            ", [PaymentCurrencyRegister].* " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "AND [PaymentCurrencyRegister].Deleted = 0 " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "WHERE [PaymentRegister].[Name] = @Name " +
            "AND [PaymentRegister].Deleted = 0 " +
            "AND [Currency].CodeOneC = @CurrencyOneCCode",
            (register, currencyRegister) => {
                register.PaymentCurrencyRegisters.Add(currencyRegister);

                return register;
            },
            new { Name = name, CurrencyOneCCode = currencyOneCCode }
        ).SingleOrDefault();
    }

    public long Add(OutcomePaymentOrder outcomePaymentOrder) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [OutcomePaymentOrder] " +
            "(Number, Comment, FromDate, Amount, Account, UserId, OrganizationId, PaymentCurrencyRegisterId, IsUnderReport, IsUnderReportDone, " +
            "ColleagueId, AdvanceNumber, ConsumableProductOrganizationId, ExchangeRate, AfterExchangeAmount, ClientAgreementId, " +
            "SupplyOrderPolandPaymentDeliveryProtocolId, SupplyOrganizationAgreementId, VAT, VatPercent, OrganizationClientId, OrganizationClientAgreementId, " +
            "TaxFreeId, SadId, Updated) " +
            "VALUES (@Number, @Comment, @FromDate, @Amount, @Account, @UserId, @OrganizationId, @PaymentCurrencyRegisterId, @IsUnderReport, @IsUnderReportDone, " +
            "@ColleagueId, @AdvanceNumber, @ConsumableProductOrganizationId, @ExchangeRate, @AfterExchangeAmount, @ClientAgreementId, " +
            "@SupplyOrderPolandPaymentDeliveryProtocolId, @SupplyOrganizationAgreementId, @VAT, @VatPercent, @OrganizationClientId, @OrganizationClientAgreementId, " +
            "@TaxFreeId, @SadId, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            outcomePaymentOrder
        ).Single();
    }

    public long Add(IncomePaymentOrder incomePaymentOrder) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [IncomePaymentOrder] (Number, BankAccount, Comment, FromDate, IncomePaymentOrderType, VatPercent, VAT, Amount, ExchangeRate, IsManagementAccounting, " +
            "IsAccounting, Account, ClientId, OrganizationId, CurrencyId, PaymentRegisterId, UserId, ColleagueId, ClientAgreementId, EuroAmount, " +
            "AgreementEuroExchangeRate, OrganizationClientId, OrganizationClientAgreementId, TaxFreeId, SadId, Updated) " +
            "VALUES (@Number, @BankAccount, @Comment, @FromDate, @IncomePaymentOrderType, @VatPercent, @VAT, @Amount, @ExchangeRate, @IsManagementAccounting, " +
            "@IsAccounting, @Account, @ClientId, @OrganizationId, @CurrencyId, @PaymentRegisterId, @UserId, @ColleagueId, @ClientAgreementId, @EuroAmount, " +
            "@AgreementEuroExchangeRate, @OrganizationClientId, @OrganizationClientAgreementId, @TaxFreeId, @SadId, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            incomePaymentOrder
        ).Single();
    }

    public void Add(PaymentMovementOperation paymentMovementOperation) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [PaymentMovementOperation] " +
            "(PaymentMovementId, IncomePaymentOrderId, OutcomePaymentOrderId, PaymentRegisterTransferId, PaymentRegisterCurrencyExchangeId, Updated) " +
            "VALUES " +
            "(@PaymentMovementId, @IncomePaymentOrderId, @OutcomePaymentOrderId, @PaymentRegisterTransferId, @PaymentRegisterCurrencyExchangeId, getutcdate())",
            paymentMovementOperation
        );
    }

    public long Add(PaymentMovement paymentMovement) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [PaymentMovement] (OperationName, Updated) " +
            "VALUES (@OperationName, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            paymentMovement
        ).Single();
    }

    public PaymentMovement GetPaymentMovementByName(string name) {
        return _remoteSyncConnection.Query<PaymentMovement>(
            "SELECT * " +
            "FROM [PaymentMovement] " +
            "WHERE [PaymentMovement].[OperationName] = @Name " +
            "AND [PaymentMovement].Deleted = 0",
            new { Name = name }
        ).SingleOrDefault();
    }

    public IEnumerable<SyncOrderItem> GetAllSyncOrderItems(
        DateTime fromDate,
        DateTime toDate,
        long clientCode,
        string organizationName,
        string agreementName,
        string currencyCode,
        string typePriceName) {
        return _oneCConnection.Query<SyncOrderItem>(
            "SELECT " +
            "T1._Number [DocumentNumber], " +
            "T1._Date_Time [DocumentDate], " +
            "CAST(T9._Code AS bigint) [ProductCode], " +
            "T2._Fld3223 [Qty] " +
            "FROM dbo._Document186 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document186_VT3219 T2 WITH(NOLOCK) " +
            "ON (T1._IDRRef = T2._Document186_IDRRef) " +
            "LEFT OUTER JOIN dbo._Document290 T3 WITH(NOLOCK) " +
            "ON (0x08 = T3._Fld8558_TYPE AND 0x000000BA = T3._Fld8558_RTRef AND T1._IDRRef = T3._Fld8558_RRRef) " +
            "LEFT OUTER JOIN dbo._Reference17 T4 WITH(NOLOCK) " +
            "ON T1._Fld3186RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T5 WITH(NOLOCK) " +
            "ON T1._Fld3191RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T6 WITH(NOLOCK) " +
            "ON T1._Fld3196RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T7 WITH(NOLOCK) " +
            "ON T1._Fld3200RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference143 T8 WITH(NOLOCK) " +
            "ON T1._Fld3207RRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 T9 WITH(NOLOCK) " +
            "ON T2._Fld3226RRef = T9._IDRRef " +
            "WHERE T1._Marked = 0x00 " +
            "AND T1._Date_Time >= @FromDate " +
            "AND T1._Date_Time < @ToDate " +
            "AND T6._Code = @ClientCode " +
            "AND T7._Description = @OrganizationName " +
            "AND T5._Description = @AgreementName " +
            "AND T4._Code = @CurrencyCode " +
            "AND T8._Description = @TypePriceName " +
            "AND T3._IDRRef IS NULL " +
            "AND [T1]._Posted = 0x01 " +
            "AND T2._Fld3226RRef IS NOT NULL",
            new {
                FromDate = fromDate.AddYears(2000),
                ToDate = toDate.AddYears(2000),
                ClientCode = clientCode,
                OrganizationName = organizationName,
                AgreementName = agreementName,
                CurrencyCode = currencyCode,
                TypePriceName = typePriceName
            }
        );
    }

    public IEnumerable<SyncOrderItem> GetAmgAllSyncOrderItems(
        DateTime fromDate,
        DateTime toDate,
        long clientCode,
        string organizationName,
        string agreementName,
        string currencyCode,
        string typePriceName) {
        return _amgSyncConnection.Query<SyncOrderItem>(
            "SELECT " +
            "T2._Number [DocumentNumber], " +
            "T2._Date_Time [DocumentDate], " +
            "T3._Code [ProductCode], " +
            "T1._Fld4026 [Qty] " +
            "FROM dbo._Document229_VT4022 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document229 T2 WITH(NOLOCK) " +
            "ON (T1._Document229_IDRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Document345 T9 WITH(NOLOCK) " +
            "ON (0x08 = T9._Fld10340_TYPE AND 0x000000E5 = T9._Fld10340_RTRef AND T2._IDRRef = T9._Fld10340_RRRef) " +
            "LEFT OUTER JOIN dbo._Reference108 T3 WITH(NOLOCK) " +
            "ON T1._Fld4029RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T4 WITH(NOLOCK) " +
            "ON T2._Fld3989RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference116 T5 WITH(NOLOCK) " +
            "ON T2._Fld3993RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 T6 WITH(NOLOCK) " +
            "ON T2._Fld3984RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 T7 WITH(NOLOCK) " +
            "ON T2._Fld3979RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference171 T8 WITH(NOLOCK) " +
            "ON T2._Fld4000RRef = T8._IDRRef " +
            "WHERE (T2._Marked = 0x00) " +
            "AND T1._Fld4029RRef IS NOT NULL " +
            "AND T9._IDRRef IS NULL " +
            "AND T2._Date_Time >= @FromDate " +
            "AND T2._Date_Time < @ToDate " +
            "AND T4._Code = @ClientCode " +
            "AND T5._Description = @OrganizationName " +
            "AND T6._Description = @AgreementName " +
            "AND T7._Code = @CurrencyCode " +
            "AND [T2]._Posted = 0x01 " +
            "AND T8._Description = @TypePriceName ",
            new {
                FromDate = fromDate.AddYears(2000),
                ToDate = toDate.AddYears(2000),
                ClientCode = clientCode,
                OrganizationName = organizationName,
                AgreementName = agreementName,
                CurrencyCode = currencyCode,
                TypePriceName = typePriceName
            }
        );
    }

    public decimal GetExchangeRateAmountToEuroByDate(long fromCurrencyId, DateTime fromDate) {
        return _remoteSyncConnection.Query<decimal>(
            "DECLARE @ExchangeRate money; " +
            "DECLARE @CrossExchangeRate money; " +
            "DECLARE @InverseCrossExchangeRate money; " +
            "DECLARE @EuroCurrencyId bigint; " +
            " " +
            "SELECT @EuroCurrencyId = (SELECT TOP(1) [Currency].ID FROM [Currency] WHERE [Currency].Deleted = 0 AND [Currency].Code = 'EUR'); " +
            "SELECT @ExchangeRate = " +
            "( " +
            "SELECT TOP(1) IIF([ExchangeRateHistory].Amount IS NOT NULL, [ExchangeRateHistory].Amount, [ExchangeRate].Amount) " +
            "FROM [ExchangeRate] " +
            "LEFT JOIN [ExchangeRateHistory] " +
            "ON [ExchangeRateHistory].ExchangeRateID = [ExchangeRate].ID " +
            "AND [ExchangeRateHistory].Created <= @FromDate " +
            "WHERE [ExchangeRate].CurrencyID = @FromCurrencyId " +
            "AND [ExchangeRate].Code = 'EUR' " +
            "AND [ExchangeRate].Deleted = 0 " +
            "ORDER BY [ExchangeRateHistory].ID DESC " +
            ") " +
            "SELECT @CrossExchangeRate = " +
            "( " +
            "SELECT TOP(1) IIF([CrossExchangeRateHistory].Amount IS NOT NULL, [CrossExchangeRateHistory].Amount, [CrossExchangeRate].Amount) " +
            "FROM [CrossExchangeRate] " +
            "LEFT JOIN [CrossExchangeRateHistory] " +
            "ON [CrossExchangeRateHistory].CrossExchangeRateID = [CrossExchangeRate].ID " +
            "AND [CrossExchangeRateHistory].Created <= @FromDate " +
            "WHERE [CrossExchangeRate].CurrencyFromID = @FromCurrencyId " +
            "AND [CrossExchangeRate].CurrencyToID = @EuroCurrencyId " +
            "AND [CrossExchangeRate].Deleted = 0 " +
            "ORDER BY [CrossExchangeRateHistory].ID DESC " +
            "); " +
            "SELECT @InverseCrossExchangeRate = " +
            "( " +
            "SELECT TOP(1) IIF([CrossExchangeRateHistory].Amount IS NOT NULL, [CrossExchangeRateHistory].Amount, [CrossExchangeRate].Amount) " +
            "FROM [CrossExchangeRate] " +
            "LEFT JOIN [CrossExchangeRateHistory] " +
            "ON [CrossExchangeRateHistory].CrossExchangeRateID = [CrossExchangeRate].ID " +
            "AND [CrossExchangeRateHistory].Created <= @FromDate " +
            "WHERE [CrossExchangeRate].CurrencyFromID = @EuroCurrencyId " +
            "AND [CrossExchangeRate].CurrencyToID = @FromCurrencyId " +
            "AND [CrossExchangeRate].Deleted = 0 " +
            "ORDER BY [CrossExchangeRateHistory].ID DESC " +
            "); " +
            "SELECT " +
            "CASE " +
            "WHEN (@FromCurrencyId = @EuroCurrencyId) " +
            "THEN 1.00 " +
            "WHEN (@ExchangeRate IS NOT NULL) " +
            "THEN @ExchangeRate " +
            "WHEN (@CrossExchangeRate IS NOT NULL) " +
            "THEN @CrossExchangeRate " +
            "WHEN (@InverseCrossExchangeRate IS NOT NULL) " +
            "THEN 0 - @InverseCrossExchangeRate " +
            "ELSE 1.00 " +
            "END ",
            new {
                FromCurrencyId = fromCurrencyId,
                FromDate = fromDate
            }
        ).Single();
    }

    public void CleanDebtsAndBalances() {
        _remoteSyncConnection.Execute(
            "DELETE FROM [ClientInDebt] WHERE ID IN ( " +
            "SELECT [ClientInDebt].ID " +
            "FROM [ClientInDebt] " +
            "LEFT JOIN [Client] " +
            "ON [ClientInDebt].ClientID = Client.ID " +
            "LEFT JOIN [Debt] " +
            "ON [ClientInDebt].DebtID = [Debt].ID " +
            "WHERE [ClientInDebt].Deleted = 0 " +
            "AND [Debt].Total > 0 " +
            "AND ([Client].SourceAmgCode <> 0 " +
            "OR [Client].SourceFenixCode <> 0) " +
            "AND [Client].Deleted = 0 " +
            "); " +
            "DELETE FROM [Debt] WHERE ID IN ( " +
            "SELECT [Debt].ID " +
            "FROM [Debt] " +
            "LEFT JOIN [ClientInDebt] " +
            "ON [ClientInDebt].DebtID = [Debt].ID " +
            "WHERE [ClientInDebt].ID IS NULL " +
            "); " +
            "UPDATE [ClientAgreement] SET CurrentAmount = 0 " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [ClientAgreement].Deleted = 0 " +
            "AND ([Client].SourceAmgCode <> 0 " +
            "OR [Client].SourceFenixCode <> 0) " +
            "AND [Client].Deleted = 0 " +
            "AND CurrentAmount <> 0"
        );
    }

    public IEnumerable<SyncAccounting> GetSyncAccountingFiltered(long clientCode, string agreementName, string organizationName) {
        return _oneCConnection.Query<SyncAccounting>(
            "SELECT " +
            "CASE WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000AF THEN T6._Number WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x000000FF THEN T7._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = " +
            "0x00000123 THEN T8._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000011B THEN T9._Number WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x0000010D THEN T10._Number WHEN T1.Fld12432_TYPE = 0x08 AND " +
            "T1.Fld12432_RTRef = 0x0000011F THEN T11._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000113 THEN T12._Number WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x000000B9 THEN T13._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000116 THEN T14._Number " +
            "WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000FD THEN T15._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000F7 " +
            "THEN T16._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000010F THEN T17._Number WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x00000100 THEN T18._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000A3 THEN T19._Number " +
            "WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000114 THEN T20._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00003CF2 " +
            "THEN T21._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000DE THEN T22._Number WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x000000B0 THEN T23._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000112 THEN T24._Number " +
            "WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000010C THEN T25._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000EF " +
            "THEN T26._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000115 THEN T27._Number WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x0000010E THEN T28._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000F6 THEN T29._Number " +
            "WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000B1 THEN T30._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000122 " +
            "THEN T31._Number WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000A2 THEN T32._Number WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x000000DD THEN T33._Number ELSE CAST(NULL AS NVARCHAR(11)) END [Number], " +
            "DATEADD(YEAR, -2000, CASE WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000AF THEN T6._Date_Time WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x000000FF THEN T7._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = " +
            "0x00000123 THEN T8._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000011B THEN T9._Date_Time WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x0000010D THEN T10._Date_Time WHEN T1.Fld12432_TYPE = " +
            "0x08 AND T1.Fld12432_RTRef = 0x0000011F THEN T11._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000113 THEN T12._Date_Time " +
            "WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000B9 THEN T13._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000116 " +
            "THEN T14._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000FD THEN T15._Date_Time WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x000000F7 THEN T16._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000010F THEN T17._Date_Time " +
            "WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000100 THEN T18._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000A3 " +
            "THEN T19._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000114 THEN T20._Date_Time WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x00003CF2 THEN T21._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000DE THEN T22._Date_Time " +
            "WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000B0 THEN T23._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000112 " +
            "THEN T24._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000010C THEN T25._Date_Time WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x000000EF THEN T26._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000115 THEN T27._Date_Time " +
            "WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000010E THEN T28._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000F6 " +
            "THEN T29._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000B1 THEN T30._Date_Time WHEN T1.Fld12432_TYPE = 0x08 " +
            "AND T1.Fld12432_RTRef = 0x00000122 THEN T31._Date_Time WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000A2 THEN T32._Date_Time " +
            "WHEN T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000DD THEN T33._Date_Time ELSE CAST(NULL AS DATETIME) END) [Date], " +
            "T1.Fld12436Balance_ [Value] " +
            "FROM (SELECT " +
            "T2._Fld12434RRef AS Fld12434RRef, " +
            "T2._Fld12432_TYPE AS Fld12432_TYPE, " +
            "T2._Fld12432_RTRef AS Fld12432_RTRef, " +
            "T2._Fld12432_RRRef AS Fld12432_RRRef, " +
            "T2._Fld12430RRef AS Fld12430RRef, " +
            "T2._Fld12435RRef AS Fld12435RRef, " +
            "CAST(SUM(T2._Fld12436) AS NUMERIC(33, 4)) AS Fld12436Balance_ " +
            "FROM dbo._AccumRgT12438 T2 WITH(NOLOCK) " +
            "WHERE T2._Period = CAST('5999-11-01 00:00:00' AS datetime2(3)) AND (T2._Fld12436 <> 0) AND (T2._Fld12436 <> 0) " +
            "GROUP BY T2._Fld12434RRef, " +
            "T2._Fld12432_TYPE, " +
            "T2._Fld12432_RTRef, " +
            "T2._Fld12432_RRRef, " +
            "T2._Fld12430RRef, " +
            "T2._Fld12435RRef " +
            "HAVING (CAST(SUM(T2._Fld12436) AS NUMERIC(33, 4))) <> 0.0) T1 " +
            "LEFT OUTER JOIN dbo._Reference90 T3 WITH(NOLOCK) " +
            "ON T1.Fld12434RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T4 WITH(NOLOCK) " +
            "ON T1.Fld12435RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T5 WITH(NOLOCK) " +
            "ON T1.Fld12430RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Document175 T6 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000AF AND T1.Fld12432_RRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Document255 T7 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000FF AND T1.Fld12432_RRRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Document291 T8 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000123 AND T1.Fld12432_RRRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Document283 T9 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000011B AND T1.Fld12432_RRRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Document269 T10 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000010D AND T1.Fld12432_RRRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Document287 T11 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000011F AND T1.Fld12432_RRRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Document275 T12 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000113 AND T1.Fld12432_RRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Document185 T13 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000B9 AND T1.Fld12432_RRRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Document278 T14 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000116 AND T1.Fld12432_RRRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Document253 T15 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000FD AND T1.Fld12432_RRRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Document247 T16 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000F7 AND T1.Fld12432_RRRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Document271 T17 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000010F AND T1.Fld12432_RRRef = T17._IDRRef " +
            "LEFT OUTER JOIN dbo._Document256 T18 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000100 AND T1.Fld12432_RRRef = T18._IDRRef " +
            "LEFT OUTER JOIN dbo._Document163 T19 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000A3 AND T1.Fld12432_RRRef = T19._IDRRef " +
            "LEFT OUTER JOIN dbo._Document276 T20 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000114 AND T1.Fld12432_RRRef = T20._IDRRef " +
            "LEFT OUTER JOIN dbo._Document15602 T21 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00003CF2 AND T1.Fld12432_RRRef = T21._IDRRef " +
            "LEFT OUTER JOIN dbo._Document222 T22 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000DE AND T1.Fld12432_RRRef = T22._IDRRef " +
            "LEFT OUTER JOIN dbo._Document176 T23 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000B0 AND T1.Fld12432_RRRef = T23._IDRRef " +
            "LEFT OUTER JOIN dbo._Document274 T24 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000112 AND T1.Fld12432_RRRef = T24._IDRRef " +
            "LEFT OUTER JOIN dbo._Document268 T25 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000010C AND T1.Fld12432_RRRef = T25._IDRRef " +
            "LEFT OUTER JOIN dbo._Document239 T26 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000EF AND T1.Fld12432_RRRef = T26._IDRRef " +
            "LEFT OUTER JOIN dbo._Document277 T27 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000115 AND T1.Fld12432_RRRef = T27._IDRRef " +
            "LEFT OUTER JOIN dbo._Document270 T28 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x0000010E AND T1.Fld12432_RRRef = T28._IDRRef " +
            "LEFT OUTER JOIN dbo._Document246 T29 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000F6 AND T1.Fld12432_RRRef = T29._IDRRef " +
            "LEFT OUTER JOIN dbo._Document177 T30 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000B1 AND T1.Fld12432_RRRef = T30._IDRRef " +
            "LEFT OUTER JOIN dbo._Document290 T31 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x00000122 AND T1.Fld12432_RRRef = T31._IDRRef " +
            "LEFT OUTER JOIN dbo._Document162 T32 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000A2 AND T1.Fld12432_RRRef = T32._IDRRef " +
            "LEFT OUTER JOIN dbo._Document221 T33 WITH(NOLOCK) " +
            "ON T1.Fld12432_TYPE = 0x08 AND T1.Fld12432_RTRef = 0x000000DD AND T1.Fld12432_RRRef = T33._IDRRef " +
            "WHERE T4._Fld1112 = 0x01 " +
            "AND CAST(T4._Code AS bigint) = @ClientCode " +
            "AND T5._Description = @AgreementName " +
            "AND T3._Description = @OrganizationName " +
            "ORDER BY [Date] DESC",
            new {
                ClientCode = clientCode,
                AgreementName = agreementName,
                OrganizationName = organizationName
            }
        );
    }

    public IEnumerable<SyncAccounting> GetAmgSyncAccountingFiltered(long clientCode, string agreementName, string organizationName) {
        return _amgSyncConnection.Query<SyncAccounting>(
            "SELECT " +
            "T1.Fld14891FinalBalance_ [Value], " +
            "CASE " +
            "WHEN (CASE " +
            "WHEN T1.RecorderTRef = 0x00000159 " +
            "THEN T4._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000DC " +
            "THEN T5._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000E0 " +
            "THEN T6._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000147 " +
            "THEN T7._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014A " +
            "THEN T8._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000010D " +
            "THEN T9._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000140 " +
            "THEN T10._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014C " +
            "THEN T11._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000013F " +
            "THEN T12._Date_Time " +
            "ELSE T13._Date_Time " +
            "END END END END END END END END " +
            "END ) = N'2001-01-01 00:00:00.000' " +
            "THEN NULL " +
            "ELSE DATEADD(YEAR, -2000, (CASE " +
            "WHEN T1.RecorderTRef = 0x00000159 " +
            "THEN T4._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000DC " +
            "THEN T5._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000E0 " +
            "THEN T6._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000147 " +
            "THEN T7._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014A " +
            "THEN T8._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000010D " +
            "THEN T9._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000140 " +
            "THEN T10._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014C " +
            "THEN T11._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000013F " +
            "THEN T12._Date_Time " +
            "ELSE T13._Date_Time " +
            "END END END END END END END END END)) " +
            "END [Date], " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000159 " +
            "THEN T4._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000DC " +
            "THEN T5._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000E0 " +
            "THEN T6._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000147 " +
            "THEN T7._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014A " +
            "THEN T8._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000010D " +
            "THEN T9._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000140 " +
            "THEN T10._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014C " +
            "THEN T11._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000013F " +
            "THEN T12._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000DB " +
            "THEN T13._Number " +
            "ELSE '' " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END [Number] " +
            "FROM (SELECT " +
            "T2.Fld14890RRef AS Fld14890RRef, " +
            "T2.Fld14888_TYPE AS Fld14888_TYPE, " +
            "T2.Fld14888_RTRef AS Fld14888_RTRef, " +
            "T2.Fld14888_RRRef AS Fld14888_RRRef, " +
            "T2.Fld14889RRef AS Fld14889RRef, " +
            "T2.Fld14887RRef AS Fld14887RRef, " +
            "T2.RecorderRRef AS RecorderRRef, " +
            "T2.RecorderTRef AS RecorderTRef, " +
            "CAST(T2.Fld14891Balance_ + T2.Fld14891Receipt_ - T2.Fld14891Expense_ AS NUMERIC(35, 8)) AS Fld14891FinalBalance_ " +
            "FROM (SELECT " +
            "T3._Fld14890RRef AS Fld14890RRef, " +
            "T3._Fld14888_TYPE AS Fld14888_TYPE, " +
            "T3._Fld14888_RTRef AS Fld14888_RTRef, " +
            "T3._Fld14888_RRRef AS Fld14888_RRRef, " +
            "T3._Fld14889RRef AS Fld14889RRef, " +
            "T3._Fld14887RRef AS Fld14887RRef, " +
            "T3._RecorderRRef AS RecorderRRef, " +
            "T3._RecorderTRef AS RecorderTRef, " +
            "CAST(0.0 AS NUMERIC(15, 8)) AS Fld14891Balance_, " +
            "CAST(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld14891 ELSE 0.0 END AS NUMERIC(27, 8)) AS Fld14891Receipt_, " +
            "CAST(CASE WHEN T3._RecordKind = 0.0 THEN 0.0 ELSE T3._Fld14891 END AS NUMERIC(27, 8)) AS Fld14891Expense_ " +
            "FROM dbo._AccumRg14886 T3 WITH(NOLOCK) " +
            "WHERE T3._Active = 0x01 " +
            "AND T3._Period = CAST('5999-11-01 00:00:00' AS datetime2(3)) " +
            ") T2) T1 " +
            "LEFT OUTER JOIN dbo._Document345 T4 WITH(NOLOCK) " +
            "ON T4._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document220 T5 WITH(NOLOCK) " +
            "ON T5._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document224 T6 WITH(NOLOCK) " +
            "ON T6._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document327 T7 WITH(NOLOCK) " +
            "ON T7._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document330 T8 WITH(NOLOCK) " +
            "ON T8._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document269 T9 WITH(NOLOCK) " +
            "ON T9._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document320 T10 WITH(NOLOCK) " +
            "ON T10._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document332 T11 WITH(NOLOCK) " +
            "ON T11._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document319 T12 WITH(NOLOCK) " +
            "ON T12._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document219 T13 WITH(NOLOCK) " +
            "ON T13._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Reference116 T14 WITH(NOLOCK) " +
            "ON T1.Fld14889RRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 T15 WITH(NOLOCK) " +
            "ON T1.Fld14887RRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T16 WITH(NOLOCK) " +
            "ON T1.Fld14890RRef = T16._IDRRef " +
            "WHERE T14._Description = @OrganizationName " +
            "AND T15._Description = @AgreementName " +
            "AND T16._Code = @ClientCode " +
            "ORDER BY [Date] DESC ",
            new {
                ClientCode = clientCode,
                AgreementName = agreementName,
                OrganizationName = organizationName
            }
        );
    }

    public void Update(ClientAgreement agreement) {
        _remoteSyncConnection.Execute(
            "UPDATE [ClientAgreement] " +
            "SET CurrentAmount = @CurrentAmount, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            agreement
        );
    }

    public Product GetDevProduct() {
        return _remoteSyncConnection.Query<Product>(
            "SELECT TOP(1) * " +
            "FROM [Product] " +
            "WHERE [Product].VendorCode = N'Борг' " +
            "AND [Product].Deleted = 1"
        ).SingleOrDefault();
    }

    public MeasureUnit GetMeasureUnit() {
        return _remoteSyncConnection.Query<MeasureUnit>(
            "SELECT TOP(1) * FROM [MeasureUnit] WHERE Deleted = 0"
        ).SingleOrDefault();
    }

    public long Add(MeasureUnit measureUnit) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [MeasureUnit] " +
            "([Name], [Description], [CodeOneC], Updated, Deleted) " +
            "VALUES " +
            "(@Name, @Description, @CodeOneC, GETUTCDATE(), @Deleted); " +
            "SELECT SCOPE_IDENTITY()",
            measureUnit
        ).Single();
    }

    public void Add(MeasureUnitTranslation translation) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [MeasureUnitTranslation] " +
            "([Name], [Description], MeasureUnitID, [CultureCode], Updated) " +
            "VALUES " +
            "(@Name, @Description, @MeasureUnitID, @CultureCode, GETUTCDATE())",
            translation
        );
    }

    public long Add(Product product) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO Product (Description, HasAnalogue, HasComponent, HasImage, IsForSale, IsForWeb, IsForZeroSale, MainOriginalNumber, MeasureUnitID, " +
            "Name, OrderStandard, PackingStandard, Size, UCGFEA, VendorCode, Volume, Weight, [Top], NameUA, NamePL, DescriptionUA, DescriptionPL, NotesPL, NotesUA, " +
            "SynonymsUA, SynonymsPL, SearchSynonymsUA, SearchSynonymsPL, SearchName, SearchNameUA, SearchDescription, SearchDescriptionUA, SearchSize, " +
            "SearchVendorCode, SearchNamePL, SearchDescriptionPL, Updated, Deleted) " +
            "VALUES(@Description, @HasAnalogue, @HasComponent, @HasImage, @IsForSale, @IsForWeb, @IsForZeroSale, @MainOriginalNumber, @MeasureUnitID, " +
            "@Name, @OrderStandard, @PackingStandard, @Size, @UCGFEA, @VendorCode, @Volume, @Weight, @Top, @NameUA, @NamePL, @DescriptionUA, @DescriptionPL, " +
            "@NotesPL, @NotesUA, @SynonymsUA, @SynonymsPL, @SearchSynonymsUA, @SearchSynonymsPL, @SearchName, @SearchNameUA, @SearchDescription, @SearchDescriptionUA, " +
            "@SearchSize, @SearchVendorCode, @SearchNamePL, @SearchDescriptionPL, getutcdate(), @Deleted); " +
            "SELECT SCOPE_IDENTITY()",
            product
        ).Single();
    }

    public Sale GetSaleIfExists(long clientAgreementId, string number, DateTime fromDate) {
        return _remoteSyncConnection.Query<Sale>(
            "SELECT TOP(1) [Sale].* " +
            "FROM [Sale] " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "WHERE [Sale].Deleted = 0 " +
            "AND [Sale].ChangedToInvoice = @FromDate " +
            "AND [Sale].ClientAgreementID = @ClientAgreementId " +
            "AND [SaleNumber].[Value] = @Number",
            new {
                ClientAgreementId = clientAgreementId,
                Number = number,
                FromDate = fromDate
            }
        ).SingleOrDefault();
    }

    public IEnumerable<ProductAvailability> GetAvailabilities(
        long productId,
        long organizationId,
        bool vatStoragesFirst) {
        return _remoteSyncConnection.Query<ProductAvailability>(
            "SELECT [ProductAvailability].* " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [ProductAvailability].[Amount] > 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [ProductAvailability].ProductID = @ProductId " +
            "ORDER BY CASE WHEN [Organization].ID = @OrganizationId THEN 0 ELSE 1 END, " +
            "CASE WHEN [Storage].ForVatProducts = @VatStoragesFirst THEN 0 ELSE 1 END",
            new {
                ProductId = productId,
                OrganizationId = organizationId,
                VatStoragesFirst = vatStoragesFirst
            }
        );
    }

    public ProductAvailability GetAvailability(
        long productId,
        long storageId) {
        return _remoteSyncConnection.Query<ProductAvailability>(
            "SELECT * " +
            "FROM [ProductAvailability] " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [ProductAvailability].ProductID = @ProductId " +
            "AND [ProductAvailability].StorageID = @StorageId " +
            "ORDER BY ID DESC",
            new {
                ProductId = productId,
                StorageId = storageId
            }
        ).SingleOrDefault();
    }

    public void Add(ProductAvailability availability) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ProductAvailability] " +
            "([ProductID], [StorageID], [Amount], [Updated]) " +
            "VALUES " +
            "(@ProductId, @StorageId, @Amount, GETUTCDATE())",
            availability
        );
    }

    public void Update(ProductAvailability availability) {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductAvailability] " +
            "SET Amount = @Amount, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            availability
        );
    }

    public void Add(ProductReservation reservation) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ProductReservation] " +
            "([Qty], [OrderItemID], [ProductAvailabilityID], [Updated]) " +
            "VALUES " +
            "(@Qty, @OrderItemId, @ProductAvailabilityId, GETUTCDATE())",
            reservation
        );
    }

    public IEnumerable<SyncOrderSaleItem> GetAmgFilteredSyncOrderSaleItems(
        DateTime fromDate,
        DateTime toDate) {
        IEnumerable<SyncOrderSaleItem> syncOrderSaleItems = _amgSyncConnection.Query<SyncOrderSaleItem>(
            "SELECT " +
            "[Order]._IDRRef [OrderId], " +
            "[Order].[_Date_Time] [OrderDateTime], " +
            "[Order].[_Number] [OrderNumber], " +
            "[Order].[_Fld3987] [OrderComment], " +
            "CASE WHEN [Sale]._IDRRef IS NULL THEN 0 ELSE 1 END [IsSale], " +
            "[Sale].[_Date_Time] [SaleDateTime], " +
            "[Sale].[_Number] [SaleNumber], " +
            "[Sale].[_Fld10339] [SaleComment], " +
            "[Currency]._Code [CurrencyCode], " +
            "[Agreement]._Code [AgreementCode], " +
            "[Client]._Code [ClientCode], " +
            "[Organization]._Description [Organization], " +
            "[Storage]._Description [Storage], " +
            "CASE WHEN [Order]._Fld3998 = 0x01 THEN 1 ELSE 0 END [WithVat], " +
            "[Order]._Fld3999 [TotalValue], " +
            "[Order]._Fld3991 / [Order]._Fld3990 [ExchangeRate], " +
            "[Product]._Code [ProductCode], " +
            "[Product]._Fld1765 [VendorCode], " +
            "[Product]._Description [ProductName], " +
            "[OrderItem]._Fld4026 [Qty], " +
            "[OrderItem]._Fld4037 [UnitPrice], " +
            "[OrderItem]._Fld4034 [Price], " +
            "[OrderItem]._Fld4035 [Vat], " +
            "[TypeVat]._EnumOrder [VatTypeAmg], " +
            "[OrderItem]._Fld4038 [Discount], " +
            "( " +
            "SELECT TOP 1 [Specification].[_Code] [Specification] " +
            "FROM dbo._Reference86 [Specification] WITH(NOLOCK) " +
            "LEFT JOIN dbo._Document345_VT10399 [SaleItem] " +
            "ON [SaleItem]._Fld16062RRef = [Specification]._IDRRef " +
            "LEFT JOIN dbo._Document345 [Sale] " +
            "ON [SaleItem]._Document345_IDRRef = [Sale].[_IDRRef] " +
            "LEFT OUTER JOIN dbo._Reference108 [ProductFromSale] WITH(NOLOCK) " +
            "ON [SaleItem]._Fld10407RRef = [ProductFromSale]._IDRRef " +
            "WHERE [Sale]._Fld10340_RRRef = [Order].[_IDRRef] " +
            "AND [ProductFromSale].[_IDRRef] = [OrderItem]._Fld4029RRef " +
            ") [Specification] " +
            "FROM dbo._Document229 [Order] WITH(NOLOCK) " +
            "LEFT JOIN dbo._Document345 [Sale] " +
            "ON [Sale]._Fld10340_RRRef = [Order]._IDRRef " +
            "AND [Sale].[_Marked] = 0x00 " +
            "AND [Sale]._Posted = 0x01 " +
            "LEFT OUTER JOIN dbo._Reference35 [Currency] WITH(NOLOCK) " +
            "ON [Order]._Fld3979RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 [Agreement] WITH(NOLOCK) " +
            "ON [Order]._Fld3984RRef = [Agreement]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 [Client] WITH(NOLOCK) " +
            "ON [Order]._Fld3989RRef = [Client]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference116 [Organization] WITH(NOLOCK) " +
            "ON [Order]._Fld3993RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference144 [Storage] WITH(NOLOCK) " +
            "ON [Order]._Fld3996_RRRef = [Storage]._IDRRef " +
            "LEFT JOIN dbo._Document229_VT4022 [OrderItem] " +
            "ON [OrderItem].[_Document229_IDRRef] = [Order].[_IDRRef] " +
            "LEFT OUTER JOIN dbo._Reference108 [Product] WITH(NOLOCK) " +
            "ON [OrderItem]._Fld4029RRef = [Product]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum652 [TypeVat] WITH(NOLOCK) " +
            "ON [OrderItem]._Fld4033RRef = [TypeVat]._IDRRef " +
            "LEFT JOIN dbo._Document232_VT4242 [CloseSaleItem] " +
            "ON [CloseSaleItem].[_Fld4244_RRRef] = [Order]._IDRRef " +
            "WHERE [Order].[_Date_Time] >= @From " +
            "AND [Order].[_Date_Time] <= @To " +
            //"AND [Sale].[_Number] LIKE 'КНо00000970' " +
            "AND [Order].[_Marked] = 0x00 " +
            "AND [Order]._Posted = 0x01 " +
            "AND [CloseSaleItem].[_Document232_IDRRef] IS NULL " +
            "ORDER BY [Order].[_Date_Time] ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }, commandTimeout: 3600
        );

        syncOrderSaleItems.ToList().ForEach(item => {
            item.OrderDateTime = TimeZoneInfo.ConvertTimeToUtc(
                item.OrderDateTime,
                TimeZoneInfo.FindSystemTimeZoneById(
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                        ? "FLE Standard Time"
                        : "Central European Standard Time"
                ));

            if (item.SaleDateTime.HasValue)
                item.SaleDateTime = TimeZoneInfo.ConvertTimeToUtc(
                    item.SaleDateTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(
                        CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time"
                    ));
        });

        return syncOrderSaleItems;
    }

    public IEnumerable<SyncOrderSaleItem> GetFilteredSyncOrderSaleItems(
        DateTime fromDate,
        DateTime toDate) {
        IEnumerable<SyncOrderSaleItem> syncOrderSaleItems = _oneCConnection.Query<SyncOrderSaleItem>(
            "SELECT " +
            "[Order]._IDRRef [OrderId], " +
            "[Order].[_Date_Time] [OrderDateTime], " +
            "[Order].[_Number] [OrderNumber], " +
            "[Order].[_Fld3194] [OrderComment], " +
            "CASE WHEN [Sale]._IDRRef IS NULL THEN 0 ELSE 1 END [IsSale], " +
            "[Sale].[_Date_Time] [SaleDateTime], " +
            "[Sale].[_Number] [SaleNumber], " +
            "[Sale].[_Fld8557] [SaleComment], " +
            "[Currency]._Code [CurrencyCode], " +
            "[Agreement]._Code [AgreementCode], " +
            "[Client]._Code [ClientCode], " +
            "[Organization]._Description [Organization], " +
            "[Storage]._Description [Storage], " +
            "[Order]._Fld3198 / [Order]._Fld3197 [ExchangeRate], " +
            "CASE WHEN [Order]._Fld3205 = 0x01 THEN 1 ELSE 0 END [WithVat], " +
            "[Order]._Fld3206 [TotalValue], " +
            "[Product]._Code [ProductCode], " +
            "[Product]._Description [ProductName], " +
            "[Product]._Fld1306 [VendorCode], " +
            "[OrderItem]._Fld3223 [Qty], " +
            "[TypeVat]._EnumOrder [VatTypeFenix], " +
            "[OrderItem]._Fld3231 [Price], " +
            "[OrderItem]._Fld3232 [Vat], " +
            "[OrderItem]._Fld3234 [UnitPrice], " +
            "[OrderItem]._Fld3235 [Discount], " +
            "NULL [Specification] " +
            "FROM dbo._Document186 [Order] WITH(NOLOCK) " +
            "LEFT JOIN dbo._Document290 [Sale] " +
            "ON [Sale].[_Fld8558_RRRef] = [Order].[_IDRRef] " +
            "AND [Sale]._Posted = 0x01 " +
            "AND [Sale].[_Marked] = 0x00 " +
            "LEFT OUTER JOIN dbo._Reference17 [Currency] WITH(NOLOCK) " +
            "ON [Order]._Fld3186RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 [Agreement] WITH(NOLOCK) " +
            "ON [Order]._Fld3191RRef = [Agreement]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 [Client] WITH(NOLOCK) " +
            "ON [Order]._Fld3196RRef = [Client]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 [Organization] WITH(NOLOCK) " +
            "ON [Order]._Fld3200RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 [Storage] WITH(NOLOCK) " +
            "ON [Order]._Fld3203_RRRef = [Storage]._IDRRef " +
            "LEFT JOIN dbo._Document186_VT3219 [OrderItem] " +
            "ON [OrderItem]._Document186_IDRRef = [Order]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 [Product] WITH(NOLOCK) " +
            "ON [OrderItem]._Fld3226RRef = [Product]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum563 [TypeVat] WITH(NOLOCK) " +
            "ON [OrderItem]._Fld3230RRef = [TypeVat]._IDRRef " +
            "LEFT JOIN dbo._Document189_VT3398 [CloseSaleItem] " +
            "ON [CloseSaleItem].[_Fld3400_RRRef] = [Order]._IDRRef " +
            "WHERE [Order].[_Date_Time] >= @From " +
            "AND [Order].[_Date_Time] <= @To " +
            "AND [Order].[_Marked] = 0x00 " +
            "AND [Order]._Posted = 0x01 " +
            "AND [Organization].[_Description] != 'ТОВ «АМГ «КОНКОРД»' " +
            "AND [CloseSaleItem].[_Document189_IDRRef] IS NULL " +
            //"AND [Sale].[_Number] LIKE '00000042583' " +
            "ORDER BY [Order].[_Date_Time] ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }, commandTimeout: 3600
        );

        syncOrderSaleItems.ToList().ForEach(item => {
            item.OrderDateTime = TimeZoneInfo.ConvertTimeToUtc(
                item.OrderDateTime,
                TimeZoneInfo.FindSystemTimeZoneById(
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                        ? "FLE Standard Time"
                        : "Central European Standard Time"
                ));

            if (item.SaleDateTime.HasValue)
                item.SaleDateTime = TimeZoneInfo.ConvertTimeToUtc(
                    item.SaleDateTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(
                        CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time"
                    ));
        });

        return syncOrderSaleItems;
    }

    public IEnumerable<Product> GetProductByCodes(long[] productCodes, bool forAmg) {
        List<Product> toReturn = new();

        int skip = 0;
        int take = 1000;

        while (productCodes.Length > skip) {
            long[] filteredCodes = productCodes.Skip(skip).Take(take).ToArray();

            IEnumerable<Product> products = _remoteSyncConnection.Query<Product>(
                "SELECT * FROM [Product] " +
                (forAmg ? "WHERE [Product].[SourceAmgCode] IN @Codes" : "WHERE [Product].[SourceFenixCode] IN @Codes"),
                new {
                    Codes = filteredCodes
                });

            toReturn.AddRange(products);

            skip += take;
        }

        return toReturn;
    }

    public IEnumerable<Storage> GetAllStorages() {
        return _remoteSyncConnection.Query<Storage>(
            "SELECT * FROM [Storage] ");
    }

    public IEnumerable<SupplyOrganization> GetSupplyOrganizationWithData(long[] supplierCodes, bool forAmg) {
        List<SupplyOrganization> supplyOrganizations = new();

        _remoteSyncConnection.Query<SupplyOrganization, SupplyOrganizationAgreement, Currency, Organization, SupplyOrganization>(
            "SELECT * FROM [SupplyOrganization] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].SupplyOrganizationID = [SupplyOrganization].ID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            //"WHERE [SupplyOrganization].Deleted = 0 " +
            (forAmg ? "WHERE [SupplyOrganization].SourceAmgCode IN @Codes " : "WHERE [SupplyOrganization].SourceFenixCode IN @Codes "),
            (supplyOrganization, agreement, currency, organization) => {
                if (supplyOrganizations.Any(s => s.Id.Equals(supplyOrganization.Id)))
                    supplyOrganization = supplyOrganizations.First(s => s.Id.Equals(supplyOrganization.Id));
                else
                    supplyOrganizations.Add(supplyOrganization);

                if (agreement == null) return supplyOrganization;

                agreement.Currency = currency;
                agreement.Organization = organization;

                supplyOrganization.SupplyOrganizationAgreements.Add(agreement);

                return supplyOrganization;
            },
            new { Codes = supplierCodes });

        return supplyOrganizations;
    }

    public IEnumerable<Client> GetClientsWithData(long[] clientCodes, bool forAmg) {
        List<Client> toReturn = new();

        int skip = 0;
        int take = 1000;

        while (clientCodes.Length > skip) {
            long[] filteredCodes = clientCodes.Skip(skip).Take(take).ToArray();

            List<Client> clients = new();

            _remoteSyncConnection.Query<Client, ClientAgreement, Agreement, Currency, Organization, ClientInRole, ClientType, Client>(
                "SELECT * " +
                "FROM [Client] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [Agreement].CurrencyID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [Agreement].OrganizationID " +
                "LEFT JOIN [ClientInRole] " +
                "ON [ClientInRole].ClientID = [Client].ID " +
                "LEFT JOIN [ClientType] " +
                "ON [ClientType].[ID] = [ClientInRole].[ClientTypeID] " +
                //"WHERE [Client].Deleted = 0 " +
                (forAmg ? "WHERE [Client].SourceAmgCode IN @Codes " : "WHERE [Client].SourceFenixCode IN @Codes")
                ,
                (client, clientAgreement, agreement, currency, organization, clientInRole, clientType) => {
                    if (clients.Any(c => c.Id.Equals(client.Id)))
                        client = clients.First(c => c.Id.Equals(client.Id));
                    else
                        clients.Add(client);

                    clientInRole.ClientType = clientType;

                    client.ClientInRole = clientInRole;

                    if (clientAgreement == null || agreement == null || currency == null || organization == null) return client;

                    agreement.Currency = currency;
                    agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;

                    client.ClientAgreements.Add(clientAgreement);

                    return client;
                }, new { Codes = filteredCodes }
            );

            toReturn.AddRange(clients);

            skip += take;
        }

        return toReturn;
    }

    public IEnumerable<SyncIncomeCashBankOrder> GetAmgFilteredSyncIncomeCashOrders(
        DateTime fromDate,
        DateTime toDate) {
        return _amgSyncConnection.Query<SyncIncomeCashBankOrder>(
            "SELECT " +
            "[IncomeCashOrder]._IDRRef [OrderId], " +
            "[IncomeCashOrder]._Number [Number], " +
            "[IncomeCashOrder]._Date_Time [FromDate], " +
            "[Organization]._Description [Organization], " +
            "[PaymentRegister]._Description [CashPaymentRegister], " +
            "[OrganizationPaymentRegister].[_Description] [OrganizationPaymentRegister], " +
            "CASE WHEN [IncomeCashOrder]._Fld9654 = 0x01 THEN 1 ELSE 0 END [IsManagementAccounting], " +
            "CASE WHEN [IncomeCashOrder]._Fld9655 = 0x01 THEN 1 ELSE 0 END [IsAccounting], " +
            "CASE " +
            "WHEN T4._EnumOrder = 0 THEN 0 " +
            "WHEN T4._EnumOrder = 1 THEN 7 " +
            "WHEN T4._EnumOrder = 2 THEN 9 " +
            "WHEN T4._EnumOrder = 3 THEN 1 " +
            "WHEN T4._EnumOrder = 4 THEN 11 " +
            "WHEN T4._EnumOrder = 5 THEN 7 " +
            "WHEN T4._EnumOrder = 6 THEN 3 " +
            "WHEN T4._EnumOrder = 7 THEN 7 " +
            "ELSE 7 END [TypeOperation], " +
            "CASE " +
            "WHEN [Client1]._Code IS NOT NULL THEN [Client1]._Code " +
            "WHEN [Client2]._Code IS NOT NULL THEN [Client2]._Code " +
            "WHEN [Client3]._Code IS NOT NULL THEN [Client3]._Code " +
            "WHEN [Client4]._Code IS NOT NULL THEN [Client4]._Code " +
            "ELSE CAST(NULL AS NVARCHAR(10)) " +
            "END [ClientCode], " +
            "[Agreement]._Code [AgreementCode], " +
            "[Currency]._Code [CurrencyCode], " +
            "[IncomeCashOrder]._Fld9642 [TotalValue], " +
            "[IncomeCashOrder]._Fld9644 [FromUser], " +
            "[User]._Description [Responsible], " +
            "[IncomeCashOrder]._Fld9652 [Comment], " +
            "(SELECT SUM([DocumentItems]._Fld9696) " +
            "FROM dbo._Document332_VT9687 [DocumentItems] " +
            "WHERE [DocumentItems]._Document332_IDRRef = [IncomeCashOrder]._IDRRef) [TotalVat], " +
            "(SELECT TOP 1 [TypeVat]._EnumOrder " +
            "FROM dbo._Document332_VT9687 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Enum652 [TypeVat] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld9695RRef = [TypeVat]._IDRRef " +
            "WHERE [DocumentItems]._Document332_IDRRef = [IncomeCashOrder]._IDRRef) [VatTypeAmg], " +
            "(SELECT TOP 1 [ArticlesOfMoneyAccounts]._Description " +
            "FROM dbo._Document332_VT9687 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Reference155 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld9697RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "WHERE [DocumentItems]._Document332_IDRRef = [IncomeCashOrder]._IDRRef) [ArticlesOfMoneyAccounts] " +
            "FROM dbo._Document332 [IncomeCashOrder] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference116 [Organization] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9634RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference76 [PaymentRegister] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9635RRef = [PaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference33 [OrganizationPaymentRegister] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9657RRef = [OrganizationPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum483 T4 WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9637RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference77 [Client1] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9638_RRRef = [Client1]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 [Client2] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9638_RRRef = [Client2]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference144 [Client3] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9638_RRRef = [Client3]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference179 [Client4] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9638_RRRef = [Client4]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 [Agreement] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9639RRef = [Agreement]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 [Currency] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9641RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference126 [User] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld9651RRef = [User]._IDRRef " +
            "WHERE ([IncomeCashOrder]._Marked = 0x00) " +
            "AND [IncomeCashOrder].[_Date_Time] <= @To " +
            "AND [IncomeCashOrder].[_Date_Time] >= @From " +
            "AND [IncomeCashOrder]._Posted = 0x01 " +
            "ORDER BY ([IncomeCashOrder]._Date_Time) ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }
        );
    }

    public IEnumerable<SyncIncomeCashBankOrder> GetFilteredSyncIncomeCashOrders(
        DateTime fromDate,
        DateTime toDate) {
        return _oneCConnection.Query<SyncIncomeCashBankOrder>(
            "SELECT " +
            "[IncomeCashOrder]._IDRRef [OrderId], " +
            "[IncomeCashOrder]._Number [Number], " +
            "[IncomeCashOrder]._Date_Time [FromDate], " +
            "[Organization]._Description [Organization], " +
            "[PaymentRegister]._Description [CashPaymentRegister], " +
            "[OrganizationPaymentRegister].[_Description] [OrganizationPaymentRegister], " +
            "CASE WHEN [IncomeCashOrder]._Fld8225 = 0x01 THEN 1 ELSE 0 END [IsManagementAccounting], " +
            "CASE WHEN [IncomeCashOrder]._Fld8226 = 0x01 THEN 1 ELSE 0 END [IsAccounting], " +
            "CASE " +
            "WHEN T4._EnumOrder = 0 THEN 0 " +
            "WHEN T4._EnumOrder = 1 THEN 7 " +
            "WHEN T4._EnumOrder = 2 THEN 9 " +
            "WHEN T4._EnumOrder = 3 THEN 1 " +
            "WHEN T4._EnumOrder = 4 THEN 11 " +
            "WHEN T4._EnumOrder = 5 THEN 7 " +
            "WHEN T4._EnumOrder = 6 THEN 3 " +
            "WHEN T4._EnumOrder = 7 THEN 7 " +
            "ELSE 7 END [TypeOperation], " +
            "CASE " +
            "WHEN [Client1]._Code IS NOT NULL THEN [Client1]._Code " +
            "WHEN [Client2]._Code IS NOT NULL THEN [Client2]._Code " +
            "WHEN [Client3]._Code IS NOT NULL THEN [Client3]._Code " +
            "WHEN [Client4]._Code IS NOT NULL THEN [Client4]._Code " +
            "ELSE CAST(NULL AS NVARCHAR(10)) " +
            "END [ClientCode], " +
            "[Agreement]._Code [AgreementCode], " +
            "[Currency]._Code [CurrencyCode], " +
            "[IncomeCashOrder]._Fld8213 [TotalValue], " +
            "[IncomeCashOrder]._Fld8215 [FromUser], " +
            "[User]._Description [Responsible], " +
            "[IncomeCashOrder]._Fld8223 [Comment], " +
            "(SELECT SUM([DocumentItems]._Fld8266) " +
            "FROM dbo._Document283_VT8257 [DocumentItems] " +
            "WHERE [DocumentItems]._Document283_IDRRef = [IncomeCashOrder]._IDRRef) [TotalVat], " +
            "(SELECT TOP 1 [TypeVat]._EnumOrder " +
            "FROM dbo._Document283_VT8257 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Enum563 [TypeVat] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld8265RRef = [TypeVat]._IDRRef " +
            "WHERE [DocumentItems]._Document283_IDRRef = [IncomeCashOrder]._IDRRef) [VatTypeFenix], " +
            "(SELECT TOP 1 [ArticlesOfMoneyAccounts]._Description " +
            "FROM dbo._Document283_VT8257 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Reference127 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld8267RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "WHERE [DocumentItems]._Document283_IDRRef = [IncomeCashOrder]._IDRRef) [ArticlesOfMoneyAccounts] " +
            "FROM dbo._Document283 [IncomeCashOrder] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference90 [Organization] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8205RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference56 [PaymentRegister] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8206RRef = [PaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 [OrganizationPaymentRegister] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8228RRef = [OrganizationPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum407 T4 WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8208RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference57 [Client1] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8209_RRRef = [Client1]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 [Client2] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8209_RRRef = [Client2]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 [Client3] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8209_RRRef = [Client3]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference151 [Client4] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8209_RRRef = [Client4]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 [Agreement] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8210RRef = [Agreement]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 [Currency] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8212RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 [User] WITH(NOLOCK) " +
            "ON [IncomeCashOrder]._Fld8222RRef = [User]._IDRRef " +
            "WHERE ([IncomeCashOrder]._Marked = 0x00) " +
            "AND [IncomeCashOrder].[_Date_Time] <= @To " +
            "AND [IncomeCashOrder].[_Date_Time] >= @From " +
            "AND [Organization].[_Description] != 'ТОВ «АМГ «КОНКОРД»' " +
            "AND [IncomeCashOrder]._Posted = 0x01 " +
            "ORDER BY ([IncomeCashOrder]._Date_Time) ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }
        );
    }

    public IEnumerable<SyncIncomeCashBankOrder> GetAmgFilteredSyncIncomeBankOrders(
        DateTime fromDate,
        DateTime toDate) {
        return _amgSyncConnection.Query<SyncIncomeCashBankOrder>(
            "SELECT " +
            "[IncomeBankOrder]._IDRRef [OrderId], " +
            "[IncomeBankOrder]._Number [Number], " +
            "[IncomeBankOrder]._Date_Time [FromDate], " +
            "[IncomeBankOrder]._Fld8438 [DocumentArrivalDate], " +
            "[IncomeBankOrder]._Fld8439 [DocumentArrivalNumber], " +
            "[Organization]._Description [Organization], " +
            "[OrganizationPaymentRegister]._Description [OrganizationPaymentRegister], " +
            "[Client]._Code [ClientCode], " +
            "[IncomeBankOrder]._Fld8443 [PaymentPurpose], " +
            "[ClientPaymentRegister]._Description [ClientPaymentRegister], " +
            "[Agreement]._Code [AgreementCode], " +
            "[IncomeBankOrder]._Fld8446 [TotalValue], " +
            "[Currency]._Code [CurrencyCode], " +
            "CASE " +
            "WHEN T8._EnumOrder = 0 THEN 0 " +
            "WHEN T8._EnumOrder = 1 THEN 1 " +
            "WHEN T8._EnumOrder = 2 THEN 7 " +
            "WHEN T8._EnumOrder = 3 THEN 3 " +
            "WHEN T8._EnumOrder = 4 THEN 7 " +
            "WHEN T8._EnumOrder = 5 THEN 11 " +
            "WHEN T8._EnumOrder = 6 THEN 3 " +
            "WHEN T8._EnumOrder = 7 THEN 7 " +
            "ELSE 7 END [TypeOperation], " +
            "[IncomeBankOrder]._Fld8451 [PaymentDate], " +
            "[IncomeBankOrder]._Fld8453 [Comment], " +
            "[User]._Description [Responsible], " +
            "CASE WHEN [IncomeBankOrder]._Fld8449 = 0x01 THEN 1  ELSE 0 END [IsManagementAccounting], " +
            "CASE WHEN [IncomeBankOrder]._Fld8455 = 0x01 THEN 1  ELSE 0 END [IsAccounting], " +
            "(SELECT SUM([DocumentItems]._Fld8484) " +
            "FROM dbo._Document319_VT8475 [DocumentItems] " +
            "WHERE [DocumentItems]._Document319_IDRRef = [IncomeBankOrder]._IDRRef) [TotalVat], " +
            "(SELECT TOP 1 [TypeVat]._EnumOrder " +
            "FROM dbo._Document319_VT8475 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Enum652 [TypeVat] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld8483RRef = [TypeVat]._IDRRef " +
            "WHERE [DocumentItems]._Document319_IDRRef = [IncomeBankOrder]._IDRRef) [VatTypeAmg], " +
            "(SELECT TOP 1 [ArticlesOfMoneyAccounts]._Description " +
            "FROM dbo._Document319_VT8475 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Reference155 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld8485RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "WHERE [DocumentItems]._Document319_IDRRef = [IncomeBankOrder]._IDRRef) [ArticlesOfMoneyAccounts] " +
            "FROM dbo._Document319 [IncomeBankOrder] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference116 [Organization] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld8440RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference33 [OrganizationPaymentRegister] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld8441RRef = [OrganizationPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 [Client] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld8442RRef = [Client]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference33 [ClientPaymentRegister] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld8444RRef = [ClientPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 [Agreement] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld8445RRef = [Agreement]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 [Currency] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld8447RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum486 T8 WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld8448RRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference126 [User] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld8454RRef = [User]._IDRRef " +
            "WHERE ([IncomeBankOrder]._Marked = 0x00) " +
            "AND [IncomeBankOrder].[_Date_Time] <= @To " +
            "AND [IncomeBankOrder].[_Date_Time] >= @From " +
            "AND [IncomeBankOrder]._Posted = 0x01 " +
            "ORDER BY ([IncomeBankOrder]._Date_Time) ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }
        );
    }

    public IEnumerable<SyncIncomeCashBankOrder> GetFilteredSyncIncomeBankOrders(
        DateTime fromDate,
        DateTime toDate) {
        return _oneCConnection.Query<SyncIncomeCashBankOrder>(
            "SELECT " +
            "[IncomeBankOrder]._IDRRef [OrderId], " +
            "[IncomeBankOrder]._Number [Number], " +
            "[IncomeBankOrder]._Date_Time [FromDate], " +
            "[IncomeBankOrder]._Fld6885 [DocumentArrivalDate], " +
            "[IncomeBankOrder]._Fld6886 [DocumentArrivalNumber], " +
            "[Organization]._Description [Organization], " +
            "[OrganizationPaymentRegister]._Description [OrganizationPaymentRegister], " +
            "[Client]._Code [ClientCode], " +
            "[IncomeBankOrder]._Fld6890 [PaymentPurpose], " +
            "[ClientPaymentRegister]._Description [ClientPaymentRegister], " +
            "[Agreement]._Code [AgreementCode], " +
            "[IncomeBankOrder]._Fld6893 [TotalValue], " +
            "[Currency]._Code [CurrencyCode], " +
            "CASE " +
            "WHEN T8._EnumOrder = 0 THEN 0 " +
            "WHEN T8._EnumOrder = 1 THEN 1 " +
            "WHEN T8._EnumOrder = 2 THEN 7 " +
            "WHEN T8._EnumOrder = 3 THEN 3 " +
            "WHEN T8._EnumOrder = 4 THEN 7 " +
            "WHEN T8._EnumOrder = 5 THEN 11 " +
            "WHEN T8._EnumOrder = 6 THEN 3 " +
            "WHEN T8._EnumOrder = 7 THEN 7 " +
            "ELSE 7 END [TypeOperation], " +
            "[IncomeBankOrder]._Fld6898 [PaymentDate], " +
            "[IncomeBankOrder]._Fld6900 [Comment], " +
            "[User]._Description [Responsible], " +
            "CASE WHEN [IncomeBankOrder]._Fld6896 = 0x01 THEN 1  ELSE 0 END [IsManagementAccounting], " +
            "CASE WHEN [IncomeBankOrder]._Fld6902 = 0x01 THEN 1  ELSE 0 END [IsAccounting], " +
            "(SELECT SUM([DocumentItems]._Fld6931) " +
            "FROM dbo._Document268_VT6922 [DocumentItems] " +
            "WHERE [DocumentItems]._Document268_IDRRef = [IncomeBankOrder]._IDRRef) [TotalVat], " +
            "(SELECT TOP 1 [TypeVat]._EnumOrder " +
            "FROM dbo._Document268_VT6922 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Enum563 [TypeVat] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld6930RRef = [TypeVat]._IDRRef " +
            "WHERE [DocumentItems]._Document268_IDRRef = [IncomeBankOrder]._IDRRef) [VatTypeFenix], " +
            "(SELECT TOP 1 [ArticlesOfMoneyAccounts]._Description " +
            "FROM dbo._Document268_VT6922 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Reference127 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld6932RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "WHERE [DocumentItems]._Document268_IDRRef = [IncomeBankOrder]._IDRRef) [ArticlesOfMoneyAccounts] " +
            "FROM dbo._Document268 [IncomeBankOrder] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference90 [Organization] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld6887RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 [OrganizationPaymentRegister] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld6888RRef = [OrganizationPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 [Client] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld6889RRef = [Client]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 [ClientPaymentRegister] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld6891RRef = [ClientPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 [Agreement] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld6892RRef = [Agreement]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 [Currency] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld6894RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum410 T8 WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld6895RRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 [User] WITH(NOLOCK) " +
            "ON [IncomeBankOrder]._Fld6901RRef = [User]._IDRRef " +
            "WHERE ([IncomeBankOrder]._Marked = 0x00) " +
            "AND [IncomeBankOrder].[_Date_Time] <= @To " +
            "AND [IncomeBankOrder].[_Date_Time] >= @From " +
            "AND [Organization].[_Description] != 'ТОВ «АМГ «КОНКОРД»' " +
            "AND [IncomeBankOrder]._Posted = 0x01 " +
            "ORDER BY ([IncomeBankOrder]._Date_Time) ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }
        );
    }


    public IEnumerable<SyncOutcomeCashBankOrder> GetAmgFilteredSyncOutcomeCashOrders(
        DateTime fromDate,
        DateTime toDate) {
        return _amgSyncConnection.Query<SyncOutcomeCashBankOrder>(
            "SELECT " +
            "[OutcomeCashOrder]._IDRRef [OrderId], " +
            "[OutcomeCashOrder]._Number [Number], " +
            "[OutcomeCashOrder]._Date_Time [FromDate], " +
            "[Organization]._Description [Organization], " +
            "[IncomeCash]._Description [CashPaymentRegister], " +
            "CASE " +
            "WHEN [TypeOperation]._EnumOrder = 0 THEN 5 " +
            "WHEN [TypeOperation]._EnumOrder = 1 THEN 6 " +
            "WHEN [TypeOperation]._EnumOrder = 2 THEN 11 " +
            "WHEN [TypeOperation]._EnumOrder = 3 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 4 THEN 10 " +
            "WHEN [TypeOperation]._EnumOrder = 5 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 6 THEN 11 " +
            "WHEN [TypeOperation]._EnumOrder = 7 THEN 8 " +
            "WHEN [TypeOperation]._EnumOrder = 8 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 9 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 10 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 11 THEN 7 " +
            "ELSE 7 END [TypeOperation], " +
            "[OutcomeCashOrder]._Fld10137 [Comment], " +
            "CASE " +
            "WHEN [Client1]._Code IS NOT NULL THEN [Client1]._Code " +
            "WHEN [Client2]._Code IS NOT NULL THEN [Client2]._Code " +
            "WHEN [Client3]._Code IS NOT NULL THEN [Client3]._Code " +
            "WHEN [Client4]._Code IS NOT NULL THEN [Client4]._Code " +
            "WHEN [Client5]._Code IS NOT NULL THEN [Client5]._Code " +
            "ELSE CAST(NULL AS NVARCHAR(10)) " +
            "END [ClientCode], " +
            "[Storage]._Description [StorageName], " +
            "[User]._Description [UserName], " +
            "[User]._Description [EmployeeName], " +
            "[Agreement]._Code [AgreementCode], " +
            "[Currency]._Code [CurrencyCode], " +
            "[OutcomeCashOrder]._Fld10142 [TotalValue], " +
            "[Responsible]._Description [Responsible], " +
            "[PaymentRegisterOrganization]._Description [PaymentRegisterOrganization], " +
            "CASE WHEN [OutcomeCashOrder]._Fld10154 = 0x01 THEN 1 ELSE 0 END [IsManagementAccounting], " +
            "CASE WHEN [OutcomeCashOrder]._Fld10155 = 0x01 THEN 1 ELSE 0 END [IsAccounting], " +
            "(SELECT SUM([DocumentItems]._Fld10185) " +
            "FROM dbo._Document340_VT10176 [DocumentItems] " +
            "WHERE [DocumentItems]._Document340_IDRRef = [OutcomeCashOrder]._IDRRef) [TotalVat], " +
            "CASE WHEN [VatType]._EnumOrder IS NOT NULL THEN [VatType]._EnumOrder ELSE (SELECT TOP 1 [TypeVat]._EnumOrder " +
            "FROM dbo._Document340_VT10176 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Enum652 [TypeVat] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld10184RRef = [TypeVat]._IDRRef " +
            "WHERE [DocumentItems]._Document340_IDRRef = [OutcomeCashOrder]._IDRRef) END [VatTypeAmg], " +
            "CASE WHEN [ArticlesOfMoneyAccounts].[_Description] IS NOT NULL THEN [ArticlesOfMoneyAccounts].[_Description] ELSE " +
            "(SELECT TOP 1 [ArticlesOfMoneyAccounts]._Description " +
            "FROM dbo._Document340_VT10176 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Reference155 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld10186RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "WHERE [DocumentItems]._Document340_IDRRef = [OutcomeCashOrder]._IDRRef) END [ArticlesOfMoneyAccounts] " +
            "FROM dbo._Document340 [OutcomeCashOrder] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference116 [Organization] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10133RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference76 [IncomeCash] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10134RRef = [IncomeCash]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum502 [TypeOperation] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10136RRef = [TypeOperation]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 [Client1] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10138_RRRef = [Client1]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference77 [Client2] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10138_RRRef = [Client2]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference147 [Client3] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10138_RRRef = [Client3]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference144 [Client4] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10138_RRRef = [Client4]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference179 [Client5] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10138_RRRef = [Client5]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 [Storage] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10138_RRRef = [Storage]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference179 [User] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10138_RRRef = [User]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 [Agreement] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10139RRef = [Agreement]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 [Currency] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10141RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference126 [Responsible] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10152RRef = [Responsible]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference33 [PaymentRegisterOrganization] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10157RRef = [PaymentRegisterOrganization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference155 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10156RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum652 [VatType] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld10165RRef = [VatType]._IDRRef " +
            "WHERE ([OutcomeCashOrder]._Marked = 0x00) " +
            "AND [OutcomeCashOrder]._Posted = 0x01 " +
            "AND [OutcomeCashOrder].[_Date_Time] >= @From " +
            "AND [OutcomeCashOrder].[_Date_Time] <= @To " +
            "ORDER BY ([OutcomeCashOrder]._Date_Time) ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }
        );
    }

    public IEnumerable<SyncOutcomeCashBankOrder> GetFilteredSyncOutcomeCashOrders(
        DateTime fromDate,
        DateTime toDate) {
        return _oneCConnection.Query<SyncOutcomeCashBankOrder>(
            "SELECT " +
            "[OutcomeCashOrder]._IDRRef [OrderId], " +
            "[OutcomeCashOrder]._Number [Number], " +
            "[OutcomeCashOrder]._Date_Time [FromDate], " +
            "[Organization]._Description [Organization], " +
            "[IncomeCash]._Description [CashPaymentRegister], " +
            "CASE " +
            "WHEN [TypeOperation]._EnumOrder = 0 THEN 5 " +
            "WHEN [TypeOperation]._EnumOrder = 1 THEN 6 " +
            "WHEN [TypeOperation]._EnumOrder = 2 THEN 11 " +
            "WHEN [TypeOperation]._EnumOrder = 3 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 4 THEN 10 " +
            "WHEN [TypeOperation]._EnumOrder = 5 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 6 THEN 11 " +
            "WHEN [TypeOperation]._EnumOrder = 7 THEN 8 " +
            "WHEN [TypeOperation]._EnumOrder = 8 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 9 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 10 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 11 THEN 7 " +
            "ELSE 7 END [TypeOperation], " +
            "[OutcomeCashOrder]._Fld8407 [Comment], " +
            "CASE " +
            "WHEN [Client1]._Code IS NOT NULL THEN [Client1]._Code " +
            "WHEN [Client2]._Code IS NOT NULL THEN [Client2]._Code " +
            "WHEN [Client3]._Code IS NOT NULL THEN [Client3]._Code " +
            "WHEN [Client4]._Code IS NOT NULL THEN [Client4]._Code " +
            "WHEN [Client5]._Code IS NOT NULL THEN [Client5]._Code " +
            "ELSE CAST(NULL AS NVARCHAR(10)) " +
            "END [ClientCode], " +
            "[Storage]._Description [StorageName], " +
            "[User]._Description [UserName], " +
            "[User]._Description [EmployeeName], " +
            "[Agreement]._Code [AgreementCode], " +
            "[Currency]._Code [CurrencyCode], " +
            "[OutcomeCashOrder]._Fld8412 [TotalValue], " +
            "[Responsible]._Description [Responsible], " +
            "[PaymentRegisterOrganization]._Description [PaymentRegisterOrganization], " +
            "CASE WHEN [OutcomeCashOrder]._Fld8424 = 0x01 THEN 1 ELSE 0 END [IsManagementAccounting], " +
            "CASE WHEN [OutcomeCashOrder]._Fld8425 = 0x01 THEN 1 ELSE 0 END [IsAccounting], " +
            "(SELECT SUM([DocumentItems]._Fld8454) " +
            "FROM dbo._Document287_VT8445 [DocumentItems] " +
            "WHERE [DocumentItems]._Document287_IDRRef = [OutcomeCashOrder]._IDRRef) [TotalVat], " +
            "CASE WHEN [VatType].[_EnumOrder] IS NOT NULL THEN [VatType].[_EnumOrder] ELSE " +
            "(SELECT TOP 1 [TypeVat]._EnumOrder " +
            "FROM dbo._Document287_VT8445 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Enum563 [TypeVat] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld8453RRef = [TypeVat]._IDRRef " +
            "WHERE [DocumentItems]._Document287_IDRRef = [OutcomeCashOrder]._IDRRef) END [VatTypeFenix], " +
            "CASE WHEN [ArticlesOfMoneyAccounts].[_Description] IS NOT NULL THEN [ArticlesOfMoneyAccounts].[_Description] ELSE " +
            "(SELECT TOP 1 [ArticlesOfMoneyAccounts]._Description " +
            "FROM dbo._Document287_VT8445 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Reference127 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld8455RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "WHERE [DocumentItems]._Document287_IDRRef = [OutcomeCashOrder]._IDRRef) END [ArticlesOfMoneyAccounts] " +
            "FROM dbo._Document287 [OutcomeCashOrder] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference90 [Organization] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8403RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference56 [IncomeCash] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8404RRef = [IncomeCash]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum426 [TypeOperation] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8406RRef = [TypeOperation]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference57 [Client1] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8408_RRRef = [Client1]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference120 [Client2] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8408_RRRef = [Client2]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 [Client3] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8408_RRRef = [Client3]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 [Client4] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8408_RRRef = [Client4]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference151 [Client5] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8408_RRRef = [Client5]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 [Storage] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8408_RRRef = [Storage]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference151 [User] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8408_RRRef = [User]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 [Agreement] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8409RRef = [Agreement]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 [Currency] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8411RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 [Responsible] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8422RRef = [Responsible]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 [PaymentRegisterOrganization] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8428RRef = [PaymentRegisterOrganization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference127 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8426RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum563 [VatType] WITH(NOLOCK) " +
            "ON [OutcomeCashOrder]._Fld8436RRef = [VatType]._IDRRef " +
            "WHERE ([OutcomeCashOrder]._Marked = 0x00) " +
            "AND [OutcomeCashOrder]._Posted = 0x01 " +
            "AND [OutcomeCashOrder].[_Date_Time] >= @From " +
            "AND [OutcomeCashOrder].[_Date_Time] <= @To " +
            "AND [Organization].[_Description] != 'ТОВ «АМГ «КОНКОРД»' " +
            "ORDER BY ([OutcomeCashOrder]._Date_Time) ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }
        );
    }


    public IEnumerable<SyncOutcomeCashBankOrder> GetAmgFilteredSyncOutcomeBankOrders(
        DateTime fromDate,
        DateTime toDate) {
        return _amgSyncConnection.Query<SyncOutcomeCashBankOrder>(
            "SELECT " +
            "[OutcomeBankOrder]._IDRRef [OrderId], " +
            "[OutcomeBankOrder]._Number [Number], " +
            "[OutcomeBankOrder]._Date_Time [FromDate], " +
            "[Organization]._Description [Organization], " +
            "[OrganizationPaymentRegister]._Description [PaymentRegisterOrganization], " +
            "[Client]._Code [ClientCode], " +
            "[ClientPaymentRegister]._Description [PaymentRegisterClient], " +
            "[Agreement]._Code [AgreementCode], " +
            "[OutcomeBankOrder]._Fld8532 [PaymentPurpose], " +
            "[OutcomeBankOrder]._Fld8533 [TotalValue], " +
            "[Currency]._Code [CurrencyCode], " +
            "CASE WHEN [OutcomeBankOrder]._Fld8536 = 0x01 THEN 1 ELSE 0 END [IsAccounting], " +
            "CASE WHEN [OutcomeBankOrder]._Fld8537 = 0x01 THEN 1 ELSE 0 END [IsManagementAccounting], " +
            "[OutcomeBankOrder]._Fld8540 [Comment], " +
            "[Responsible]._Description [Responsible], " +
            "CASE " +
            "WHEN [TypeOperation]._EnumOrder = 0 THEN 5 " +
            "WHEN [TypeOperation]._EnumOrder = 1 THEN 6 " +
            "WHEN [TypeOperation]._EnumOrder = 2 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 3 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 4 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 5 THEN 11 " +
            "WHEN [TypeOperation]._EnumOrder = 6 THEN 8 " +
            "WHEN [TypeOperation]._EnumOrder = 7 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 8 THEN 11 " +
            "WHEN [TypeOperation]._EnumOrder = 9 THEN 10 " +
            "ELSE 7 END [TypeOperation], " +
            "[OutcomeBankOrder]._Fld8527 [PaymentDate], " +
            "(SELECT SUM([DocumentItems]._Fld8573) " +
            "FROM dbo._Document320_VT8564 [DocumentItems] " +
            "WHERE [DocumentItems]._Document320_IDRRef = [OutcomeBankOrder]._IDRRef) [TotalVat], " +
            "(SELECT TOP 1 [TypeVat]._EnumOrder " +
            "FROM dbo._Document320_VT8564 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Enum652 [TypeVat] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld8572RRef = [TypeVat]._IDRRef " +
            "WHERE [DocumentItems]._Document320_IDRRef = [OutcomeBankOrder]._IDRRef) [VatTypeAmg], " +
            "CASE WHEN [ArticlesOfMoneyAccounts].[_Description] IS NOT NULL THEN [ArticlesOfMoneyAccounts].[_Description] ELSE (SELECT TOP 1 [ArticlesOfMoneyAccounts]._Description " +
            "FROM dbo._Document320_VT8564 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Reference155 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld8574RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "WHERE [DocumentItems]._Document320_IDRRef = [OutcomeBankOrder]._IDRRef) END [ArticlesOfMoneyAccounts], " +
            "[Employee]._Description [EmployeeName] " +
            "FROM dbo._Document320 [OutcomeBankOrder] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference116 [Organization] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld8525RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference33 [OrganizationPaymentRegister] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld8528RRef = [OrganizationPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 [Client] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld8529RRef = [Client]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference33 [ClientPaymentRegister] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld8530RRef = [ClientPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 [Agreement] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld8531RRef = [Agreement]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 [Currency] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld8534RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference126 [Responsible] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld8541RRef = [Responsible]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum489 [TypeOperation] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld8542RRef = [TypeOperation]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference179 [Employee] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld8550RRef = [Employee]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference155 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld8543RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "WHERE ([OutcomeBankOrder]._Marked = 0x00) " +
            "AND [OutcomeBankOrder]._Posted = 0x01 " +
            "AND [OutcomeBankOrder].[_Date_Time] >= @From " +
            "AND [OutcomeBankOrder].[_Date_Time] <= @To " +
            "ORDER BY ([OutcomeBankOrder]._Date_Time) ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }
        );
    }

    public IEnumerable<SyncOutcomeCashBankOrder> GetFilteredSyncOutcomeBankOrders(
        DateTime fromDate,
        DateTime toDate) {
        return _oneCConnection.Query<SyncOutcomeCashBankOrder>(
            "SELECT " +
            "[OutcomeBankOrder]._IDRRef [OrderId], " +
            "[OutcomeBankOrder]._Number [Number], " +
            "[OutcomeBankOrder]._Date_Time [FromDate], " +
            "[Organization]._Description [Organization], " +
            "[OrganizationPaymentRegister]._Description [PaymentRegisterOrganization], " +
            "[Client]._Code [ClientCode], " +
            "[ClientPaymentRegister]._Description [PaymentRegisterClient], " +
            "[Agreement]._Code [AgreementCode], " +
            "[OutcomeBankOrder]._Fld6971 [PaymentPurpose], " +
            "[OutcomeBankOrder]._Fld6979 [TotalValue], " +
            "[Currency]._Code [CurrencyCode], " +
            "CASE WHEN [OutcomeBankOrder]._Fld6975 = 0x01 THEN 1 ELSE 0 END [IsAccounting], " +
            "CASE WHEN [OutcomeBankOrder]._Fld6977 = 0x01 THEN 1 ELSE 0 END [IsManagementAccounting], " +
            "[OutcomeBankOrder]._Fld6969 [Comment], " +
            "[Responsible]._Description [Responsible], " +
            "CASE " +
            "WHEN [TypeOperation]._EnumOrder = 0 THEN 5 " +
            "WHEN [TypeOperation]._EnumOrder = 1 THEN 6 " +
            "WHEN [TypeOperation]._EnumOrder = 2 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 3 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 4 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 5 THEN 11 " +
            "WHEN [TypeOperation]._EnumOrder = 6 THEN 8 " +
            "WHEN [TypeOperation]._EnumOrder = 7 THEN 7 " +
            "WHEN [TypeOperation]._EnumOrder = 8 THEN 11 " +
            "WHEN [TypeOperation]._EnumOrder = 9 THEN 10 " +
            "ELSE 7 END [TypeOperation], " +
            "[OutcomeBankOrder]._Fld6966 [PaymentDate], " +
            "(SELECT SUM([DocumentItems]._Fld7012) " +
            "FROM dbo._Document269_VT7003 [DocumentItems] " +
            "WHERE [DocumentItems]._Document269_IDRRef = [OutcomeBankOrder]._IDRRef) [TotalVat], " +
            "(SELECT TOP 1 [TypeVat]._EnumOrder " +
            "FROM dbo._Document269_VT7003 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Enum563 [TypeVat] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld7011RRef = [TypeVat]._IDRRef " +
            "WHERE [DocumentItems]._Document269_IDRRef = [OutcomeBankOrder]._IDRRef) [VatTypeFenix], " +
            "CASE WHEN [ArticlesOfMoneyAccounts].[_Description] IS NOT NULL THEN [ArticlesOfMoneyAccounts].[_Description] ELSE (SELECT TOP 1 [ArticlesOfMoneyAccounts]._Description " +
            "FROM dbo._Document269_VT7003 [DocumentItems] " +
            "LEFT OUTER JOIN dbo._Reference127 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [DocumentItems]._Fld7013RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "WHERE [DocumentItems]._Document269_IDRRef = [OutcomeBankOrder]._IDRRef) END [ArticlesOfMoneyAccounts], " +
            "[Employee]._Description [EmployeeName] " +
            "FROM dbo._Document269 [OutcomeBankOrder] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference90 [Organization] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld6973RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 [OrganizationPaymentRegister] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld6981RRef = [OrganizationPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 [Client] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld6970RRef = [Client]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference15 [ClientPaymentRegister] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld6980RRef = [ClientPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 [Agreement] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld6967RRef = [Agreement]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 [Currency] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld6963RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 [Responsible] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld6974RRef = [Responsible]._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum413 [TypeOperation] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld6964RRef = [TypeOperation]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference151 [Employee] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld6991RRef = [Employee]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference127 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [OutcomeBankOrder]._Fld6978RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "WHERE ([OutcomeBankOrder]._Marked = 0x00) " +
            "AND [OutcomeBankOrder]._Posted = 0x01 " +
            "AND [OutcomeBankOrder].[_Date_Time] >= @From " +
            "AND [OutcomeBankOrder].[_Date_Time] <= @To " +
            "AND [Organization].[_Description] != 'ТОВ «АМГ «КОНКОРД»' " +
            "ORDER BY ([OutcomeBankOrder]._Date_Time) ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }
        );
    }

    public IEnumerable<PaymentMovement> GetAllPaymentMovementOperations() {
        return _remoteSyncConnection.Query<PaymentMovement>(
            "SELECT * FROM [PaymentMovement] " +
            "WHERE [PaymentMovement].[Deleted] = 0 ");
    }

    public void AddPaymentMovementTranslation(PaymentMovementTranslation paymentMovementTranslation) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [PaymentMovementTranslation] (Name, PaymentMovementId, CultureCode, Updated) " +
            "VALUES (@Name, @PaymentMovementId, @CultureCode, getutcdate())",
            paymentMovementTranslation
        );
    }

    public long AddPaymentMovement(PaymentMovement paymentMovement) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [PaymentMovement] (OperationName, Updated) " +
                "VALUES (@OperationName, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                paymentMovement
            )
            .Single();
    }

    public IEnumerable<Client> GetDeletedClients() {
        return _remoteSyncConnection.Query<Client>(
            "SELECT * " +
            "FROM [Client] " +
            "WHERE [Client].MainClientID IS NOT NULL "
        ).AsEnumerable();
    }

    public IEnumerable<Client> GetClientsByIds(long[] mainClientIds) {
        List<Client> toReturn = new();

        int skip = 0;
        int take = 1000;

        while (mainClientIds.Length > skip) {
            long[] filteredCodes = mainClientIds.Skip(skip).Take(take).ToArray();

            List<Client> clients = new();

            _remoteSyncConnection.Query<Client, ClientAgreement, Agreement, Currency, Organization, ClientInRole, ClientType, Client>(
                "SELECT * " +
                "FROM [Client] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [Agreement].CurrencyID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [Agreement].OrganizationID " +
                "LEFT JOIN [ClientInRole] " +
                "ON [ClientInRole].ClientID = [Client].ID " +
                "LEFT JOIN [ClientType] " +
                "ON [ClientType].[ID] = [ClientInRole].[ClientTypeID] ",
                (client, clientAgreement, agreement, currency, organization, clientInRole, clientType) => {
                    if (clients.Any(c => c.Id.Equals(client.Id)))
                        client = clients.First(c => c.Id.Equals(client.Id));
                    else
                        clients.Add(client);

                    clientInRole.ClientType = clientType;

                    client.ClientInRole = clientInRole;

                    if (clientAgreement == null || agreement == null || currency == null || organization == null) return client;

                    agreement.Currency = currency;
                    agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;

                    client.ClientAgreements.Add(clientAgreement);

                    return client;
                }, new { Codes = filteredCodes }
            );

            toReturn.AddRange(clients);

            skip += take;
        }

        return toReturn;
    }

    public IEnumerable<SyncIncomeCashBankOrder> GetAmgFilteredSyncInternalMovementCashOrders(
        DateTime fromDate,
        DateTime toDate) {
        return _amgSyncConnection.Query<SyncIncomeCashBankOrder>(
            "SELECT " +
            "[InternalMovementCash]._IDRRef [OrderId], " +
            "[InternalMovementCash]._Number [Number], " +
            "[InternalMovementCash]._Date_Time [FromDate], " +
            "[CashPaymentRegister]._Description [CashPaymentRegister], " +
            "[RecipientCashPaymentRegister]._Description [ClientPaymentRegister], " +
            "[Currency]._Code [CurrencyCode], " +
            "[InternalMovementCash]._Fld3298 [TotalValue], " +
            "[ArticlesOfMoneyAccounts]._Description [ArticlesOfMoneyAccounts], " +
            "[Responsible]._Description [Responsible], " +
            "[InternalMovementCash]._Fld3302 [Comment], " +
            "[Organization]._Description [Organization], " +
            "[RecipientOrganization]._Description [RecipientOrganization] " +
            "FROM dbo._Document216 [InternalMovementCash] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference76 [CashPaymentRegister] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld3295RRef = [CashPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference76 [RecipientCashPaymentRegister] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld3296RRef = [RecipientCashPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 [Currency] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld3297RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference155 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld3300RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference126 [Responsible] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld3301RRef = [Responsible]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference116 [Organization] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld3303RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference116 [RecipientOrganization] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld3304RRef = [RecipientOrganization]._IDRRef " +
            "WHERE [InternalMovementCash]._Marked = 0x00 " +
            "AND [InternalMovementCash].[_Date_Time] <= @To " +
            "AND [InternalMovementCash].[_Date_Time] >= @From " +
            "AND [InternalMovementCash]._Posted = 0x01 ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }
        );
    }

    public IEnumerable<SyncIncomeCashBankOrder> GetFilteredSyncInternalMovementCashOrders(
        DateTime fromDate,
        DateTime toDate) {
        return _oneCConnection.Query<SyncIncomeCashBankOrder>(
            "SELECT " +
            "[InternalMovementCash]._IDRRef [OrderId], " +
            "[InternalMovementCash].[_Number] [Number], " +
            "[InternalMovementCash].[_Date_Time] [FromDate], " +
            "[CashPaymentRegister].[_Description] [CashPaymentRegister], " +
            "[RecipientCashPaymentRegister].[_Description] [ClientPaymentRegister], " +
            "[Currency].[_Code] [CurrencyCode], " +
            "[InternalMovementCash].[_Fld2528] [TotalValue], " +
            "[ArticlesOfMoneyAccounts].[_Description] [ArticlesOfMoneyAccounts], " +
            "[Responsible].[_Description] [Responsible], " +
            "[InternalMovementCash].[_Fld2532] [Comment], " +
            "[Organization].[_Description] [Organization], " +
            "[RecipientOrganization].[_Description] [RecipientOrganization] " +
            "FROM dbo._Document172 [InternalMovementCash] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference56 [CashPaymentRegister] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld2525RRef = [CashPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference56 [RecipientCashPaymentRegister] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld2526RRef = [RecipientCashPaymentRegister]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 [Currency] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld2527RRef = [Currency]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference127 [ArticlesOfMoneyAccounts] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld2530RRef = [ArticlesOfMoneyAccounts]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference100 [Responsible] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld2531RRef = [Responsible]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 [Organization] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld2533RRef = [Organization]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 [RecipientOrganization] WITH(NOLOCK) " +
            "ON [InternalMovementCash]._Fld2534RRef = [RecipientOrganization]._IDRRef " +
            "WHERE [InternalMovementCash]._Marked = 0x00 " +
            "AND [InternalMovementCash].[_Date_Time] <= @To " +
            "AND [InternalMovementCash].[_Date_Time] >= @From " +
            "AND [InternalMovementCash]._Posted = 0x01 ",
            new {
                From = fromDate.AddYears(2000),
                To = toDate.AddYears(2000)
            }
        );
    }
}