using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync;

public sealed class ProductsSyncRepository : IProductsSyncRepository {
    private readonly IDbConnection _amgSyncConnection;
    private readonly IDbConnection _oneCConnection;

    private readonly IDbConnection _remoteSyncConnection;

    public ProductsSyncRepository(
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgSyncConnection) {
        _oneCConnection = oneCConnection;

        _remoteSyncConnection = remoteSyncConnection;

        _amgSyncConnection = amgSyncConnection;
    }

    public IEnumerable<SyncProduct> GetAllSyncProducts() {
        return _oneCConnection.Query<SyncProduct>(
            "SELECT " +
            "[Product]._IDRRef [SourceId], " +
            "[Product]._ParentIDRRef [ParentId], " +
            "CAST([Product]._Code as bigint) [Code], " +
            "[Product]._Fld1306 [VendorCode], " +
            "[Product]._Description  AS [Name], " +
            "ISNULL(( " +
            "SELECT [ProductTranslation]._Fld15489 [Name] " +
            "FROM dbo._InfoRg15486 [ProductTranslation] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference15549 [ProductTranslationLang] WITH(NOLOCK) " +
            "ON [ProductTranslation]._Fld15488_TYPE = 0x08 " +
            "AND CASE WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 THEN 0x00003CBD ELSE 0x00000000 END = 0x00003CBD " +
            "AND [ProductTranslation]._Fld15488_RRRef = [ProductTranslationLang]._IDRRef " +
            "WHERE ([ProductTranslation]._Fld15487_TYPE = 0x08 " +
            "AND [ProductTranslation]._Fld15487_RTRef = 0x00000054) " +
            "AND [ProductTranslation]._Fld15487_TYPE = 0x08 " +
            "AND [ProductTranslation]._Fld15487_RTRef = 0x00000054 " +
            "AND [ProductTranslation]._Fld15487_RRRef = [Product]._IDRRef " +
            "AND CASE " +
            "WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 AND CASE WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 THEN 0x00003CBD ELSE 0x00000000 END = 0x00003CBD " +
            "THEN [ProductTranslationLang]._Description " +
            "ELSE CAST(NULL AS NVARCHAR(2)) " +
            "END = N'uk' " +
            "), [Product]._Description) [NameUa], " +
            "ISNULL(( " +
            "SELECT [ProductTranslation]._Fld15489 [Name] " +
            "FROM dbo._InfoRg15486 [ProductTranslation] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference15549 [ProductTranslationLang] WITH(NOLOCK) " +
            "ON [ProductTranslation]._Fld15488_TYPE = 0x08 " +
            "AND CASE WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 THEN 0x00003CBD ELSE 0x00000000 END = 0x00003CBD " +
            "AND [ProductTranslation]._Fld15488_RRRef = [ProductTranslationLang]._IDRRef " +
            "WHERE ([ProductTranslation]._Fld15487_TYPE = 0x08 " +
            "AND [ProductTranslation]._Fld15487_RTRef = 0x00000054) " +
            "AND [ProductTranslation]._Fld15487_TYPE = 0x08 " +
            "AND [ProductTranslation]._Fld15487_RTRef = 0x00000054 " +
            "AND [ProductTranslation]._Fld15487_RRRef = [Product]._IDRRef " +
            "AND CASE " +
            "WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 AND CASE WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 THEN 0x00003CBD ELSE 0x00000000 END = 0x00003CBD " +
            "THEN [ProductTranslationLang]._Description " +
            "ELSE CAST(NULL AS NVARCHAR(2)) " +
            "END = N'pl' " +
            "), [Product]._Description) [NamePl], " +
            "[Product]._Fld1315 [Description], " +
            "ISNULL(( " +
            "SELECT [ProductTranslation]._Fld15552 " +
            "FROM dbo._InfoRg15486 [ProductTranslation] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference15549 [ProductTranslationLang] WITH(NOLOCK) " +
            "ON [ProductTranslation]._Fld15488_TYPE = 0x08 " +
            "AND CASE WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 THEN 0x00003CBD ELSE 0x00000000 END = 0x00003CBD " +
            "AND [ProductTranslation]._Fld15488_RRRef = [ProductTranslationLang]._IDRRef " +
            "WHERE ([ProductTranslation]._Fld15487_TYPE = 0x08 " +
            "AND [ProductTranslation]._Fld15487_RTRef = 0x00000054) " +
            "AND [ProductTranslation]._Fld15487_TYPE = 0x08 " +
            "AND [ProductTranslation]._Fld15487_RTRef = 0x00000054 " +
            "AND [ProductTranslation]._Fld15487_RRRef = [Product]._IDRRef " +
            "AND CASE " +
            "WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 AND CASE WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 THEN 0x00003CBD ELSE 0x00000000 END = 0x00003CBD " +
            "THEN [ProductTranslationLang]._Description " +
            "ELSE CAST(NULL AS NVARCHAR(2)) " +
            "END = N'uk' " +
            "), N'') [DescriptionUa], " +
            "[Product]._Fld1315 [Description], " +
            "ISNULL(( " +
            "SELECT [ProductTranslation]._Fld15552 " +
            "FROM dbo._InfoRg15486 [ProductTranslation] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference15549 [ProductTranslationLang] WITH(NOLOCK) " +
            "ON [ProductTranslation]._Fld15488_TYPE = 0x08 " +
            "AND CASE WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 THEN 0x00003CBD ELSE 0x00000000 END = 0x00003CBD " +
            "AND [ProductTranslation]._Fld15488_RRRef = [ProductTranslationLang]._IDRRef " +
            "WHERE ([ProductTranslation]._Fld15487_TYPE = 0x08 " +
            "AND [ProductTranslation]._Fld15487_RTRef = 0x00000054) " +
            "AND [ProductTranslation]._Fld15487_TYPE = 0x08 " +
            "AND [ProductTranslation]._Fld15487_RTRef = 0x00000054 " +
            "AND [ProductTranslation]._Fld15487_RRRef = [Product]._IDRRef " +
            "AND CASE " +
            "WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 AND CASE WHEN [ProductTranslation]._Fld15488_TYPE = 0x08 THEN 0x00003CBD ELSE 0x00000000 END = 0x00003CBD " +
            "THEN [ProductTranslationLang]._Description " +
            "ELSE CAST(NULL AS NVARCHAR(2)) " +
            "END = N'pl' " +
            "), N'') [DescriptionPl], " +
            "CAST([Product]._Fld13635 AS NVARCHAR) [PackingStandard], " +
            "CAST([Product]._Fld13572 AS NVARCHAR) [Standard], " +
            "CAST([Product]._Fld13572 AS NVARCHAR) [OrderStandard], " +
            "( " +
            "SELECT CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END " +
            "FROM dbo._InfoRg10917 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference59 T3 WITH(NOLOCK) " +
            "ON (T1._Fld10919RRef = T3._IDRRef) " +
            "WHERE (T1._Fld10918_TYPE = 0x08 AND T1._Fld10918_RTRef = 0x00000054) " +
            "AND (T1._Fld10918_TYPE = CASE WHEN [Product]._IDRRef IS NOT NULL THEN 0x08 END " +
            "AND T1._Fld10918_RTRef = CASE WHEN [Product]._IDRRef IS NOT NULL THEN 0x00000054 END AND T1._Fld10918_RRRef = [Product]._IDRRef) " +
            "AND T3._Code = N'000000105' " +
            ") [IsForSale], " +
            "( " +
            "SELECT CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END " +
            "FROM dbo._InfoRg10917 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference59 T3 WITH(NOLOCK) " +
            "ON (T1._Fld10919RRef = T3._IDRRef) " +
            "WHERE (T1._Fld10918_TYPE = 0x08 AND T1._Fld10918_RTRef = 0x00000054) " +
            "AND (T1._Fld10918_TYPE = CASE WHEN [Product]._IDRRef IS NOT NULL THEN 0x08 END " +
            "AND T1._Fld10918_RTRef = CASE WHEN [Product]._IDRRef IS NOT NULL THEN 0x00000054 END AND T1._Fld10918_RRRef = [Product]._IDRRef) " +
            "AND T3._Code = N'000000106' " +
            ") [IsZeroForSale], " +
            "[Product]._Fld1339 [Size], " +
            "[Product]._Fld1340 [Top], " +
            "[MeasureClassifier]._Fld1040 [Weight], " +
            "CAST ([MeasureClassifier]._Fld1041 AS NVARCHAR) [Volume], " +
            "[UKVED]._Code [UCGFEA], " +
            "[ProductOriginalCode]._Code [OriginalNumberCode], " +
            "[ProductOriginalCode]._Description [OriginalNumberName], " +
            "[MeasureUnit]._Code [MeasureUnitCode], " +
            "[MeasureUnit]._Description [MeasureUnitName] " +
            "FROM dbo._Reference84 [Product] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference159 [ProductOriginalCode] WITH(NOLOCK) " +
            "ON [Product]._Fld1338RRef = [ProductOriginalCode]._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference61 [MeasureUnit] WITH(NOLOCK) " +
            "ON [Product]._Fld1307RRef = [MeasureUnit]._IDRRef " +
            "LEFT JOIN dbo._Reference52 [MeasureClassifier] " +
            "ON [MeasureClassifier]._IDRRef = ( " +
            "SELECT TOP(1) [JoinClassifier]._IDRRef " +
            "FROM dbo._Reference52 [JoinClassifier] " +
            "WHERE [JoinClassifier]._OwnerID_TYPE = 0x08 AND [JoinClassifier]._OwnerID_RTRef = 0x00000054 AND [JoinClassifier]._OwnerID_RRRef = [Product]._IDRRef " +
            "ORDER BY [JoinClassifier]._Code DESC " +
            ") " +
            "LEFT OUTER JOIN dbo._Reference14682 [UKVED] WITH(NOLOCK) " +
            "ON [UKVED]._IDRRef = ( " +
            "SELECT TOP(1) [Join]._IDRRef " +
            "FROM dbo._Reference14682 [Join] " +
            "LEFT JOIN dbo._Reference14922 T1 " +
            "ON T1._Fld14924RRef = [Join]._IDRRef " +
            "WHERE (T1._Marked = 0x00) " +
            "AND T1._OwnerIDRRef = [Product]._IDRRef " +
            ") " +
            "WHERE ([Product]._Marked = 0x00) " +
            "AND ([Product]._Folder) = 0x01 " +
            "AND [MeasureUnit]._Code IS NOT NULL ",
            commandTimeout: 7200
        );
    }

