using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Actors.Clients;

public sealed class ClientTypesActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public ClientTypesActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddClientTypeMessage>(ProcessAddClientTypeMessage);

        Receive<UpdateClientTypeMessage>(ProcessUpdateClientTypeMessage);

        Receive<GetAllClientTypesMessage>(ProcessGetAllClientTypesMessage);

        Receive<GetClientTypeByNetIdMessage>(ProcessGetClientTypeByNetIdMessage);

        Receive<DeleteClientTypeMessage>(ProcessDeleteClientTypeMessage);
    }

    private void ProcessAddClientTypeMessage(AddClientTypeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientTypeRepository clientTypeRepository = _clientRepositoriesFactory.NewClientTypeRepository(connection);

        long clientTypeId = clientTypeRepository.Add(message.ClientType);

        Sender.Tell(clientTypeRepository.GetById(clientTypeId));
    }

    private void ProcessUpdateClientTypeMessage(UpdateClientTypeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientTypeRepository clientTypeRepository = _clientRepositoriesFactory.NewClientTypeRepository(connection);

        clientTypeRepository.Update(message.ClientType);

        Sender.Tell(clientTypeRepository.GetByNetId(message.ClientType.NetUid));
    }

    private void ProcessGetAllClientTypesMessage(GetAllClientTypesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientTypeRepository(connection).GetAll());
    }

    private void ProcessGetClientTypeByNetIdMessage(GetClientTypeByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientTypeRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessDeleteClientTypeMessage(DeleteClientTypeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _clientRepositoriesFactory.NewClientTypeRepository(connection).Remove(message.NetId);
    }
}