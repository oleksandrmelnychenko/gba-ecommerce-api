using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Transporters;
using GBA.Domain.Repositories.Transporters.Contracts;

namespace GBA.Services.Actors.Transporters;

public sealed class TransporterTypesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITransporterRepositoriesFactory _transporterRepositoriesFactory;

    public TransporterTypesActor(
        IDbConnectionFactory connectionFactory,
        ITransporterRepositoriesFactory transporterRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _transporterRepositoriesFactory = transporterRepositoriesFactory;

        Receive<GetAllTransporterTypesMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_transporterRepositoriesFactory.NewTransporterTypeRepository(connection).GetAll());
        });

        Receive<GetTransporterByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_transporterRepositoriesFactory.NewTransporterTypeRepository(connection).GetByNetId(message.NetId));
        });

        Receive<AddTransporterTypeMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITransporterTypeRepository transporterTypeRepository = _transporterRepositoriesFactory.NewTransporterTypeRepository(connection);

            long transporterTypeId = transporterTypeRepository.Add(message.TransporterType);

            Sender.Tell(transporterTypeRepository.GetById(transporterTypeId));
        });
    }
}