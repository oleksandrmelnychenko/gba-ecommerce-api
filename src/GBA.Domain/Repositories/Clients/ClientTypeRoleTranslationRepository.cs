using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientTypeRoleTranslationRepository : IClientTypeRoleTranslationRepository {
    private readonly IDbConnection _connection;

    public ClientTypeRoleTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ClientTypeRoleTranslation clientTypeRoleTranslation) {
        return _connection.Query<long>(
                "INSERT INTO ClientTypeRoleTranslation (Name, Description, CultureCode, ClientTypeRoleId, Updated) " +
                "VALUES (@Name, @Description, @CultureCode, @ClientTypeRoleId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                clientTypeRoleTranslation
            )
            .Single();
    }

    public void Update(ClientTypeRoleTranslation clientTypeRoleTranslation) {
        _connection.Execute(
            "UPDATE ClientTypeRoleTranslation " +
            "SET Name = @Name, Description = @Description, CultureCode = @CultureCode, ClientTypeRoleId = @ClientTypeRoleId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            clientTypeRoleTranslation
        );
    }

    public ClientTypeRoleTranslation GetById(long id) {
        return _connection.Query<ClientTypeRoleTranslation>(
                "SELECT * FROM ClientTypeRoleTranslation " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ClientTypeRoleTranslation GetByNetId(Guid netId) {
        return _connection.Query<ClientTypeRoleTranslation>(
                "SELECT * FROM ClientTypeRoleTranslation " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<ClientTypeRoleTranslation> GetAll() {
        return _connection.Query<ClientTypeRoleTranslation>(
                "SELECT * FROM ClientTypeRoleTranslation " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ClientTypeRoleTranslation SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}