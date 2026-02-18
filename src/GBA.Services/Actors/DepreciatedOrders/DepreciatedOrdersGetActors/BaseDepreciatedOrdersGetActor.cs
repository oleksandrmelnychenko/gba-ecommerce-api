using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Messages.DepreciatedOrders;
using GBA.Domain.Repositories.DepreciatedOrders.Contracts;

namespace GBA.Services.Actors.DepreciatedOrders.DepreciatedOrdersGetActors;

public sealed class BaseDepreciatedOrdersGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDepreciatedRepositoriesFactory _depreciatedRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseDepreciatedOrdersGetActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IDepreciatedRepositoriesFactory depreciatedRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _depreciatedRepositoriesFactory = depreciatedRepositoriesFactory;

        Receive<GetDepreciatedOrderByNetIdMessage>(ProcessGetDepreciatedOrderByNetIdMessage);

        Receive<GetAllDepreciatedOrdersMessage>(ProcessGetAllDepreciatedOrdersMessage);

        Receive<GetAllDepreciatedOrdersFilteredMessage>(ProcessGetAllDepreciatedOrdersFilteredMessage);

        Receive<ExportDepreciatedOrderDocumentMessage>(ProcessExportDepreciatedOrderDocumentMessage);
    }

    private void ProcessGetDepreciatedOrderByNetIdMessage(GetDepreciatedOrderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _depreciatedRepositoriesFactory
                .NewDepreciatedOrderRepository(connection)
                .GetByNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetAllDepreciatedOrdersMessage(GetAllDepreciatedOrdersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _depreciatedRepositoriesFactory
                .NewDepreciatedOrderRepository(connection)
                .GetAll()
        );
    }

    private void ProcessGetAllDepreciatedOrdersFilteredMessage(GetAllDepreciatedOrdersFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _depreciatedRepositoriesFactory
                .NewDepreciatedOrderRepository(connection)
                .GetAllFiltered(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessExportDepreciatedOrderDocumentMessage(ExportDepreciatedOrderDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            DepreciatedOrder depreciatedOrder =
                _depreciatedRepositoriesFactory
                    .NewDepreciatedOrderRepository(connection)
                    .GetByNetIdForExportDocument(
                        message.NetId
                    );

            if (depreciatedOrder == null) {
                Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));

                return;
            }

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewOrderXlsManager()
                    .ExportDepreciatedOrderDocumentToXlsx(
                        message.PathToFolder,
                        depreciatedOrder
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }
}