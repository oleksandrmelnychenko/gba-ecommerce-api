using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientWorkplaceRepository : IClientWorkplaceRepository {
    private readonly IDbConnection _connection;

    public ClientWorkplaceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long AddClientWorkplace(ClientWorkplace workplace) {
        return _connection.Query<long>(
                "INSERT INTO ClientWorkplace " +
                "(Updated, MainClientID, WorkplaceID) " +
                "VALUES (GETUTCDATE(), @MainClientId, @WorkplaceId)" +
                "SELECT SCOPE_IDENTITY(); ",
                workplace)
            .FirstOrDefault();
    }

    public void Update(ClientWorkplace workplace) {
        _connection.Execute(
            "UPDATE ClientWorkplace SET " +
            "Updated = GETUTCDATE(), " +
            "Deleted = @Deleted, " +
            "MainClientID = @MainClientId, " +
            "WorkplaceID = @WorkplaceId " +
            "WHERE Workplace.ID = @Id",
            workplace);
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE ClientWorkplace SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id });
    }

    public IEnumerable<Client> GetWorkplacesByMainClientId(long id) {
        return _connection.Query<Client>(
            "SELECT Workplace.* FROM Client AS Workplace " +
            "LEFT JOIN ClientWorkplace " +
            "ON ClientWorkplace.WorkplaceID = Workplace.ID " +
            "AND ClientWorkplace.Deleted = 0 " +
            "LEFT JOIN Client AS MainClient " +
            "ON MainClient.ID = ClientWorkplace.MainClientID " +
            "AND MainClient.Deleted = 0 " +
            "WHERE MainClient.ID = @Id ",
            new { Id = id });
    }

    public IEnumerable<Client> GetWorkplacesByMainClientNetId(Guid netId) {
        return _connection.Query<Client>(
            "SELECT Workplace.* FROM Client AS Workplace " +
            "LEFT JOIN ClientWorkplace " +
            "ON ClientWorkplace.WorkplaceID = Workplace.ID " +
            "AND ClientWorkplace.Deleted = 0 " +
            "LEFT JOIN Client AS MainClient " +
            "ON MainClient.ID = ClientWorkplace.MainClientID " +
            "AND MainClient.Deleted = 0 " +
            "WHERE MainClient.NetUID = @NetId ",
            new { NetId = netId });
    }

    public IEnumerable<Client> GetWorkplacesByClientGroupId(long id) {
        return _connection.Query<Client>(
            "SELECT Client.* FROM ClientGroup " +
            "LEFT JOIN ClientClientGroup " +
            "ON ClientClientGroup.ClientGroupID = ClientGroup.ID " +
            "AND ClientClientGroup.Deleted = 0 " +
            "LEFT JOIN Client " +
            "ON Client.ID = ClientClientGroup.ClientID " +
            "WHERE ClientGroup.ID = @Id " +
            "AND Client.IsWorkplace = 1 " +
            "AND Client.Deleted = 0 ",
            new { Id = id });
    }

    public IEnumerable<Client> GetWorkplacesByClientGroupNetId(Guid netId) {
        return _connection.Query<Client>(
            "SELECT Client.* FROM ClientGroup " +
            "LEFT JOIN ClientClientGroup " +
            "ON ClientClientGroup.ClientGroupID = ClientGroup.ID " +
            "AND ClientClientGroup.Deleted = 0 " +
            "LEFT JOIN Client " +
            "ON Client.ID = ClientClientGroup.ClientID " +
            "WHERE ClientGroup.NetUID = @NetId " +
            "AND Client.IsWorkplace = 1 " +
            "AND Client.Deleted = 0 ",
            new { NetId = netId });
    }
}