using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.PerfectClientValueTranslations;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Services.Actors.Translations;

public sealed class PerfectClientValuesTranslationsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public PerfectClientValuesTranslationsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddPerfectClientValueTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPerfectClientValueTranslationRepository perfectClientValueTranslationRepository =
                _clientRepositoriesFactory.NewPerfectClientValueTranslationRepository(connection);

            message.PerfectClientValueTranslation.PerfectClientValueId = message.PerfectClientValueTranslation.PerfectClientValue.Id;

            PerfectClientValueTranslation translation =
                perfectClientValueTranslationRepository.GetByValueIdAndCultureCode(message.PerfectClientValueTranslation.PerfectClientValueId,
                    message.PerfectClientValueTranslation.CultureCode);

            if (translation != null) {
                translation.PerfectClientValueId = message.PerfectClientValueTranslation.PerfectClientValueId;
                translation.Value = message.PerfectClientValueTranslation.Value;

                perfectClientValueTranslationRepository.Update(translation);

                Sender.Tell(perfectClientValueTranslationRepository.GetByNetId(translation.NetUid));
            } else {
                long translationId = perfectClientValueTranslationRepository.Add(message.PerfectClientValueTranslation);

                Sender.Tell(perfectClientValueTranslationRepository.GetById(translationId));
            }
        });

        Receive<UpdatePerfectClientValueTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPerfectClientValueTranslationRepository perfectClientValueTranslationRepository =
                _clientRepositoriesFactory.NewPerfectClientValueTranslationRepository(connection);

            perfectClientValueTranslationRepository.Update(message.PerfectClientValueTranslation);

            Sender.Tell(perfectClientValueTranslationRepository.GetByNetId(message.PerfectClientValueTranslation.NetUid));
        });

        Receive<GetAllPerfectClientValueTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_clientRepositoriesFactory.NewPerfectClientValueTranslationRepository(connection).GetAll());
        });

        Receive<GetPerfectClientValueTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_clientRepositoriesFactory.NewPerfectClientValueTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeletePerfectClientValueTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _clientRepositoriesFactory.NewPerfectClientValueTranslationRepository(connection).Remove(message.NetId);
        });
    }
}