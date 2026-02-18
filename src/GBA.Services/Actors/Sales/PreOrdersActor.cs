using System;
using System.Data;
using System.Globalization;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Messages.Sales.PreOrders;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.Sales;

public sealed class PreOrdersActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public PreOrdersActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<GetAllPreOrdersByCurrentCultureFilteredMessage>(ProcessGetAllPreOrdersByCurrentCultureFiltered);

        Receive<AddNewPreOrderMessage>(ProcessAddNewPreOrderMessage);
    }

    private void ProcessGetAllPreOrdersByCurrentCultureFiltered(GetAllPreOrdersByCurrentCultureFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _saleRepositoriesFactory
                .NewPreOrderRepository(connection)
                .GetAllByCurrentCultureFiltered(
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessAddNewPreOrderMessage(AddNewPreOrderMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.Qty <= 0) {
            Sender.Tell(new Exception(PreOrderResourceNames.INVALID_QTY));

            return;
        }

        Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(message.ProductNetId);

        if (product == null) {
            Sender.Tell(new Exception(PreOrderResourceNames.INVALID_PRODUCT));

            return;
        }

        ClientAgreement clientAgreement = _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByNetIdWithClientInfo(message.ClientAgreementNetId);

        if (clientAgreement?.Client == null) {
            Sender.Tell(new Exception(PreOrderResourceNames.INVALID_CLIENT_AGREEMENT));

            return;
        }

        IPreOrderRepository preOrderRepository = _saleRepositoriesFactory.NewPreOrderRepository(connection);

        Sender.Tell(
            preOrderRepository
                .GetById(
                    preOrderRepository
                        .Add(new PreOrder {
                            Comment = message.Comment,
                            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                            ClientId = clientAgreement.Client.Id,
                            ProductId = product.Id,
                            MobileNumber = clientAgreement.Client.MobileNumber,
                            Qty = message.Qty
                        })
                )
        );
    }
}