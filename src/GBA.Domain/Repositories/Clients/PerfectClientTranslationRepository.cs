using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class PerfectClientTranslationRepository : IPerfectClientTranslationRepository {
    private readonly IDbConnection _connection;

    public PerfectClientTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PerfectClientTranslation perfectClientTranslation) {
        return _connection.Query<long>(
                "INSERT INTO PerfectClientTranslation (Name, Description, PerfectClientId, CultureCode, Updated) " +
                "VALUES (@Name, @Description, @PerfectClientId, @CultureCode, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                perfectClientTranslation
            )
            .Single();
    }

    public void Add(IEnumerable<PerfectClientTranslation> perfectClientTranslations) {
        _connection.Execute(
            "INSERT INTO PerfectClientTranslation (Name, Description, PerfectClientId, CultureCode, Updated) " +
            "VALUES (@Name, @Description, @PerfectClientId, @CultureCode, getutcdate())",
            perfectClientTranslations
        );
    }

    public void Update(PerfectClientTranslation perfectClientTranslation) {
        _connection.Execute(
            "UPDATE PerfectClientTranslation SET " +
            "Name = @Name, Description = @Description, PerfectClientId = @PerfectClientId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            perfectClientTranslation
        );
    }

    public void Update(IEnumerable<PerfectClientTranslation> perfectClientTranslations) {
        _connection.Execute(
            "UPDATE PerfectClientTranslation SET " +
            "Name = @Name, Description = @Description, PerfectClientId = @PerfectClientId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            perfectClientTranslations
        );
    }

    public PerfectClientTranslation GetById(long id) {
        return _connection.Query<PerfectClientTranslation>(
                "SELECT * FROM PerfectClientTranslation " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public PerfectClientTranslation GetByNetId(Guid netId) {
        return _connection.Query<PerfectClientTranslation>(
                "SELECT * FROM PerfectClientTranslation " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }


    public PerfectClientTranslation GetByClientIdAndCultureCode(long id, string code) {
        return _connection.Query<PerfectClientTranslation>(
                "SELECT * FROM PerfectClientTranslation " +
                "WHERE PerfectClientID = @Id AND CultureCode = @Code",
                new { Id = id, Code = code }
            )
            .SingleOrDefault();
    }

    public List<PerfectClientTranslation> GetAll() {
        return _connection.Query<PerfectClientTranslation>(
                "SELECT * FROM PerfectClientTranslation " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE PerfectClientTranslation SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}