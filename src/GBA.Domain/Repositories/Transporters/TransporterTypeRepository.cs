using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Repositories.Transporters.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Transporters;

public sealed class TransporterTypeRepository : ITransporterTypeRepository {
    private readonly IDbConnection _connection;

    public TransporterTypeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(TransporterType transporterType) {
        return _connection.Query<long>(
                "INSERT INTO TransporterType " +
                "(Name, Updated) VALUES(@Name, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                transporterType
            )
            .Single();
    }

    public List<TransporterType> GetAll() {
        return _connection.Query<TransporterType, TransporterTypeTranslation, TransporterType>(
                "SELECT * FROM TransporterType " +
                "LEFT OUTER JOIN TransporterTypeTranslation " +
                "ON TransporterType.Id = TransporterTypeTranslation.TransporterTypeId " +
                "AND TransporterTypeTranslation.CultureCode = @Culture " +
                "WHERE TransporterType.Deleted = 0",
                (transporterType, translation) => {
                    if (translation != null) transporterType.Name = translation.Name;

                    return transporterType;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }

    public TransporterType GetById(long id) {
        return _connection.Query<TransporterType>(
                "SELECT * FROM TransporterType WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public TransporterType GetByNetId(Guid netId) {
        return _connection.Query<TransporterType>(
                "SELECT * FROM TransporterType " +
                "LEFT OUTER JOIN TransporterTypeTranslation " +
                "ON TransporterType.Id = TransporterTypeTranslation.TransporterTypeId " +
                "AND TransporterTypeTranslation.CultureCode = @Culture " +
                "WHERE TransporterType.NetUID = @NetId ",
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE TransporterType SET Deleted = 1 WHERE NetUID = NetId",
            new { NetId = netId }
        );
    }

    public void Update(TransporterType transporterType) {
        _connection.Execute(
            "UPDATE TransporterType SET Name = @Name, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid");
    }
}