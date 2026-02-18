using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Repositories.CalculationTypes.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.CalculationTypes;

public sealed class CalculationTypeRepository : ICalculationTypeRepository {
    private readonly IDbConnection _connection;

    public CalculationTypeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CalculationType calculationType) {
        return _connection.Query<long>(
                "INSERT INTO CalculationType (Name, Updated) " +
                "VALUES (@Name, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                calculationType
            )
            .Single();
    }

    public void Update(CalculationType calculationType) {
        _connection.Execute(
            "UPDATE CalculationType SET " +
            "Name = @Name, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            calculationType
        );
    }

    public CalculationType GetById(long id) {
        return _connection.Query<CalculationType>(
                "SELECT * FROM CalculationType " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public CalculationType GetByNetId(Guid netId) {
        return _connection.Query<CalculationType, CalculationTypeTranslation, CalculationType>(
                "SELECT * FROM CalculationType " +
                "LEFT JOIN CalculationTypeTranslation " +
                "ON CalculationType.ID = CalculationTypeTranslation.CalculationTypeID " +
                "AND CalculationTypeTranslation.CultureCode = @Culture " +
                "WHERE CalculationType.NetUID = @NetId",
                (type, translation) => {
                    type.CalculationTypeTranslations.Add(translation);

                    return type;
                },
                new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName })
            .SingleOrDefault();
    }

    public List<CalculationType> GetAll() {
        return _connection.Query<CalculationType, CalculationTypeTranslation, CalculationType>(
                "SELECT * FROM CalculationType " +
                "LEFT JOIN CalculationTypeTranslation " +
                "ON CalculationType.ID = CalculationTypeTranslation.CalculationTypeID " +
                "AND CalculationTypeTranslation.CultureCode = @Culture " +
                "WHERE CalculationType.Deleted = 0",
                (type, translation) => {
                    type.CalculationTypeTranslations.Add(translation);

                    return type;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName })
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE CalculationType SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}