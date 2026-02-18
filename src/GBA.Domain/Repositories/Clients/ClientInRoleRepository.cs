using System;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientInRoleRepository : IClientInRoleRepository {
    private readonly IDbConnection _connection;

    public ClientInRoleRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ClientInRole clientInRole) {
        _connection.Execute(
            "INSERT INTO ClientInRole (ClientId, ClientTypeId, ClientTypeRoleId, Updated) " +
            "VALUES (@ClientId, @ClientTypeId, @ClientTypeRoleId, getutcdate())",
            clientInRole
        );
    }

    public void Update(ClientInRole clientInRole) {
        _connection.Execute(
            "UPDATE ClientInRole SET " +
            "ClientId = @ClientId, ClientTypeId = @ClientTypeId, ClientTypeRoleId = @ClientTypeRoleId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            clientInRole
        );
    }

    public void Remove(Guid netid) {
        _connection.Execute(
            "UPDATE ClientInRole SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netid.ToString() }
        );
    }
}