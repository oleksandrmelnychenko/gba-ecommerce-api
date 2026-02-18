using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Transporters;
using GBA.Domain.Repositories.Transporters.Contracts;

namespace GBA.Services.Actors.Transporters;

public sealed class TransportersActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITransporterRepositoriesFactory _transporterRepositoriesFactory;

    public TransportersActor(
        IDbConnectionFactory connectionFactory,
        ITransporterRepositoriesFactory transporterRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _transporterRepositoriesFactory = transporterRepositoriesFactory;

        Receive<GetAllTransportersMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_transporterRepositoriesFactory.NewTransporterRepository(connection).GetAll());
        });

        Receive<GetAllTransportersByTransporterTypeNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_transporterRepositoriesFactory
                .NewTransporterRepository(connection)
                .GetAllByTransporterTypeNetIdDeleted(message.TransporterTypeNetId)
            );
        });

        Receive<GetAllTransportersByTransporterIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_transporterRepositoriesFactory
                .NewTransporterRepository(connection)
                .GetAllByTransporterTypeNetId(message.TransporterTypeNetId)
            );
        });

        Receive<AddTransporterMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITransporterRepository transporterRepository = _transporterRepositoriesFactory.NewTransporterRepository(connection);

            long transporterId = transporterRepository.Add(message.Transporter);

            Sender.Tell(transporterRepository.GetById(transporterId));
        });

        Receive<GetTransporterByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_transporterRepositoriesFactory.NewTransporterRepository(connection).GetByNetId(message.NetId));
        });

        Receive<UpdateTransporterMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITransporterRepository transporterRepository = _transporterRepositoriesFactory.NewTransporterRepository(connection);

            transporterRepository.Update(message.Transporter);

            Sender.Tell(transporterRepository.GetByNetId(message.Transporter.NetUid));
        });

        Receive<DeleteTransporterMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _transporterRepositoriesFactory.NewTransporterRepository(connection).Remove(message.NetId);
        });

        Receive<ChangeTransporterPriorityMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITransporterRepository transporterRepository = _transporterRepositoriesFactory.NewTransporterRepository(connection);

            if (message.DecreaseTo != null && !message.DecreaseTo.Equals(Guid.Empty)) transporterRepository.DecreasePriority((Guid)message.DecreaseTo);

            transporterRepository.IncreasePriority(message.IncreaseTo);
        });
    }
}