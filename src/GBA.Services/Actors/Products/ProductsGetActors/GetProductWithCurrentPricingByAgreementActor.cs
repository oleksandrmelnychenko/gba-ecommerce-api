using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.Messages.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.Products.ProductsGetActors;

public sealed class GetProductWithCurrentPricingByAgreementActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public GetProductWithCurrentPricingByAgreementActor(IDbConnectionFactory connectionFactory, IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<GetProductWithCurrentPricingByAgreementMessage>(ProcessGetProductWithCurrentPricingByAgreementMessage);
    }

    private void ProcessGetProductWithCurrentPricingByAgreementMessage(GetProductWithCurrentPricingByAgreementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(message.ProductNetId, message.ClientAgreementNetId);
        Sender.Tell(Math.Round(product.CurrentPrice, 2, MidpointRounding.AwayFromZero));
    }
}