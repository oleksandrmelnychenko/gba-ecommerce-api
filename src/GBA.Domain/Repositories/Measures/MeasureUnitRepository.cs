using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.Measures.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Measures;

public sealed class MeasureUnitRepository : IMeasureUnitRepository {
    private readonly IDbConnection _connection;

    public MeasureUnitRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(MeasureUnit measureUnit) {
        return _connection.Query<long>(
                "INSERT INTO MeasureUnit (Name, Description, CodeOneC, Updated) VALUES(@Name, @Description, @CodeOneC, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()", measureUnit)
            .Single();
    }

    public List<MeasureUnit> GetAll() {
        return _connection.Query<MeasureUnit, MeasureUnitTranslation, MeasureUnit>(
                "SELECT * FROM MeasureUnit " +
                "LEFT OUTER JOIN MeasureUnitTranslation " +
                "ON MeasureUnit.ID = MeasureUnitTranslation.MeasureUnitID AND MeasureUnitTranslation.CultureCode = @Culture " +
                "WHERE MeasureUnit.Deleted = 0",
                (measureUnit, translation) => {
                    if (translation != null) {
                        measureUnit.Name = translation.Name;
                        measureUnit.Description = translation.Description;
                    }

                    return measureUnit;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName })
            .ToList();
    }

    public IEnumerable<MeasureUnit> GetAllFromSearch(string value) {
        return _connection.Query<MeasureUnit>(
            "SELECT [MeasureUnit].ID " +
            ", [MeasureUnit].Created " +
            ", [MeasureUnit].Updated " +
            ", [MeasureUnit].NetUID " +
            ", [MeasureUnit].Deleted " +
            ", [MeasureUnit].[Name] " +
            ", [MeasureUnit].[Description] " +
            "FROM [views].[MeasureUnitView] [MeasureUnit] " +
            "WHERE [MeasureUnit].Deleted = 0 " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "AND ( " +
            "PATINDEX('%' + @Value + '%', [MeasureUnit].[Name]) > 0 " +
            "OR " +
            "PATINDEX('%' + @Value + '%', [MeasureUnit].[Description]) > 0 " +
            "OR " +
            "PATINDEX('%' + @Value + '%', [MeasureUnit].[OriginalName]) > 0 " +
            "OR " +
            "PATINDEX('%' + @Value + '%', [MeasureUnit].[OriginalDescription]) > 0 " +
            ")",
            new { Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public MeasureUnit GetById(long id) {
        return _connection.Query<MeasureUnit>(
                "SELECT * FROM MeasureUnit WHERE ID = @Id",
                new { Id = id })
            .SingleOrDefault();
    }

    public MeasureUnit GetByName(string name) {
        return _connection.Query<MeasureUnit>(
                "SELECT TOP(1) [MeasureUnit].* " +
                "FROM [MeasureUnit] " +
                "LEFT JOIN [MeasureUnitTranslation] " +
                "ON [MeasureUnitTranslation].MeasureUnitID = [MeasureUnit].ID " +
                "AND [MeasureUnitTranslation].Deleted = 0 " +
                "WHERE [MeasureUnit].Deleted = 0 " +
                "AND (" +
                "[MeasureUnit].[Name] = @Name " +
                "OR " +
                "[MeasureUnitTranslation].[Name] = @Name" +
                ")",
                new { Name = name }
            )
            .SingleOrDefault();
    }

    public MeasureUnit GetByNetId(Guid netId) {
        return _connection.Query<MeasureUnit, MeasureUnitTranslation, MeasureUnit>(
                "SELECT * FROM MeasureUnit " +
                "LEFT OUTER JOIN MeasureUnitTranslation " +
                "ON MeasureUnit.ID = MeasureUnitTranslation.MeasureUnitID AND MeasureUnitTranslation.CultureCode = @Culture " +
                "WHERE MeasureUnit.NetUID = @NetId",
                (measureUnit, measureUnitTranslation) => {
                    measureUnit.Name = measureUnitTranslation?.Name;
                    measureUnit.Description = measureUnitTranslation?.Description;

                    return measureUnit;
                },
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName })
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute("UPDATE MeasureUnit SET Deleted = 1 WHERE NetUID = @NetId", new { NetId = netId });
    }

    public void Update(MeasureUnit measureUnit) {
        _connection.Execute(
            "UPDATE MeasureUnit SET " +
            "Name = @Name, " +
            "Description = @Description, " +
            "CodeOneC = @CodeOneC, " +
            "Updated = getutcdate() " +
            "WHERE NetUID = @NetUid", measureUnit);
    }
}