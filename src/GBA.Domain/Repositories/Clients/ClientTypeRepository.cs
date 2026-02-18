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

public sealed class ClientTypeRepository : IClientTypeRepository {
    private readonly IDbConnection _connection;

    public ClientTypeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ClientType clientType) {
        return _connection.Query<long>(
            "INSERT INTO ClientType (Name, ClientTypeIcon, AllowMultiple, Type, Updated) " +
            "VALUES (@Name, @ClientTypeIcon, @AllowMultiple, @Type, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            clientType
        ).Single();
    }

    public void Update(ClientType clientType) {
        _connection.Execute(
            "UPDATE ClientType SET " +
            "Name = @Name, ClientTypeIcon = @ClientTypeIcon, AllowMultiple = @AllowMultiple, Type = @Type, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            clientType
        );
    }

    public ClientType GetById(long id) {
        return _connection.Query<ClientType>(
            "SELECT * FROM ClientType " +
            "WHERE ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public ClientType GetByNetId(Guid netId) {
        List<ClientType> types = new();

        _connection.Query<ClientType, ClientTypeTranslation, ClientTypeRole, ClientTypeRoleTranslation, ClientType>(
            "SELECT * " +
            "FROM [ClientType] " +
            "LEFT JOIN [ClientTypeTranslation] " +
            "ON [ClientType].ID = [ClientTypeTranslation].ClientTypeID " +
            "AND [ClientTypeTranslation].CultureCode = @Culture " +
            "AND [ClientTypeTranslation].Deleted = 0" +
            "LEFT JOIN [ClientTypeRole] " +
            "ON [ClientType].ID = [ClientTypeRole].ClientTypeID " +
            "AND [ClientTypeRole].Deleted = 0 " +
            "LEFT JOIN [ClientTypeRoleTranslation] " +
            "ON [ClientTypeRole].ID = [ClientTypeRoleTranslation].ClientTypeRoleID " +
            "AND [ClientTypeRoleTranslation].CultureCode = @Culture " +
            "AND [ClientTypeRoleTranslation].Deleted = 0 " +
            "WHERE [ClientType].NetUID = @NetId",
            (type, typeTranslation, role, roleTranslation) => {
                if (types.Any(t => t.Id.Equals(type.Id))) {
                    type = types.First(t => t.Id.Equals(type.Id));
                } else {
                    type.Name = typeTranslation?.Name ?? type.Name;

                    types.Add(type);
                }

                if (role == null) return type;

                role.Name = roleTranslation?.Name ?? role.Name;
                role.Description = roleTranslation?.Description ?? role.Description;

                type.ClientTypeRoles.Add(role);

                return type;
            },
            new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return types.SingleOrDefault();
    }

    public List<ClientType> GetAll(bool withReSale = false) {
        List<ClientType> types = new();

        _connection.Query<ClientType, ClientTypeTranslation, ClientTypeRole, ClientTypeRoleTranslation, ClientType>(
            "SELECT * " +
            "FROM [ClientType] " +
            "LEFT JOIN [ClientTypeTranslation] " +
            "ON [ClientType].ID = [ClientTypeTranslation].ClientTypeID " +
            "AND [ClientTypeTranslation].CultureCode = @Culture " +
            "AND [ClientTypeTranslation].Deleted = 0" +
            "LEFT JOIN [ClientTypeRole] " +
            "ON [ClientType].ID = [ClientTypeRole].ClientTypeID " +
            "AND [ClientTypeRole].Deleted = 0 " +
            "LEFT JOIN [ClientTypeRoleTranslation] " +
            "ON [ClientTypeRole].ID = [ClientTypeRoleTranslation].ClientTypeRoleID " +
            "AND [ClientTypeRoleTranslation].CultureCode = @Culture " +
            "AND [ClientTypeRoleTranslation].Deleted = 0 " +
            "WHERE [ClientType].Deleted = 0 " +
            (
                withReSale
                    ? string.Empty
                    : "AND [ClientType].[Type] <> 2"
            ),
            (type, typeTranslation, role, roleTranslation) => {
                if (types.Any(t => t.Id.Equals(type.Id))) {
                    type = types.First(t => t.Id.Equals(type.Id));
                } else {
                    type.Name = typeTranslation?.Name ?? type.Name;

                    types.Add(type);
                }

                if (role == null) return type;

                role.Name = roleTranslation?.Name ?? role.Name;
                role.Description = roleTranslation?.Description ?? role.Description;

                type.ClientTypeRoles.Add(role);

                return type;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return types;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ClientType SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}