using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.CalculationTypes.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.CalculationTypes;

public sealed class CalculationTypeTranslationRepository : ICalculationTypeTranslationRepository {
    private readonly IDbConnection _connection;

    public CalculationTypeTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CalculationTypeTranslation calculationTypeTranslation) {
        return _connection.Query<long>(
                "INSERT INTO CalculationTypeTranslation (Name, CalculationTypeId, CultureCode, Updated) " +
                "VALUES (@Name, @CalculationTypeId, @CultureCode, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                calculationTypeTranslation
            )
            .Single();
    }

    public void Update(CalculationTypeTranslation calculationTypeTranslation) {
        _connection.Execute(
            "UPDATE CalculationTypeTranslation SET " +
            "Name = @Name, CalculationTypeId = @CalculationTypeId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            calculationTypeTranslation
        );
    }

    public CalculationTypeTranslation GetById(long id) {
        return _connection.Query<CalculationTypeTranslation>(
                "SELECT * FROM CalculationTypeTranslation " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public CalculationTypeTranslation GetByNetId(Guid netId) {
        return _connection.Query<CalculationTypeTranslation>(
                "SELECT * FROM CalculationTypeTranslation " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<CalculationTypeTranslation> GetAll() {
        return _connection.Query<CalculationTypeTranslation>(
                "SELECT * FROM CalculationTypeTranslation " +
                "WHERE CalculationTypeTranslation.Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE CalculationTypeTranslation SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}