using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Agreements;
using GBA.Domain.Repositories.CalculationTypes.Contracts;

namespace GBA.Services.Actors.Agreements;

public sealed class CalculationTypesActor : ReceiveActor {
    private readonly ICalculationTypeRepositoriesFactory _calculationTypeRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public CalculationTypesActor(
        IDbConnectionFactory connectionFactory,
        ICalculationTypeRepositoriesFactory calculationTypeRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _calculationTypeRepositoriesFactory = calculationTypeRepositoriesFactory;

        Receive<AddCalculationTypeMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICalculationTypeRepository calculationTypeRepository = _calculationTypeRepositoriesFactory.NewCalculationTypeRepository(connection);

            Sender.Tell(calculationTypeRepository.GetById(calculationTypeRepository.Add(message.CalculationType)));
        });

        Receive<UpdateCalculationTypeMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICalculationTypeRepository calculationTypeRepository = _calculationTypeRepositoriesFactory.NewCalculationTypeRepository(connection);

            calculationTypeRepository.Update(message.CalculationType);

            Sender.Tell(calculationTypeRepository.GetByNetId(message.CalculationType.NetUid));
        });

        Receive<GetAllCalculationTypesMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_calculationTypeRepositoriesFactory.NewCalculationTypeRepository(connection).GetAll());
        });

        Receive<GetCalculationTypeByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_calculationTypeRepositoriesFactory.NewCalculationTypeRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteCalculationTypeMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _calculationTypeRepositoriesFactory.NewCalculationTypeRepository(connection).Remove(message.NetId);
        });
    }
}