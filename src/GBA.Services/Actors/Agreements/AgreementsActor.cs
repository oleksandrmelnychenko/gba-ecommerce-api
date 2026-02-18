using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Agreements;
using GBA.Domain.Repositories.Agreements.Contracts;

namespace GBA.Services.Actors.Agreements;

public sealed class AgreementsActor : ReceiveActor {
    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public AgreementsActor(
        IDbConnectionFactory connectionFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;

        Receive<AddAgreementMessage>(ProcessAddAgreementMessage);

        Receive<UpdateAgreementMessage>(ProcessUpdateAgreementMessage);

        Receive<DeleteAgreementMessage>(ProcessDeleteAgreementMessage);
    }

    private void ProcessAddAgreementMessage(AddAgreementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);

        message.Agreement.CurrencyId = message.Agreement.Currency?.Id;

        long agreementId = agreementRepository.Add(message.Agreement);

        Sender.Tell(agreementRepository.GetById(agreementId));
    }

    private void ProcessUpdateAgreementMessage(UpdateAgreementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);

        agreementRepository.Update(message.Agreement);

        Sender.Tell(agreementRepository.GetByNetId(message.Agreement.NetUid));
    }

    private void ProcessDeleteAgreementMessage(DeleteAgreementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _agreementRepositoriesFactory.NewAgreementRepository(connection).Remove(message.NetId);
    }
}