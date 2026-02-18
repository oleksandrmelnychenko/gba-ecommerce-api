using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Services.Services.Clients.Contracts;

namespace GBA.Services.Services.Clients;

public sealed class ClientRegistrationTaskService : IClientRegistrationTaskService {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public ClientRegistrationTaskService(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
    }

    public Task Add(Client client) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _clientRepositoriesFactory.NewClientRegistrationTaskRepository(connection).Add(new ClientRegistrationTask {
            ClientId = client.Id
        });

        return Task.CompletedTask;
    }
}
