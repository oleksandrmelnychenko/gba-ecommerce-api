using System.Data;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientRegistrationTaskRepository : IClientRegistrationTaskRepository {
    private readonly IDbConnection _connection;

    public ClientRegistrationTaskRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ClientRegistrationTask clientRegistrationTask) {
        _connection.Execute(
            "INSERT INTO [ClientRegistrationTask] (ClientID, IsDone, Updated) " +
            "VALUES(@ClientID, 0, getutcdate())",
            clientRegistrationTask
        );
    }

    public void SetDoneByClientId(long id) {
        _connection.Execute(
            "UPDATE [ClientRegistrationTask] " +
            "SET IsDone = 1 " +
            "WHERE [ClientRegistrationTask].ClientID = @Id",
            new { Id = id }
        );
    }
}