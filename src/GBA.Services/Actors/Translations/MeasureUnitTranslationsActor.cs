using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.MeasureUnitTranslations;
using GBA.Domain.Repositories.Measures.Contracts;

namespace GBA.Services.Actors.Translations;

public sealed class MeasureUnitTranslationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMeasureRepositoriesFactory _measureRepositoriesFactory;

    public MeasureUnitTranslationsActor(
        IDbConnectionFactory connectionFactory,
        IMeasureRepositoriesFactory measureRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _measureRepositoriesFactory = measureRepositoriesFactory;

        Receive<AddMeasureUnitTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IMeasureUnitTranslationRepository measureUnitTranslationRepository = _measureRepositoriesFactory.NewMeasureUnitTranslationRepository(connection);

            message.MeasureUnitTranslation.MeasureUnitId = message.MeasureUnitTranslation.MeasureUnit.Id;

            Sender.Tell(measureUnitTranslationRepository.GetById(measureUnitTranslationRepository.Add(message.MeasureUnitTranslation)));
        });

        Receive<GetAllMeasureUnitTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_measureRepositoriesFactory.NewMeasureUnitTranslationRepository(connection).GetAll());
        });

        Receive<GetMeasureUnitTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_measureRepositoriesFactory.NewMeasureUnitTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<UpdateMeasureUnitTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IMeasureUnitTranslationRepository measureUnitTranslationRepository = _measureRepositoriesFactory.NewMeasureUnitTranslationRepository(connection);

            measureUnitTranslationRepository.Update(message.MeasureUnitTranslation);

            Sender.Tell(measureUnitTranslationRepository.GetByNetId(message.MeasureUnitTranslation.NetUid));
        });

        Receive<DeleteMeasureUnitTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _measureRepositoriesFactory.NewMeasureUnitTranslationRepository(connection).Remove(message.NetId);
        });
    }
}