using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.CalculationTypeTranslations;
using GBA.Domain.Repositories.CalculationTypes.Contracts;

namespace GBA.Services.Actors.Translations;

public sealed class CalculationTypeTranslationsActor : ReceiveActor {
    private readonly ICalculationTypeRepositoriesFactory _calculationTypeRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public CalculationTypeTranslationsActor(
        IDbConnectionFactory connectionFactory,
        ICalculationTypeRepositoriesFactory calculationTypeRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _calculationTypeRepositoriesFactory = calculationTypeRepositoriesFactory;

        Receive<AddCalculationTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICalculationTypeTranslationRepository calculationTypeTranslationRepository =
                _calculationTypeRepositoriesFactory.NewCalculationTypeTranslationRepository(connection);

            message.CalculationTypeTranslation.CalculationTypeId = message.CalculationTypeTranslation.CalculationType.Id;

            Sender.Tell(calculationTypeTranslationRepository.GetById(calculationTypeTranslationRepository.Add(message.CalculationTypeTranslation)));
        });

        Receive<UpdateCalculationTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICalculationTypeTranslationRepository calculationTypeTranslationRepository =
                _calculationTypeRepositoriesFactory.NewCalculationTypeTranslationRepository(connection);

            calculationTypeTranslationRepository.Update(message.CalculationTypeTranslation);

            Sender.Tell(calculationTypeTranslationRepository.GetByNetId(message.CalculationTypeTranslation.NetUid));
        });

        Receive<GetAllCalculationTypeTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_calculationTypeRepositoriesFactory.NewCalculationTypeTranslationRepository(connection).GetAll());
        });

        Receive<GetCalculationTypeTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_calculationTypeRepositoriesFactory.NewCalculationTypeTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteCalculationTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _calculationTypeRepositoriesFactory.NewCalculationTypeTranslationRepository(connection).Remove(message.NetId);
        });
    }
}