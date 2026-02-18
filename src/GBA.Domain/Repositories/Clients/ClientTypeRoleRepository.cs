using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientTypeRoleRepository : IClientTypeRoleRepository {
    private readonly IDbConnection _connection;

    public ClientTypeRoleRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ClientTypeRole clientTypeRole) {
        return _connection.Query<long>(
                "INSERT INTO ClientTypeRole (Name, Description, ClientTypeId, Updated) " +
                "VALUES (@Name, @Description, @ClientTypeId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                clientTypeRole
            )
            .Single();
    }

    public void Update(ClientTypeRole clientTypeRole) {
        _connection.Execute(
            "UPDATE ClientTypeRole SET " +
            "Name = @Name, Description = @Description, ClientTypeId = @ClientTypeId, OrderExpireDays = @OrderExpireDays, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            clientTypeRole
        );
    }

    public ClientTypeRole GetById(long id) {
        return _connection.Query<ClientTypeRole, ClientTypeRoleTranslation, ClientTypeRole>(
                "SELECT * FROM ClientTypeRole " +
                "INNER JOIN ClientTypeRoleTranslation " +
                "ON ClientTypeRole.ID = ClientTypeRoleTranslation.ClientTypeRoleID " +
                "AND ClientTypeRoleTranslation.CultureCode = @Culture " +
                "AND ClientTypeRoleTranslation.Deleted = 0 " +
                "WHERE ClientTypeRole.ID = @Id",
                (role, translation) => {
                    role.Name = translation?.Name;
                    role.Description = translation?.Description;

                    return role;
                },
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public ClientTypeRole GetByNetId(Guid netId) {
        return _connection.Query<ClientTypeRole, ClientTypeRoleTranslation, ClientTypeRole>(
                "SELECT * FROM ClientTypeRole " +
                "INNER JOIN ClientTypeRoleTranslation " +
                "ON ClientTypeRole.ID = ClientTypeRoleTranslation.ClientTypeRoleID " +
                "AND ClientTypeRoleTranslation.CultureCode = @Culture " +
                "AND ClientTypeRoleTranslation.Deleted = 0 " +
                "WHERE ClientTypeRole.NetUID = @NetId",
                (role, translation) => {
                    role.Name = translation?.Name;
                    role.Description = translation?.Description;

                    return role;
                },
                new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public List<ClientTypeRole> GetAll() {
        return _connection.Query<ClientTypeRole, ClientTypeRoleTranslation, ClientTypeRole>(
                "SELECT * FROM ClientTypeRole " +
                "INNER JOIN ClientTypeRoleTranslation " +
                "ON ClientTypeRole.ID = ClientTypeRoleTranslation.ClientTypeRoleID " +
                "AND ClientTypeRoleTranslation.CultureCode = @Culture " +
                "AND ClientTypeRoleTranslation.Deleted = 0 " +
                "WHERE ClientTypeRole.Deleted = 0",
                (role, translation) => {
                    role.Name = translation?.Name;
                    role.Description = translation?.Description;

                    return role;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ClientTypeRole SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}