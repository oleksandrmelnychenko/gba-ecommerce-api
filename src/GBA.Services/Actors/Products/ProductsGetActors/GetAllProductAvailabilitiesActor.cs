using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Products.ProductAvailabilities;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.Products.ProductsGetActors;

public sealed class GetAllProductAvailabilitiesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public GetAllProductAvailabilitiesActor(IDbConnectionFactory connectionFactory, IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<GetAllProductAvailabilitiesMessage>(ProcessGetAllProductAvailabilities);
    }

    private void ProcessGetAllProductAvailabilities(GetAllProductAvailabilitiesMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _productRepositoriesFactory
                    .NewGetSingleProductRepository(connection)
                    .GetAllProductAvailabilities(message.NetId, message.ClientAgreementNetId, message.SaleNetId)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}