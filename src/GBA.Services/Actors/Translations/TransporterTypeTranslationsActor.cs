using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.TransporterTypeTranslations;
using GBA.Domain.Repositories.Transporters.Contracts;

namespace GBA.Services.Actors.Translations;

public sealed class TransporterTypeTranslationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITransporterRepositoriesFactory _transporterRepositoriesFactory;

    public TransporterTypeTranslationsActor(
        IDbConnectionFactory connectionFactory,
        ITransporterRepositoriesFactory transporterRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _transporterRepositoriesFactory = transporterRepositoriesFactory;

        Receive<AddTransporterTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITransporterTypeTranslationRepository transporterTypeTranslationRepository =
                _transporterRepositoriesFactory
                    .NewTransporterTypeTranslationRepository(connection);

            Sender.Tell(transporterTypeTranslationRepository.GetById(transporterTypeTranslationRepository.Add(message.TransporterTypeTranslation)));
        });

        Receive<GetAllTransporterTypeTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_transporterRepositoriesFactory
                .NewTransporterTypeTranslationRepository(connection)
                .GetAll()
            );
        });
    }
}