    public IEnumerable<SyncProduct> GetAmgAllSyncProducts() {
        return _amgSyncConnection.Query<SyncProduct>(
            "SELECT " +
            "[Product]._IDRRef [SourceId], " +
            "T6._IDRRef [ParentId], " +
            "CAST([Product]._Code as bigint) [Code], " +
            "[Product]._Fld1765 [VendorCode], " +
            "[Product]._Description  AS [Name], " +
            "ISNULL(( " +
            "SELECT TOP 1 [Tranlate].[_Fld14698] FROM dbo._InfoRg14695 [Tranlate] " +
            "LEFT JOIN dbo._Reference197 [Language] " +
            "ON [Language].[_IDRRef] = [Tranlate]._Fld14697_RRRef " +
            "WHERE [Tranlate]._Fld14696_RTRef = 0x0000006C " +
            "AND [Tranlate].[_Fld14696_RRRef] = [Product]._IDRRef " +
            "AND [Language].[_Description] = 'uk' " +
            ") , [Product]._Description) [NameUa], " +
            "ISNULL(( " +
            "SELECT TOP 1 [Tranlate].[_Fld14698] FROM dbo._InfoRg14695 [Tranlate] " +
            "LEFT JOIN dbo._Reference197 [Language] " +
            "ON [Language].[_IDRRef] = [Tranlate]._Fld14697_RRRef " +
            "WHERE [Tranlate]._Fld14696_RTRef = 0x0000006C " +
            "AND [Tranlate].[_Fld14696_RRRef] = [Product]._IDRRef " +
            "AND [Language].[_Description] = 'pl' " +
            ") , [Product]._Description) [NamePl], " +
            "[Product].[_Fld1819] [Description], " +
            "ISNULL(( " +
            "SELECT TOP 1 [Tranlate].[_Fld14699] FROM dbo._InfoRg14695 [Tranlate] " +
            "LEFT JOIN dbo._Reference197 [Language] " +
            "ON [Language].[_IDRRef] = [Tranlate]._Fld14697_RRRef " +
            "WHERE [Tranlate]._Fld14696_RTRef = 0x0000006C " +
            "AND [Tranlate].[_Fld14696_RRRef] = [Product]._IDRRef " +
            "AND [Language].[_Description] = 'uk' " +
            ") , [Product]._Fld1819) [DescriptionUa], " +
            "ISNULL(( " +
            "SELECT TOP 1 [Tranlate].[_Fld14699] FROM dbo._InfoRg14695 [Tranlate] " +
            "LEFT JOIN dbo._Reference197 [Language] " +
            "ON [Language].[_IDRRef] = [Tranlate]._Fld14697_RRRef " +
            "WHERE [Tranlate]._Fld14696_RTRef = 0x0000006C " +
            "AND [Tranlate].[_Fld14696_RRRef] = [Product]._IDRRef " +
            "AND [Language].[_Description] = 'pl' " +
            ") , [Product]._Fld1819) [DescriptionPl], " +
            "CAST([Product]._Fld1805 AS NVARCHAR) [PackingStandard], " +
            "CAST([Product]._Fld1803 AS NVARCHAR) [Standard], " +
            "[Product]._Fld1798 [Size], " +
            "[Product]._Fld1799 [Top], " +
            "[T3]._Fld1384 [Weight], " +
            "[T3]._Fld1385 [Volume], " +
            "T2._Code [UCGFEA], " +
            "T4._Code [MeasureUnitCode], " +
            "T4._Description [MeasureUnitName], " +
            "[OriginalNumber]._Code [OriginalNumberCode], " +
            "[OriginalNumber]._Description [OriginalNumberName] " +
            "FROM dbo._Reference108 [Product] WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference108 T6 WITH(NOLOCK) " +
            "ON [Product]._ParentIDRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference86 T2 WITH(NOLOCK) " +
            "ON [Product]._Fld1812RRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference72 T3 WITH(NOLOCK) " +
            "ON [Product]._Fld1772RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference81 T4 WITH(NOLOCK) " +
            "ON T3._Fld1383RRef = T4._IDRRef " +
            "LEFT JOIN dbo._Reference192 [OriginalNumber] " +
            "ON [OriginalNumber]._OwnerIDRRef = [Product]._IDRRef " +
            "WHERE ([Product]._Marked = 0x00) " +
            "AND ([Product]._Folder) = 0x01 " +
            "AND [T4]._Code IS NOT NULL ",
            commandTimeout: 7200
        );
    }

