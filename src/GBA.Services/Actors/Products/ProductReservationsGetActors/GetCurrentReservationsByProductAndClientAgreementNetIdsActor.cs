using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Messages.Products.ProductReservations;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.Products.ProductReservationsGetActors;

public sealed class GetCurrentReservationsByProductAndClientAgreementNetIdsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public GetCurrentReservationsByProductAndClientAgreementNetIdsActor(
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<GetCurrentReservationsByProductAndClientAgreementNetIdsMessage>(ProcessGetCurrentReservationsByProductAndClientAgreementNetIdsMessage);
    }

    private void ProcessGetCurrentReservationsByProductAndClientAgreementNetIdsMessage(GetCurrentReservationsByProductAndClientAgreementNetIdsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ClientAgreement clientAgreement =
            _clientRepositoriesFactory
                .NewClientAgreementRepository(connection)
                .GetByNetIdWithOrganizationInfo(
                    message.ClientAgreementNetId
                );

        if (clientAgreement?.Agreement?.Organization == null) {
            Sender.Tell(new List<ProductReservation>());

            return;
        }

        Sender.Tell(
            _productRepositoriesFactory
                .NewProductReservationRepository(connection)
                .GetAllCurrentReservationsByProductNetId(
                    message.ProductNetId,
                    clientAgreement.Agreement.Organization.Id,
                    clientAgreement.Agreement.WithVATAccounting
                )
        );
    }
}