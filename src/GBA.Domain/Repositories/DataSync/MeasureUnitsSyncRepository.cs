using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync;

public sealed class MeasureUnitsSyncRepository : IMeasureUnitsSyncRepository {
    private readonly IDbConnection _amgSyncConnection;
    private readonly IDbConnection _oneCConnection;

    private readonly IDbConnection _remoteSyncConnection;

    public MeasureUnitsSyncRepository(
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgSyncConnection) {
        _oneCConnection = oneCConnection;

        _remoteSyncConnection = remoteSyncConnection;
        _amgSyncConnection = amgSyncConnection;
    }

    public IEnumerable<SyncMeasureUnit> GetAllSyncMeasureUnits() {
        return _oneCConnection.Query<SyncMeasureUnit>(
            "SELECT  " +
            "T1._Code AS [Code], " +
            "T1._Description AS [Name], " +
            "T1._Fld1066 AS [FullName] " +
            "FROM dbo._Reference61 T1 WITH(NOLOCK) " +
            "WHERE (T1._Marked = 0x00) "
        );
    }

    public IEnumerable<SyncMeasureUnit> GetAmgAllSyncMeasureUnits() {
        return _amgSyncConnection.Query<SyncMeasureUnit>(
            "SELECT " +
            "T1._Code [Code], " +
            "T1._Description [Name], " +
            "T1._Fld1410 [FullName] " +
            "FROM dbo._Reference81 T1 WITH(NOLOCK) " +
            "WHERE (T1._Marked = 0x00) "
        );
    }

    public IEnumerable<MeasureUnit> GetAllMeasureUnit() {
        return _remoteSyncConnection.Query<MeasureUnit, MeasureUnitTranslation, MeasureUnit>(
            "SELECT * " +
            "FROM [MeasureUnit] " +
            "LEFT JOIN [MeasureUnitTranslation] " +
            "ON [MeasureUnitTranslation].MeasureUnitID = [MeasureUnit].ID " +
            "AND [MeasureUnitTranslation].Deleted = 0 " +
            "AND [MeasureUnitTranslation].CultureCode = @Culture " +
            "WHERE [MeasureUnit].[CodeOneC] IS NOT NULL ",
            (unit, translation) => {
                if (translation != null) unit.MeasureUnitTranslations.Add(translation);

                return unit;
            },
            new { Culture = "uk" }
        );
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

    public void Update(MeasureUnit measureUnit) {
        _remoteSyncConnection.Execute(
            "UPDATE [MeasureUnit] " +
            "SET [Name] = @Name, [Description] = @Description, [CodeOneC] = @CodeOneC, Updated = GETUTCDATE(), [Deleted] = @Deleted " +
            "WHERE ID = @Id",
            measureUnit
        );
    }

    public void Update(MeasureUnitTranslation translation) {
        _remoteSyncConnection.Execute(
            "UPDATE [MeasureUnitTranslation] " +
            "SET [Name] = @Name, [Description] = @Description, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            translation
        );
    }
}