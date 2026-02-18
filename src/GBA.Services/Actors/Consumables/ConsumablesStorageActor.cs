using System;
using System.Data;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Messages.Consumables.Storages;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Services.Actors.Consumables;

public sealed class ConsumablesStorageActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsumablesRepositoriesFactory _consumablesRepositoriesFactory;

    public ConsumablesStorageActor(
        IDbConnectionFactory connectionFactory,
        IConsumablesRepositoriesFactory consumablesRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _consumablesRepositoriesFactory = consumablesRepositoriesFactory;

        Receive<AddNewConsumablesStorageMessage>(message => {
            if (message.ConsumablesStorage.OrganizationId.Equals(0) && (message.ConsumablesStorage.Organization == null || message.ConsumablesStorage.Organization.IsNew())) {
                Sender.Tell(new Tuple<ConsumablesStorage, string>(null, ConsumableStoragesResourceNames.ORGANIZATION_NOT_SPECIFIED));
            } else if (message.ConsumablesStorage.ResponsibleUserId.Equals(0) &&
                       (message.ConsumablesStorage.ResponsibleUser == null || message.ConsumablesStorage.ResponsibleUser.IsNew())) {
                Sender.Tell(new Tuple<ConsumablesStorage, string>(null, ConsumableStoragesResourceNames.RESPONSIBLE_USER_NOT_SPECIFIED));
            } else {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                IConsumablesStorageRepository consumablesStorageRepository = _consumablesRepositoriesFactory.NewConsumablesStorageRepository(connection);

                if (message.ConsumablesStorage.Organization != null) message.ConsumablesStorage.OrganizationId = message.ConsumablesStorage.Organization.Id;
                if (message.ConsumablesStorage.ResponsibleUser != null) message.ConsumablesStorage.ResponsibleUserId = message.ConsumablesStorage.ResponsibleUser.Id;

                message.ConsumablesStorage.Id = consumablesStorageRepository.Add(message.ConsumablesStorage);

                Sender.Tell(new Tuple<ConsumablesStorage, string>(consumablesStorageRepository.GetById(message.ConsumablesStorage.Id), string.Empty));
            }
        });

        Receive<UpdateConsumablesStorageMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IConsumablesStorageRepository consumablesStorageRepository = _consumablesRepositoriesFactory.NewConsumablesStorageRepository(connection);

            if (message.ConsumablesStorage.Organization != null) message.ConsumablesStorage.OrganizationId = message.ConsumablesStorage.Organization.Id;
            if (message.ConsumablesStorage.ResponsibleUser != null) message.ConsumablesStorage.ResponsibleUserId = message.ConsumablesStorage.ResponsibleUser.Id;

            consumablesStorageRepository.Update(message.ConsumablesStorage);

            Sender.Tell(consumablesStorageRepository.GetById(message.ConsumablesStorage.Id));
        });

        Receive<GetAllConsumablesStoragesMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewConsumablesStorageRepository(connection).GetAll());
        });

        Receive<GetConsumablesStorageByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewConsumablesStorageRepository(connection).GetByNetId(message.NetId));
        });

        Receive<GetAllConsumablesStoragesFromSearchMessage>(message => {
            if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewConsumablesStorageRepository(connection).GetAllFromSearch(message.Value));
        });

        Receive<DeleteConsumablesStorageByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _consumablesRepositoriesFactory.NewConsumablesStorageRepository(connection).Remove(message.NetId);
        });
    }
}