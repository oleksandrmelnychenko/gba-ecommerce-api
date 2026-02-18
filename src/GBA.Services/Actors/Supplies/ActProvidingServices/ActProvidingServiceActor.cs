using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Supplies.ActProvidingServices;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies.ActProvidingServices;

public sealed class ActProvidingServiceActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public ActProvidingServiceActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<GetAllActProvidingServicesMessage>(ProcessGetAllActProvidingServices);

        Receive<GetActProvidingServiceByNetIdMessage>(ProcessGetActProvidingServiceByNetId);

        Receive<UpdateActProvidingServiceMessage>(ProcessUpdateActProvidingService);
    }

    private void ProcessGetAllActProvidingServices(GetAllActProvidingServicesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewActProvidingServiceRepository(connection)
            .GetAll(message.From, message.To, message.Limit, message.Offset));
    }

    private void ProcessGetActProvidingServiceByNetId(GetActProvidingServiceByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewActProvidingServiceRepository(connection)
            .GetByNetId(message.NetId));
    }

    private void ProcessUpdateActProvidingService(UpdateActProvidingServiceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _supplyRepositoriesFactory
            .NewActProvidingServiceRepository(connection)
            .Update(message.Act);

        Sender.Tell(_supplyRepositoriesFactory
            .NewActProvidingServiceRepository(connection)
            .GetByNetId(message.Act.NetUid));
    }
}