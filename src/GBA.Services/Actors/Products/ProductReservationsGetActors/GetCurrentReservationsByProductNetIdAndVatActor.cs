using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Products.ProductReservations;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.Products.ProductReservationsGetActors;

public sealed class GetCurrentReservationsByProductNetIdAndVatActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public GetCurrentReservationsByProductNetIdAndVatActor(IDbConnectionFactory connectionFactory, IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<GetCurrentReservationsByProductNetIdAndVatMessage>(ProcessGetCurrentReservationsByProductNetIdAndVatMessage);
    }

    private void ProcessGetCurrentReservationsByProductNetIdAndVatMessage(GetCurrentReservationsByProductNetIdAndVatMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductReservationRepository(connection)
                .GetAllCurrentReservationsByProductNetId(
                    message.NetId,
                    message.WithVat
                )
        );
    }
}