using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Actors.Clients;

public sealed class ClientTypeRolesActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public ClientTypeRolesActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddClientTypeRoleMessage>(ProcessAddClientTypeRoleMessage);

        Receive<UpdateClientTypeRoleMessage>(ProcessUpdateClientTypeRoleMessage);

        Receive<GetAllClientTypeRolesMessage>(ProcessGetAllClientTypeRolesMessage);

        Receive<GetClientTypeRoleByNetIdMessage>(ProcessGetClientTypeRoleByNetIdMessage);

        Receive<DeleteClientTypeRoleMessage>(ProcessDeleteClientTypeRoleMessage);
    }

    private void ProcessAddClientTypeRoleMessage(AddClientTypeRoleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientTypeRoleRepository clientTypeRoleRepository = _clientRepositoriesFactory.NewClientTypeRoleRepository(connection);

        message.ClientTypeRole.ClientTypeId = message.ClientTypeRole.ClientType.Id;

        long clientTypeRoleId = clientTypeRoleRepository.Add(message.ClientTypeRole);

        Sender.Tell(clientTypeRoleRepository.GetById(clientTypeRoleId));
    }

    private void ProcessUpdateClientTypeRoleMessage(UpdateClientTypeRoleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientTypeRoleRepository clientTypeRoleRepository = _clientRepositoriesFactory.NewClientTypeRoleRepository(connection);

        clientTypeRoleRepository.Update(message.ClientTypeRole);

        Sender.Tell(clientTypeRoleRepository.GetByNetId(message.ClientTypeRole.NetUid));

        IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
        clientRepository.UpdateOrderExpireDaysByType(message.ClientTypeRole.NetUid, message.ClientTypeRole.OrderExpireDays);
    }

    private void ProcessGetAllClientTypeRolesMessage(GetAllClientTypeRolesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientTypeRoleRepository(connection).GetAll());
    }

    private void ProcessGetClientTypeRoleByNetIdMessage(GetClientTypeRoleByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientTypeRoleRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessDeleteClientTypeRoleMessage(DeleteClientTypeRoleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _clientRepositoriesFactory.NewClientTypeRoleRepository(connection).Remove(message.NetId);
    }
}