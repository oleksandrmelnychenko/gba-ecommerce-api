using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.PerfectClientTranslations;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Services.Actors.Translations;

public sealed class PerfectClientTranslationsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public PerfectClientTranslationsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddPerfectClientTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPerfectClientTranslationRepository perfectClientTranslationRepository = _clientRepositoriesFactory.NewPerfectClientTranslationRepository(connection);

            message.PerfectClientTranslation.PerfectClientId = message.PerfectClientTranslation.PerfectClient.Id;

            PerfectClientTranslation translation =
                perfectClientTranslationRepository.GetByClientIdAndCultureCode(message.PerfectClientTranslation.PerfectClientId,
                    message.PerfectClientTranslation.CultureCode);

            if (translation != null) {
                translation.Name = message.PerfectClientTranslation.Name;
                translation.Description = message.PerfectClientTranslation.Description;
                translation.PerfectClientId = message.PerfectClientTranslation.PerfectClientId;

                perfectClientTranslationRepository.Update(translation);

                Sender.Tell(perfectClientTranslationRepository.GetByNetId(translation.NetUid));
            } else {
                long translationId = perfectClientTranslationRepository.Add(message.PerfectClientTranslation);

                Sender.Tell(perfectClientTranslationRepository.GetById(translationId));
            }
        });

        Receive<UpdatePerfectClientTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPerfectClientTranslationRepository perfectClientTranslationRepository = _clientRepositoriesFactory.NewPerfectClientTranslationRepository(connection);

            perfectClientTranslationRepository.Update(message.PerfectClientTranslation);

            Sender.Tell(perfectClientTranslationRepository.GetByNetId(message.PerfectClientTranslation.NetUid));
        });

        Receive<GetAllPerfectClientTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_clientRepositoriesFactory.NewPerfectClientTranslationRepository(connection).GetAll());
        });

        Receive<GetPerfectClientTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_clientRepositoriesFactory.NewPerfectClientTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeletePerfectClientTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _clientRepositoriesFactory.NewPerfectClientTranslationRepository(connection).Remove(message.NetId);
        });
    }
}