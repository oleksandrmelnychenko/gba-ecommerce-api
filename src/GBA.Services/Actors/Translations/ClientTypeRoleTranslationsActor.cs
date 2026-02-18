using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.ClientTypeRoleTranslations;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Actors.Translations;

public sealed class ClientTypeRoleTranslationsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public ClientTypeRoleTranslationsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddClientTypeRoleTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientTypeRoleTranslationRepository clientTypeRoleTranslationRepository = _clientRepositoriesFactory.NewClientTypeRoleTranslationRepository(connection);

            message.ClientTypeRoleTranslation.ClientTypeRoleId = message.ClientTypeRoleTranslation.ClientTypeRole.Id;

            Sender.Tell(clientTypeRoleTranslationRepository.GetById(clientTypeRoleTranslationRepository.Add(message.ClientTypeRoleTranslation)));
        });

        Receive<UpdateClientTypeRoleTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientTypeRoleTranslationRepository clientTypeRoleTranslationRepository = _clientRepositoriesFactory.NewClientTypeRoleTranslationRepository(connection);

            clientTypeRoleTranslationRepository.Update(message.ClientTypeRoleTranslation);

            Sender.Tell(clientTypeRoleTranslationRepository.GetByNetId(message.ClientTypeRoleTranslation.NetUid));
        });

        Receive<GetAllClientTypeRoleTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_clientRepositoriesFactory.NewClientTypeRoleTranslationRepository(connection).GetAll());
        });

        Receive<GetClientTypeRoleTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_clientRepositoriesFactory.NewClientTypeRoleTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteClientTypeRoleTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _clientRepositoriesFactory.NewClientTypeRoleTranslationRepository(connection).Remove(message.NetId);
        });
    }
}