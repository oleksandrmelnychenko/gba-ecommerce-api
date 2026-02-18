using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Deliveries;
using GBA.Domain.Repositories.Delivery.Contracts;

namespace GBA.Services.Actors.Deliveries;

public class TermsOfDeliveriesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDeliveryRepositoriesFactory _deliveryRepositoriesFactory;

    public TermsOfDeliveriesActor(
        IDbConnectionFactory connectionFactory,
        IDeliveryRepositoriesFactory deliveryRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _deliveryRepositoriesFactory = deliveryRepositoriesFactory;

        Receive<GetAllTermsOfDeliveriesMessage>(ProcessGetAllTermsOfDeliveriesMessage);
    }

    private void ProcessGetAllTermsOfDeliveriesMessage(GetAllTermsOfDeliveriesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_deliveryRepositoriesFactory
            .NewTermsOfDeliveryRepository(connection)
            .GetAll()
        );
    }
}