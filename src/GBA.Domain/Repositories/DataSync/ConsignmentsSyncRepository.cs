using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync;

public sealed class ConsignmentsSyncRepository : IConsignmentsSyncRepository {
    private readonly IDbConnection _amgOneCConnection;
    private readonly IDbConnection _oneCConnection;

    private readonly IDbConnection _remoteSyncConnection;

    private readonly string[] ORGANIZATION_NAMES = new[] {
        "Фенікс",
        "ФОП Самолюк Алла Дмитрівна",
        "ФОП Пархоменко Ганна Юріївна",
        "ФОП Самолюк Юрій Миколайович",
        "ТОВ «АМГ «КОНКОРД»"
    };

    public ConsignmentsSyncRepository(
        IDbConnection oneCConnection,
        IDbConnection amgOneCConnection,
        IDbConnection remoteSyncConnection) {
        _oneCConnection = oneCConnection;

        _amgOneCConnection = amgOneCConnection;

        _remoteSyncConnection = remoteSyncConnection;
    }

    public IEnumerable<SyncOrganization> GetAllSyncOrganizations(
        string[] organizationNames) {
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
            "WHERE (T5._Marked = 0x00) " +
            "AND T5._Description IN @Organizations ",
            new { Organizations = organizationNames }
        );
    }

    public IEnumerable<SyncOrganization> GetAmgAllSyncOrganizations() {
        return _amgOneCConnection.Query<SyncOrganization>(
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
            "ON T5._Fld1888RRef = T10._IDRRef"
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
        return _amgOneCConnection.Query<SyncOrganizationAddress>(
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
            "AND [Translation].Deleted = 0 ",
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
            ",[Manager]" +
            ",[VatRateID]" +
            ",[IsVatAgreements]" +
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
            ",@Manager " +
            ",@VatRateId " +
            ",@IsVatAgreements " +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            organization
        ).Single();
    }

    public void Add(OrderProductSpecification specification) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [OrderProductSpecification] (Qty, SupplyInvoiceId, ProductSpecificationId, SadId, UnitPrice, Updated) " +
            "VALUES (@Qty, @SupplyInvoiceId, @ProductSpecificationId, @SadId, @UnitPrice, GETUTCDATE())",
            specification
        );
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
            ",[Manager] = @Manager " +
            ",[VatRateID] = @VatRateId " +
            ",[IsVatAgreements] = @IsVatAgreements " +
            "WHERE ID = @Id ",
            organization
        );
    }

    public void UpdatePrices(IEnumerable<ConsignmentItem> items) {
        _remoteSyncConnection.Execute(
            "UPDATE [ConsignmentItem] " +
            "SET NetPrice = @NetPrice, Updated = GETUTCDATE(), AccountingPrice = @AccountingPrice, [Price] = @Price " +
            "WHERE ID = @Id",
            items
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
        return _amgOneCConnection.Query<SyncTaxInspection>(
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

    public IEnumerable<SyncStorage> GetAllSyncStorages() {
        return _oneCConnection.Query<SyncStorage>(
            "SELECT " +
            "T1._Description [StorageName], " +
            "T2._Description [OrganizationName] " +
            "FROM dbo._Reference117 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference90 T2 WITH(NOLOCK) " +
            "ON (T1._IDRRef = T2._Fld16181RRef) " +
            "WHERE (T1._Folder) = 0x01 AND (T1._Marked = 0x00) " +
            "AND T2._Description IS NOT NULL "
        );
    }

    public IEnumerable<SyncStorage> GetAmgAllSyncStorages() {
        return _amgOneCConnection.Query<SyncStorage>(
            "SELECT " +
            "T1._Description [StorageName], " +
            "T2._Description [OrganizationName] " +
            "FROM dbo._Reference144 T1 " +
            "LEFT JOIN dbo._Reference116 T2 " +
            "ON (T2._Fld15976RRef = T1._IDRRef) " +
            "WHERE (T1._Folder) = 0x01 AND (T1._Marked = 0x00) "
        );
    }

    public IEnumerable<SyncStorage> GetSyncStoragesFromSyncConsignments() {
        return _oneCConnection.Query<SyncStorage>(
            "SELECT [T1].StorageName, [T1].OrganizationName " +
            "FROM ( " +
            "SELECT " +
            "CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2) THEN 0 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF) " +
            "THEN 1 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2) THEN 2 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000115) THEN 3 ELSE 4 END END END END [TypeDocument], " +
            "T3._Fld7593 [DocumentArrivalNumber], " +
            "T3._Fld7586 [DocumentArrivalDate], " +
            "T3._Date_Time [DocumentDate], " +
            "T3._Number [DocumentNumber], " +
            "T3._Fld7592 [RateExhcange], " +
            "T3._Fld7591 [Kratnost], " +
            "T5._Code [ProductCode], " +
            "T5._Fld1306 [ProductArticle], " +
            "MAX(T1.Fld12802Balance_) [Quantity], " +
            "MAX(T4._Fld15561) [WeightNetto], " +
            "(CASE WHEN (ISNULL(CAST(T3._Fld15560 AS NUMERIC(15, 8)),0.0) = 0.0) THEN 1.0 ELSE T3._Fld15560 END * T4._Fld15561) [WeightBrutto], " +
            "MAX(T1.Fld12803Balance_) [Value], " +
            "MAX(T1.Fld12804Balance_) [VAT], " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T7._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T9._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T11._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 " +
            "THEN T13._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA THEN T15._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 " +
            "THEN T17._Code ELSE CAST(NULL AS NVARCHAR(9)) END [ClientCode], " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T7._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T9._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T11._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000116 THEN T13._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA THEN T15._Description " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T17._Description ELSE CAST(NULL AS NVARCHAR(100)) END [ClientName], " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T18._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T19._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T20._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F9 " +
            "THEN T22._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T23._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB " +
            "THEN T25._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA THEN T26._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 " +
            "THEN T28._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T29._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000104 " +
            "THEN T31._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T33._Code ELSE CAST(NULL AS NVARCHAR(9)) END [OrganizationCode], " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T18._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T19._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T20._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000F9 THEN T22._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T23._Description " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB THEN T25._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA " +
            "THEN T26._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 THEN T28._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000115 THEN T29._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000104 THEN T31._Description " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T33._Description ELSE CAST(NULL AS NVARCHAR(50)) END [OrganizationName], " +
            "T34._Code [StorageCode], " +
            "T34._Description [StorageName], " +
            "T35._EnumOrder [StatusPackageList] " +
            "FROM (SELECT " +
            "T2._Fld12799RRef AS Fld12799RRef, " +
            "T2._Fld12794RRef AS Fld12794RRef, " +
            "T2._Fld12795RRef AS Fld12795RRef, " +
            "T2._Fld12798_TYPE AS Fld12798_TYPE, " +
            "T2._Fld12798_RTRef AS Fld12798_RTRef, " +
            "T2._Fld12798_RRRef AS Fld12798_RRRef, " +
            "CAST(SUM(T2._Fld12804) AS NUMERIC(33, 8)) AS Fld12804Balance_, " +
            "CAST(SUM(T2._Fld12803) AS NUMERIC(33, 8)) AS Fld12803Balance_, " +
            "CAST(SUM(T2._Fld12802) AS NUMERIC(32, 8)) AS Fld12802Balance_ " +
            "FROM dbo._AccumRgT12814 T2 WITH(NOLOCK) " +
            "WHERE T2._Period = CAST('5999-11-01 00:00:00' AS datetime2(3)) AND (T2._Fld12804 <> 0 OR T2._Fld12803 <> 0 OR T2._Fld12802 <> 0) AND (T2._Fld12804 <> 0 OR T2._Fld12803 <> 0 " +
            "OR T2._Fld12802 <> 0) " +
            "GROUP BY T2._Fld12799RRef, " +
            "T2._Fld12794RRef, " +
            "T2._Fld12795RRef, " +
            "T2._Fld12798_TYPE, " +
            "T2._Fld12798_RTRef, " +
            "T2._Fld12798_RRRef " +
            "HAVING (CAST(SUM(T2._Fld12804) AS NUMERIC(33, 8))) <> 0.0 OR (CAST(SUM(T2._Fld12803) AS NUMERIC(33, 8))) <> 0.0 " +
            "OR (CAST(SUM(T2._Fld12802) AS NUMERIC(32, 8))) <> 0.0) T1 " +
            "LEFT OUTER JOIN dbo._Document277 T3 WITH(NOLOCK) " +
            "ON (T1.Fld12798_TYPE = CASE WHEN T3._IDRRef IS NOT NULL THEN 0x08 END AND T1.Fld12798_RTRef = CASE WHEN T3._IDRRef IS NOT NULL THEN 0x00000115 END " +
            "AND T1.Fld12798_RRRef = T3._IDRRef) " +
            "LEFT OUTER JOIN dbo._Document277_VT7622 T4 WITH(NOLOCK) " +
            "ON (T1.Fld12794RRef = T4._Fld7624RRef) " +
            "LEFT OUTER JOIN dbo._Reference84 T5 WITH(NOLOCK) " +
            "ON T1.Fld12794RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Document175 T6 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF AND T1.Fld12798_RRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T7 WITH(NOLOCK) " +
            "ON T6._Fld2601RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Document284 T8 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C AND T1.Fld12798_RRRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T9 WITH(NOLOCK) " +
            "ON T8._Fld8326RRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Document275 T10 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 AND T1.Fld12798_RRRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T11 WITH(NOLOCK) " +
            "ON T10._Fld7408RRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Document278 T12 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 AND T1.Fld12798_RRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T13 WITH(NOLOCK) " +
            "ON T12._Fld7783RRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Document250 T14 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA AND T1.Fld12798_RRRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T15 WITH(NOLOCK) " +
            "ON T14._Fld6254RRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Document277 T16 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 AND T1.Fld12798_RRRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T17 WITH(NOLOCK) " +
            "ON T16._Fld7590RRef = T17._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T18 WITH(NOLOCK) " +
            "ON T6._Fld2594RRef = T18._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T19 WITH(NOLOCK) " +
            "ON T8._Fld8324RRef = T19._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T20 WITH(NOLOCK) " +
            "ON T10._Fld7413RRef = T20._IDRRef " +
            "LEFT OUTER JOIN dbo._Document249 T21 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F9 AND T1.Fld12798_RRRef = T21._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T22 WITH(NOLOCK) " +
            "ON T21._Fld6154RRef = T22._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T23 WITH(NOLOCK) " +
            "ON T12._Fld7787RRef = T23._IDRRef " +
            "LEFT OUTER JOIN dbo._Document219 T24 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB AND T1.Fld12798_RRRef = T24._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T25 WITH(NOLOCK) " +
            "ON T24._Fld4251RRef = T25._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T26 WITH(NOLOCK) " +
            "ON T14._Fld6253RRef = T26._IDRRef " +
            "LEFT OUTER JOIN dbo._Document242 T27 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 AND T1.Fld12798_RRRef = T27._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T28 WITH(NOLOCK) " +
            "ON T27._Fld5726RRef = T28._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T29 WITH(NOLOCK) " +
            "ON T16._Fld7594RRef = T29._IDRRef " +
            "LEFT OUTER JOIN dbo._Document260 T30 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000104 AND T1.Fld12798_RRRef = T30._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T31 WITH(NOLOCK) " +
            "ON T30._Fld6601RRef = T31._IDRRef " +
            "LEFT OUTER JOIN dbo._Document162 T32 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 AND T1.Fld12798_RRRef = T32._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T33 WITH(NOLOCK) " +
            "ON T32._Fld1929RRef = T33._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T34 WITH(NOLOCK) " +
            "ON T1.Fld12795RRef = T34._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum566 T35 WITH(NOLOCK) " +
            "ON T1.Fld12799RRef = T35._IDRRef " +
            "WHERE (T1.Fld12802Balance_ > 0) " +
            "AND CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2) THEN 0 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000AF) THEN 1 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2) THEN 2 " +
            "ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115) THEN 3 ELSE 4 END END END END NOT IN (0, 4) " +
            "GROUP BY T1.Fld12798_TYPE, " +
            "T1.Fld12798_RTRef, " +
            "T1.Fld12798_RRRef, " +
            "T3._Fld7593, " +
            "T3._Fld7586, " +
            "T3._Date_Time, " +
            "T3._Number, " +
            "T3._Fld7592, " +
            "T3._Fld7591, " +
            "T5._Code, " +
            "T5._Fld1306, " +
            "(CASE WHEN (ISNULL(CAST(T3._Fld15560 AS NUMERIC(15, 8)),0.0) = 0.0) THEN 1.0 ELSE T3._Fld15560 END * T4._Fld15561), " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T7._Code WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x0000011C THEN T9._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T11._Code " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T13._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA " +
            "THEN T15._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T17._Code ELSE CAST(NULL AS NVARCHAR(9)) END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T7._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x0000011C THEN T9._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T11._Description " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T13._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA " +
            "THEN T15._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T17._Description ELSE CAST(NULL AS NVARCHAR(100)) END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T18._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T19._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T20._Code WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000F9 THEN T22._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T23._Code WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000DB THEN T25._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA THEN T26._Code WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000F2 THEN T28._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T29._Code WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000104 THEN T31._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T33._Code " +
            "ELSE CAST(NULL AS NVARCHAR(9)) END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T18._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T19._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T20._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000F9 THEN T22._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T23._Description " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB THEN T25._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA " +
            "THEN T26._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 THEN T28._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000115 THEN T29._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000104 THEN T31._Description " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T33._Description ELSE CAST(NULL AS NVARCHAR(50)) END, " +
            "CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2) THEN 0.0 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF) " +
            "THEN 1.0 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2) THEN 2.0 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000115) THEN 3.0 ELSE 4.0 END END END END, " +
            "T34._Code, " +
            "T34._Description, " +
            "T35._EnumOrder " +
            ") [T1] " +
            "WHERE [T1].StorageName NOT IN ( " +
            "N'Склад ТМЦ ПЛ', " +
            "N'Склад ТМЦ', " +
            "N'Основні засоби (ХАЗ)', " +
            "N'Основні засоби' " +
            ") " +
            "GROUP BY [T1].StorageName, [T1].[OrganizationName]",
            commandTimeout: 3600
        );
    }

    public IEnumerable<SyncStorage> GetAmgSyncStoragesFromSyncConsignments() {
        return _amgOneCConnection.Query<SyncStorage>(
            "SELECT " +
            "T12.[_Description], " +
            "T13.[_Description] " +
            "FROM (SELECT " +
            "T2.RecorderTRef AS RecorderTRef, " +
            "T2.RecorderRRef AS RecorderRRef, " +
            "T2.Fld15690RRef AS Fld15690RRef, " +
            "T2.Fld15694RRef AS Fld15694RRef, " +
            "T2.Fld15692RRef AS Fld15692RRef, " +
            "T2.Fld15693RRef AS Fld15693RRef, " +
            "T2.Fld15691RRef AS Fld15691RRef, " +
            "CAST(SUM(T2.Fld15695Turnover_) AS NUMERIC(32, 8)) AS Fld15695Turnover_, " +
            "CAST(SUM(T2.Fld15695Receipt_) AS NUMERIC(32, 8)) AS Fld15695Receipt_, " +
            "CAST(SUM(T2.Fld15695Expense_) AS NUMERIC(32, 8)) AS Fld15695Expense_, " +
            "CAST(SUM(T2.Fld15695Balance_) AS NUMERIC(21, 8)) AS Fld15695InitialBalance_, " +
            "CAST(SUM(T2.Fld15695Balance_ + T2.Fld15695Turnover_) AS NUMERIC(33, 8)) AS Fld15695FinalBalance_ " +
            "FROM (SELECT " +
            "T3._RecorderTRef AS RecorderTRef, " +
            "T3._RecorderRRef AS RecorderRRef, " +
            "T3._Fld15690RRef AS Fld15690RRef, " +
            "T3._Fld15694RRef AS Fld15694RRef, " +
            "T3._Fld15692RRef AS Fld15692RRef, " +
            "T3._Fld15693RRef AS Fld15693RRef, " +
            "T3._Fld15691RRef AS Fld15691RRef, " +
            "CAST(SUM(0.0) AS NUMERIC(15, 8)) AS Fld15695Balance_, " +
            "CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld15695 ELSE -T3._Fld15695 END) AS NUMERIC(26, 8)) AS Fld15695Turnover_, " +
            "CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld15695 ELSE 0.0 END) AS NUMERIC(26, 8)) AS Fld15695Receipt_, " +
            "CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN 0.0 ELSE T3._Fld15695 END) AS NUMERIC(26, 8)) AS Fld15695Expense_ " +
            "FROM dbo._AccumRg15689 T3 WITH(NOLOCK) " +
            "WHERE T3._Active = 0x01 " +
            "AND T3._Fld15695 > 0 " +
            "GROUP BY T3._Fld15690RRef, T3._Fld15694RRef, T3._Fld15692RRef, T3._Fld15693RRef, T3._Fld15691RRef, T3._RecorderRRef, T3._RecorderTRef " +
            "HAVING (CAST(SUM(0.0) AS NUMERIC(15, 8))) <> 0.0 OR (CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld15695 ELSE -T3._Fld15695 END) AS NUMERIC(26, 8))) <> 0.0 OR (CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld15695 ELSE 0.0 END) AS NUMERIC(26, 8))) <> 0.0 OR (CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN 0.0 ELSE T3._Fld15695 END) AS NUMERIC(26, 8))) <> 0.0) T2 " +
            "GROUP BY T2.Fld15690RRef, T2.Fld15694RRef, T2.Fld15692RRef, T2.Fld15693RRef, T2.Fld15691RRef, T2.RecorderRRef, T2.RecorderTRef " +
            "HAVING (CAST(SUM(T2.Fld15695Turnover_) AS NUMERIC(32, 8))) <> 0.0 OR (CAST(SUM(T2.Fld15695Receipt_) AS NUMERIC(32, 8))) <> 0.0 OR (CAST(SUM(T2.Fld15695Expense_) AS NUMERIC(32, 8))) <> 0.0 OR (CAST(SUM(T2.Fld15695Balance_) AS NUMERIC(21, 8))) <> 0.0 OR (CAST(SUM(T2.Fld15695Balance_ + T2.Fld15695Turnover_) AS NUMERIC(33, 8))) <> 0.0) T1 " +
            "LEFT JOIN dbo._Document203 T2 ON T2._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document209 T3 ON T3._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document219 T4 ON T4._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document220 T5 ON T5._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document292 T6 ON T6._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document301 T7 ON T7._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document309 T8 ON T8._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document330 T9 ON T9._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document345 T10 ON T10._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document362 T11 ON T11._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Reference144 T12 " +
            "ON T12.[_IDRRef] = T1.Fld15690RRef " +
            "LEFT JOIN dbo._Reference116 T13 " +
            "ON T13._Fld15976RRef = T12.[_IDRRef] " +
            "WHERE T1.Fld15695FinalBalance_ > 0 " +
            "GROUP BY T12.[_Description], T13.[_Description] ",
            commandTimeout: 3600
        );
    }

    public IEnumerable<string> GetStorageNamesFromSyncConsignmentsExceptProvided(IEnumerable<string> names) {
        return _oneCConnection.Query<string>(
            "SELECT " +
            "T34._Description [StorageName] " +
            "FROM (SELECT " +
            "T2._Fld12799RRef AS Fld12799RRef, " +
            "T2._Fld12794RRef AS Fld12794RRef, " +
            "T2._Fld12795RRef AS Fld12795RRef, " +
            "T2._Fld12798_TYPE AS Fld12798_TYPE, " +
            "T2._Fld12798_RTRef AS Fld12798_RTRef, " +
            "T2._Fld12798_RRRef AS Fld12798_RRRef, " +
            "CAST(SUM(T2._Fld12804) AS NUMERIC(33, 8)) AS Fld12804Balance_, " +
            "CAST(SUM(T2._Fld12803) AS NUMERIC(33, 8)) AS Fld12803Balance_, " +
            "CAST(SUM(T2._Fld12802) AS NUMERIC(32, 8)) AS Fld12802Balance_ " +
            "FROM dbo._AccumRgT12814 T2 WITH(NOLOCK) " +
            "WHERE T2._Period = CAST('5999-11-01 00:00:00' AS datetime2(3)) AND (T2._Fld12804 <> 0 OR T2._Fld12803 <> 0 OR T2._Fld12802 <> 0) AND (T2._Fld12804 <> 0 OR T2._Fld12803 <> 0 " +
            "OR T2._Fld12802 <> 0) " +
            "GROUP BY T2._Fld12799RRef, " +
            "T2._Fld12794RRef, " +
            "T2._Fld12795RRef, " +
            "T2._Fld12798_TYPE, " +
            "T2._Fld12798_RTRef, " +
            "T2._Fld12798_RRRef " +
            "HAVING (CAST(SUM(T2._Fld12804) AS NUMERIC(33, 8))) <> 0.0 OR (CAST(SUM(T2._Fld12803) AS NUMERIC(33, 8))) <> 0.0 " +
            "OR (CAST(SUM(T2._Fld12802) AS NUMERIC(32, 8))) <> 0.0) T1 " +
            "LEFT OUTER JOIN dbo._Document277 T3 WITH(NOLOCK) " +
            "ON (T1.Fld12798_TYPE = CASE WHEN T3._IDRRef IS NOT NULL THEN 0x08 END AND T1.Fld12798_RTRef = CASE WHEN T3._IDRRef IS NOT NULL THEN 0x00000115 END " +
            "AND T1.Fld12798_RRRef = T3._IDRRef) " +
            "LEFT OUTER JOIN dbo._Document277_VT7622 T4 WITH(NOLOCK) " +
            "ON (T1.Fld12794RRef = T4._Fld7624RRef) " +
            "LEFT OUTER JOIN dbo._Reference84 T5 WITH(NOLOCK) " +
            "ON T1.Fld12794RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Document175 T6 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF AND T1.Fld12798_RRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T7 WITH(NOLOCK) " +
            "ON T6._Fld2601RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Document284 T8 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C AND T1.Fld12798_RRRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T9 WITH(NOLOCK) " +
            "ON T8._Fld8326RRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Document275 T10 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 AND T1.Fld12798_RRRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T11 WITH(NOLOCK) " +
            "ON T10._Fld7408RRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Document278 T12 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 AND T1.Fld12798_RRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T13 WITH(NOLOCK) " +
            "ON T12._Fld7783RRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Document250 T14 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA AND T1.Fld12798_RRRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T15 WITH(NOLOCK) " +
            "ON T14._Fld6254RRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Document277 T16 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 AND T1.Fld12798_RRRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T17 WITH(NOLOCK) " +
            "ON T16._Fld7590RRef = T17._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T18 WITH(NOLOCK) " +
            "ON T6._Fld2594RRef = T18._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T19 WITH(NOLOCK) " +
            "ON T8._Fld8324RRef = T19._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T20 WITH(NOLOCK) " +
            "ON T10._Fld7413RRef = T20._IDRRef " +
            "LEFT OUTER JOIN dbo._Document249 T21 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F9 AND T1.Fld12798_RRRef = T21._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T22 WITH(NOLOCK) " +
            "ON T21._Fld6154RRef = T22._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T23 WITH(NOLOCK) " +
            "ON T12._Fld7787RRef = T23._IDRRef " +
            "LEFT OUTER JOIN dbo._Document219 T24 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB AND T1.Fld12798_RRRef = T24._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T25 WITH(NOLOCK) " +
            "ON T24._Fld4251RRef = T25._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T26 WITH(NOLOCK) " +
            "ON T14._Fld6253RRef = T26._IDRRef " +
            "LEFT OUTER JOIN dbo._Document242 T27 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 AND T1.Fld12798_RRRef = T27._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T28 WITH(NOLOCK) " +
            "ON T27._Fld5726RRef = T28._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T29 WITH(NOLOCK) " +
            "ON T16._Fld7594RRef = T29._IDRRef " +
            "LEFT OUTER JOIN dbo._Document260 T30 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000104 AND T1.Fld12798_RRRef = T30._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T31 WITH(NOLOCK) " +
            "ON T30._Fld6601RRef = T31._IDRRef " +
            "LEFT OUTER JOIN dbo._Document162 T32 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 AND T1.Fld12798_RRRef = T32._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T33 WITH(NOLOCK) " +
            "ON T32._Fld1929RRef = T33._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T34 WITH(NOLOCK) " +
            "ON T1.Fld12795RRef = T34._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum566 T35 WITH(NOLOCK) " +
            "ON T1.Fld12799RRef = T35._IDRRef " +
            "WHERE (T1.Fld12802Balance_ > 0) " +
            "AND CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2) THEN 0 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000AF) THEN 1 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2) THEN 2 " +
            "ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115) THEN 3 ELSE 4 END END END END NOT IN (0, 4) " +
            "AND [T34]._Description NOT IN ( " +
            "N'Склад ТМЦ ПЛ', " +
            "N'Склад ТМЦ', " +
            "N'Основні засоби (ХАЗ)', " +
            "N'Основні засоби' " +
            ") " +
            "AND [T34]._Description NOT IN @Names " +
            "GROUP BY [T34]._Description",
            new { Names = names },
            commandTimeout: 3600
        );
    }

    public IEnumerable<string> GetAmgStorageNamesFromSyncConsignmentsExceptProvided(IEnumerable<string> names) {
        return _amgOneCConnection.Query<string>(
            "SELECT " +
            "T12.[_Description] " +
            "FROM (SELECT " +
            "T2.RecorderTRef AS RecorderTRef, " +
            "T2.RecorderRRef AS RecorderRRef, " +
            "T2.Fld15690RRef AS Fld15690RRef, " +
            "T2.Fld15694RRef AS Fld15694RRef, " +
            "T2.Fld15692RRef AS Fld15692RRef, " +
            "T2.Fld15693RRef AS Fld15693RRef, " +
            "T2.Fld15691RRef AS Fld15691RRef, " +
            "CAST(SUM(T2.Fld15695Turnover_) AS NUMERIC(32, 8)) AS Fld15695Turnover_, " +
            "CAST(SUM(T2.Fld15695Receipt_) AS NUMERIC(32, 8)) AS Fld15695Receipt_, " +
            "CAST(SUM(T2.Fld15695Expense_) AS NUMERIC(32, 8)) AS Fld15695Expense_, " +
            "CAST(SUM(T2.Fld15695Balance_) AS NUMERIC(21, 8)) AS Fld15695InitialBalance_, " +
            "CAST(SUM(T2.Fld15695Balance_ + T2.Fld15695Turnover_) AS NUMERIC(33, 8)) AS Fld15695FinalBalance_ " +
            "FROM (SELECT " +
            "T3._RecorderTRef AS RecorderTRef, " +
            "T3._RecorderRRef AS RecorderRRef, " +
            "T3._Fld15690RRef AS Fld15690RRef, " +
            "T3._Fld15694RRef AS Fld15694RRef, " +
            "T3._Fld15692RRef AS Fld15692RRef, " +
            "T3._Fld15693RRef AS Fld15693RRef, " +
            "T3._Fld15691RRef AS Fld15691RRef, " +
            "CAST(SUM(0.0) AS NUMERIC(15, 8)) AS Fld15695Balance_, " +
            "CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld15695 ELSE -T3._Fld15695 END) AS NUMERIC(26, 8)) AS Fld15695Turnover_, " +
            "CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld15695 ELSE 0.0 END) AS NUMERIC(26, 8)) AS Fld15695Receipt_, " +
            "CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN 0.0 ELSE T3._Fld15695 END) AS NUMERIC(26, 8)) AS Fld15695Expense_ " +
            "FROM dbo._AccumRg15689 T3 WITH(NOLOCK) " +
            "WHERE T3._Active = 0x01 " +
            "AND T3._Fld15695 > 0 " +
            "GROUP BY T3._Fld15690RRef, T3._Fld15694RRef, T3._Fld15692RRef, T3._Fld15693RRef, T3._Fld15691RRef, T3._RecorderRRef, T3._RecorderTRef " +
            "HAVING (CAST(SUM(0.0) AS NUMERIC(15, 8))) <> 0.0 OR (CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld15695 ELSE -T3._Fld15695 END) AS NUMERIC(26, 8))) <> 0.0 OR (CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld15695 ELSE 0.0 END) AS NUMERIC(26, 8))) <> 0.0 OR (CAST(SUM(CASE WHEN T3._RecordKind = 0.0 THEN 0.0 ELSE T3._Fld15695 END) AS NUMERIC(26, 8))) <> 0.0) T2 " +
            "GROUP BY T2.Fld15690RRef, T2.Fld15694RRef, T2.Fld15692RRef, T2.Fld15693RRef, T2.Fld15691RRef, T2.RecorderRRef, T2.RecorderTRef " +
            "HAVING (CAST(SUM(T2.Fld15695Turnover_) AS NUMERIC(32, 8))) <> 0.0 OR (CAST(SUM(T2.Fld15695Receipt_) AS NUMERIC(32, 8))) <> 0.0 OR (CAST(SUM(T2.Fld15695Expense_) AS NUMERIC(32, 8))) <> 0.0 OR (CAST(SUM(T2.Fld15695Balance_) AS NUMERIC(21, 8))) <> 0.0 OR (CAST(SUM(T2.Fld15695Balance_ + T2.Fld15695Turnover_) AS NUMERIC(33, 8))) <> 0.0) T1 " +
            "LEFT JOIN dbo._Document203 T2 ON T2._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document209 T3 ON T3._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document219 T4 ON T4._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document220 T5 ON T5._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document292 T6 ON T6._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document301 T7 ON T7._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document309 T8 ON T8._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document330 T9 ON T9._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document345 T10 ON T10._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Document362 T11 ON T11._IDRRef = T1.RecorderRRef " +
            "LEFT JOIN dbo._Reference144 T12 " +
            "ON T12.[_IDRRef] = T1.Fld15690RRef " +
            "LEFT JOIN dbo._Reference116 T13 " +
            "ON T13._Fld15976RRef = T12.[_IDRRef] " +
            "WHERE T1.Fld15695FinalBalance_ > 0 " +
            "AND T12.[_Description] IN @Names " +
            "GROUP BY T12.[_Description] ",
            new { Names = names },
            commandTimeout: 3600
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

    public IEnumerable<SyncConsignment> GetAllSyncConsignments(IEnumerable<string> storageNames) {
        return _oneCConnection.Query<SyncConsignment>(
            "SELECT " +
            "T1.Fld12798_RRRef [DocumentId], " +
            "T12._Fld7604 [DocumentValue], " +
            "CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2) THEN 0 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF) " +
            "THEN 1 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2) THEN 2 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000115) THEN 3 ELSE 4 END END END END [TypeDocument], " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T4._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T5._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T6._Number WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000F9 THEN T7._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T8._Number " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB THEN T9._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA " +
            "THEN T10._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 THEN T11._Number WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000104 THEN T13._Number " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T14._Number ELSE CAST(NULL AS NVARCHAR(11)) END [DocumentNumber], " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T4._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T5._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T6._Date_Time WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000F9 THEN T7._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T8._Date_Time " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB THEN T9._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA " +
            "THEN T10._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 THEN T11._Date_Time WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000104 THEN T13._Date_Time " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T14._Date_Time ELSE CAST(NULL AS DATETIME) END [DocumentDate], " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C THEN T5._Fld8327 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 " +
            "THEN T8._Fld7786 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Fld7593 ELSE CAST(NULL AS NVARCHAR(30)) " +
            "END [DocumentArrivalNumber], " +
            "CASE WHEN CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T5._Fld8328 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 " +
            "THEN T8._Fld7780 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 " +
            "THEN T12._Fld7586 ELSE CAST(NULL AS DATETIME) END = N'2001-01-01 00:00:00.000' THEN NULL ELSE CASE " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C THEN T5._Fld8328 WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000116 THEN T8._Fld7780 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 " +
            "THEN T12._Fld7586 ELSE CAST(NULL AS DATETIME) END END [DocumentArrivalDate], " +
            "ISNULL(CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T4._Fld2604 WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000113 THEN T6._Fld7412 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T8._Fld7785 " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Fld7592 ELSE CAST(NULL AS NUMERIC(10, 4)) END, 1.0000) [RateExchange], " +
            "CAST(CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T15._Code WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x0000011C THEN T16._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T17._Code " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T18._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA " +
            "THEN T19._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T20._Code ELSE CAST(NULL AS NVARCHAR(9)) END AS bigint) [ClientCode], " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T21._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x0000011C THEN T22._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 " +
            "THEN T23._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F9 THEN T24._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000116 THEN T25._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB THEN T26._Description " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA THEN T27._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 " +
            "THEN T28._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T29._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000104 THEN T30._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T31._Description " +
            "ELSE CAST(NULL AS NVARCHAR(50)) END [OrganizationName], " +
            "T32._Description [StorageName], " +
            "CASE WHEN T45._Description IS NOT NULL THEN T45._Description ELSE T46._Description END [IncomeStorageName], " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T33._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 " +
            "THEN T34._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T35._Code WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000FA THEN T36._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T37._Code " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T38._Code ELSE CAST(NULL AS NVARCHAR(3)) END [CurrencyCode], " +
            "CAST(T39._Code AS bigint) [ProductCode], " +
            "T39._Fld1306 [VendorCode], " +
            "T1.Fld12802Balance_ [Qty], " +
            "T3._Fld7629 [IncomeQty], " +
            "T3._Fld7630 [NetValue], " +
            "T1.Fld12803Balance_ [Value], " +
            "T3._Fld16225 [CustomsRate], " +
            "T3._Fld16226 [CustomsValue], " +
            "T1.Fld12804Balance_ [VAT], " +
            "T47._EnumOrder [VatTypeFenix], " +
            "ISNULL((CAST(T3._Fld15561 AS NUMERIC(20, 8)) / T3._Fld7629), 0.000) [WeightPer], " +
            "ISNULL((CASE WHEN (CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Fld15560 ELSE CAST(NULL AS NUMERIC(15, 8)) END = 0.0) " +
            "THEN 1.0 ELSE CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Fld15560 ELSE CAST(NULL AS NUMERIC(15, 8)) END " +
            "END * T3._Fld15561) / T3._Fld7629, 0.000) [WeightBruttoPer], " +
            "CASE " +
            "WHEN T40._IDRRef IS NOT NULL " +
            "THEN T40._Code " +
            "WHEN T41._IDRRef IS NOT NULL " +
            "THEN T41._Code " +
            "WHEN T42._IDRRef IS NOT NULL " +
            "THEN T42._Code " +
            "WHEN T43._IDRRef IS NOT NULL " +
            "THEN T43._Code " +
            "WHEN T44._IDRRef IS NOT NULL " +
            "THEN T44._Code " +
            "ELSE NULL " +
            "END [AgreementCode] " +
            ", T3._Fld16225 [Rate] " +
            ", CAST(CASE " +
            "WHEN T12._IDRRef IS NOT NULL " +
            "THEN  T12._Fld7589 " +
            "WHEN T11._IDRRef IS NOT NULL " +
            "THEN  T11._Fld5735 " +
            "WHEN T4._IDRRef IS NOT NULL " +
            "THEN  T4._Fld2606 " +
            "ELSE NULL " +
            "END AS nvarchar(max)) [Comment] " +
            ", T3._Fld7631 [TotalValue], " +
            "T3._Fld7633 [TotalVat] " +
            "FROM (SELECT " +
            "T2._Fld12794RRef AS Fld12794RRef, " +
            "T2._Fld12795RRef AS Fld12795RRef, " +
            "T2._Fld12798_TYPE AS Fld12798_TYPE, " +
            "T2._Fld12798_RTRef AS Fld12798_RTRef, " +
            "T2._Fld12798_RRRef AS Fld12798_RRRef, " +
            "CAST(SUM(T2._Fld12804) AS NUMERIC(33, 8)) AS Fld12804Balance_, " +
            "CAST(SUM(T2._Fld12803) AS NUMERIC(33, 8)) AS Fld12803Balance_, " +
            "CAST(SUM(T2._Fld12802) AS NUMERIC(32, 8)) AS Fld12802Balance_ " +
            "FROM dbo._AccumRgT12814 T2 WITH(NOLOCK) " +
            "WHERE T2._Period = CAST('5999-11-01 00:00:00' AS datetime2(3)) AND (T2._Fld12804 <> 0 OR T2._Fld12803 <> 0 OR T2._Fld12802 <> 0) AND (T2._Fld12804 <> 0 OR T2._Fld12803 <> 0 " +
            "OR T2._Fld12802 <> 0) " +
            "GROUP BY T2._Fld12794RRef, " +
            "T2._Fld12795RRef, " +
            "T2._Fld12798_TYPE, " +
            "T2._Fld12798_RTRef, " +
            "T2._Fld12798_RRRef " +
            "HAVING (CAST(SUM(T2._Fld12804) AS NUMERIC(33, 8))) <> 0.0 OR (CAST(SUM(T2._Fld12803) AS NUMERIC(33, 8))) <> 0.0 OR (CAST(SUM(T2._Fld12802) " +
            "AS NUMERIC(32, 8))) <> 0.0) T1 " +
            "LEFT OUTER JOIN dbo._Document277_VT7622 T3 WITH(NOLOCK) " +
            "ON ((T1.Fld12794RRef = T3._Fld7624RRef) AND (T1.Fld12798_TYPE = CASE WHEN T3._Document277_IDRRef IS NOT NULL THEN 0x08 END AND T1.Fld12798_RTRef = CASE " +
            "WHEN T3._Document277_IDRRef IS NOT NULL THEN 0x00000115 END AND T1.Fld12798_RRRef = T3._Document277_IDRRef)) " +
            "LEFT OUTER JOIN dbo._Enum563 T47 WITH(NOLOCK) " +
            "ON T3._Fld7632RRef = T47._IDRRef " +
            "LEFT OUTER JOIN dbo._Document175 T4 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF AND T1.Fld12798_RRRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Document284 T5 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C AND T1.Fld12798_RRRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Document275 T6 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 AND T1.Fld12798_RRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Document249 T7 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F9 AND T1.Fld12798_RRRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Document278 T8 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 AND T1.Fld12798_RRRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Document219 T9 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB AND T1.Fld12798_RRRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Document250 T10 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA AND T1.Fld12798_RRRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Document242 T11 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 AND T1.Fld12798_RRRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Document277 T12 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 AND T1.Fld12798_RRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T45 WITH(NOLOCK) " +
            "ON T12._Fld7602_RRRef = T45._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T46 WITH(NOLOCK) " +
            "ON T11._Fld5728RRef = T46._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T40 WITH(NOLOCK) " +
            "ON T4._Fld2602RRef = T40._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T41 WITH(NOLOCK) " +
            "ON T6._Fld7409RRef = T41._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T42 WITH(NOLOCK) " +
            "ON T8._Fld7781RRef = T42._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T43 WITH(NOLOCK) " +
            "ON T10._Fld6255RRef = T43._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T44 WITH(NOLOCK) " +
            "ON T12._Fld7587RRef = T44._IDRRef " +
            "LEFT OUTER JOIN dbo._Document260 T13 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000104 AND T1.Fld12798_RRRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Document162 T14 WITH(NOLOCK) " +
            "ON T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 AND T1.Fld12798_RRRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T15 WITH(NOLOCK) " +
            "ON T4._Fld2601RRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T16 WITH(NOLOCK) " +
            "ON T5._Fld8326RRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T17 WITH(NOLOCK) " +
            "ON T6._Fld7408RRef = T17._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T18 WITH(NOLOCK) " +
            "ON T8._Fld7783RRef = T18._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T19 WITH(NOLOCK) " +
            "ON T10._Fld6254RRef = T19._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T20 WITH(NOLOCK) " +
            "ON T12._Fld7590RRef = T20._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T21 WITH(NOLOCK) " +
            "ON T4._Fld2594RRef = T21._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T22 WITH(NOLOCK) " +
            "ON T5._Fld8324RRef = T22._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T23 WITH(NOLOCK) " +
            "ON T6._Fld7413RRef = T23._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T24 WITH(NOLOCK) " +
            "ON T7._Fld6154RRef = T24._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T25 WITH(NOLOCK) " +
            "ON T8._Fld7787RRef = T25._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T26 WITH(NOLOCK) " +
            "ON T9._Fld4251RRef = T26._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T27 WITH(NOLOCK) " +
            "ON T10._Fld6253RRef = T27._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T28 WITH(NOLOCK) " +
            "ON T11._Fld5726RRef = T28._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T29 WITH(NOLOCK) " +
            "ON T12._Fld7594RRef = T29._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T30 WITH(NOLOCK) " +
            "ON T13._Fld6601RRef = T30._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T31 WITH(NOLOCK) " +
            "ON T14._Fld1929RRef = T31._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T32 WITH(NOLOCK) " +
            "ON T1.Fld12795RRef = T32._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T33 WITH(NOLOCK) " +
            "ON T4._Fld2597RRef = T33._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T34 WITH(NOLOCK) " +
            "ON T6._Fld7407RRef = T34._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T35 WITH(NOLOCK) " +
            "ON T8._Fld7778RRef = T35._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T36 WITH(NOLOCK) " +
            "ON T10._Fld6256RRef = T36._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T37 WITH(NOLOCK) " +
            "ON T12._Fld7584RRef = T37._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T38 WITH(NOLOCK) " +
            "ON T14._Fld1925RRef = T38._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 T39 WITH(NOLOCK) " +
            "ON T1.Fld12794RRef = T39._IDRRef " +
            "WHERE T1.Fld12802Balance_ > 0 " +
            "AND CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2) THEN 0 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000AF) THEN 1 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2) THEN 2 ELSE CASE " +
            "WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115) THEN 3 ELSE 4 END END END END NOT IN (0, 4) " +
            "AND T32._Description IN @StorageNames " +
            "GROUP BY " +
            "T3._Fld7629, " +
            "T12._Fld7604, " +
            "T47._EnumOrder, " +
            "CASE " +
            "WHEN T40._IDRRef IS NOT NULL " +
            "THEN T40._Code " +
            "WHEN T41._IDRRef IS NOT NULL " +
            "THEN T41._Code " +
            "WHEN T42._IDRRef IS NOT NULL " +
            "THEN T42._Code " +
            "WHEN T43._IDRRef IS NOT NULL " +
            "THEN T43._Code " +
            "WHEN T44._IDRRef IS NOT NULL " +
            "THEN T44._Code " +
            "ELSE NULL " +
            "END, " +
            "CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2) THEN 0 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000AF) THEN 1 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2) " +
            "THEN 2 ELSE CASE WHEN (T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115) THEN 3 ELSE 4 END END END END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T4._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T5._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T6._Number WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000F9 THEN T7._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T8._Number " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB THEN T9._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA " +
            "THEN T10._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 THEN T11._Number WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Number WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000104 THEN T13._Number " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T14._Number ELSE CAST(NULL AS NVARCHAR(11)) END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T4._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T5._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T6._Date_Time WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000F9 THEN T7._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T8._Date_Time " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB THEN T9._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA " +
            "THEN T10._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 THEN T11._Date_Time WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Date_Time WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000104 THEN T13._Date_Time " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T14._Date_Time ELSE CAST(NULL AS DATETIME) END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C THEN T5._Fld8327 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 " +
            "THEN T8._Fld7786 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Fld7593 ELSE CAST(NULL AS NVARCHAR(30)) END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C THEN T5._Fld8328 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 " +
            "THEN T8._Fld7780 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Fld7586 ELSE CAST(NULL AS DATETIME) END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T4._Fld2604 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 " +
            "THEN T6._Fld7412 WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T8._Fld7785 WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Fld7592 ELSE CAST(NULL AS NUMERIC(10, 4)) END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T15._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x0000011C " +
            "THEN T16._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 THEN T17._Code WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000116 THEN T18._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA THEN T19._Code " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T20._Code ELSE CAST(NULL AS NVARCHAR(9)) END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T21._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x0000011C THEN T22._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 " +
            "THEN T23._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F9 THEN T24._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000116 THEN T25._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000DB THEN T26._Description " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000FA THEN T27._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000F2 " +
            "THEN T28._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T29._Description WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x00000104 THEN T30._Description WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T31._Description " +
            "ELSE CAST(NULL AS NVARCHAR(50)) END, " +
            "T32._Description, " +
            "CASE WHEN T45._Description IS NOT NULL THEN T45._Description ELSE T46._Description END, " +
            "CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000AF THEN T33._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000113 " +
            "THEN T34._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000116 THEN T35._Code WHEN T1.Fld12798_TYPE = 0x08 " +
            "AND T1.Fld12798_RTRef = 0x000000FA THEN T36._Code WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T37._Code " +
            "WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x000000A2 THEN T38._Code ELSE CAST(NULL AS NVARCHAR(3)) END, " +
            "T39._Code, " +
            "T39._Fld1306, " +
            "T1.Fld12802Balance_, " +
            "T3._Fld7630, " +
            "T3._Fld16225, " +
            "T3._Fld16226, " +
            "T3._Fld7631, " +
            "T3._Fld7633, " +
            "T1.Fld12803Balance_, " +
            "T1.Fld12804Balance_, " +
            "T1.Fld12798_RRRef, " +
            "(CAST(T3._Fld15561 AS NUMERIC(20, 8)) / T3._Fld7629), " +
            "CASE WHEN T3._Fld7629 IS NOT NULL THEN T3._Fld7629 ELSE T1.Fld12802Balance_ END, " +
            "((CASE WHEN (CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Fld15560 ELSE CAST(NULL AS NUMERIC(15, 8)) END = 0.0) " +
            "THEN 1.0 ELSE CASE WHEN T1.Fld12798_TYPE = 0x08 AND T1.Fld12798_RTRef = 0x00000115 THEN T12._Fld15560 ELSE CAST(NULL AS NUMERIC(15, 8)) END " +
            "END * T3._Fld15561) / T3._Fld7629) " +
            ", T3._Fld16225 " +
            ", CAST(CASE " +
            "WHEN T12._IDRRef IS NOT NULL " +
            "THEN  T12._Fld7589 " +
            "WHEN T11._IDRRef IS NOT NULL " +
            "THEN  T11._Fld5735 " +
            "WHEN T4._IDRRef IS NOT NULL " +
            "THEN  T4._Fld2606 " +
            "ELSE NULL " +
            "END AS nvarchar(max)) " +
            "ORDER BY [DocumentDate], [DocumentNumber]",
            new { StorageNames = storageNames }
        );
    }

    public IEnumerable<SyncConsignment> GetAmgAllSyncConsignments(IEnumerable<string> storageNames) {
        return _amgOneCConnection.Query<SyncConsignment>(
            "SELECT " +
            "T1.DocumentRef [DocumentId], " +
            "T15._Fld9307 [DocumentValue], " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000CB " +
            "THEN 0 " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000DB " +
            "THEN 1 " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x00000124 " +
            "THEN 2 " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x0000014A " +
            "THEN 3 " +
            "ELSE 4 " +
            "END " +
            "END " +
            "END " +
            "END [TypeDocument], " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000CB " +
            "THEN T11._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000DB " +
            "THEN T12._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x00000124 " +
            "THEN T13._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x00000135 " +
            "THEN T14._Number " +
            "ELSE T15._Number " +
            "END " +
            "END " +
            "END " +
            "END AS [DocumentNumber], " +
            "CASE " +
            "WHEN (CASE " +
            "WHEN T1.[Type] = 0x000000CB " +
            "THEN T11._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000DB " +
            "THEN T12._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x00000124 " +
            "THEN T13._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x00000135 " +
            "THEN T14._Date_Time " +
            "ELSE T15._Date_Time " +
            "END " +
            "END " +
            "END " +
            "END) = N'2001-01-01 00:00:00.000' " +
            "THEN NULL " +
            "ELSE CASE " +
            "WHEN T1.[Type] = 0x000000CB " +
            "THEN T11._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000DB " +
            "THEN T12._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x00000124 " +
            "THEN T13._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x00000135 " +
            "THEN T14._Date_Time " +
            "ELSE T15._Date_Time " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END AS [DocumentDate], " +
            "CASE " +
            "WHEN T1.[Type] = 0x0000014A " +
            "THEN T15._Fld9296 " +
            "ELSE NULL " +
            "END AS [DocumentArrivalNumber], " +
            "CASE " +
            "WHEN (CASE " +
            "WHEN  T1.[Type] = 0x0000014A " +
            "THEN T15._Fld9289 " +
            "ELSE GETDATE() " +
            "END) = N'2001-01-01 00:00:00.000' " +
            "THEN NULL " +
            "ELSE CASE " +
            "WHEN  T1.[Type] = 0x0000014A " +
            "THEN CASE WHEN T15._Fld9289 = N'4001-01-01 00:00:00.000' THEN NULL ELSE T15._Fld9289 END " +
            "ELSE NULL " +
            "END " +
            "END AS [DocumentArrivalDate], " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000CB " +
            "THEN T11._Fld2531 / T11._Fld2532 " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000DB " +
            "THEN T12._Fld3386 / T12._Fld3389 " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x0000014A " +
            "THEN T15._Fld9295 / T15._Fld9294 " +
            "ELSE NULL " +
            "END " +
            "END " +
            "END AS [RateExchange], " +
            "T19.[_Code] [ClientCode], " +
            "'ТОВ «АМГ «КОНКОРД»' [OrganizationName], " +
            "T3._Description [StorageName], " +
            "CASE WHEN T32._Description IS NOT NULL THEN T32._Description ELSE T33._Description END [IncomeStorageName], " +
            "T20._Code [CurrencyCode], " +
            "T4._Code [ProductCode], " +
            "T4._Fld1765 [VendorCode], " +
            "T18._Fld9340 [IncomeQty], " +
            "T1.Fld15285Balance_ [Qty], " +
            "T18._Fld9341 [NetValue], " +
            "T1.Fld15286Balance_ [Value], " +
            "T18._Fld16060 [CustomsRate], " +
            "T18._Fld16128 [CustomsValue], " +
            "T1.Fld15287Balance_ [Vat], " +
            "T34._EnumOrder [VatTypeAmg], " +
            "CAST(ISNULL((TRY_CAST(T18._Fld9358 AS NUMERIC(20, 8)) / T18._Fld9340), 0.000) AS NUMERIC(20,4)) [WeightPer], " +
            "CAST(ISNULL((CASE WHEN (CASE WHEN T1.[TypeDocument] = 0x08 AND T1.[Type] = 0x0000014A THEN T15._Fld9330 ELSE CAST(NULL AS NUMERIC(15, 8)) END = 0.0) " +
            "THEN 1.0 ELSE CASE WHEN T1.[TypeDocument] = 0x08 AND T1.[Type] = 0x0000014A THEN T15._Fld9330 ELSE CAST(NULL AS NUMERIC(15, 8)) END " +
            "END * T18._Fld9358) / T18._Fld9340, 0.000) AS NUMERIC(20,4)) [WeightBruttoPer], " +
            "CASE " +
            "WHEN T21._IDRRef IS NOT NULL " +
            "THEN T21._Code " +
            "WHEN T23._IDRRef IS NOT NULL " +
            "THEN T23._Code " +
            "WHEN T25._IDRRef IS NOT NULL " +
            "THEN T25._Code " +
            "WHEN T27._IDRRef IS NOT NULL " +
            "THEN T27._Code " +
            "WHEN T28._IDRRef IS NOT NULL " +
            "THEN T28._Code " +
            "ELSE NULL " +
            "END [AgreementCode], " +
            "( " +
            "SELECT " +
            "TOP 1 SUM(DISTINCT T30._Fld9020) " +
            "FROM dbo._Document327_VT9037 T29 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document327 T30 WITH(NOLOCK) " +
            "ON (T29._Document327_IDRRef = T30._IDRRef) " +
            "WHERE T29._Fld9048_RRRef = T1.DocumentRef " +
            ") [TotalSpendAmount] " +
            ", T18._Fld16060 [Rate] " +
            ", T29._Code  [UKTVEDCode] " +
            ", T29._Description  [UKTVEDName] " +
            ", T30._EnumOrder [IsImported] " +
            ", CASE " +
            "WHEN T15._IDRRef IS NOT NULL " +
            "THEN T15._Fld9292 " +
            "WHEN T13._IDRRef IS NOT NULL " +
            "THEN T13._Fld7115 " +
            "WHEN T12._IDRRef IS NOT NULL " +
            "THEN T12._Fld3388 " +
            "ELSE NULL " +
            "END [Comment], " +
            "T18._Fld9342 [TotalValue], " +
            "T18._Fld9344 [TotalVat] " +
            "FROM (SELECT " +
            "T2._Fld15281_TYPE AS [TypeDocument], " +
            "T2._Fld15281_RTRef AS [Type], " +
            "T2._Fld15281_RRRef AS [DocumentRef], " +
            "T2._Fld15277RRef AS Fld15277RRef, " +
            "T2._Fld15278RRef AS Fld15278RRef, " +
            "CAST(SUM(T2._Fld15287) AS NUMERIC(33, 8)) AS Fld15287Balance_, " +
            "CAST(SUM(T2._Fld15286) AS NUMERIC(33, 8)) AS Fld15286Balance_, " +
            "CAST(SUM(T2._Fld15285) AS NUMERIC(32, 8)) AS Fld15285Balance_ " +
            "FROM dbo._AccumRgT15297 T2 WITH(NOLOCK) " +
            "WHERE T2._Period = CAST('5999-11-01 00:00:00' AS datetime2(3)) AND " +
            "(T2._Fld15287 <> 0 OR T2._Fld15286 <> 0 OR T2._Fld15285 <> 0) AND (T2._Fld15287 <> 0 OR T2._Fld15286 <> 0 OR T2._Fld15285 <> 0) " +
            "AND T2._Fld15285 <> 0 " +
            "GROUP BY T2._Fld15277RRef, " +
            "T2._Fld15278RRef, " +
            "T2._Fld15281_TYPE, " +
            "T2._Fld15281_RTRef, " +
            "T2._Fld15281_RRRef " +
            "HAVING (CAST(SUM(T2._Fld15287) AS NUMERIC(33, 8))) <> 0.0 OR (CAST(SUM(T2._Fld15286) AS NUMERIC(33, 8))) <> 0.0 OR (CAST(SUM(T2._Fld15285) AS NUMERIC(32, 8))) <> 0.0 " +
            ") T1 " +
            "LEFT OUTER JOIN dbo._Reference144 T3 WITH(NOLOCK) " +
            "ON T1.Fld15278RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference108 T4 WITH(NOLOCK) " +
            "ON T1.Fld15277RRef = T4._IDRRef " +
            "LEFT JOIN dbo._Document203 T11 ON T11._IDRRef = T1.[DocumentRef] " +
            "LEFT JOIN dbo._Document219 T12 ON T12._IDRRef = T1.[DocumentRef] " +
            "LEFT OUTER JOIN dbo._Reference66 T21 WITH(NOLOCK) " +
            "ON T12._Fld3384RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Document328 T22 WITH(NOLOCK) " +
            "ON T1.[DocumentRef] = T22._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 T23 WITH(NOLOCK) " +
            "ON T22._Fld9098RRef = T23._IDRRef " +
            "LEFT OUTER JOIN dbo._Document331 T24 WITH(NOLOCK) " +
            "ON T1.[DocumentRef] = T24._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 T25 WITH(NOLOCK) " +
            "ON T24._Fld9506RRef = T25._IDRRef " +
            "LEFT OUTER JOIN dbo._Document299 T26 WITH(NOLOCK) " +
            "ON T1.[DocumentRef] = T26._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 T27 WITH(NOLOCK) " +
            "ON T26._Fld7639RRef = T27._IDRRef " +
            "LEFT JOIN dbo._Document292 T13 ON T13._IDRRef = T1.[DocumentRef] " +
            "LEFT JOIN dbo._Document309 T14 ON T14._IDRRef = T1.[DocumentRef] " +
            "LEFT JOIN dbo._Document330 T15 ON T15._IDRRef = T1.[DocumentRef] " +
            "LEFT OUTER JOIN dbo._Reference144 T32 WITH(NOLOCK) " +
            "ON T15._Fld9305_RRRef = T32._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference144 T33 WITH(NOLOCK) " +
            "ON T13._Fld7108RRef = T33._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 T28 WITH(NOLOCK) " +
            "ON T15._Fld9290RRef = T28._IDRRef " +
            "LEFT JOIN dbo._Document203_VT2555 T16 " +
            "ON T16._Document203_IDRRef = T11._IDRRef AND T16._Fld2561RRef = T4._IDRRef " +
            "LEFT JOIN dbo._Document292_VT7132 T17 " +
            "ON T17._Document292_IDRRef = T13._IDRRef AND T17._Fld7140RRef = T4._IDRRef " +
            "LEFT JOIN dbo._Document330_VT9333 T18 " +
            "ON T18._Document330_IDRRef = T15._IDRRef AND T18._Fld9335RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum652 T34 WITH(NOLOCK) " +
            "ON T18._Fld9343RRef = T34._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference86 T29 WITH(NOLOCK) " +
            "ON T18._Fld16059RRef = T29._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum445 T30 WITH(NOLOCK) " +
            "ON T29._Fld1428RRef = T30._IDRRef " +
            "LEFT JOIN dbo._Reference90 T19 " +
            "ON T19._IDRRef = " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000CB " +
            "THEN T16._Fld2560RRef " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000DB " +
            "THEN T12._Fld3383RRef " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x0000014A " +
            "THEN T15._Fld9293RRef " +
            "ELSE NULL " +
            "END " +
            "END " +
            "END " +
            "LEFT JOIN dbo._Reference35 T20 ON " +
            "T20._IDRRef = CASE " +
            "WHEN T1.[Type] = 0x000000CB " +
            "THEN T11._Fld2529RRef " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x000000DB " +
            "THEN T12._Fld3379RRef " +
            "ELSE " +
            "CASE " +
            "WHEN T1.[Type] = 0x0000014A " +
            "THEN T15._Fld9287RRef " +
            "ELSE NULL " +
            "END " +
            "END " +
            "END " +
            "WHERE T1.[Type] IN ( " +
            "0x000000CB, " +
            "0x000000DB, " +
            "0x00000124, " +
            "0x00000135, " +
            "0x0000014A " +
            ") " +
            "AND T3._Description IN @StorageNames ",
            new { StorageNames = storageNames },
            commandTimeout: 3600
        );
    }

    public IEnumerable<SyncConsignment> GetFilteredSyncOrderConsignments(DateTime from, DateTime to) {
        return _oneCConnection.Query<SyncConsignment>(
            "SELECT " +
            "T12._IDRRef [DocumentId], " +
            "T12._Fld7604 [DocumentValue], " +
            "3 [TypeDocument], " +
            "T12._Number [DocumentNumber], " +
            "T12._Date_Time [DocumentDate], " +
            "T12._Fld7593 [DocumentArrivalNumber], " +
            "CASE WHEN T12._Fld7586 = N'2001-01-01 00:00:00.000' THEN NULL ELSE T12._Fld7586 END [DocumentArrivalDate], " +
            "ISNULL(T12._Fld7592, 1.0000) [RateExchange], " +
            "CAST(T20._Code AS bigint) [ClientCode], " +
            "T29._Description [OrganizationName], " +
            "T45._Description [StorageName], " +
            "T45._Description [IncomeStorageName], " +
            "T37._Code [CurrencyCode], " +
            "CAST(T39._Code AS bigint) [ProductCode], " +
            "T39._Fld1306 [VendorCode], " +
            "T3._Fld7629 [Qty], " +
            "T3._Fld7629 [IncomeQty], " +
            "T3._Fld7630 [NetValue], " +
            "T3._Fld7631 [Value], " +
            "T3._Fld7631 [TotalValue], " +
            "T3._Fld16225 [CustomsRate], " +
            "T3._Fld16226 [CustomsValue], " +
            "T3._Fld7633 [VAT], " +
            "T47._EnumOrder [VatTypeFenix], " +
            "ISNULL((CAST(T3._Fld15561 AS NUMERIC(20, 8)) / T3._Fld7629), 0.000) [WeightPer], " +
            "ISNULL((T12._Fld15560 * T3._Fld15561) / T3._Fld7629, 0.000) [WeightBruttoPer], " +
            "CASE " +
            "WHEN T44._IDRRef IS NOT NULL " +
            "THEN T44._Code " +
            "ELSE NULL " +
            "END [AgreementCode] " +
            ", T3._Fld16225 [Rate] " +
            ", T12._Fld7589 [Comment] " +
            "FROM dbo._Document277 T12 " +
            "LEFT OUTER JOIN dbo._Document277_VT7622 T3 WITH(NOLOCK) " +
            "ON T3._Document277_IDRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 T39 WITH(NOLOCK) " +
            "ON T3._Fld7624RRef = T39._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum563 T47 WITH(NOLOCK) " +
            "ON T3._Fld7632RRef = T47._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T45 WITH(NOLOCK) " +
            "ON T12._Fld7602_RRRef = T45._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T44 WITH(NOLOCK) " +
            "ON T12._Fld7587RRef = T44._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T29 WITH(NOLOCK) " +
            "ON T12._Fld7594RRef = T29._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T20 WITH(NOLOCK) " +
            "ON T12._Fld7590RRef = T20._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T37 WITH(NOLOCK) " +
            "ON T12._Fld7584RRef = T37._IDRRef " +
            "WHERE T12._Date_Time >= @From " +
            "AND (T12._Marked = 0x00) " +
            "AND T12._Posted = 0x01 " +
            "AND T12._Date_Time <= @To " +
            "AND T29._Description IN @OrganizationNames ",
            new { From = from, To = to, OrganizationNames = ORGANIZATION_NAMES }
        );
    }

    public IEnumerable<SyncConsignmentDocumentInfo> GetSyncConsignmentDocumentInfos(DateTime from, DateTime to) {
        return _oneCConnection.Query<SyncConsignmentDocumentInfo>(
            "SELECT " +
            "T12._IDRRef [DocumentId], " +
            "CAST(T20._Code AS bigint) [ClientCode], " +
            "T45._Description [StorageName], " +
            "T12._Fld7593 [DocumentArrivalNumber], " +
            "CASE " +
            "WHEN T12._Fld7586 = N'2001-01-01 00:00:00.000' " +
            "THEN NULL " +
            "ELSE T12._Fld7586 " +
            "END [DocumentArrivalDate] " +
            "FROM dbo._Document277 T12 " +
            "LEFT OUTER JOIN dbo._Reference68 T20 WITH(NOLOCK) " +
            "ON T12._Fld7590RRef = T20._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T45 WITH(NOLOCK) " +
            "ON T12._Fld7602_RRRef = T45._IDRRef " +
            "WHERE T12._Date_Time >= @From " +
            "AND T12._Posted = 0x01 " +
            "AND T12._Date_Time <= @To ", new { From = from, To = to });
    }

    public IEnumerable<SyncConsignment> GetAmgFilteredSyncOrderConsignments(SyncConsignmentDocumentInfo[] syncConsignmentDocuments) {
        StringBuilder builder = new();

        builder.Append("CREATE TABLE #TempDocument ");
        builder.Append("( ");
        builder.Append("[DocumentArrivalNumber] nvarchar(80), ");
        builder.Append("[DocumentArrivalDate] datetime null, ");
        builder.Append(") ");

        for (int i = 0; i < syncConsignmentDocuments.Length; i++) {
            if (i.Equals(0) || i % 100 == 0) {
                builder.Append("INSERT INTO #TempDocument ");
                builder.Append("( ");
                builder.Append("[DocumentArrivalNumber] ");
                builder.Append(",[DocumentArrivalDate] ");
                builder.Append(") ");

                builder.Append("VALUES ");
            } else {
                builder.Append(", ");
            }

            builder.Append("( ");
            builder.Append($"N'{syncConsignmentDocuments[i].DocumentArrivalNumber}' ");
            builder.Append(syncConsignmentDocuments[i].DocumentArrivalDate.HasValue
                ? $",N'{syncConsignmentDocuments[i].DocumentArrivalDate.Value.Date.ToString("yyyy-dd-MM")}' "
                : $",N'{2001 - 01 - 01}' ");
            builder.Append(") ");
        }

        string query = builder +
                       "SELECT " +
                       "T15._IDRRef [DocumentId], " +
                       "T15._Fld9307 [DocumentValue], " +
                       "3 [TypeDocument], " +
                       "T15._Number AS [DocumentNumber], " +
                       "T15._Date_Time [DocumentDate], " +
                       "CASE " +
                       "WHEN T15._Fld9296 IS NOT NULL " +
                       "THEN T15._Fld9296 " +
                       "ELSE NULL " +
                       "END AS [DocumentArrivalNumber], " +
                       "CASE " +
                       "WHEN T15._Fld9289 = N'2001-01-01 00:00:00.000' " +
                       "THEN NULL " +
                       "ELSE " +
                       "CASE WHEN T15._Fld9289 = N'4001-01-01 00:00:00.000' THEN NULL ELSE T15._Fld9289 END " +
                       "END AS [DocumentArrivalDate], " +
                       "T15._Fld9295 / T15._Fld9294 AS [RateExchange], " +
                       "T19.[_Code] [ClientCode], " +
                       "'ТОВ «АМГ «КОНКОРД»' [OrganizationName], " +
                       "T3._Description [StorageName], " +
                       "T32._Description [IncomeStorageName], " +
                       "T20._Code [CurrencyCode], " +
                       "T4._Code [ProductCode], " +
                       "T4._Fld1765 [VendorCode], " +
                       "T18._Fld9340 [IncomeQty], " +
                       "T18._Fld9340 [Qty], " +
                       "T18._Fld9341 [NetValue], " +
                       "T18._Fld9342 [Value], " +
                       "T18._Fld9342 [TotalValue], " +
                       "T18._Fld16060 [CustomsRate], " +
                       "T18._Fld16128 [CustomsValue], " +
                       "T18._Fld9344 [Vat], " +
                       "T34._EnumOrder [VatTypeAmg], " +
                       "CAST(ISNULL((TRY_CAST(T18._Fld9358 AS NUMERIC(20, 8)) / T18._Fld9340), 0.000) AS NUMERIC(20,4)) [WeightPer], " +
                       "CAST(ISNULL((T15._Fld9330 * T18._Fld9358) / T18._Fld9340, 0.000) AS NUMERIC(20,4)) [WeightBruttoPer], " +
                       "T23._Code [AgreementCode], " +
                       "( " +
                       "SELECT " +
                       "TOP 1 SUM(DISTINCT T30._Fld9020) " +
                       "FROM dbo._Document327_VT9037 T29 WITH(NOLOCK) " +
                       "LEFT OUTER JOIN dbo._Document327 T30 WITH(NOLOCK) " +
                       "ON (T29._Document327_IDRRef = T30._IDRRef) " +
                       "WHERE T29._Fld9048_RRRef = T15._IDRRef " +
                       ") [TotalSpendAmount] " +
                       ", T18._Fld16060 [Rate] " +
                       ", T29._Code  [UKTVEDCode] " +
                       ", T29._Description  [UKTVEDName] " +
                       ", T30._EnumOrder [IsImported] " +
                       ", T15._Fld9292 [Comment] " +
                       "FROM dbo._Document330 T15 " +
                       "LEFT JOIN dbo._Document330_VT9333 T18 " +
                       "ON T18._Document330_IDRRef = T15._IDRRef " +
                       "LEFT OUTER JOIN dbo._Reference108 T4 WITH(NOLOCK) " +
                       "ON T18._Fld9335RRef = T4._IDRRef " +
                       "LEFT OUTER JOIN dbo._Reference144 T3 WITH(NOLOCK) " +
                       "ON T15._Fld9305_RRRef = T3._IDRRef " +
                       "LEFT OUTER JOIN dbo._Reference66 T23 WITH(NOLOCK) " +
                       "ON T15._Fld9290RRef = T23._IDRRef " +
                       "LEFT OUTER JOIN dbo._Reference144 T32 WITH(NOLOCK) " +
                       "ON T15._Fld9305_RRRef = T32._IDRRef " +
                       "LEFT OUTER JOIN dbo._Enum652 T34 WITH(NOLOCK) " +
                       "ON T18._Fld9343RRef = T34._IDRRef " +
                       "LEFT OUTER JOIN dbo._Reference86 T29 WITH(NOLOCK) " +
                       "ON T18._Fld16059RRef = T29._IDRRef " +
                       "LEFT OUTER JOIN dbo._Enum445 T30 WITH(NOLOCK) " +
                       "ON T29._Fld1428RRef = T30._IDRRef " +
                       "LEFT JOIN dbo._Reference90 T19 " +
                       "ON T19._IDRRef = T15._Fld9293RRef " +
                       "LEFT JOIN dbo._Reference35 T20 ON " +
                       "T20._IDRRef = T15._Fld9287RRef " +
                       "LEFT JOIN #TempDocument T21 ON " +
                       "T21.DocumentArrivalNumber = T15._Fld9296 " +
                       "AND T21.DocumentArrivalDate = CONVERT(date, T15._Date_Time) " +
                       "WHERE T15._IDRRef IS NOT NULL " +
                       "AND (T15._Marked = 0x00) " +
                       "AND T15._Posted = 0x01 " +
                       "AND T21.DocumentArrivalNumber IS NOT NULL ";

        IEnumerable<SyncConsignment> toReturn = _amgOneCConnection.Query<SyncConsignment>(
            query,
            commandTimeout: 3600
        );

        return toReturn;
    }

    public ProductIncome GetLastProductIncome() {
        return _remoteSyncConnection.Query<ProductIncome>(
            "SELECT TOP(1) * FROM [ProductIncome] " +
            "WHERE [ProductIncome].Deleted = 0 " +
            "ORDER BY ID DESC ").FirstOrDefault();
    }

    public IEnumerable<Client> GetAllClients() {
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
            "WHERE [Client].Deleted = 0 " +
            "AND ([Client].SourceAmgCode <> 0 " +
            "OR [Client].SourceFenixCode <> 0" +
            "OR [Agreement].[IsDefaultForSyncConsignment] = 1) ",
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
            }
        );

        return clients;
    }

    public IEnumerable<Product> GetAllProductsByProductCodes(string inStatement, bool forAmg) {
        List<Product> products = new();

        string cond = forAmg ? "AND [Product].SourceAmgCode IN " : "AND [Product].SourceFenixCode IN ";

        _remoteSyncConnection.Query<Product, ProductAvailability, Product>(
            "SELECT * " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "WHERE [Product].Deleted = 0 " +
            cond +
            inStatement,
            (product, availability) => {
                if (products.Any(p => p.Id.Equals(product.Id)))
                    product = products.First(p => p.Id.Equals(product.Id));
                else
                    products.Add(product);

                if (availability == null) return product;

                product.ProductAvailabilities.Add(availability);

                return product;
            }
        );

        return products;
    }

    public long Add(ProductIncome productIncome) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProductIncome] (FromDate, Number, UserId, StorageId, ProductIncomeType, Comment, Updated, [IsHide], IsFromOneC) " +
            "VALUES (@FromDate, @Number, @UserId, @StorageId, @ProductIncomeType, @Comment, GETUTCDATE(), @IsHide, @IsFromOneC); " +
            "SELECT SCOPE_IDENTITY()",
            productIncome
        ).Single();
    }

    public long Add(ProductCapitalization productCapitalization) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProductCapitalization] (Number, Comment, FromDate, OrganizationId, ResponsibleId, StorageId, Updated) " +
            "VALUES (@Number, @Comment, @FromDate, @OrganizationId, @ResponsibleId, @StorageId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productCapitalization
        ).Single();
    }

    public long Add(ProductCapitalizationItem item) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProductCapitalizationItem] (Qty, RemainingQty, Weight, UnitPrice, ProductId, ProductCapitalizationId, Updated) " +
            "VALUES (@Qty, @RemainingQty, @Weight, @UnitPrice, @ProductId, @ProductCapitalizationId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            item
        ).Single();
    }

    public long Add(Consignment consignment) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Consignment] " +
            "(IsVirtual, [FromDate], StorageId, OrganizationId, ProductIncomeId, ProductTransferId, IsImportedFromOneC, Updated) " +
            "VALUES " +
            "(@IsVirtual, @FromDate, @StorageId, @OrganizationId, @ProductIncomeId, @ProductTransferId, @IsImportedFromOneC, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            consignment
        ).First();
    }

    public void Update(Consignment consignment) {
        _remoteSyncConnection.Execute(
            "UPDATE [Consignment] " +
            "SET Deleted = @Deleted, Updated= GETUTCDATE() " +
            "WHERE ID = @Id",
            consignment
        );
    }

    public void Update(IEnumerable<Consignment> consignments) {
        _remoteSyncConnection.Execute(
            "UPDATE [Consignment] " +
            "SET Deleted = @Deleted, Updated= GETUTCDATE(), [FromDate] = @FromDate " +
            "WHERE ID = @Id",
            consignments
        );
    }

    public void Update(IEnumerable<ProductIncome> incomes) {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductIncome] " +
            "SET Deleted = @Deleted, Updated= GETUTCDATE(), [FromDate] = @FromDate " +
            "WHERE ID = @Id",
            incomes
        );
    }

    public long Add(ProductIncomeItem item) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProductIncomeItem] (SaleReturnItemId, ProductIncomeId, PackingListPackageOrderItemId, SupplyOrderUkraineItemId, Qty, RemainingQty, " +
            "ActReconciliationItemId, ProductCapitalizationItemId, Updated) " +
            "VALUES (@SaleReturnItemId, @ProductIncomeId, @PackingListPackageOrderItemId, @SupplyOrderUkraineItemId, @Qty, @RemainingQty, @ActReconciliationItemId, " +
            "@ProductCapitalizationItemId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            item
        ).Single();
    }

    public long Add(ConsignmentItem consignmentItem) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ConsignmentItem] " +
            "(Qty, RemainingQty, Weight, Price, NetPrice, DutyPercent, ProductId, ConsignmentId, RootConsignmentItemId, ProductIncomeItemId, " +
            "ProductSpecificationId, AccountingPrice, ExchangeRate, Updated) " +
            "VALUES " +
            "(@Qty, @RemainingQty, @Weight, @Price, @NetPrice, @DutyPercent, @ProductId, @ConsignmentId, @RootConsignmentItemId, @ProductIncomeItemId, " +
            "@ProductSpecificationId, @AccountingPrice, @ExchangeRate, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            consignmentItem
        ).Single();
    }

    public void Update(ConsignmentItem item) {
        _remoteSyncConnection.Execute(
            "UPDATE [ConsignmentItem] " +
            "SET Deleted = @Deleted, Qty = @Qty, RemainingQty = @RemainingQty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            item
        );
    }

    public long Add(ProductSpecification productSpecification) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProductSpecification] ([Name], SpecificationCode, [Locale], DutyPercent, AddedById, ProductId, IsActive, CustomsValue, Updated, [Duty], [VATPercent], [VATValue]) " +
            "VALUES (@Name, @SpecificationCode, @Locale, @DutyPercent, @AddedById, @ProductId, @IsActive, @CustomsValue, GETUTCDATE(), @Duty, @VATPercent, @VATValue); " +
            "SELECT SCOPE_IDENTITY();",
            productSpecification
        ).Single();
    }

    public void RemoveConsignmentItemMovementsByItemId(long consignmentItemId) {
        _remoteSyncConnection.Execute(
            "UPDATE [ConsignmentItemMovement] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ConsignmentItemId = @ConsignmentItemId",
            new { ConsignmentItemId = consignmentItemId }
        );
    }

    public void Add(ConsignmentItemMovement movement) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ConsignmentItemMovement] " +
            "(IsIncomeMovement, Qty, RemainingQty, MovementType, ConsignmentItemId, ProductIncomeItemId, DepreciatedOrderItemId, SupplyReturnItemId, OrderItemId, " +
            "ProductTransferItemId, OrderItemBaseShiftStatusId, TaxFreeItemId, SadItemId, Updated) " +
            "VALUES " +
            "(@IsIncomeMovement, @Qty, @RemainingQty, @MovementType, @ConsignmentItemId, @ProductIncomeItemId, @DepreciatedOrderItemId, @SupplyReturnItemId, @OrderItemId, " +
            "@ProductTransferItemId, @OrderItemBaseShiftStatusId, @TaxFreeItemId, @SadItemId, GETUTCDATE())",
            movement
        );
    }

    public long Add(SupplyOrderNumber supplyOrderNumber) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [SupplyOrderNumber] ([Number], [Updated]) " +
            "VALUES(@Number, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            supplyOrderNumber
        ).Single();
    }

    public long Add(SupplyProForm supplyProForm) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO SupplyProForm (NetPrice, Number, IsSkipped, DateFrom, ServiceNumber, Updated) " +
            "VALUES(@NetPrice, @Number, @IsSkipped, @DateFrom, @ServiceNumber, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            supplyProForm
        ).Single();
    }

    public long Add(SupplyOrder supplyOrder) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO SupplyOrder (IsOrderArrived, OrderArrivedDate, VechicalArrived, PlaneArrived, ShipArrived, CompleteDate, OrderShippedDate, " +
            "IsOrderShipped, IsCompleted, TransportationType, GrossPrice, NetPrice, ClientID, OrganizationID, Qty, SupplyOrderNumberID, SupplyProFormID, " +
            "PortWorkServiceID, TransportationServiceID, DateFrom, CustomAgencyServiceId, PortCustomAgencyServiceId, PlaneDeliveryServiceId, " +
            "VehicleDeliveryServiceId, IsDocumentSet, IsPlaced, Comment, ClientAgreementId, IsGrossPricesCalculated, IsPartiallyPlaced, IsFullyPlaced, " +
            "IsOrderInsidePoland, AdditionalAmount, AdditionalPercent, AdditionalPaymentCurrencyId, AdditionalPaymentFromDate, Updated) " +
            "VALUES(@IsOrderArrived, @OrderArrivedDate, @VechicalArrived, @PlaneArrived, @ShipArrived, @CompleteDate, @OrderShippedDate, @IsOrderShipped, " +
            "@IsCompleted, @TransportationType, @GrossPrice, @NetPrice, @ClientID, @OrganizationID, @Qty, @SupplyOrderNumberID, @SupplyProFormID, " +
            "@PortWorkServiceID, @TransportationServiceID, @DateFrom, @CustomAgencyServiceId, @PortCustomAgencyServiceId, @PlaneDeliveryServiceId, " +
            "@VehicleDeliveryServiceId, @IsDocumentSet, 0, @Comment, @ClientAgreementId, @IsGrossPricesCalculated, @IsPartiallyPlaced, " +
            "@IsFullyPlaced, @IsOrderInsidePoland, 0.00, 0.00, NULL, NULL, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            supplyOrder
        ).Single();
    }


    public long Add(SupplyInvoice supplyInvoice) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO SupplyInvoice (SupplyOrderID, Number, NetPrice, IsShipped, DateFrom, PaymentTo, ServiceNumber, Comment, IsPartiallyPlaced, " +
            "IsFullyPlaced, DateCustomDeclaration, Updated, [NumberCustomDeclaration]) " +
            "VALUES(@SupplyOrderID, @Number, @NetPrice, @IsShipped, @DateFrom, @PaymentTo, @ServiceNumber, @Comment, @IsPartiallyPlaced, @IsFullyPlaced, @DateCustomDeclaration, getutcdate(), " +
            "@NumberCustomDeclaration); " +
            "SELECT SCOPE_IDENTITY()",
            supplyInvoice
        ).Single();
    }

    public long Add(PackingList packingList) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [PackingList] " +
            "(MarkNumber, InvNo, PlNo, RefNo, No, FromDate, SupplyInvoiceId, IsDocumentsAdded, ExtraCharge, ContainerServiceId, Comment, IsPlaced, " +
            "IsVatOneApplied, IsVatTwoApplied, VatOnePercent, VatTwoPercent, Updated) " +
            "VALUES " +
            "(@MarkNumber, @InvNo, @PlNo, @RefNo, @No, @FromDate, @SupplyInvoiceId, @IsDocumentsAdded, @ExtraCharge, @ContainerServiceId, " +
            "@Comment, @IsPlaced, @IsVatOneApplied, @IsVatTwoApplied, @VatOnePercent, @VatTwoPercent, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            packingList
        ).Single();
    }

    public long Add(PackingListPackageOrderItem packingListPackageOrderItem) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [PackingListPackageOrderItem] " +
            "(Qty, RemainingQty, SupplyInvoiceOrderItemId, PackingListPackageId, PackingListId, IsPlaced, IsErrorInPlaced, AccountingGrossUnitPriceEur, " +
            "IsReadyToPlaced, UnitPrice, GrossWeight, NetWeight, UploadedQty, Placement, UnitPriceEur, GrossUnitPriceEur, ContainerUnitPriceEur, ExchangeRateAmount, " +
            "VatPercent, PlacedQty, Updated, [ProductIsImported], [ExchangeRateAmountUahToEur], [VatAmount], [UnitPriceEurWithVat]) " +
            "VALUES " +
            "(@Qty, @RemainingQty, @SupplyInvoiceOrderItemId, @PackingListPackageId, @PackingListId, @IsPlaced, @IsErrorInPlaced, @IsReadyToPlaced, @AccountingGrossUnitPriceEur, " +
            "@UnitPrice, @GrossWeight, @NetWeight, @UploadedQty, @Placement, " +
            "@UnitPriceEur, @GrossUnitPriceEur, @ContainerUnitPriceEur, @ExchangeRateAmount, @VatPercent, @PlacedQty, getutcdate(), @ProductIsImported, " +
            "@ExchangeRateAmountUahToEur, @VatAmount, @UnitPriceEurWithVat); " +
            "SELECT SCOPE_IDENTITY()",
            packingListPackageOrderItem
        ).Single();
    }

    public long Add(ProductAvailability productAvailability) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProductAvailability] (ProductId, StorageId, Amount, Updated) " +
            "VALUES (@ProductId, @StorageId, @Amount, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productAvailability
        ).Single();
    }

    public void Update(ProductAvailability productAvailability) {
        _remoteSyncConnection.Execute(
            "UPDATE ProductAvailability " +
            "SET Amount = @Amount, Updated = getutcdate() " +
            "WHERE ID = @Id",
            productAvailability
        );
    }

    public ProductAvailability GetProductAvailabilityById(long id) {
        return _remoteSyncConnection.Query<ProductAvailability>(
            "SELECT * " +
            "FROM [ProductAvailability] " +
            "WHERE [ID] = @Id",
            new { Id = id }
        ).Single();
    }

    public void Add(ProductPlacement productPlacement) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ProductPlacement] " +
            "(Qty, StorageNumber, RowNumber, CellNumber, ProductId, StorageId, PackingListPackageOrderItemId, SupplyOrderUkraineItemId, " +
            "ProductIncomeItemId, ConsignmentItemId, Updated) " +
            "VALUES " +
            "(@Qty, @StorageNumber, @RowNumber, @CellNumber, @ProductId, @StorageId, @PackingListPackageOrderItemId, @SupplyOrderUkraineItemId, " +
            "@ProductIncomeItemId, @ConsignmentItemId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productPlacement
        );
    }

    public long Add(SupplyInvoiceOrderItem supplyInvoiceOrderItem) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [SupplyInvoiceOrderItem] (Qty, SupplyOrderItemId, SupplyInvoiceId, UnitPrice, Updated, ProductID) " +
            "VALUES (@Qty, @SupplyOrderItemId, @SupplyInvoiceId, @UnitPrice, getutcdate(), @ProductId); " +
            "SELECT SCOPE_IDENTITY()",
            supplyInvoiceOrderItem
        ).Single();
    }

    public long Add(SupplyOrderItem supplyOrderItem) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO SupplyOrderItem " +
            "([Description], ItemNo, ProductID, Qty, StockNo, SupplyOrderID, TotalAmount, UnitPrice, GrossWeight, NetWeight, IsPacked, Updated) " +
            "VALUES " +
            "(@Description, @ItemNo, @ProductID, @Qty, @StockNo, @SupplyOrderID, @TotalAmount, @UnitPrice, @GrossWeight, @NetWeight, @IsPacked, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            supplyOrderItem
        ).Single();
    }

    public void RemoveProductCapitalizationById(long id) {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductCapitalization] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveProductCapitalizationItemById(long id) {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductCapitalizationItem] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemovePackingListPackageOrderItemById(long id) {
        _remoteSyncConnection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemovePackingListById(long id) {
        _remoteSyncConnection.Execute(
            "UPDATE [PackingList] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveSupplyInvoiceOrderItemById(long id) {
        _remoteSyncConnection.Execute(
            "UPDATE [SupplyInvoiceOrderItem] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveSupplyInvoiceById(long id) {
        _remoteSyncConnection.Execute(
            "UPDATE [SupplyInvoice] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveSupplyOrderItemById(long id) {
        _remoteSyncConnection.Execute(
            "UPDATE [SupplyOrderItem] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveSupplyOrderById(long id) {
        _remoteSyncConnection.Execute(
            "UPDATE [SupplyOrder] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveProductIncomeItemById(long id) {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductIncomeItem] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveProductIncomeById(long id) {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductIncome] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public IEnumerable<Consignment> GetAllConsignmentsToDelete() {
        List<Consignment> consignments = new();

        Type[] types = {
            typeof(Consignment),
            typeof(ConsignmentItem),
            typeof(Product),
            typeof(ProductAvailability),
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(SupplyInvoice),
            typeof(SupplyOrderUkraineItem),
            typeof(ActReconciliationItem),
            typeof(ProductCapitalizationItem)
        };

        Func<object[], Consignment> mapper = objects => {
            Consignment consignment = (Consignment)objects[0];
            ConsignmentItem item = (ConsignmentItem)objects[1];
            Product product = (Product)objects[2];
            ProductAvailability availability = (ProductAvailability)objects[3];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[4];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[5];
            PackingList packingList = (PackingList)objects[6];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[7];
            SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[8];
            ActReconciliationItem actReconciliationItem = (ActReconciliationItem)objects[9];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[10];

            if (item == null) return consignment;

            if (consignments.Any(c => c.Id.Equals(consignment.Id)))
                consignment = consignments.First(c => c.Id.Equals(consignment.Id));
            else
                consignments.Add(consignment);

            if (availability != null) product.ProductAvailabilities.Add(availability);

            item.Product = product;
            item.ProductIncomeItem = productIncomeItem;

            if (productIncomeItem.PackingListPackageOrderItemId.HasValue) {
                productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;
                packingListPackageOrderItem.PackingList = packingList;
                packingList.SupplyInvoice = supplyInvoice;
            } else if (productIncomeItem.SupplyOrderUkraineItemId.HasValue) {
                productIncomeItem.SupplyOrderUkraineItem = supplyOrderUkraineItem;
            } else if (productIncomeItem.ActReconciliationItemId.HasValue) {
                productIncomeItem.ActReconciliationItem = actReconciliationItem;
            } else if (productIncomeItem.ProductCapitalizationItemId.HasValue) {
                productIncomeItem.ProductCapitalizationItem = productCapitalizationItem;
            }

            consignment.ConsignmentItems.Add(item);

            return consignment;
        };

        _remoteSyncConnection.Query(
            "SELECT * " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = ( " +
            "SELECT TOP(1) [JoinAvailability].ID " +
            "FROM [ProductAvailability] AS [JoinAvailability] " +
            "WHERE [JoinAvailability].ProductID = [Product].ID " +
            "AND [JoinAvailability].StorageID = [Consignment].StorageID " +
            "AND [JoinAvailability].Deleted = 0 " +
            ") " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ConsignmentItem].[ProductIncomeItemID] = [ProductIncomeItem].[ID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID] " +
            "LEFT JOIN [ActReconciliationItem] " +
            "ON [ActReconciliationItem].[ID] = [ProductIncomeItem].[ActReconciliationItemID] " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].[ID] = [ProductIncomeItem].[ProductCapitalizationItemID] " +
            "WHERE [Consignment].IsImportedFromOneC = 1 " +
            "AND [Consignment].Deleted = 0 ",
            types,
            mapper
        );

        return consignments;
    }

    public void CleanAllConsignmentsToDelete() {
        _remoteSyncConnection.Execute(
            "UPDATE [SupplyOrder] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [SupplyOrderItem].SupplyOrderID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [SupplyOrderItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [SupplyOrderItem] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [SupplyOrderItem].ID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [SupplyOrderItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [SupplyInvoice] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [SupplyInvoiceOrderItem].SupplyInvoiceID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [SupplyInvoiceOrderItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [SupplyInvoiceOrderItem] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [SupplyInvoiceOrderItem].ID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [SupplyInvoiceOrderItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [PackingList] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [PackingListPackageOrderItem].PackingListID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [PackingListPackageOrderItem] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [PackingListPackageOrderItem].ID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [PackingListPackageOrderItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [ProductCapitalization] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [ProductCapitalizationItem].ProductCapitalizationID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [ProductCapitalizationItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [ProductCapitalizationItem] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [ProductCapitalizationItem].ID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [ProductCapitalizationItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [ProductIncome] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [ProductIncomeItem].ProductIncomeID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [ProductIncomeItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [ConsignmentItemMovement] SET Deleted = 1 " +
            "WHERE ProductIncomeItemID IN (" +
            "SELECT [ProductIncomeItem].ID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [ProductIncomeItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [ProductIncomeItem] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [ProductIncomeItem].ID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "AND [ProductIncomeItem].ID IS NOT NULL " +
            "); " +
            "UPDATE [ProductPlacement] " +
            "SET [Deleted] = 1, [Qty] = 0 " +
            "WHERE ConsignmentItemID IN ( " +
            "SELECT [ConsignmentItem].ID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "); " +
            "UPDATE [ConsignmentItem] " +
            "SET [Deleted] = 1, [Qty] = 0, [RemainingQty] = 0 " +
            "WHERE ID IN ( " +
            "SELECT [ConsignmentItem].ID " +
            "FROM [Consignment] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ConsignmentID = [Consignment].ID " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "); " +
            "UPDATE [Consignment] SET Deleted = 1 " +
            "WHERE ID IN ( " +
            "SELECT [Consignment].ID " +
            "FROM [Consignment] " +
            "WHERE [Consignment].Deleted = 0 " +
            "AND [Consignment].IsImportedFromOneC = 1 " +
            "); " +
            "UPDATE [DeliveryProductProtocol] " +
            "SET [Deleted] = 1 " +
            "WHERE [Comment] LIKE '%' + N'Ввід залишків з 1С.' + '%' "
        );
    }

    public IEnumerable<SyncConsignmentSpecification> GetAmgAllSyncConsignmentSpecifications(
        byte[] documentId) {
        return _amgOneCConnection.Query<SyncConsignmentSpecification>(
            "SELECT " +
            "T1._Date_Time [CustomsDeclarationDate] " +
            ", T1._Fld3781 [NumberDeclarationDate] " +
            ", T3._Code [ProductCode] " +
            ", T3._Fld1765 [ProductVendorCode] " +
            ", T2._Fld3846 [Duty] " +
            ", T2._Fld3848 [Vat] " +
            "FROM dbo._Document224 T1 " +
            "LEFT JOIN [_Document224_VT3836] T2 " +
            "ON T2._Document224_IDRRef = T1._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference108 T3 WITH(NOLOCK) " +
            "ON T2._Fld3839RRef = T3._IDRRef " +
            "WHERE T2._Fld3852_RRRef = @Id " +
            "AND T1._Marked = 0x00 ",
            new { Id = documentId },
            commandTimeout: 3600
        );
    }

    // SELECT
    //     T3._Code [ProductCode],
    // T2._Fld9289 [CustomsDeclarationDate],
    // T2._Fld9296 [NumberDeclarationDate],
    // T3._Fld1765 [ProductVendorCode],
    // T1._Fld16128 [Duty],
    // T1._Fld9344 [Vat],
    // T4._Description [SpecificationName],
    // T4._Code [SpecificationCode]
    // FROM dbo._Document330_VT9333 T1 WITH(NOLOCK)
    // LEFT OUTER JOIN dbo._Document330 T2 WITH(NOLOCK)
    // ON (T1._Document330_IDRRef = T2._IDRRef)
    // LEFT OUTER JOIN dbo._Reference108 T3 WITH(NOLOCK)
    // ON T1._Fld9335RRef = T3._IDRRef
    //     LEFT OUTER JOIN dbo._Reference86 T4 WITH(NOLOCK)
    // ON T1._Fld16059RRef = T4._IDRRef
    //     WHERE T2._IDRRef = 0x88DDB8CB29DC7A6911EDBF533349A2AD


    public IEnumerable<SyncConsignmentSpecification> GetFenixAllSyncConsignmentSpecifications(
        byte[] documentId) {
        return _oneCConnection.Query<SyncConsignmentSpecification>(
            "SELECT " +
            "T2._Date_Time [CustomsDeclarationDate] " +
            ",T2._Fld3001 [NumberDeclarationDate] " +
            ",T3._Code [ProductCode] " +
            ",T3._Fld1306 [ProductVendorCode] " +
            ",T1._Fld3058 [Duty] " +
            ",T1._Fld3060 [Vat] " +
            "FROM dbo._Document181_VT3048 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document181 T2 WITH(NOLOCK) " +
            "ON (T1._Document181_IDRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference84 T3 WITH(NOLOCK) " +
            "ON T1._Fld3051RRef = T3._IDRRef " +
            "WHERE T1._Fld3064_RRRef = @Id " +
            "AND T2._Marked = 0x00 ",
            new { Id = documentId },
            commandTimeout: 3600
        );
    }

    public void UpdateActiveSpecification() {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductSpecification] SET IsActive = 0 WHERE IsActive = 1 " +
            "INSERT INTO [ProductSpecification] " +
            "([SpecificationCode], [Name], [Locale], [AddedByID], [DutyPercent], [ProductID], [IsActive], [Updated]) " +
            "SELECT [ProductSpecification].SpecificationCode " +
            ", [ProductSpecification].[Name] " +
            ", [Organization].Culture " +
            ", [ProductSpecification].AddedByID " +
            ", [ProductSpecification].DutyPercent " +
            ", [ProductSpecification].ProductID " +
            ", 1 " +
            ", GETUTCDATE() " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ID = ( " +
            "SELECT TOP(1) [ProductSpecification].ID " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ProductSpecificationID = [ProductSpecification].ID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "WHERE [ProductSpecification].ProductID = [Product].ID " +
            "ORDER BY CASE WHEN [ProductSpecification].SpecificationCode <> N'' THEN 0 ELSE 1 END, [Consignment].FromDate DESC " +
            ") " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ProductSpecificationID = [ProductSpecification].ID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "WHERE [Product].Deleted = 0 " +
            "AND [ProductSpecification].ID IS NOT NULL " +
            "EXEC sp_updatestats ",
            commandTimeout: 3600
        );
    }

    public GovExchangeRate GetGovByCurrencyIdAndCode(long id, string code, DateTime fromDate) {
        return _remoteSyncConnection.Query<GovExchangeRate>(
            "SELECT TOP(1) " +
            "[GovExchangeRate].ID, " +
            "(CASE " +
            "WHEN [GovExchangeRateHistory].Amount IS NOT NULL " +
            "THEN [GovExchangeRateHistory].Amount " +
            "ELSE [GovExchangeRate].Amount " +
            "END) AS [Amount] " +
            "FROM [GovExchangeRate] " +
            "LEFT JOIN [GovExchangeRateHistory] " +
            "ON [GovExchangeRateHistory].GovExchangeRateID = [GovExchangeRate].ID " +
            "AND [GovExchangeRateHistory].Created <= @FromDate " +
            "WHERE [GovExchangeRate].CurrencyID = @Id " +
            "AND [GovExchangeRate].Code = @Code " +
            "ORDER BY [GovExchangeRateHistory].ID DESC",
            new { Id = id, Code = code, FromDate = fromDate }
        ).FirstOrDefault();
    }

    public ExchangeRate GetByCurrencyIdAndCode(long id, string code, DateTime fromDate) {
        return _remoteSyncConnection.Query<ExchangeRate>(
            "SELECT TOP(1) " +
            "[ExchangeRate].ID, " +
            "(CASE " +
            "WHEN [ExchangeRateHistory].Amount IS NOT NULL " +
            "THEN [ExchangeRateHistory].Amount " +
            "ELSE [ExchangeRate].Amount " +
            "END) AS [Amount] " +
            "FROM [ExchangeRate] " +
            "LEFT JOIN [ExchangeRateHistory] " +
            "ON [ExchangeRateHistory].ExchangeRateID = [ExchangeRate].ID " +
            "AND [ExchangeRateHistory].Created <= @FromDate " +
            "WHERE [ExchangeRate].CurrencyID = @Id " +
            "AND [ExchangeRate].Code = @Code " +
            "ORDER BY [ExchangeRateHistory].ID DESC",
            new { Id = id, Code = code, FromDate = fromDate }
        ).FirstOrDefault();
    }

    public IEnumerable<SupplyInvoice> GetSupplyInvoiceByIds(List<long> invoiceIds) {
        return _remoteSyncConnection.Query<SupplyInvoice>(
            "SELECT * FROM [SupplyInvoice] " +
            "WHERE [SupplyInvoice].[ID] IN @Ids; ",
            new { Ids = invoiceIds }).AsEnumerable();
    }

    public IEnumerable<ConsignmentItem> GetConsignmentItemsByInvoiceIds(List<long> invoiceIds) {
        return _remoteSyncConnection.Query<ConsignmentItem, ProductIncomeItem, PackingListPackageOrderItem, ConsignmentItem>(
            "SELECT " +
            "[ConsignmentItem].* " +
            ", [ProductIncomeItem].* " +
            ", [PackingListPackageOrderItem].*  FROM [ConsignmentItem] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[ID] = [ConsignmentItem].[ProductIncomeItemID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "WHERE [SupplyInvoice].[ID] IN @Ids ",
            (consignmentItem, productIncomeItem, packingListPackageOrderItem) => {
                productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;
                consignmentItem.ProductIncomeItem = productIncomeItem;

                return consignmentItem;
            }, new { Ids = invoiceIds });
    }

    public IEnumerable<SyncConsignmentSpend> GetAmgConsignmentSpendsByDocumentId(byte[] documentId) {
        List<byte[]> ids = _amgOneCConnection.Query<byte[]>(
            "SELECT " +
            "T29._Document327_IDRRef [DocumentId] " +
            "FROM dbo._Document327_VT9037 T29 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document327 T30 WITH(NOLOCK) " +
            "ON (T29._Document327_IDRRef = T30._IDRRef) " +
            "WHERE T29._Fld9048_RRRef = @Id " +
            "AND T30._Marked = 0x00 ",
            new {
                Id = documentId
            }).ToList();

        List<TotalConsignmentSpend> totalValues = new();

        IEnumerable<byte[][]> idsInChunks = ids.Chunk(1000);

        foreach (byte[][] idsInChunk in idsInChunks) {
            List<TotalConsignmentSpend> totalValueItems = _amgOneCConnection.Query<TotalConsignmentSpend>(
                "SELECT " +
                "T29._Document327_IDRRef [Id] " +
                ", SUM(T29._Fld9044) [TotalValue] " +
                "FROM dbo._Document327_VT9037 T29 WITH(NOLOCK) " +
                "LEFT OUTER JOIN dbo._Document327 T30 WITH(NOLOCK) " +
                "ON (T29._Document327_IDRRef = T30._IDRRef) " +
                "WHERE T29._Document327_IDRRef IN @Ids " +
                "AND T30._Marked = 0x00 " +
                "GROUP BY T29._Document327_IDRRef ",
                new {
                    Ids = idsInChunk
                }).ToList();

            totalValues.AddRange(totalValueItems);
        }

        IEnumerable<SyncConsignmentSpend> toReturn = _amgOneCConnection.Query<SyncConsignmentSpend>(
            "SELECT " +
            "T30._Fld9020 [Amount] " +
            ", T32._Code [AgreementCode] " +
            ", T2._Code [CurrencyCode] " +
            ", T29._Document327_IDRRef [DocumentId] " +
            "FROM dbo._Document327_VT9037 T29 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document327 T30 WITH(NOLOCK) " +
            "ON (T29._Document327_IDRRef = T30._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference66 T32 WITH(NOLOCK) " +
            "ON T30._Fld9006RRef = T32._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 T2 WITH(NOLOCK) " +
            "ON T32._Fld1323RRef = T2._IDRRef " +
            "WHERE T29._Fld9048_RRRef = @Id " +
            "AND T30._Marked = 0x00 " +
            "GROUP BY T32._Code, T30._Fld9020, T30._IDRRef, T2._Code, T29._Document327_IDRRef ",
            new {
                Id = documentId
            });

        foreach (SyncConsignmentSpend spend in toReturn) {
            TotalConsignmentSpend existSpend = totalValues.FirstOrDefault(x => x.IdInString == spend.DocumentIdInString);

            if (existSpend == null) continue;

            spend.TotalSpend = existSpend.TotalValue;
        }

        return toReturn;
    }


    public IEnumerable<SyncConsignmentSpend> GetFenixConsignmentSpendsByDocumentId(byte[] documentId) {
        IEnumerable<byte[]> ids = _oneCConnection.Query<byte[]>(
            "SELECT " +
            "T1._Document274_IDRRef DocumentId " +
            "FROM dbo._Document274_VT7350 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document274 T2 WITH(NOLOCK) " +
            "ON (T1._Document274_IDRRef = T2._IDRRef) " +
            "WHERE T1._Fld7361_RRRef = @Id " +
            "AND T2._Marked = 0x00",
            new {
                Id = documentId
            });

        IEnumerable<byte[][]> idsInChunks = ids.Chunk(1000);

        List<TotalConsignmentSpend> totalValues = new();

        foreach (byte[][] idsInChunk in idsInChunks) {
            List<TotalConsignmentSpend> totalValueItems = _oneCConnection.Query<TotalConsignmentSpend>(
                "SELECT " +
                "T1._Document274_IDRRef [Id] " +
                ", SUM(T1._Fld7357) [TotalValue] " +
                "FROM dbo._Document274_VT7350 T1 WITH(NOLOCK) " +
                "LEFT OUTER JOIN dbo._Document274 T2 WITH(NOLOCK) " +
                "ON (T1._Document274_IDRRef = T2._IDRRef) " +
                "WHERE T1._Document274_IDRRef IN @Ids " +
                "AND T2._Marked = 0x00 " +
                "GROUP BY T1._Document274_IDRRef",
                new {
                    Ids = idsInChunk
                }).ToList();

            totalValues.AddRange(totalValueItems);
        }

        IEnumerable<SyncConsignmentSpend> toReturn = _oneCConnection.Query<SyncConsignmentSpend>(
            "SELECT " +
            "T2._Fld7337 [Amount], " +
            "T3._Code [AgreementCode], " +
            "T4._Code [CurrencyCode], " +
            "T1._Document274_IDRRef [DocumentId] " +
            "FROM dbo._Document274_VT7350 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Document274 T2 WITH(NOLOCK) " +
            "ON (T1._Document274_IDRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference47 T3 WITH(NOLOCK) " +
            "ON T2._Fld7323RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T4 WITH(NOLOCK) " +
            "ON T3._Fld988RRef = T4._IDRRef " +
            "WHERE T1._Fld7361_RRRef = @Id " +
            "AND T2._Marked = 0x00 " +
            "GROUP BY T3._Code, T2._Fld7337, T2._IDRRef, T4._Code, T1._Document274_IDRRef ",
            new { Id = documentId });

        foreach (SyncConsignmentSpend spend in toReturn) {
            TotalConsignmentSpend existSpend = totalValues.FirstOrDefault(x => x.IdInString == spend.DocumentIdInString);

            if (existSpend == null) continue;

            spend.TotalSpend = existSpend.TotalValue;
        }

        return toReturn;
    }

    public IEnumerable<SupplyOrganization> GetAllSupplyOrganizations(string defaultComment) {
        List<SupplyOrganization> toReturn = new();

        _remoteSyncConnection.Query<SupplyOrganization, SupplyOrganizationAgreement, Currency, SupplyOrganization>(
            "SELECT * FROM SupplyOrganization " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[SupplyOrganizationID] = [SupplyOrganization].[ID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "WHERE [SupplyOrganization].[Name] NOT LIKE @Name ",
            (supplyOrganization, agreement, currency) => {
                if (!toReturn.Any(x => x.Id == supplyOrganization.Id))
                    toReturn.Add(supplyOrganization);
                else
                    supplyOrganization = toReturn.First(x => x.Id == supplyOrganization.Id);

                if (agreement == null) return supplyOrganization;

                if (!supplyOrganization.SupplyOrganizationAgreements.Any(x => x.Id == agreement.Id))
                    supplyOrganization.SupplyOrganizationAgreements.Add(agreement);
                else
                    agreement = supplyOrganization.SupplyOrganizationAgreements.First(x => x.Id == agreement.Id);

                agreement.Currency = currency;

                return supplyOrganization;
            },
            new { Name = defaultComment });

        return toReturn;
    }

    public SupplyOrganization GetDevSupplyOrganization(string name) {
        return _remoteSyncConnection.Query<SupplyOrganization, SupplyOrganizationAgreement, Currency, SupplyOrganization>(
            "SELECT * FROM SupplyOrganization " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[SupplyOrganizationID] = [SupplyOrganization].[ID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "WHERE [SupplyOrganization].[Name] LIKE @Name ",
            (organization, agreement, currency) => {
                agreement.Currency = currency;

                organization.SupplyOrganizationAgreements.Add(agreement);

                return organization;
            },
            new { Name = name }).FirstOrDefault();
    }

    public long Add(SupplyOrganization supplyOrganization) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [SupplyOrganization] " +
                "(Name, Address, PhoneNumber, EmailAddress, Requisites, Swift, SwiftBic, IntermediaryBank, BeneficiaryBank, AccountNumber, " +
                "Beneficiary, Bank, BankAccount, NIP, BankAccountPLN, BankAccountEUR, ContactPersonName, ContactPersonPhone, ContactPersonEmail, ContactPersonViber, " +
                "ContactPersonSkype, ContactPersonComment, IsAgreementReceived, IsBillReceived, AgreementReceiveDate, BillReceiveDate, Updated, [IsNotResident]) " +
                "VALUES (@Name, @Address, @PhoneNumber, @EmailAddress, @Requisites, @Swift, @SwiftBic, @IntermediaryBank, @BeneficiaryBank, @AccountNumber, " +
                "@Beneficiary, @Bank, @BankAccount, @NIP, @BankAccountPLN, @BankAccountEUR, @ContactPersonName, @ContactPersonPhone, @ContactPersonEmail, " +
                "@ContactPersonViber, @ContactPersonSkype, @ContactPersonComment, @IsAgreementReceived, @IsBillReceived, @AgreementReceiveDate, " +
                "@BillReceiveDate, GETUTCDATE(), @IsNotResident); " +
                "SELECT SCOPE_IDENTITY()",
                supplyOrganization
            )
            .Single();
    }

    public long Add(SupplyOrganizationAgreement agreement) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [SupplyOrganizationAgreement] (Name, CurrentAmount, SupplyOrganizationId, CurrencyId, Updated, [ExistTo], [OrganizationID], [ExistFrom]) " +
            "VALUES (@Name, @CurrentAmount, @SupplyOrganizationId, @CurrencyId, GETUTCDATE(), @ExistTo, @OrganizationId, @ExistFrom); " +
            "SELECT SCOPE_IDENTITY() ",
            agreement
        ).Single();
    }

    public IEnumerable<SupplyInvoice> GetExistSupplyInvoices() {
        List<SupplyInvoice> toReturn = new();

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(ProductIncomeItem),
            typeof(ConsignmentItem),
            typeof(Consignment),
            typeof(ProductIncome),
            typeof(SupplyOrder),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(Storage),
            typeof(ClientAgreement),
            typeof(Client)
        };

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice invoice = (SupplyInvoice)objects[0];
            PackingList packingList = (PackingList)objects[1];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[2];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[3];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[4];
            Consignment consignment = (Consignment)objects[5];
            ProductIncome productIncome = (ProductIncome)objects[6];
            SupplyOrder supplyOrder = (SupplyOrder)objects[7];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[9];
            Product product = (Product)objects[10];
            Storage storage = (Storage)objects[11];
            ClientAgreement clientAgreement = (ClientAgreement)objects[12];
            Client client = (Client)objects[13];

            if (!toReturn.Any(x => x.Id == invoice.Id))
                toReturn.Add(invoice);
            else
                invoice = toReturn.First(x => x.Id == invoice.Id);

            clientAgreement.Client = client;

            supplyOrder.ClientAgreement = clientAgreement;

            invoice.SupplyOrder = supplyOrder;

            if (!invoice.PackingLists.Any(x => x.Id == packingList.Id))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.First(x => x.Id == packingList.Id);

            if (packingListPackageOrderItem != null) {
                if (!packingList.PackingListPackageOrderItems.Any(x => x.Id == packingListPackageOrderItem.Id))
                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                else
                    packingListPackageOrderItem = packingList.PackingListPackageOrderItems.First(x => x.Id == packingListPackageOrderItem.Id);
            }

            if (packingListPackageOrderItem != null) {
                packingListPackageOrderItem.ProductIncomeItem = productIncomeItem;

                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                if (supplyInvoiceOrderItem != null) {
                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    supplyInvoiceOrderItem.Product = product;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;
                }
            }

            if (consignmentItem != null && productIncomeItem != null) {
                productIncomeItem.ProductIncome = productIncome;

                if (!productIncomeItem.ConsignmentItems.Any(x => x.Id == consignmentItem.Id))
                    productIncomeItem.ConsignmentItems.Add(consignmentItem);
                else
                    consignmentItem = productIncomeItem.ConsignmentItems.First(x => x.Id == consignmentItem.Id);

                consignmentItem.Consignment = consignment;
            }

            if (productIncome != null)
                productIncome.Storage = storage;

            return invoice;
        };

        _remoteSyncConnection.Query(
            "SELECT * FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[PackingListPackageOrderItemID] = [PackingListPackageOrderItem].[ID] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ProductIncomeItemID] = [ProductIncomeItem].[ID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].[ID] = [ProductIncomeItem].[ProductIncomeID] " +
            "LEFT JOIN  [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].[ID] = [PackingListPackageOrderItem].[SupplyInvoiceOrderItemID] " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].[ID] = [SupplyInvoiceOrderItem].[SupplyOrderItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [SupplyInvoiceOrderItem].[ProductID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ProductIncome].[StorageID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
            "WHERE [SupplyInvoice].[Deleted] = 0 ",
            types,
            mapper
        );

        return toReturn;
    }

    public SupplyInvoice GetExistSupplyInvoiceById(long id) {
        SupplyInvoice toReturn = null;

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(ProductIncomeItem),
            typeof(ConsignmentItem),
            typeof(Consignment),
            typeof(ProductIncome),
            typeof(SupplyOrder),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(Storage),
            typeof(ClientAgreement),
            typeof(Client)
        };

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice invoice = (SupplyInvoice)objects[0];
            PackingList packingList = (PackingList)objects[1];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[2];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[3];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[4];
            Consignment consignment = (Consignment)objects[5];
            ProductIncome productIncome = (ProductIncome)objects[6];
            SupplyOrder supplyOrder = (SupplyOrder)objects[7];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[9];
            Product product = (Product)objects[10];
            Storage storage = (Storage)objects[11];
            ClientAgreement clientAgreement = (ClientAgreement)objects[12];
            Client client = (Client)objects[13];

            if (toReturn == null)
                toReturn = invoice;
            else
                invoice = toReturn;

            if (invoice == null) return invoice;

            clientAgreement.Client = client;

            supplyOrder.ClientAgreement = clientAgreement;

            invoice.SupplyOrder = supplyOrder;

            if (!invoice.PackingLists.Any(x => x.Id == packingList.Id))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.First(x => x.Id == packingList.Id);

            if (packingListPackageOrderItem != null) {
                if (!packingList.PackingListPackageOrderItems.Any(x => x.Id == packingListPackageOrderItem.Id))
                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                else
                    packingListPackageOrderItem = packingList.PackingListPackageOrderItems.First(x => x.Id == packingListPackageOrderItem.Id);
            }

            if (packingListPackageOrderItem != null) {
                packingListPackageOrderItem.ProductIncomeItem = productIncomeItem;

                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                if (supplyInvoiceOrderItem != null) {
                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    supplyInvoiceOrderItem.Product = product;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;
                }
            }

            if (consignmentItem != null && productIncomeItem != null) {
                productIncomeItem.ProductIncome = productIncome;

                if (!productIncomeItem.ConsignmentItems.Any(x => x.Id == consignmentItem.Id))
                    productIncomeItem.ConsignmentItems.Add(consignmentItem);
                else
                    consignmentItem = productIncomeItem.ConsignmentItems.First(x => x.Id == consignmentItem.Id);

                consignmentItem.Consignment = consignment;
            }

            if (productIncome != null)
                productIncome.Storage = storage;

            return invoice;
        };

        _remoteSyncConnection.Query(
            "SELECT * FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[PackingListPackageOrderItemID] = [PackingListPackageOrderItem].[ID] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ProductIncomeItemID] = [ProductIncomeItem].[ID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].[ID] = [ProductIncomeItem].[ProductIncomeID] " +
            "LEFT JOIN  [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].[ID] = [PackingListPackageOrderItem].[SupplyInvoiceOrderItemID] " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].[ID] = [SupplyInvoiceOrderItem].[SupplyOrderItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [SupplyInvoiceOrderItem].[ProductID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ProductIncome].[StorageID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
            "WHERE [SupplyInvoice].[Deleted] = 0 " +
            "AND [SupplyInvoice].[ID] = @Id ",
            types,
            mapper,
            new { Id = id }
        );

        return toReturn;
    }

    public IEnumerable<SupplyInvoice> GetExistSupplyInvoicesByIds(long[] ids) {
        List<SupplyInvoice> toReturn = new();

        Type[] types = {
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(ProductIncomeItem),
            typeof(ConsignmentItem),
            typeof(Consignment),
            typeof(ProductIncome),
            typeof(SupplyOrder),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(Storage),
            typeof(ClientAgreement),
            typeof(Client)
        };

        Func<object[], SupplyInvoice> mapper = objects => {
            SupplyInvoice invoice = (SupplyInvoice)objects[0];
            PackingList packingList = (PackingList)objects[1];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[2];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[3];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[4];
            Consignment consignment = (Consignment)objects[5];
            ProductIncome productIncome = (ProductIncome)objects[6];
            SupplyOrder supplyOrder = (SupplyOrder)objects[7];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[9];
            Product product = (Product)objects[10];
            Storage storage = (Storage)objects[11];
            ClientAgreement clientAgreement = (ClientAgreement)objects[12];
            Client client = (Client)objects[13];

            if (!toReturn.Any(x => x.Id == invoice.Id))
                toReturn.Add(invoice);
            else
                invoice = toReturn.First(x => x.Id == invoice.Id);

            invoice.SupplyOrder = supplyOrder;

            clientAgreement.Client = client;

            supplyOrder.ClientAgreement = clientAgreement;

            if (!invoice.PackingLists.Any(x => x.Id == packingList.Id))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.First(x => x.Id == packingList.Id);

            if (packingListPackageOrderItem != null) {
                if (!packingList.PackingListPackageOrderItems.Any(x => x.Id == packingListPackageOrderItem.Id))
                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                else
                    packingListPackageOrderItem = packingList.PackingListPackageOrderItems.First(x => x.Id == packingListPackageOrderItem.Id);
            }

            if (packingListPackageOrderItem != null) {
                packingListPackageOrderItem.ProductIncomeItem = productIncomeItem;

                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                if (supplyInvoiceOrderItem != null) {
                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    supplyInvoiceOrderItem.Product = product;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;
                }
            }

            if (consignmentItem != null && productIncomeItem != null) {
                productIncomeItem.ProductIncome = productIncome;

                if (!productIncomeItem.ConsignmentItems.Any(x => x.Id == consignmentItem.Id))
                    productIncomeItem.ConsignmentItems.Add(consignmentItem);
                else
                    consignmentItem = productIncomeItem.ConsignmentItems.First(x => x.Id == consignmentItem.Id);

                consignmentItem.Consignment = consignment;
            }

            if (productIncome != null)
                productIncome.Storage = storage;

            return invoice;
        };

        _remoteSyncConnection.Query(
            "SELECT * FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].[PackingListPackageOrderItemID] = [PackingListPackageOrderItem].[ID] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ProductIncomeItemID] = [ProductIncomeItem].[ID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].[ID] = [ProductIncomeItem].[ProductIncomeID] " +
            "LEFT JOIN  [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].[ID] = [PackingListPackageOrderItem].[SupplyInvoiceOrderItemID] " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].[ID] = [SupplyInvoiceOrderItem].[SupplyOrderItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [SupplyInvoiceOrderItem].[ProductID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ProductIncome].[StorageID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
            "WHERE [SupplyInvoice].[Deleted] = 0 " +
            "AND [SupplyInvoice].[ID] IN @Ids ",
            types,
            mapper,
            new { Ids = ids }
        );

        return toReturn;
    }

    public long Add(MergedService mergedService) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [MergedService] " +
                "(IsActive, FromDate, GrossPrice, NetPrice, Vat, VatPercent, Number, ServiceNumber, Name, UserId, SupplyPaymentTaskId, SupplyOrganizationAgreementId, " +
                "SupplyOrganizationId, SupplyOrderId, SupplyOrderUkraineId, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, AccountingPaymentTaskId, AccountingVatPercent, " +
                "DeliveryProductProtocolID, IsAutoCalculatedValue, [AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], " +
                "[ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], [ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID], [ConsumableProductID], " +
                "[ActProvidingServiceID], [AccountingActProvidingServiceID], [IsCalculatedValue]) " +
                "VALUES " +
                "(@IsActive, @FromDate, @GrossPrice, @NetPrice, @Vat, @VatPercent, @Number, @ServiceNumber, @Name, @UserId, @SupplyPaymentTaskId, @SupplyOrganizationAgreementId, " +
                "@SupplyOrganizationId, @SupplyOrderId, @SupplyOrderUkraineId, GETUTCDATE(), @AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, @AccountingPaymentTaskId, @AccountingVatPercent, " +
                "@DeliveryProductProtocolID, @IsAutoCalculatedValue, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, " +
                "@ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, @ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId, @ConsumableProductId, " +
                "@ActProvidingServiceId, @AccountingActProvidingServiceId, @IsCalculatedValue); " +
                "SELECT SCOPE_IDENTITY()",
                mergedService
            )
            .Single();
    }

    public void Add(SupplyInvoiceMergedService supplyInvoiceMergedService) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [SupplyInvoiceMergedService]([Updated], [SupplyInvoiceID], [MergedServiceID], [Value], [AccountingValue], [IsCalculatedValue]) " +
            "VALUES (getutcdate(), @SupplyInvoiceID, @MergedServiceID, @Value, @AccountingValue, @IsCalculatedValue) ",
            supplyInvoiceMergedService);
    }

    public long Add(ActProvidingService act) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ActProvidingService] ([Updated], [IsAccounting], [Price], [UserID], [FromDate], [Comment], [Number]) " +
            "VALUES (getutcdate(), @IsAccounting, @Price, @UserId, @FromDate, @Comment, @Number); " +
            "SELECT SCOPE_IDENTITY(); ", act).SingleOrDefault();
    }

    public long Add(ConsumableProduct consumableProduct) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [ConsumableProduct] (Name, VendorCode, ConsumableProductCategoryId, MeasureUnitId, Updated) " +
                "VALUES (@Name, @VendorCode, @ConsumableProductCategoryId, @MeasureUnitId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                consumableProduct
            )
            .Single();
    }

    public long Add(ConsumableProductCategory consumableProductCategory) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [ConsumableProductCategory] (Name, Description, Updated, [IsSupplyServiceCategory]) " +
                "VALUES (@Name, @Description, getutcdate(), @IsSupplyServiceCategory); " +
                "SELECT SCOPE_IDENTITY()",
                consumableProductCategory
            )
            .Single();
    }

    public ConsumableProductCategory GetSupplyServiceConsumablesProductCategory() {
        return _remoteSyncConnection.Query<ConsumableProductCategory>(
            "SELECT * " +
            "FROM [ConsumableProductCategory] " +
            "WHERE [ConsumableProductCategory].[IsSupplyServiceCategory] = 1 "
        ).FirstOrDefault();
    }

    public ConsumableProduct GetConsumablesProductByKey(string consignmentFromOneCUkKey) {
        return _remoteSyncConnection.Query<ConsumableProduct>(
            "SELECT * " +
            "FROM [ConsumableProduct] " +
            "WHERE [ConsumableProduct].[Name] = @ConsignmentFromOneCUkKey ",
            new {
                ConsignmentFromOneCUkKey = consignmentFromOneCUkKey
            }
        ).FirstOrDefault();
    }

    public long Add(DeliveryProductProtocolNumber number) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [DeliveryProductProtocolNumber] ([Number], [Updated]) " +
            "VALUES (@Number, getutcdate()); " +
            "SELECT SCOPE_IDENTITY() ",
            number).Single();
    }

    public long Add(DeliveryProductProtocol deliveryProductProtocol) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [DeliveryProductProtocol] ([TransportationType], [UserID], [Updated], [Comment], [FromDate], " +
            "[IsCompleted], [IsPartiallyPlaced], [IsPlaced], [OrganizationID], [DeliveryProductProtocolNumberID], [IsShipped]) " +
            "VALUES(@TransportationType, @UserID, @Updated, @Comment, @FromDate, @IsCompleted, @IsPartiallyPlaced, @IsPlaced, @OrganizationID, " +
            "@DeliveryProductProtocolNumberID, @IsShipped); " +
            "SELECT SCOPE_IDENTITY() ",
            deliveryProductProtocol).Single();
    }

    public void UpdateInvoiceInProtocol(SupplyInvoice invoice) {
        _remoteSyncConnection.Execute(
            "UPDATE [SupplyInvoice] " +
            "SET [DeliveryProductProtocolID] = @DeliveryProductProtocolId " +
            "WHERE [SupplyInvoice].[ID] = @Id; "
            , invoice);
    }

    public Currency GetCurrencyByInvoice(long invoiceId) {
        return _remoteSyncConnection.Query<Currency>(
            "SELECT [Currency].* FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [SupplyInvoice].[ID] = @Id ",
            new { Id = invoiceId }).FirstOrDefault();
    }

    public IEnumerable<SyncConsignment> GetAmgFilteredSyncCapitalizationConsignments(DateTime from, DateTime to) {
        return _amgOneCConnection.Query<SyncConsignment>(
            "SELECT " +
            "T13._IDRRef [DocumentId], " +
            "T13._Fld7113 [DocumentValue], " +
            "2 [TypeDocument], " +
            "T13._Number [DocumentNumber], " +
            "T13._Date_Time [DocumentDate], " +
            "'ТОВ «АМГ «КОНКОРД»' [OrganizationName], " +
            "T33._Description [StorageName], " +
            "T33._Description [IncomeStorageName], " +
            "T4._Code [ProductCode], " +
            "T4._Fld1765 [VendorCode], " +
            "T17._Fld7137 [IncomeQty], " +
            "T17._Fld7137 [Qty], " +
            "T17._Fld7145 [NetValue], " +
            "T17._Fld7143 [Value], " +
            "T17._Fld7143 [TotalValue], " +
            "T13._Fld7115 [Comment] " +
            "FROM dbo._Document292 T13 " +
            "LEFT OUTER JOIN dbo._Reference144 T33 WITH(NOLOCK) " +
            "ON T13._Fld7108RRef = T33._IDRRef " +
            "LEFT JOIN dbo._Document292_VT7132 T17 " +
            "ON T17._Document292_IDRRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference108 T4 WITH(NOLOCK) " +
            "ON T17._Fld7140RRef = T4._IDRRef " +
            "WHERE T13._Date_Time >= @From " +
            "AND (T13._Marked = 0x00) AND T13._Posted = 0x01 " +
            "AND T13._Date_Time <= @To ",
            new { From = from, To = to }
        );
    }

    public IEnumerable<SyncConsignment> GetFilteredSyncCapitalizationConsignments(DateTime from, DateTime to) {
        return _oneCConnection.Query<SyncConsignment>(
            "SELECT " +
            "T11._IDRRef [DocumentId], " +
            "T11._Fld5733 [DocumentValue], " +
            "2 [TypeDocument], " +
            "T11._Number [DocumentNumber], " +
            "T11._Date_Time [DocumentDate], " +
            "T28._Description [OrganizationName], " +
            "T46._Description [StorageName], " +
            "T46._Description [IncomeStorageName], " +
            "CAST(T39._Code AS bigint) [ProductCode], " +
            "T39._Fld1306 [VendorCode], " +
            "T3._Fld5753 [Qty], " +
            "T3._Fld5753 [IncomeQty], " +
            "T3._Fld5761 [NetValue], " +
            "T3._Fld5759 [Value], " +
            "T3._Fld5759 [TotalValue], " +
            "T11._Fld5735 [Comment] " +
            "FROM dbo._Document242 T11 " +
            "LEFT OUTER JOIN dbo._Document242_VT5748 T3 " +
            "ON T3._Document242_IDRRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T46 WITH(NOLOCK) " +
            "ON T11._Fld5728RRef = T46._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T28 WITH(NOLOCK) " +
            "ON T11._Fld5726RRef = T28._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 T39 WITH(NOLOCK) " +
            "ON T3._Fld5756RRef = T39._IDRRef " +
            "WHERE T11._Date_Time >= @From " +
            "AND T11._Date_Time <= @To " +
            "AND (T11._Marked = 0x00) AND T11._Posted = 0x01 " +
            "AND T28._Description IN @FenixNames "
            , new { From = from, To = to, FenixNames = ORGANIZATION_NAMES });
    }

    public IEnumerable<SyncConsignment> GetAmgFilteredSyncReturnConsignments(DateTime from, DateTime to) {
        return _amgOneCConnection.Query<SyncConsignment>(
            "SELECT " +
            "T12._IDRRef [DocumentId], " +
            "T12._Fld3382 [DocumentValue], " +
            "1 [TypeDocument], " +
            "T12._Number [DocumentNumber], " +
            "T12._Date_Time [DocumentDate], " +
            "T19.[_Code] [ClientCode], " +
            "'ТОВ «АМГ «КОНКОРД»' [OrganizationName], " +
            "T3._Description [StorageName], " +
            "T3._Description [IncomeStorageName], " +
            "T20._Code [CurrencyCode], " +
            "T4._Code [ProductCode], " +
            "T4._Fld1765 [VendorCode], " +
            "T1._Fld3414 [IncomeQty], " +
            "T1._Fld3414 [Qty], " +
            "T1._Fld3418 [NetValue], " +
            "T1._Fld3420 [Value], " +
            "T1._Fld3420 [TotalValue], " +
            "T1._Fld3422 [Vat], " +
            "T34._EnumOrder [VatTypeAmg], " +
            "T21._Code [AgreementCode], " +
            "T12._Fld3388 [Comment] " +
            "FROM dbo._Document219 T12 " +
            "LEFT JOIN dbo._Document219_VT3411 T1 " +
            "ON T1._Document219_IDRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference108 T4 WITH(NOLOCK) " +
            "ON T1._Fld3413RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference144 T3 WITH(NOLOCK) " +
            "ON T12._Fld3377_RRRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 T21 WITH(NOLOCK) " +
            "ON T12._Fld3384RRef = T21._IDRRef " +
            "LEFT JOIN dbo._Reference90 T19 " +
            "ON T19._IDRRef = T12._Fld3383RRef " +
            "LEFT JOIN dbo._Reference35 T20 ON " +
            "T20._IDRRef =T12._Fld3379RRef " +
            "LEFT OUTER JOIN dbo._Enum652 T34 WITH(NOLOCK) " +
            "ON T1._Fld3421RRef = T34._IDRRef " +
            "WHERE T12._Date_Time >= @From " +
            "AND (T12._Marked = 0x00) AND T12._Posted = 0x01 " +
            "AND T12._Date_Time <= @To ",
            new { From = from, To = to }
        );
    }


    public IEnumerable<SyncConsignment> GetFilteredSyncReturnConsignments(DateTime from, DateTime to) {
        return _oneCConnection.Query<SyncConsignment>(
            "SELECT " +
            "T4._IDRRef [DocumentId], " +
            "T4._Fld2600 [DocumentValue], " +
            "1 [TypeDocument], " +
            "T4._Number [DocumentNumber], " +
            "T4._Date_Time [DocumentDate], " +
            "T4._Fld2604 [RateExchange], " +
            "T15._Code [ClientCode], " +
            "T21._Description [OrganizationName], " +
            "T3._Description [StorageName], " +
            "T3._Description [IncomeStorageName], " +
            "T33._Code [CurrencyCode], " +
            "CAST(T5._Code AS bigint) [ProductCode], " +
            "T5._Fld1306 [VendorCode], " +
            "T1._Fld2626 [Qty], " +
            "T1._Fld2626 [IncomeQty], " +
            "T1._Fld2630 [NetValue], " +
            "T1._Fld2632 [Value], " +
            "T1._Fld2632 [TotalValue], " +
            "T1._Fld2634 [VAT], " +
            "T6._EnumOrder [VatTypeFenix], " +
            "T40._Code [AgreementCode], " +
            "T4._Fld2604 [Rate], " +
            "T4._Fld2606 [Comment] " +
            "FROM dbo._Document175 T4 " +
            "LEFT OUTER JOIN dbo._Document175_VT2623 T1 WITH(NOLOCK) " +
            "ON T1._Document175_IDRRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 T5 WITH(NOLOCK) " +
            "ON T1._Fld2625RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T40 WITH(NOLOCK) " +
            "ON T4._Fld2602RRef = T40._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T15 WITH(NOLOCK) " +
            "ON T4._Fld2601RRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T21 WITH(NOLOCK) " +
            "ON T4._Fld2594RRef = T21._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T33 WITH(NOLOCK) " +
            "ON T4._Fld2597RRef = T33._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference117 T3 WITH(NOLOCK) " +
            "ON T4._Fld2595_RRRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum563 T6 WITH(NOLOCK) " +
            "ON T1._Fld2633RRef = T6._IDRRef " +
            "WHERE T4._Date_Time >= @From " +
            "AND T4._Date_Time <= @To " +
            "AND (T4._Marked = 0x00) AND T4._Posted = 0x01 " +
            "AND T21._Description IN @FenixNames ", new { From = from, To = to, FenixNames = ORGANIZATION_NAMES });
    }

    public IEnumerable<PaymentMovement> GetAllPaymentMovementOperations() {
        return _remoteSyncConnection.Query<PaymentMovement>(
            "SELECT * FROM [PaymentMovement] " +
            "WHERE [PaymentMovement].[Deleted] = 0 ");
    }

    public IEnumerable<string> GetAmgPaymentMovements() {
        return _amgOneCConnection.Query<string>(
            "SELECT _Description FROM dbo._Reference155 ");
    }

    public IEnumerable<string> GetFenixPaymentMovements() {
        return _oneCConnection.Query<string>(
            "SELECT _Description FROM dbo._Reference127 ");
    }

    public void AddPaymentMovementTranslation(PaymentMovementTranslation paymentMovementTranslation) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [PaymentMovementTranslation] (Name, PaymentMovementId, CultureCode, Updated) " +
            "VALUES (@Name, @PaymentMovementId, @CultureCode, getutcdate())",
            paymentMovementTranslation);
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
}