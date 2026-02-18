using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync;

public sealed class PaymentRegistersSyncRepository : IPaymentRegistersSyncRepository {
    private readonly IDbConnection _amgSyncConnection;
    private readonly IDbConnection _oneCConnection;

    private readonly IDbConnection _remoteSyncConnection;

    public PaymentRegistersSyncRepository(
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgSyncConnection) {
        _oneCConnection = oneCConnection;

        _remoteSyncConnection = remoteSyncConnection;

        _amgSyncConnection = amgSyncConnection;
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
            "T12._Description [StorageName], " +
            "T1.Fld10933_ [PFURegistrationNumber] " +
            "FROM (SELECT " +
            "T4._Fld10931 AS Fld10931_, " +
            "T4._Fld10935RRef AS Fld10935RRef, " +
            "T4._Fld10930 AS Fld10930_, " +
            "T4._Fld10933 AS Fld10933_, " +
            "T4._Fld10937 AS Fld10937_, " +
            "T4._Fld10938 AS Fld10938_, " +
            "T4._Fld10941 AS Fld10941_, " +
            "T4._Fld10929RRef AS Fld10929RRef, " +
            "T4._Fld10936 AS Fld10936_ " +
            "FROM ( " +
            "SELECT " +
            "T3._Fld10929RRef AS Fld10929RRef, " +
            "MAX(T3._Period) AS MAXPERIOD_, " +
            "T3._Fld10933 AS Fld10933 " +
            "FROM dbo._InfoRg10928 T3 WITH(NOLOCK) " +
            "GROUP BY T3._Fld10929RRef, T3._Fld10933) T2 " +
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
            "T9._Description [StorageName], " +
            "T1.Fld13001_ [PFURegistrationNumber] " +
            "FROM (SELECT " +
            "T4._Fld12999 AS Fld12999_, " +
            "T4._Fld13006 AS Fld13006_, " +
            "T4._Fld13009 AS Fld13009_, " +
            "T4._Fld13001 AS Fld13001_, " +
            "T4._Fld12997RRef AS Fld12997RRef, " +
            "T4._Fld13003RRef AS Fld13003RRef, " +
            "T4._Fld13004 AS Fld13004_, " +
            "T4._Fld12998 AS Fld12998_, " +
            "T4._Fld13005 AS Fld13005_ " +
            "FROM (SELECT " +
            "T3._Fld12997RRef AS Fld12997RRef, " +
            "T3._Fld13001 AS Fld13001, " +
            "MAX(T3._Period) AS MAXPERIOD_ " +
            "FROM dbo._InfoRg12996 T3 WITH(NOLOCK) " +
            "GROUP BY T3._Fld12997RRef, T3._Fld13001) T2 " +
            "INNER JOIN dbo._InfoRg12996 T4 WITH(NOLOCK) " +
            "ON T2.Fld12997RRef = T4._Fld12997RRef AND T2.MAXPERIOD_ = T4._Period) T1 " +
            "LEFT OUTER JOIN dbo._Reference116 T5 WITH(NOLOCK) " +
            "ON (T1.Fld12997RRef = T5._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference33 T6 WITH(NOLOCK) " +
            "ON T5._Fld1889RRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 T7 WITH(NOLOCK) " +
            "ON T6._Fld1160RRef = T7._IDRRef " +
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
            "T4._Fld10938 AS Fld10938_, " +
            "T4._Fld10936 AS Fld10936_, " +
            "T4._Fld10930 AS Fld10930_, " +
            "T4._Fld10935RRef AS Fld10935RRef, " +
            "T4._Fld10941 AS Fld10941_, " +
            "T4._Fld10937 AS Fld10937_, " +
            "T4._Fld10929RRef AS Fld10929RRef, " +
            "T4._Fld10931 AS Fld10931_ " +
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
            ",[TaxInspectionID] " +
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
            ",[Manager] = @Manager " +
            ",[VatRateID] = @VatRateId " +
            ",[IsVatAgreements] = @IsVatAgreements " +
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
            "[Deleted] = @Deleted, " +
            "[Updated] = GETUTCDATE() " +
            "WHERE ID = @Id",
            taxInspection
        );
    }

    public IEnumerable<SyncCashRegister> GetAllSyncCashRegisters(string[] organizations) {
        return _oneCConnection.Query<SyncCashRegister>(
            "SELECT " +
            "T1._Description [CashRegisterName], " +
            "T4._Code [CurrencyCode], " +
            "T5._Description [OrganizationName], " +
            "ISNULL(T2.Fld12557Balance_, 0.00) [Value] " +
            "FROM dbo._Reference56 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN (SELECT " +
            "T3._Fld12554RRef AS Fld12554RRef, " +
            "T3._Fld12555_TYPE AS Fld12555_TYPE, " +
            "T3._Fld12555_RTRef AS Fld12555_RTRef, " +
            "T3._Fld12555_RRRef AS Fld12555_RRRef, " +
            "CAST(SUM(T3._Fld12557) AS money) AS Fld12557Balance_, " +
            "CAST(SUM(T3._Fld12558) AS money) AS Fld12558Balance_ " +
            "FROM dbo._AccumRgT12559 T3 WITH(NOLOCK) " +
            "WHERE T3._Period = CAST('5999-11-01 00:00:00' AS datetime2(3)) AND (T3._Fld12557 <> 0 OR T3._Fld12558 <> 0) AND (T3._Fld12557 <> 0 OR T3._Fld12558 <> 0) " +
            "GROUP BY T3._Fld12554RRef, " +
            "T3._Fld12555_TYPE, " +
            "T3._Fld12555_RTRef, " +
            "T3._Fld12555_RRRef " +
            "HAVING (CAST(SUM(T3._Fld12557) AS NUMERIC(33, 8))) <> 0.0 OR (CAST(SUM(T3._Fld12558) AS NUMERIC(33, 8))) <> 0.0) T2 " +
            "ON (0x08 = T2.Fld12555_TYPE AND 0x00000038 = T2.Fld12555_RTRef AND T1._IDRRef = T2.Fld12555_RRRef) " +
            "LEFT OUTER JOIN dbo._Reference17 T4 WITH(NOLOCK) " +
            "ON T1._Fld1056RRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T5 WITH(NOLOCK) " +
            "ON T1._OwnerIDRRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Enum358 T6 WITH(NOLOCK) " +
            "ON T2.Fld12554RRef = T6._IDRRef " +
            "WHERE (T1._Folder) = 0x01 AND (T1._Marked = 0x00) " +
            "AND T5._Description IN @OrganizationNames ",
            new { OrganizationNames = organizations }
        );
    }

    public IEnumerable<SyncCashRegister> GetAmgAllSyncCashRegisters(string organization) {
        return _amgSyncConnection.Query<SyncCashRegister>(
            "SELECT " +
            "T1._Description [CashRegisterName], " +
            "T7._Code [CurrencyCode], " +
            "T8._Description [OrganizationName], " +
            "T2.Fld15024FinalBalance_ [Value], " +
            "T2.Fld15025FinalBalance_ [ManagementValue] " +
            "FROM dbo._Reference76 T1 WITH(NOLOCK) " +
            "INNER JOIN (SELECT " +
            "T3.Fld15022_TYPE AS Fld15022_TYPE, " +
            "T3.Fld15022_RTRef AS Fld15022_RTRef, " +
            "T3.Fld15022_RRRef AS Fld15022_RRRef, " +
            "T3.Fld15023RRef AS Fld15023RRef, " +
            "CAST(SUM(T3.Fld15024Balance_ + T3.Fld15024Receipt_ - T3.Fld15024Expense_) AS NUMERIC(35, 8)) AS Fld15024FinalBalance_, " +
            "CAST(SUM(T3.Fld15025Balance_ + T3.Fld15025Receipt_ - T3.Fld15025Expense_) AS NUMERIC(35, 8)) AS Fld15025FinalBalance_ " +
            "FROM (SELECT " +
            "T4._Fld15022_TYPE AS Fld15022_TYPE, " +
            "T4._Fld15022_RTRef AS Fld15022_RTRef, " +
            "T4._Fld15022_RRRef AS Fld15022_RRRef, " +
            "T4._Fld15023RRef AS Fld15023RRef, " +
            "CAST(SUM(0.0) AS NUMERIC(15, 8)) AS Fld15024Balance_, " +
            "CAST(SUM(CASE WHEN T4._RecordKind = 0.0 THEN T4._Fld15024 ELSE 0.0 END) AS NUMERIC(27, 8)) AS Fld15024Receipt_, " +
            "CAST(SUM(CASE WHEN T4._RecordKind = 0.0 THEN 0.0 ELSE T4._Fld15024 END) AS NUMERIC(27, 8)) AS Fld15024Expense_, " +
            "CAST(SUM(0.0) AS NUMERIC(15, 8)) AS Fld15025Balance_, " +
            "CAST(SUM(CASE WHEN T4._RecordKind = 0.0 THEN T4._Fld15025 ELSE 0.0 END) AS NUMERIC(27, 8)) AS Fld15025Receipt_, " +
            "CAST(SUM(CASE WHEN T4._RecordKind = 0.0 THEN 0.0 ELSE T4._Fld15025 END) AS NUMERIC(27, 8)) AS Fld15025Expense_ " +
            "FROM dbo._AccumRg15020 T4 WITH(NOLOCK) " +
            "WHERE T4._Active = 0x01 " +
            "GROUP BY T4._Fld15022_TYPE, " +
            "T4._Fld15022_RTRef, " +
            "T4._Fld15022_RRRef, " +
            "T4._Fld15023RRef " +
            "HAVING (CAST(SUM(0.0) AS NUMERIC(15, 8))) <> 0.0 OR (CAST(SUM(CASE WHEN T4._RecordKind = 0.0 THEN T4._Fld15024 ELSE 0.0 END) AS NUMERIC(27, 8))) <> 0.0 OR (CAST(SUM(CASE WHEN T4._RecordKind = 0.0 THEN 0.0 ELSE T4._Fld15024 END) AS NUMERIC(27, 8))) <> 0.0 OR (CAST(SUM(0.0) AS NUMERIC(15, 8))) <> 0.0 OR (CAST(SUM(CASE WHEN T4._RecordKind = 0.0 THEN T4._Fld15025 ELSE 0.0 END) AS NUMERIC(27, 8))) <> 0.0 OR (CAST(SUM(CASE WHEN T4._RecordKind = 0.0 THEN 0.0 ELSE T4._Fld15025 END) AS NUMERIC(27, 8))) <> 0.0) T3 " +
            "GROUP BY T3.Fld15022_TYPE, " +
            "T3.Fld15022_RTRef, " +
            "T3.Fld15022_RRRef, " +
            "T3.Fld15023RRef " +
            "HAVING (CAST(SUM(T3.Fld15024Balance_ + T3.Fld15024Receipt_ - T3.Fld15024Expense_) AS NUMERIC(35, 8))) <> 0.0 OR (CAST(SUM(T3.Fld15025Balance_ + T3.Fld15025Receipt_ - T3.Fld15025Expense_) AS NUMERIC(35, 8))) <> 0.0) T2 " +
            "LEFT OUTER JOIN dbo._Reference33 T5 WITH(NOLOCK) " +
            "ON T2.Fld15022_TYPE = 0x08 AND T2.Fld15022_RTRef = 0x00000021 AND T2.Fld15022_RRRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference76 T6 WITH(NOLOCK) " +
            "ON T2.Fld15022_TYPE = 0x08 AND T2.Fld15022_RTRef = 0x0000004C AND T2.Fld15022_RRRef = T6._IDRRef " +
            "ON (0x0000004C = CASE WHEN T2.Fld15022_TYPE = 0x08 AND T2.Fld15022_RTRef = 0x00000021 THEN CASE WHEN T5._IDRRef IS NOT NULL THEN 0x00000021 END WHEN T2.Fld15022_TYPE = 0x08 AND T2.Fld15022_RTRef = 0x0000004C THEN CASE WHEN T6._IDRRef IS NOT NULL THEN 0x0000004C END ELSE CAST(NULL AS BINARY(4)) END AND T1._IDRRef = CASE WHEN T2.Fld15022_TYPE = 0x08 AND T2.Fld15022_RTRef = 0x00000021 THEN T5._IDRRef WHEN T2.Fld15022_TYPE = 0x08 AND T2.Fld15022_RTRef = 0x0000004C THEN T6._IDRRef ELSE CAST(NULL AS BINARY(16)) END) " +
            "LEFT OUTER JOIN dbo._Reference35 T7 WITH(NOLOCK) " +
            "ON T1._Fld1400RRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference116 T8 WITH(NOLOCK) " +
            "ON T2.Fld15023RRef = T8._IDRRef " +
            "WHERE T8._Description = @OrganizationName",
            new { OrganizationName = organization });
    }

    public List<PaymentRegister> GetAllPaymentRegisters() {
        List<PaymentRegister> registers = new();

        _remoteSyncConnection.Query<PaymentRegister, PaymentCurrencyRegister, PaymentRegister>(
            "SELECT * " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID ",
            (register, currencyRegister) => {
                if (registers.Any(r => r.Id.Equals(register.Id)))
                    register = registers.First(r => r.Id.Equals(register.Id));
                else
                    registers.Add(register);

                if (currencyRegister == null) return register;

                register.PaymentCurrencyRegisters.Add(currencyRegister);

                return register;
            }
        );

        return registers;
    }

    public long Add(PaymentRegister register) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [PaymentRegister] " +
            "(" +
            "[Name] " +
            ",[Updated] " +
            ",[Type] " +
            ",[OrganizationID] " +
            ",[AccountNumber] " +
            ",[BankName] " +
            ",[City] " +
            ",[FromDate] " +
            ",[IBAN] " +
            ",[SortCode] " +
            ",[SwiftCode] " +
            ",[ToDate] " +
            ",[IsActive]" +
            ") " +
            "VALUES " +
            "(" +
            "@Name " +
            ",GETUTCDATE() " +
            ",@Type " +
            ",@OrganizationID " +
            ",@AccountNumber " +
            ",@BankName " +
            ",@City " +
            ",@FromDate " +
            ",@IBAN " +
            ",@SortCode " +
            ",@SwiftCode " +
            ",@ToDate " +
            ",@IsActive " +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            register
        ).Single();
    }

    public void Update(PaymentRegister register) {
        _remoteSyncConnection.Execute(
            "UPDATE [PaymentRegister] " +
            "SET " +
            "[Updated] = GETUTCDATE() " +
            ",[Type] = @Type " +
            ",[OrganizationID] = @OrganizationId " +
            ",[AccountNumber] = @AccountNumber " +
            ",[BankName] = @BankName " +
            ",[City] = @City " +
            ",[FromDate] = @FromDate " +
            ",[IBAN] = @IBAN " +
            ",[SortCode] = @SortCode " +
            ",[SwiftCode] = @SwiftCode " +
            ",[ToDate] = @ToDate " +
            ",[IsActive] = @IsActive " +
            ",[IsMain] = @IsMain " +
            ",[Deleted] = @Deleted " +
            "WHERE ID = @Id",
            register
        );
    }

    public long Add(PaymentCurrencyRegister register) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [PaymentCurrencyRegister] " +
            "([Amount], [InitialAmount], [PaymentRegisterID], [CurrencyID], [Updated]) " +
            "VALUES " +
            "(@Amount, @InitialAmount, @PaymentRegisterId, @CurrencyId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            register
        ).Single();
    }

    public void Update(PaymentCurrencyRegister register) {
        _remoteSyncConnection.Execute(
            "UPDATE [PaymentCurrencyRegister] " +
            "SET " +
            "[Amount] = @Amount, [InitialAmount] = @InitialAmount, [PaymentRegisterID] = @PaymentRegisterId, [CurrencyID] = @CurrencyId, [Updated] = GETUTCDATE(), [Deleted] = @Deleted " +
            "WHERE ID = @Id",
            register
        );
    }

    public IEnumerable<SyncBankRegister> GetAllSyncBankRegisters(string[] organizations) {
        return _oneCConnection.Query<SyncBankRegister>(
            "SELECT " +
            "T1._Description [BankAccountName], " +
            "T1._Fld832 [BankAccountNumber], " +
            "T2._Description [OrganizationName], " +
            "T3._Code [CurrencyCode], " +
            "ISNULL(T4.Fld12557Balance_, 0.00) [Value], " +
            "(CASE WHEN T1._Fld838 = N'2001-01-01 00:00:00.000' THEN NULL ELSE DATEADD(YEAR, -2000, T1._Fld838) END)  [DateOpening], " +
            "(CASE WHEN T1._Fld839 = N'2001-01-01 00:00:00.000' THEN NULL ELSE DATEADD(YEAR, -2000, T1._Fld839) END)  [DateClosing], " +
            "T6._Code [BankCode], " +
            "T6._Description [BankName], " +
            "T6._Fld824 [BankNumber], " +
            "T6._Fld825 [City], " +
            "T6._Fld826 [Address] " +
            "FROM dbo._Reference15 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference90 T2 WITH(NOLOCK) " +
            "ON (T1._OwnerID_TYPE = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x08 END AND T1._OwnerID_RTRef = CASE WHEN T2._IDRRef IS NOT NULL THEN 0x0000005A END AND T1._OwnerID_RRRef = T2._IDRRef) " +
            "LEFT OUTER JOIN dbo._Reference17 T3 WITH(NOLOCK) " +
            "ON (T1._Fld836RRef = T3._IDRRef) " +
            "LEFT OUTER JOIN (SELECT " +
            "T5._Fld12554RRef AS Fld12554RRef, " +
            "T5._Fld12555_TYPE AS Fld12555_TYPE, " +
            "T5._Fld12555_RTRef AS Fld12555_RTRef, " +
            "T5._Fld12555_RRRef AS Fld12555_RRRef, " +
            "CAST(SUM(T5._Fld12557) AS money) AS Fld12557Balance_, " +
            "CAST(SUM(T5._Fld12558) AS money) AS Fld12558Balance_ " +
            "FROM dbo._AccumRgT12559 T5 WITH(NOLOCK) " +
            "WHERE T5._Period = CAST('5999-11-01 00:00:00' AS datetime2(3)) AND (T5._Fld12557 <> 0 OR T5._Fld12558 <> 0) AND (T5._Fld12557 <> 0 OR T5._Fld12558 <> 0) " +
            "GROUP BY T5._Fld12554RRef, " +
            "T5._Fld12555_TYPE, " +
            "T5._Fld12555_RTRef, " +
            "T5._Fld12555_RRRef " +
            "HAVING (CAST(SUM(T5._Fld12557) AS NUMERIC(33, 8))) <> 0.0 OR (CAST(SUM(T5._Fld12558) AS NUMERIC(33, 8))) <> 0.0) T4 " +
            "ON (0x08 = T4.Fld12555_TYPE AND 0x0000000F = T4.Fld12555_RTRef AND T1._IDRRef = T4.Fld12555_RRRef) " +
            "LEFT OUTER JOIN dbo._Reference14 T6 WITH(NOLOCK) " +
            "ON (T1._Fld833RRef = T6._IDRRef) " +
            "LEFT OUTER JOIN dbo._Enum358 T7 WITH(NOLOCK) " +
            "ON T4.Fld12554RRef = T7._IDRRef " +
            "WHERE (T1._OwnerID_TYPE = 0x08 AND T1._OwnerID_RTRef = 0x0000005A) AND (T1._Marked = 0x00) " +
            "AND T2._Description IN @OrganizationNames ",
            new { OrganizationNames = organizations }
        );
    }

    public IEnumerable<SyncBankRegister> GetAmgAllSyncBankRegisters(string organization) {
        return _amgSyncConnection.Query<SyncBankRegister>(
            "SELECT " +
            "T1._Description [BankAccountName], " +
            "T1._Fld1156 [BankAccountNumber], " +
            "T2._Description [OrganizationName], " +
            "T3._Code [CurrencyCode], " +
            "ISNULL(T4.Fld15024Balance_, 0.00) [Value], " +
            "(CASE WHEN T1._Fld1162 = N'2001-01-01 00:00:00.000' THEN NULL ELSE DATEADD(YEAR, -2000, T1._Fld1162) END)  [DateOpening], " +
            "(CASE WHEN T1._Fld1163 = N'2001-01-01 00:00:00.000' THEN NULL ELSE DATEADD(YEAR, -2000, T1._Fld1163) END)  [DateClosing], " +
            "T6._Code [BankCode], " +
            "T6._Description [BankName], " +
            "T6._Fld1152 [BankNumber], " +
            "T6._Fld1149 [City], " +
            "T6._Fld1150 [Address] " +
            "FROM dbo._Reference33 T1 " +
            "LEFT OUTER JOIN dbo._Reference116 T2 WITH(NOLOCK) " +
            "ON T1._OwnerID_RRRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 T3 WITH(NOLOCK) " +
            "ON T1._Fld1160RRef = T3._IDRRef " +
            "LEFT JOIN (SELECT " +
            "T3._Fld15022_TYPE AS Fld15022_TYPE, " +
            "T3._Fld15022_RTRef AS Fld15022_RTRef, " +
            "T3._Fld15022_RRRef AS Fld15022_RRRef, " +
            "T3._Fld15023RRef AS Fld15023RRef, " +
            "CAST(SUM(T3._Fld15024) AS NUMERIC(33, 8)) AS Fld15024Balance_, " +
            "CAST(SUM(T3._Fld15025) AS NUMERIC(33, 8)) AS Fld15025Balance_ " +
            "FROM dbo._AccumRgT15026 T3 WITH(NOLOCK) " +
            "WHERE T3._Period = CAST('5999-11-01 00:00:00' AS datetime2(3)) " +
            "AND (T3._Fld15024 <> 0 OR T3._Fld15025 <> 0) AND (T3._Fld15024 <> 0 OR T3._Fld15025 <> 0) " +
            "GROUP BY T3._Fld15022_TYPE, " +
            "T3._Fld15022_RTRef, " +
            "T3._Fld15022_RRRef, " +
            "T3._Fld15023RRef " +
            "HAVING (CAST(SUM(T3._Fld15024) AS NUMERIC(33, 8))) <> 0.0 OR (CAST(SUM(T3._Fld15025) AS NUMERIC(33, 8))) <> 0.0) T4 " +
            "ON (0x08 = T4.Fld15022_TYPE AND 0x00000021 = T4.Fld15022_RTRef AND T1._IDRRef = T4.Fld15022_RRRef) " +
            "LEFT OUTER JOIN dbo._Reference32 T6 WITH(NOLOCK) " +
            "ON T1._Fld1157RRef = T6._IDRRef " +
            "WHERE T1._Marked = 0x00 " +
            "AND T1._OwnerID_RTRef = 0x00000074 " +
            "AND T2._Description = @OrganizationName ",
            new { OrganizationName = organization });
    }

    public IEnumerable<SyncBank> GetAmgAllSyncBanks() {
        return _amgSyncConnection.Query<SyncBank>(
            "SELECT " +
            "T1._Code [MfoCode], " +
            "T1._Description [Name], " +
            "T1._Fld1150 [Address], " +
            "T1._Fld1152 [EdrpouCode], " +
            "T1._Fld1149 [City], " +
            "T1._Fld1151 [Phones] " +
            "FROM dbo._Reference32 T1 WITH(NOLOCK) " +
            "WHERE (T1._Folder) = 0x01 AND (T1._Marked = 0x00) "
        );
    }

    public IEnumerable<SyncBank> GetAllSyncBanks() {
        return _oneCConnection.Query<SyncBank>(
            "SELECT " +
            "T1._Code [MfoCode], " +
            "T1._Description [Name], " +
            "T1._Fld825 [City], " +
            "T1._Fld826 [Address], " +
            "T1._Fld827 [Phones], " +
            "T1._Fld828 [EdrpouCode] " +
            "FROM dbo._Reference14 T1 WITH(NOLOCK) " +
            "WHERE (T1._Marked = 0x00) AND (T1._Folder) = 0x01 "
        );
    }

    public List<Bank> GetAllBanks() {
        return _remoteSyncConnection.Query<Bank>(
            "SELECT * " +
            "FROM [Bank] "
        ).ToList();
    }

    public long Add(Bank bank) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Bank] " +
            "(" +
            "[Updated] " +
            ",[Name] " +
            ",[MfoCode] " +
            ",[EdrpouCode] " +
            ",[City] " +
            ",[Address] " +
            ",[Phones] " +
            ") " +
            "VALUES " +
            "(" +
            "GETUTCDATE() " +
            ",@Name " +
            ",@MfoCode " +
            ",@EdrpouCode " +
            ",@City " +
            ",@Address " +
            ",@Phones " +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            bank
        ).Single();
    }

    public void Update(Bank bank) {
        _remoteSyncConnection.Execute(
            "UPDATE [Bank] " +
            "SET " +
            "[Name] = @Name " +
            ",[Updated] = GETUTCDATE() " +
            ",[MfoCode] = @MfoCode " +
            ",[EdrpouCode] = @EdrpouCode " +
            ",[City] = @City " +
            ",[Address] = @Address " +
            ",[Phones] = @Phones " +
            ",[Deleted] = @Deleted " +
            "WHERE ID = @Id ",
            bank
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