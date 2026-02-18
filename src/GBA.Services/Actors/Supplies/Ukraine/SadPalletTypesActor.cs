using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Supplies.Ukraine.SadPalletTypes;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class SadPalletTypesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;

    public SadPalletTypesActor(
        IDbConnectionFactory connectionFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<AddNewSadPalletTypeMessage>(message => {
            try {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                if (message.SadPalletType == null) throw new Exception("SadPalletType entity can not be null or empty");
                if (!message.SadPalletType.IsNew()) throw new Exception("Existing SadPalletType is not valid payload");

                ISadPalletTypeRepository sadPalletTypeRepository = _supplyUkraineRepositoriesFactory.NewSadPalletTypeRepository(connection);

                message.SadPalletType.Id =
                    sadPalletTypeRepository
                        .Add(
                            message.SadPalletType
                        );

                Sender.Tell(
                    sadPalletTypeRepository
                        .GetById(
                            message.SadPalletType.Id
                        )
                );
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });

        Receive<UpdateSadPalletTypeMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.SadPalletType == null) throw new Exception("SadPalletType entity can not be null or empty");
            if (message.SadPalletType.IsNew()) throw new Exception("New SadPalletType is not valid payload");

            ISadPalletTypeRepository sadPalletTypeRepository = _supplyUkraineRepositoriesFactory.NewSadPalletTypeRepository(connection);

            sadPalletTypeRepository
                .Update(
                    message.SadPalletType
                );

            Sender.Tell(
                sadPalletTypeRepository
                    .GetById(
                        message.SadPalletType.Id
                    )
            );
        });

        Receive<GetSadPalletTypeByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyUkraineRepositoriesFactory
                    .NewSadPalletTypeRepository(connection)
                    .GetByNetId(
                        message.NetId
                    )
            );
        });

        Receive<GetAllSadPalletTypesMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyUkraineRepositoriesFactory
                    .NewSadPalletTypeRepository(connection)
                    .GetAll()
            );
        });

        Receive<DeleteSadPalletTypeByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _supplyUkraineRepositoriesFactory
                .NewSadPalletTypeRepository(connection)
                .Remove(
                    message.NetId
                );
        });
    }
}