using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class PerfectClientValueTranslationRepository : IPerfectClientValueTranslationRepository {
    private readonly IDbConnection _connection;

    public PerfectClientValueTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PerfectClientValueTranslation perfectClientValueTranslation) {
        return _connection.Query<long>(
                "INSERT INTO PerfectClientValueTranslation (Value, PerfectClientValueId, CultureCode, Updated) " +
                "VALUES (@Value, @PerfectClientValueId, @CultureCode, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                perfectClientValueTranslation
            )
            .Single();
    }

    public void Add(IEnumerable<PerfectClientValueTranslation> perfectClientValueTranslations) {
        _connection.Execute(
            "INSERT INTO PerfectClientValueTranslation (Value, PerfectClientValueId, CultureCode, Updated) " +
            "VALUES (@Value, @PerfectClientValueId, @CultureCode, getutcdate())",
            perfectClientValueTranslations
        );
    }

    public void Update(PerfectClientValueTranslation perfectClientValueTranslation) {
        _connection.Execute(
            "UPDATE PerfectClientValueTranslation SET " +
            "Value = @Value, PerfectClientValueId = @PerfectClientValueId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            perfectClientValueTranslation
        );
    }

    public void Update(IEnumerable<PerfectClientValueTranslation> perfectClientValueTranslations) {
        _connection.Execute(
            "UPDATE PerfectClientValueTranslation SET " +
            "Value = @Value, PerfectClientValueId = @PerfectClientValueId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            perfectClientValueTranslations
        );
    }

    public PerfectClientValueTranslation GetById(long id) {
        return _connection.Query<PerfectClientValueTranslation>(
                "SELECT * FROM PerfectClientValueTranslation " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public PerfectClientValueTranslation GetByValueIdAndCultureCode(long id, string code) {
        return _connection.Query<PerfectClientValueTranslation>(
                "SELECT * FROM PerfectClientValueTranslation " +
                "WHERE PerfectClientValueID = @Id AND CultureCode = @Code",
                new { Id = id, Code = code }
            )
            .SingleOrDefault();
    }

    public PerfectClientValueTranslation GetByNetId(Guid netId) {
        return _connection.Query<PerfectClientValueTranslation>(
                "SELECT * FROM PerfectClientValueTranslation " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<PerfectClientValueTranslation> GetAll() {
        return _connection.Query<PerfectClientValueTranslation>(
                "SELECT * FROM PerfectClientValueTranslation " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE PerfectClientValueTranslation SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}