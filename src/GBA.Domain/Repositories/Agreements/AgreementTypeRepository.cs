using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Agreements;

public sealed class AgreementTypeRepository : IAgreementTypeRepository {
    private readonly IDbConnection _connection;

    public AgreementTypeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(AgreementType agreementType) {
        return _connection.Query<long>(
                "INSERT INTO AgreementType (Name, Updated) VALUES (@Name, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                agreementType
            )
            .Single();
    }

    public void Update(AgreementType agreementType) {
        _connection.Execute(
            "UPDATE AgreementType SET " +
            "Name = @Name, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            agreementType
        );
    }

    public AgreementType GetById(long id) {
        return _connection.Query<AgreementType>(
                "SELECT * FROM AgreementType " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public AgreementType GetByNetId(Guid netId) {
        return _connection.Query<AgreementType, AgreementTypeTranslation, AgreementType>(
                "SELECT * FROM AgreementType " +
                "LEFT OUTER JOIN AgreementTypeTranslation " +
                "ON AgreementType.ID = AgreementTypeTranslation.AgreementTypeID " +
                "AND AgreementTypeTranslation.CultureCode = @Culture " +
                "AND AgreementTypeTranslation.Deleted = 0 " +
                "WHERE AgreementType.NetUID = @NetId",
                (type, translation) => {
                    type.Name = translation?.Name;

                    return type;
                },
                new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public List<AgreementType> GetAll() {
        return _connection.Query<AgreementType, AgreementTypeTranslation, AgreementType>(
                "SELECT * FROM AgreementType " +
                "LEFT OUTER JOIN AgreementTypeTranslation " +
                "ON AgreementType.ID = AgreementTypeTranslation.AgreementTypeID " +
                "AND AgreementTypeTranslation.CultureCode = @Culture " +
                "AND AgreementTypeTranslation.Deleted = 0 " +
                "WHERE AgreementType.Deleted = 0",
                (type, translation) => {
                    type.Name = translation?.Name;

                    return type;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATED AgreementType SET Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}