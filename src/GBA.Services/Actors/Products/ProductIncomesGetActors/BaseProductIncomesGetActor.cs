using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Messages.Products.Incomes;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.Products.ProductIncomesGetActors;

public sealed class BaseProductIncomesGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseProductIncomesGetActor(
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;

        Receive<GetAllProductIncomesFilteredMessage>(ProcessGetAllProductIncomesFilteredMessage);

        Receive<GetAllProductIncomesByProductNetIdMessage>(ProcessGetAllProductIncomesByProductNetIdMessage);

        Receive<GetProductIncomeByNetIdMessage>(ProcessGetProductIncomeByNetIdMessage);

        Receive<GetBySupplyOrderNetIdMessage>(ProcessGetBySupplyOrderNetIdMessage);

        Receive<GetByDeliveryProductProtocolNetIdMessage>(ProcessGetByDeliveryProductProtocolNetId);

        Receive<GetAllBySupplyOrderUkraineNetIdMessage>(ProcessGetAllBySupplyOrderUkraineNetIdMessage);

        Receive<ExportProductIncomeDocumentMessage>(ProcessExportProductIncomeDocumentMessage);

        Receive<GetProductIncomeSupplyOrderUkraineMessage>(ProcessGetProductIncomeSupplyOrderUkraine);

        Receive<GetSupplyOrderProductIncomeByNetIdMessage>(ProcessGetSupplyOrderProductIncomeByNetId);
    }

    private void ProcessGetAllProductIncomesFilteredMessage(GetAllProductIncomesFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductIncomeRepository(connection)
                .GetAllFiltered(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset,
                    message.Value
                )
        );
    }

    private void ProcessGetAllProductIncomesByProductNetIdMessage(GetAllProductIncomesByProductNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductIncomeRepository(connection)
                .GetAllByProductNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetProductIncomeByNetIdMessage(GetProductIncomeByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductIncomeRepository(connection)
                .GetByNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetBySupplyOrderNetIdMessage(GetBySupplyOrderNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductIncomeRepository(connection)
                .GetBySupplyOrderNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetByDeliveryProductProtocolNetId(GetByDeliveryProductProtocolNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductIncomeRepository(connection)
                .GetByDeliveryProductProtocolNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetAllBySupplyOrderUkraineNetIdMessage(GetAllBySupplyOrderUkraineNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewProductIncomeRepository(connection)
                .GetAllBySupplyOrderUkraineNetId(
                    message.NetId
                )
        );
    }

    private void ProcessExportProductIncomeDocumentMessage(ExportProductIncomeDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ProductIncome productIncome =
                _productRepositoriesFactory.NewProductIncomeRepository(connection).GetByNetIdForPrintingDocument(message.NetId);

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewProductsXlsManager()
                    .ExportProductIncomeDocumentToXlsx(
                        message.PathToFolder,
                        productIncome
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private void ProcessGetProductIncomeSupplyOrderUkraine(GetProductIncomeSupplyOrderUkraineMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _productRepositoriesFactory
                    .NewProductIncomeRepository(connection)
                    .GetSupplyOrderUkraineProductIncomeByNetId(message.NetId)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetSupplyOrderProductIncomeByNetId(GetSupplyOrderProductIncomeByNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _productRepositoriesFactory
                    .NewProductIncomeRepository(connection)
                    .GetSupplyOrderProductIncomeByNetId(message.NetId)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}