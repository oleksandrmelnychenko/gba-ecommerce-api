using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Agreements;

public sealed class AgreementTypeTranslationRepository : IAgreementTypeTranslationRepository {
    private readonly IDbConnection _connection;

    public AgreementTypeTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(AgreementTypeTranslation agreementTypeTranslation) {
        return _connection.Query<long>(
                "INSERT INTO AgreementTypeTranslation (Name, CultureCode, Updated) VALUES (@Name, @CultureCode, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                agreementTypeTranslation
            )
            .Single();
    }

    public void Update(AgreementTypeTranslation agreementTypeTranslation) {
        _connection.Execute(
            "UPDATE AgreementTypeTranslation SET " +
            "Name = @Name, CultureCode = @CultureCode, Updated = getutcdate " +
            "WHERE NetUID = @NetUid",
            agreementTypeTranslation
        );
    }

    public AgreementTypeTranslation GetById(long id) {
        return _connection.Query<AgreementTypeTranslation>(
                "SELECT * FROM AgreementTypeTranslation " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public AgreementTypeTranslation GetByNetId(Guid netId) {
        return _connection.Query<AgreementTypeTranslation>(
                "SELECT * FROM AgreementTypeTranslation " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<AgreementTypeTranslation> GetAll() {
        return _connection.Query<AgreementTypeTranslation>(
                "SELECT * FROM AgreementTypeTranslation " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE AgreementTypeTranslation SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}