using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Supplies.HelperServices;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class ServiceDetailItemsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public ServiceDetailItemsActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<DeleteServiceDetailItemMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _supplyRepositoriesFactory.NewServiceDetailItemRepository(connection).Remove(message.NetId);
        });
    }
}