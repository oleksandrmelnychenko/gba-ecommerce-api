using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Measures.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Measures;

public sealed class MeasureUnitTranslationRepository : IMeasureUnitTranslationRepository {
    private readonly IDbConnection _connection;

    public MeasureUnitTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(MeasureUnitTranslation measureUnitTranslation) {
        return _connection.Query<long>(
                "INSERT INTO MeasureUnitTranslation (Name, Description, CultureCode, MeasureUnitID, Updated) " +
                "VALUES(@Name, @Description, @CultureCode, @MeasureUnitID, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                measureUnitTranslation)
            .Single();
    }

    public List<MeasureUnitTranslation> GetAll() {
        return _connection.Query<MeasureUnitTranslation>(
                "SELECT * FROM MeasureUnitTranslation WHERE DELETED = 0")
            .ToList();
    }

    public MeasureUnitTranslation GetById(long id) {
        return _connection.Query<MeasureUnitTranslation>(
                "SELECT * FROM MeasureUnitTranslation WHERE ID = @Id", new { Id = id })
            .SingleOrDefault();
    }

    public MeasureUnitTranslation GetByNetId(Guid netId) {
        return _connection.Query<MeasureUnitTranslation>(
                "SELECT * FROM MeasureUnitTranslation WHERE NetUID = @NetId", new { NetId = netId })
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute("UPDATE MeasureUnitTranslation SET Deleted = 1 WHERE NetUID = @NetId", new { NetId = netId });
    }

    public void Update(MeasureUnitTranslation measureUnitTranslation) {
        _connection.Execute(
            "UPDATE MeasureUnitTranslation " +
            "SET Name = @Name, Description = @Description, CultureCode = @CultureCode, MeasureUnitID = @MeasureUnitID, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            measureUnitTranslation);
    }
}