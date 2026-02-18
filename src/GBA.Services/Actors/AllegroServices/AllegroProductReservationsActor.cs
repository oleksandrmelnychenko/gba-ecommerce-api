using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.AllegroServices.Reservations;
using GBA.Domain.Repositories.AllegroServices.Contracts;

namespace GBA.Services.Actors.AllegroServices;

public sealed class AllegroProductReservationsActor : ReceiveActor {
    private readonly IAllegroServicesRepositoriesFactory _allegroServicesRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public AllegroProductReservationsActor(
        IDbConnectionFactory connectionFactory,
        IAllegroServicesRepositoriesFactory allegroServicesRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _allegroServicesRepositoriesFactory = allegroServicesRepositoriesFactory;

        Receive<GetAllAllegroReservationsByProductNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_allegroServicesRepositoriesFactory.NewAllegroProductReservationRepository(connection).GetAllByProductNetId(message.NetId));
        });
    }
}