    public List<MeasureUnit> GetAllMeasureUnits() {
        return _remoteSyncConnection.Query<MeasureUnit>(
            "SELECT * " +
            "FROM [MeasureUnit] " +
            "WHERE [MeasureUnit].Deleted = 0"
        ).ToList();
    }

    public long Add(MeasureUnit measureUnit) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [MeasureUnit] " +
            "([Name], [Description], [CodeOneC], Updated) " +
            "VALUES " +
            "(@Name, @Description, @CodeOneC, GETUTCDATE()); " +
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

    public void ExecuteQuery(string sqlExpression) {
        _remoteSyncConnection.Execute(
            sqlExpression,
            commandTimeout: 36000
        );
    }

    public void AssignProductsToProductGroups() {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductProductGroup] SET Deleted = 1, Updated = GETUTCDATE() WHERE Deleted = 0 " +
            "INSERT INTO [ProductProductGroup] (ProductGroupID, ProductID, VendorCode, OrderStandard, Updated) " +
            "SELECT " +
            "[ProductGroup].ID [ProductGroupID], " +
            "[Product].ID [ProductID], " +
            "[Product].VendorCode [VendorCode], " +
            "(CASE " +
            "WHEN [Product].OrderStandard IS NOT NULL AND [Product].OrderStandard <> N'' " +
            "THEN CAST([Product].OrderStandard AS float) " +
            "ELSE CAST(N'0.000' AS float) " +
            "END) [OrderStandard], " +
            "GETUTCDATE() [Updated] " +
            "FROM [Product] " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].SourceAmgID = [Product].ParentAmgId " +
            "OR [ProductGroup].SourceFenixID = [Product].ParentFenixId " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = [Product].ID " +
            "AND [ProductProductGroup].ProductGroupID = [ProductGroup].ID " +
            "AND [ProductProductGroup].Deleted = 0 " +
            "WHERE [ProductGroup].ID IS NOT NULL " +
            "AND [ProductProductGroup].ID IS NULL",
            commandTimeout: 3600
        );
    }

    public List<SyncAnalogue> GetAllSyncAnalogues() {
        return _oneCConnection.Query<SyncAnalogue>(
            "SELECT " +
            "CAST(T2._Code AS bigint) [BaseProductCode], " +
            "CAST(T3._Code AS bigint) [AnalogueProductCode] " +
            "FROM dbo._InfoRg12352 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference84 T2 WITH(NOLOCK) " +
            "ON T1._Fld12353RRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 T3 WITH(NOLOCK) " +
            "ON T1._Fld12355RRef = T3._IDRRef",
            commandTimeout: 3600
        ).ToList();
    }

    public List<SyncAnalogue> GetAmgAllSyncAnalogues() {
        return _amgSyncConnection.Query<SyncAnalogue>(
            "SELECT " +
            "CAST(T2._Code AS bigint) [BaseProductCode], " +
            "CAST(T3._Code AS bigint) [AnalogueProductCode] " +
            "FROM dbo._InfoRg14603 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference108 T2 WITH(NOLOCK) " +
            "ON T1._Fld14604RRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference108 T3 WITH(NOLOCK) " +
            "ON T1._Fld14606RRef = T3._IDRRef ",
            commandTimeout: 3600
        ).ToList();
    }

    public List<SyncComponent> GetAllSyncComponents() {
        return _oneCConnection.Query<SyncComponent>(
            "SELECT " +
            "CAST(T2._Code AS bigint) [BaseProductCode], " +
            "CAST(T3._Code AS bigint) [ComponentProductCode], " +
            "T1._Fld10966 [SetComponentsQty] " +
            "FROM dbo._InfoRg10961 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference84 T2 WITH(NOLOCK) " +
            "ON T1._Fld10962RRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 T3 WITH(NOLOCK) " +
            "ON T1._Fld10964RRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference52 T4 WITH(NOLOCK) " +
            "ON T1._Fld10967RRef = T4._IDRRef ",
            commandTimeout: 3600
        ).ToList();
    }

    public List<SyncComponent> GetAmgAllSyncComponents() {
        return _amgSyncConnection.Query<SyncComponent>(
            "SELECT " +
            "CAST(T2._Code AS bigint) [BaseProductCode], " +
            "CAST(T3._Code AS bigint) [ComponentProductCode] " +
            "FROM dbo._InfoRg13040 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference108 T2 WITH(NOLOCK) " +
            "ON T1._Fld13041RRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference108 T3 WITH(NOLOCK) " +
            "ON T1._Fld13043RRef = T3._IDRRef ",
            commandTimeout: 3600
        ).ToList();
    }

    public List<SyncOriginalNumber> GetAllSyncOriginalNumbers() {
        return _oneCConnection.Query<SyncOriginalNumber>(
            "SELECT " +
            "CAST(T2._Code AS bigint) [ProductCode], " +
            "T1._Fld13575 [OriginalNumber], " +
            "CASE WHEN (T2._Fld13576 = T1._Fld13575) THEN 1 ELSE 0 END [IsMainNumber] " +
            "FROM dbo._InfoRg13573 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference84 T2 WITH(NOLOCK) " +
            "ON T1._Fld13574RRef = T2._IDRRef ",
            commandTimeout: 3600
        ).ToList();
    }

    public List<SyncOriginalNumber> GetAmgAllSyncOriginalNumbers() {
        return _amgSyncConnection.Query<SyncOriginalNumber>(
            "SELECT " +
            "T2._Code [ProductCode], " +
            "T1._Fld14637 [OriginalNumber], " +
            "CASE WHEN (T2._Fld1804 = T1._Fld14637) THEN 1 ELSE 0 END [IsMainNumber] " +
            "FROM dbo._InfoRg14635 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference108 T2 WITH(NOLOCK) " +
            "ON T1._Fld14636RRef = T2._IDRRef ",
            commandTimeout: 3600
        ).ToList();
    }

    public IEnumerable<Currency> GetAllCurrencies() {
        return _remoteSyncConnection.Query<Currency>(
            "SELECT * " +
            "FROM [Currency] " +
            "WHERE [Currency].Deleted = 0"
        );
    }

    public IEnumerable<SyncPricing> GetAllSyncPricings() {
        return _oneCConnection.Query<SyncPricing>(
            "SELECT " +
            "T1._Description  AS [Name], " +
            "T3._Description  AS [BaseName], " +
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
            "T1._Description  AS [Name], " +
            "T2._Description  AS [BaseName], " +
            "T1._Fld2329 [Discount], " +
            "CAST(T1._Fld2330 as bit) [ForVat] " +
            "FROM dbo._Reference171 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference171 T2 WITH(NOLOCK) " +
            "ON T1._Fld2327RRef = T2._IDRRef " +
            "WHERE (T1._Marked = 0x00) " +
            "AND T1._Description NOT LIKE N'(до%'; ");
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

    public IEnumerable<SyncProductPrice> GetAllSyncProductPrices() {
        return _oneCConnection.Query<SyncProductPrice>(
            "SELECT " +
            "CASE WHEN T5._Description = N'ЦЗ (Фіксована націнка)' THEN N'ЦЗ' ELSE T5._Description END [PricingName], " +
            "CAST(T6._Code AS bigint) [ProductCode], " +
            "T1.Fld12282_ [Price] " +
            "FROM (SELECT " +
            "T4._Fld12283RRef AS Fld12283RRef, " +
            "T4._Active AS Active_, " +
            "T4._Fld12278RRef AS Fld12278RRef, " +
            "T4._Fld12281RRef AS Fld12281RRef, " +
            "T4._Fld12284 AS Fld12284_, " +
            "T4._Fld12282 AS Fld12282_, " +
            "T4._Fld12279RRef AS Fld12279RRef, " +
            "T4._Fld12285RRef AS Fld12285RRef " +
            "FROM (SELECT " +
            "T3._Fld12278RRef AS Fld12278RRef, " +
            "T3._Fld12279RRef AS Fld12279RRef, " +
            "T3._Fld12280RRef AS Fld12280RRef, " +
            "MAX(T3._Period) AS MAXPERIOD_ " +
            "FROM dbo._InfoRg12277 T3 WITH(NOLOCK) " +
            "WHERE T3._Active = 0x01 " +
            "GROUP BY T3._Fld12278RRef, " +
            "T3._Fld12279RRef, " +
            "T3._Fld12280RRef) T2 " +
            "INNER JOIN dbo._InfoRg12277 T4 WITH(NOLOCK) " +
            "ON T2.Fld12278RRef = T4._Fld12278RRef AND T2.Fld12279RRef = T4._Fld12279RRef AND T2.Fld12280RRef = T4._Fld12280RRef AND T2.MAXPERIOD_ = T4._Period) T1 " +
            "LEFT OUTER JOIN dbo._Reference143 T5 WITH(NOLOCK) " +
            "ON T1.Fld12278RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference84 T6 WITH(NOLOCK) " +
            "ON T1.Fld12279RRef = T6._IDRRef " +
            "WHERE T5._Description NOT LIKE N'(до%'"
        );
    }

    public IEnumerable<SyncProductPrice> GetAmgAllSyncProductPrices() {
        return _amgSyncConnection.Query<SyncProductPrice>(
            "SELECT " +
            "T5._Description [PricingName], " +
            "T6._Code [ProductCode], " +
            "T1.Fld14643_ [Price] " +
            "FROM (SELECT " +
            "T4._Fld14639RRef AS Fld14639RRef, " +
            "T4._Fld14640RRef AS Fld14640RRef, " +
            "T4._Fld14643 AS Fld14643_ " +
            "FROM (SELECT " +
            "T3._Fld14639RRef AS Fld14639RRef, " +
            "T3._Fld14640RRef AS Fld14640RRef, " +
            "T3._Fld14641RRef AS Fld14641RRef, " +
            "MAX(T3._Period) AS MAXPERIOD_ " +
            "FROM dbo._InfoRg14638 T3 WITH(NOLOCK) " +
            "WHERE T3._Active = 0x01 " +
            "GROUP BY T3._Fld14639RRef, " +
            "T3._Fld14640RRef, " +
            "T3._Fld14641RRef) T2 " +
            "INNER JOIN dbo._InfoRg14638 T4 WITH(NOLOCK) " +
            "ON T2.Fld14639RRef = T4._Fld14639RRef AND T2.Fld14640RRef = T4._Fld14640RRef AND T2.Fld14641RRef = T4._Fld14641RRef AND T2.MAXPERIOD_ = T4._Period) T1 " +
            "LEFT OUTER JOIN dbo._Reference171 T5 WITH(NOLOCK) " +
            "ON T1.Fld14639RRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference108 T6 WITH(NOLOCK) " +
            "ON T1.Fld14640RRef = T6._IDRRef " +
            "WHERE T5._Description NOT LIKE N'(до%' ",
            commandTimeout: 3600);
    }

    public Product GetProductByVendorCode(string vendorCode) {
        return _remoteSyncConnection.Query<Product>(
            "SELECT TOP(1) * " +
            "FROM [Product] " +
            "WHERE Deleted = 0 " +
            "AND VendorCode = @VendorCode",
            new { VendorCode = vendorCode }
        ).SingleOrDefault();
    }

    public Product SearchProductByVendorCode(string vendorCode) {
        return _remoteSyncConnection.Query<Product>(
            "SELECT TOP(1) * " +
            "FROM [Product] " +
            "WHERE Deleted = 0 " +
            "AND VendorCode like @VendorCode",
            new { VendorCode = vendorCode }
        ).SingleOrDefault();
    }

    public List<Product> GetAllProducts() {
        return _remoteSyncConnection.Query<Product>(
            "SELECT" +
            "[Product].ID " +
            ", [Product].[Name] " +
            ", [Product].[NameUA] " +
            ", [Product].[NameUA] " +
            ", [Product].VendorCode " +
            ", [Product].ParentAmgId " +
            ", [Product].ParentFenixId " +
            ", [Product].SourceAmgId " +
            ", [Product].SourceFenixId " +
            ", [Product].SourceAmgCode " +
            ", [Product].SourceFenixCode " +
            "FROM [Product] " +
            "WHERE [Product].Deleted = 0 "
        ).ToList();
    }

    public void CleanProductImages() {
        _remoteSyncConnection.Execute(
            "DELETE FROM [ProductImage]; " +
            "UPDATE [Product] SET HasImage = 0, [Image] = N'' "
        );
    }

    public void CleanCarBrandsAndAssignments() {
        _remoteSyncConnection.Execute(
            "DELETE FROM [ProductCarBrand]; " +
            "DELETE FROM [CarBrand]"
        );
    }

    public long Add(CarBrand carBrand) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [CarBrand] " +
            "([Updated],[Name],[Description],[ImageUrl],[Alias]) " +
            "VALUES " +
            "(GETUTCDATE(), @Name, @Description, @ImageUrl, @Alias); " +
            "SELECT SCOPE_IDENTITY()",
            carBrand
        ).Single();
    }

    public IEnumerable<PriceType> GetAllPriceTypes() {
        return _remoteSyncConnection.Query<PriceType>(
            "SELECT * FROM [PriceType] " +
            "WHERE [PriceType].[Deleted] = 0;"
        ).ToList();
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
}