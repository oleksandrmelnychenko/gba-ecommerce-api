using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.AgreementTypeTranslations;
using GBA.Domain.Repositories.Agreements.Contracts;

namespace GBA.Services.Actors.Translations;

public sealed class AgreementTypeTranslationsActor : ReceiveActor {
    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public AgreementTypeTranslationsActor(
        IDbConnectionFactory connectionFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;

        Receive<AddAgreementTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IAgreementTypeTranslationRepository agreementTypeTranslationRepository = _agreementRepositoriesFactory.NewAgreementTypeTranslationRepository(connection);

            message.AgreementTypeTranslation.AgreementTypeId = message.AgreementTypeTranslation.AgreementType.Id;

            Sender.Tell(agreementTypeTranslationRepository.GetById(agreementTypeTranslationRepository.Add(message.AgreementTypeTranslation)));
        });

        Receive<UpdateAgreementTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IAgreementTypeTranslationRepository agreementTypeTranslationRepository = _agreementRepositoriesFactory.NewAgreementTypeTranslationRepository(connection);

            agreementTypeTranslationRepository.Update(message.AgreementTypeTranslation);

            Sender.Tell(agreementTypeTranslationRepository.GetByNetId(message.AgreementTypeTranslation.NetUid));
        });

        Receive<GetAllAgreementTypeTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_agreementRepositoriesFactory.NewAgreementTypeTranslationRepository(connection).GetAll());
        });

        Receive<GetAgreementTypeTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_agreementRepositoriesFactory.NewAgreementTypeTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteAgreementTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _agreementRepositoriesFactory.NewAgreementTypeTranslationRepository(connection).Remove(message.NetId);
        });
    }
}