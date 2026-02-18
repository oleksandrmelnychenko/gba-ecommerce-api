using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Clients.OrganizationClients;
using GBA.Domain.Repositories.Clients.OrganizationClients.Contracts;

namespace GBA.Services.Actors.Clients.OrganizationClientsGetActors;

public sealed class BaseOrganizationClientsGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOrganizationClientRepositoriesFactory _organizationClientRepositoriesFactory;

    public BaseOrganizationClientsGetActor(
        IDbConnectionFactory connectionFactory,
        IOrganizationClientRepositoriesFactory organizationClientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _organizationClientRepositoriesFactory = organizationClientRepositoriesFactory;

        Receive<GetOrganizationClientByNetIdMessage>(ProcessGetOrganizationClientByNetIdMessage);

        Receive<GetAllOrganizationClientsMessage>(ProcessGetAllOrganizationClientsMessage);

        Receive<GetAllOrganizationClientsFromSearchMessage>(ProcessGetAllOrganizationClientsFromSearchMessage);
    }

    private void ProcessGetOrganizationClientByNetIdMessage(GetOrganizationClientByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _organizationClientRepositoriesFactory
                .NewOrganizationClientRepository(connection)
                .GetByNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetAllOrganizationClientsMessage(GetAllOrganizationClientsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _organizationClientRepositoriesFactory
                .NewOrganizationClientRepository(connection)
                .GetAll()
        );
    }

    private void ProcessGetAllOrganizationClientsFromSearchMessage(GetAllOrganizationClientsFromSearchMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _organizationClientRepositoriesFactory
                .NewOrganizationClientRepository(connection)
                .GetAllFromSearch(
                    message.Value
                )
        );
    }
}