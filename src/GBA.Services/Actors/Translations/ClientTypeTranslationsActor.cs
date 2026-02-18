using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.ClientTypeTranslations;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Actors.Translations;

public sealed class ClientTypeTranslationsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public ClientTypeTranslationsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddClientTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientTypeTranslationRepository clientTypeTranslationRepository = _clientRepositoriesFactory.NewClientTypeTranslationRepository(connection);

            message.ClientTypeTranslation.ClientTypeId = message.ClientTypeTranslation.ClientType.Id;

            Sender.Tell(clientTypeTranslationRepository.GetById(clientTypeTranslationRepository.Add(message.ClientTypeTranslation)));
        });

        Receive<UpdateClientTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientTypeTranslationRepository clientTypeTranslationRepository = _clientRepositoriesFactory.NewClientTypeTranslationRepository(connection);

            clientTypeTranslationRepository.Update(message.ClientTypeTranslation);

            Sender.Tell(clientTypeTranslationRepository.GetByNetId(message.ClientTypeTranslation.NetUid));
        });

        Receive<GetAllClientTypeTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_clientRepositoriesFactory.NewClientTypeTranslationRepository(connection).GetAll());
        });

        Receive<GetClientTypeTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_clientRepositoriesFactory.NewClientTypeTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteClientTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _clientRepositoriesFactory.NewClientTypeTranslationRepository(connection).Remove(message.NetId);
        });
    }
}