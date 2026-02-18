using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientGroupRepository : IClientGroupRepository {
    private readonly IDbConnection _connection;

    public ClientGroupRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ClientGroup clientGroup) {
        return _connection.Query<long>(
                "INSERT INTO [ClientGroup] ([Name], [ClientID], [Updated]) VALUES (@Name, @ClientId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY() ",
                clientGroup)
            .First();
    }

    public void Update(ClientGroup clientGroup) {
        _connection.Execute(
            "UPDATE [ClientGroup] SET " +
            "[Name] = @Name, " +
            "[ClientID] = @ClientId, " +
            "[Updated] = GETUTCDATE(), " +
            "[Deleted] = @Deleted " +
            "WHERE ID = @Id",
            clientGroup);
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [ClientGroup] SET " +
            "[Deleted] = 1 " +
            "WHERE ID = @Id",
            new { Id = id });
    }

    public IEnumerable<ClientGroup> GetAll() {
        return _connection.Query<ClientGroup>(
            "SELECT * FROM ClientGroup " +
            "WHERE Deleted = 0");
    }

    public IEnumerable<ClientGroup> GetAllByClientNetId(Guid netId) {
        return _connection.Query<ClientGroup>(
            "SELECT ClientGroup.* FROM ClientGroup " +
            "LEFT JOIN Client " +
            "ON Client.ID = ClientGroup.ClientID " +
            "WHERE Client.NetUID = @NetId " +
            "AND ClientGroup.Deleted = 0 ",
            new { NetId = netId });
    }

    public IEnumerable<ClientGroup> GetAllByClientId(long id) {
        return _connection.Query<ClientGroup>(
            "SELECT * FROM ClientGroup " +
            "WHERE ClientID = @Id " +
            "AND ClientGroup.Deleted = 0 ",
            new { Id = id });
    }
}