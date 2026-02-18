using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Clients.Incoterms;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Actors.Clients;

public sealed class IncotermsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public IncotermsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddNewIncotermMessage>(ProcessAddNewIncotermMessage);

        Receive<UpdateIncotermMessage>(ProcessUpdateIncotermMessage);

        Receive<GetIncotermByNetIdMessage>(ProcessGetIncotermByNetIdMessage);

        Receive<GetAllIncotermsMessage>(ProcessGetAllIncotermsMessage);

        Receive<DeleteIncotermByNetIdMessage>(ProcessDeleteIncotermByNetIdMessage);
    }

    private void ProcessAddNewIncotermMessage(AddNewIncotermMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IIncotermRepository incotermRepository = _clientRepositoriesFactory.NewIncotermRepository(connection);

        message.Incoterm.Id = incotermRepository.Add(message.Incoterm);

        Sender.Tell(
            incotermRepository
                .GetById(
                    message.Incoterm.Id
                )
        );
    }

    private void ProcessUpdateIncotermMessage(UpdateIncotermMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IIncotermRepository incotermRepository = _clientRepositoriesFactory.NewIncotermRepository(connection);

        incotermRepository.Update(message.Incoterm);

        Sender.Tell(
            incotermRepository
                .GetById(
                    message.Incoterm.Id
                )
        );
    }

    private void ProcessGetIncotermByNetIdMessage(GetIncotermByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _clientRepositoriesFactory
                .NewIncotermRepository(connection)
                .GetByNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetAllIncotermsMessage(GetAllIncotermsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _clientRepositoriesFactory
                .NewIncotermRepository(connection)
                .GetAll()
        );
    }

    private void ProcessDeleteIncotermByNetIdMessage(DeleteIncotermByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _clientRepositoriesFactory
            .NewIncotermRepository(connection)
            .Remove(message.NetId);
    }
}