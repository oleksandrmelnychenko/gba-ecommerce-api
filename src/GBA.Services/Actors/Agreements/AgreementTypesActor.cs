using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Agreements;
using GBA.Domain.Repositories.Agreements.Contracts;

namespace GBA.Services.Actors.Agreements;

public sealed class AgreementTypesActor : ReceiveActor {
    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public AgreementTypesActor(
        IDbConnectionFactory connectionFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;

        Receive<AddAgreementTypeMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IAgreementTypeRepository agreementTypeRepository = _agreementRepositoriesFactory.NewAgreementTypeRepository(connection);

            Sender.Tell(agreementTypeRepository.GetById(agreementTypeRepository.Add(message.AgreementType)));
        });

        Receive<UpdateAgreementTypeMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IAgreementTypeRepository agreementTypeRepository = _agreementRepositoriesFactory.NewAgreementTypeRepository(connection);

            agreementTypeRepository.Update(message.AgreementType);

            Sender.Tell(agreementTypeRepository.GetByNetId(message.AgreementType.NetUid));
        });

        Receive<GetAllAgreementTypesMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_agreementRepositoriesFactory.NewAgreementTypeRepository(connection).GetAll());
        });

        Receive<GetAgreementTypeByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_agreementRepositoriesFactory.NewAgreementTypeRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteAgreementTypeMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _agreementRepositoriesFactory.NewAgreementTypeRepository(connection).Remove(message.NetId);
        });
    }
}