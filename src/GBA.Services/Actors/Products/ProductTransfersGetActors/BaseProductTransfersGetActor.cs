using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Messages.Products.Transfers;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.Products.ProductTransfersGetActors;

public sealed class BaseProductTransfersGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseProductTransfersGetActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<GetProductTransferByNetIdMessage>(ProcessGetProductTransferByNetIdMessage);

        Receive<GetAllProductTransfersMessage>(ProcessGetAllProductTransfersMessage);

        Receive<GetAllProductTransfersFilteredMessage>(ProcessGetAllProductTransfersFilteredMessage);

        Receive<ExportProductTransferDocumentMessage>(ProcessExportProductTransferDocumentMessage);
    }

    private void ProcessGetProductTransferByNetIdMessage(GetProductTransferByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductTransferRepository(connection)
                .GetByNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetAllProductTransfersMessage(GetAllProductTransfersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductTransferRepository(connection)
                .GetAll()
        );
    }

    private void ProcessGetAllProductTransfersFilteredMessage(GetAllProductTransfersFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductTransferRepository(connection)
                .GetAllFiltered(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessExportProductTransferDocumentMessage(ExportProductTransferDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ProductTransfer productTransfer = _productRepositoriesFactory.NewProductTransferRepository(connection).GetByNetId(message.NetId);

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewProductsXlsManager()
                    .ExportProductTransferToXlsx(
                        message.PathToFolder,
                        productTransfer
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }
}