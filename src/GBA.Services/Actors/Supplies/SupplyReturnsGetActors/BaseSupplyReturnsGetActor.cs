using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Messages.Supplies.Returns;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies.SupplyReturnsGetActors;

public sealed class BaseSupplyReturnsGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseSupplyReturnsGetActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;

        Receive<GetSupplyReturnByNetIdMessage>(ProcessGetSupplyReturnByNetIdMessage);

        Receive<GetAllSupplyReturnsMessage>(ProcessGetAllSupplyReturnsMessage);

        Receive<GetAllSupplyReturnsFilteredMessage>(ProcessGetAllSupplyReturnsFilteredMessage);

        Receive<ExportSupplierReturnDocumentMessage>(ProcessExportSupplierReturnDocumentMessage);
    }

    private void ProcessGetSupplyReturnByNetIdMessage(GetSupplyReturnByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyRepositoriesFactory
                .NewSupplyReturnRepository(connection)
                .GetByNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetAllSupplyReturnsMessage(GetAllSupplyReturnsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyRepositoriesFactory
                .NewSupplyReturnRepository(connection)
                .GetAll()
        );
    }

    private void ProcessGetAllSupplyReturnsFilteredMessage(GetAllSupplyReturnsFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyRepositoriesFactory
                .NewSupplyReturnRepository(connection)
                .GetAllFiltered(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessExportSupplierReturnDocumentMessage(ExportSupplierReturnDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            SupplyReturn supplyReturn =
                _supplyRepositoriesFactory
                    .NewSupplyReturnRepository(connection)
                    .GetByNetIdForPrintingDocument(message.NetId);

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewSaleReturnXlsManager()
                    .ExportSupplyReturnToXlsx(
                        message.PathToFolder,
                        supplyReturn
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }
}