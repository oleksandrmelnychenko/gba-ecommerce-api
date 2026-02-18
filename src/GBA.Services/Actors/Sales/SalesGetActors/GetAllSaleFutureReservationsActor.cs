using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Sales.Reservations;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.Sales.SalesGetActors;

public sealed class GetAllSaleFutureReservationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public GetAllSaleFutureReservationsActor(IDbConnectionFactory connectionFactory, ISaleRepositoriesFactory saleRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;

        Receive<GetAllSaleFutureReservationsMessage>(ProcessGetAllSaleFutureReservationsMessage);
    }

    private void ProcessGetAllSaleFutureReservationsMessage(GetAllSaleFutureReservationsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory.NewSaleFutureReservationRepository(connection).GetAll());
    }
}