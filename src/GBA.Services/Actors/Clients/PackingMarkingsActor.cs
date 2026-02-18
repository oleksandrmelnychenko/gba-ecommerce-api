using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Clients.PackingMarkings;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Actors.Clients;

public sealed class PackingMarkingsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public PackingMarkingsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<GetAllPackingMarkingPaymentsMessage>(ProcessGetAllPackingMarkingPaymentsMessage);

        Receive<GetAllPackingMarkingsMessage>(ProcessGetAllPackingMarkingsMessage);
    }

    private void ProcessGetAllPackingMarkingPaymentsMessage(GetAllPackingMarkingPaymentsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory
            .NewPackingMarkingPaymentRepository(connection)
            .GetAll()
        );
    }

    private void ProcessGetAllPackingMarkingsMessage(GetAllPackingMarkingsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory
            .NewPackingMarkingRepository(connection)
            .GetAll()
        );
    }
}