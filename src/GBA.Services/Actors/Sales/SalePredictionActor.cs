using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Messages.Sales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;

// using machine.learning.concord.SalePrediction.Contracts;

namespace GBA.Services.Actors.Sales;

public sealed class SalePredictionActor : ReceiveActor {
    // private readonly ISalePredictionManager _salePredictionManager;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public SalePredictionActor(
        IDbConnectionFactory connectionFactory,
        // ISalePredictionManager salePredictionManager,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        // _salePredictionManager = salePredictionManager;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<GetSalePredictionMessage>(ProcessGetSalePredictionMessage);
    }

    private void ProcessGetSalePredictionMessage(GetSalePredictionMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Client client = _clientRepositoriesFactory
                .NewClientRepository(connection)
                .GetByNetIdWithoutIncludes(message.ClientNetId);

            string clientRefId = client == null || string.IsNullOrEmpty(client.RefId) ? null : client.RefId;

            Product product = _productRepositoriesFactory
                .NewGetSingleProductRepository(connection)
                .GetByNetIdWithoutIncludes(message.ProductNetId);

            string productRefId = product == null || string.IsNullOrEmpty(product.RefId) ? null : product.RefId;

            // Sender.Tell(_salePredictionManager
            //     .GetMonthPredictions(
            //         connection,
            //         clientRefId,
            //         productRefId
            //     )
            // );
            Sender.Tell(null);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}