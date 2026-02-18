using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Actors.Clients.PerfectClientsGetActors;

public sealed class BasePerfectClientsGetActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public BasePerfectClientsGetActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<GetAllPerfectClientsByTypeMessage>(ProcessGetAllPerfectClientsByTypeMessage);

        Receive<GetAllPerfectClientsMessage>(ProcessGetAllPerfectClientsMessage);

        Receive<GetAllPerfectClientsByRoleMessage>(ProcessGetAllPerfectClientsByRoleMessage);

        Receive<GetPerfectClientByNetIdMessage>(ProcessGetPerfectClientByNetIdMessage);
    }

    private void ProcessGetAllPerfectClientsByTypeMessage(GetAllPerfectClientsByTypeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewPerfectClientRepository(connection).GetAllByType(message.PerfectClientType));
    }

    private void ProcessGetAllPerfectClientsMessage(GetAllPerfectClientsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewPerfectClientRepository(connection).GetAll());
    }

    private void ProcessGetAllPerfectClientsByRoleMessage(GetAllPerfectClientsByRoleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewPerfectClientRepository(connection).GetAll(message.ClientTypeRoleId));
    }

    private void ProcessGetPerfectClientByNetIdMessage(GetPerfectClientByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewPerfectClientRepository(connection).GetByNetId(message.NetId));
    }
}