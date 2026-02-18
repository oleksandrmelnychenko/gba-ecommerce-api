using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientTypeTranslationRepository : IClientTypeTranslationRepository {
    private readonly IDbConnection _connection;

    public ClientTypeTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ClientTypeTranslation clientTypeTranslation) {
        return _connection.Query<long>(
                "INSERT INTO ClientTypeTranslation (Name, ClientTypeId, CultureCode, Updated) " +
                "VALUES (@Name, @ClientTypeId, @CultureCode, getutcdate()); " +
                "SELECT SCOPE_IDENTITY() ",
                clientTypeTranslation
            )
            .Single();
    }

    public void Update(ClientTypeTranslation clientTypeTranslation) {
        _connection.Execute(
            "UPDATE ClientTypeTranslation SET Name = @Name, ClientTypeId = @ClientTypeId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid ",
            clientTypeTranslation
        );
    }

    public ClientTypeTranslation GetById(long id) {
        return _connection.Query<ClientTypeTranslation>(
                "SELECT * FROM ClientTypeTranslation " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ClientTypeTranslation GetByNetId(Guid netId) {
        return _connection.Query<ClientTypeTranslation>(
                "SELECT * FROM ClientTypeTranslation " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<ClientTypeTranslation> GetAll() {
        return _connection.Query<ClientTypeTranslation>(
                "SELECT * FROM ClientTypeTranslation " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ClientTypeTranslation SET Deleted = 1 " +
            "WHERE NetUID = @NetId ",
            new { NetId = netId.ToString() }
        );
    }
}