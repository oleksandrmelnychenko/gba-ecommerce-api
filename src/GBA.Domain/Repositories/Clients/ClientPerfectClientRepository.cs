using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientPerfectClientRepository : IClientPerfectClientRepository {
    private readonly IDbConnection _connection;

    public ClientPerfectClientRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<ClientPerfectClient> clients) {
        _connection.Execute(
            "INSERT INTO ClientPerfectClient (PerfectClientId, ClientId, PerfectClientValueId, Value, IsChecked, Updated) " +
            "VALUES (@PerfectClientId, @ClientId, @PerfectClientValueId, @Value, @IsChecked, getutcdate()); " +
            "SELECT SCOPE_IDENTITY() ",
            clients
        );
    }

    public void Update(IEnumerable<ClientPerfectClient> clients) {
        _connection.Execute(
            "UPDATE ClientPerfectClient SET " +
            "PerfectClientId = @PerfectClientId, ClientId = @ClientId, PerfectClientValueId = @PerfectClientValueId, Value = @Value, IsChecked = @IsChecked, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            clients
        );
    }

    public IEnumerable<ClientPerfectClient> GetAllByClientId(long id) {
        return _connection.Query<ClientPerfectClient>(
            "SELECT * FROM ClientPerfectClient " +
            "WHERE ClientId = @Id AND Deleted = 0",
            new { Id = id }
        );
    }

    public void Remove(IEnumerable<ClientPerfectClient> clients) {
        _connection.Execute(
            "UPDATE ClientPerfectClient SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            clients
        );
    }
}