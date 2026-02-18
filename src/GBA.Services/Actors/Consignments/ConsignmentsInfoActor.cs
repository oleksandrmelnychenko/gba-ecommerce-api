using System;
using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.EntityHelpers.Consignments;
using GBA.Domain.Messages.Consignments.Infos;
using GBA.Domain.Repositories.Consignments.Contracts;

namespace GBA.Services.Actors.Consignments;

public sealed class ConsignmentsInfoActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsignmentRepositoriesFactory _consignmentRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public ConsignmentsInfoActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IConsignmentRepositoriesFactory consignmentRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _consignmentRepositoriesFactory = consignmentRepositoriesFactory;

        Receive<GetIncomeConsignmentInfoFilteredMessage>(ProcessGetIncomeConsignmentInfoFilteredMessage);

        Receive<GetOutcomeConsignmentInfoFilteredMessage>(ProcessGetOutcomeConsignmentInfoFilteredMessage);

        Receive<GetMovementConsignmentInfoFilteredMessage>(ProcessGetMovementConsignmentInfoFilteredMessage);

        Receive<GetFullMovementConsignmentInfoByConsignmentNetIdMessage>(ProcessGetFullMovementConsignmentInfoByConsignmentNetIdMessage);

        Receive<GetClientMovementConsignmentInfoFilteredMessage>(ProcessGetClientMovementConsignmentInfoFilteredMessage);

        Receive<ExportClientMovementInfoFilteredDocumentMessage>(ProcessExportClientMovementInfoFilteredDocumentMessage);

        Receive<ExportInfoIncomeMessage>(ProcessExportInfoIncomeMessage);

        Receive<ExportIncomeMovementConsignmentInfoMessage>(ProcessExportIncomeMovementConsignmentInfoMessage);

        Receive<ExportOutcomeMovementConsignmentInfoMessage>(ProcessExportOutcomeMovementConsignmentInfoMessage);

        Receive<ExportMovementInfoDocumentMessage>(ProcessExportMovementInfoDocumentMessage);

        Receive<GetConsignmentAvailabilityFilteredMessage>(ProcessGetConsignmentAvailabilityFilteredMessage);

        Receive<ExportConsignmentAvailabilityFilteredMessage>(ProcessExportConsignmentAvailabilityFiltered);
    }

    private void ProcessGetIncomeConsignmentInfoFilteredMessage(GetIncomeConsignmentInfoFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _consignmentRepositoriesFactory
                .NewConsignmentInfoRepository(connection)
                .GetIncomeConsignmentInfoFiltered(
                    message.ProductNetId,
                    message.From,
                    message.To
                )
        );
    }

    private void ProcessGetConsignmentAvailabilityFilteredMessage(GetConsignmentAvailabilityFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _consignmentRepositoriesFactory
                .NewConsignmentInfoRepository(connection)
                .GetConsignmentAvailabilityFiltered(
                    message.StorageNetId,
                    message.From,
                    message.To,
                    message.VendorCode,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessExportConsignmentAvailabilityFiltered(ExportConsignmentAvailabilityFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            IEnumerable<ConsignmentAvailabilityItem> availabilities =
                _consignmentRepositoriesFactory
                    .NewConsignmentInfoRepository(connection)
                    .GetAllConsignmentAvailabilityFiltered(
                        message.StorageNetId,
                        message.From,
                        message.To,
                        message.VendorCode
                    );

            if (availabilities == null)
                Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewProductsXlsManager()
                    .ExportAllConsignmentAvailabilityFilteredToXlsx(
                        message.Path,
                        availabilities
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private void ProcessGetOutcomeConsignmentInfoFilteredMessage(GetOutcomeConsignmentInfoFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _consignmentRepositoriesFactory
                .NewConsignmentInfoRepository(connection)
                .GetOutcomeConsignmentInfoFiltered(
                    message.ProductNetId,
                    message.From,
                    message.To
                )
        );
    }

    private void ProcessGetMovementConsignmentInfoFilteredMessage(GetMovementConsignmentInfoFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _consignmentRepositoriesFactory
                .NewConsignmentInfoRepository(connection)
                .GetMovementConsignmentInfoFiltered(
                    message.MovementTypes,
                    message.ProductNetId,
                    message.From,
                    message.To,
                    message.MovementType
                )
        );
    }

    private void ProcessGetFullMovementConsignmentInfoByConsignmentNetIdMessage(GetFullMovementConsignmentInfoByConsignmentNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _consignmentRepositoriesFactory
                .NewConsignmentInfoRepository(connection)
                .GetFullMovementConsignmentInfoByConsignmentItemNetId(
                    message.ConsignmentItemNetId,
                    message.From,
                    message.To
                )
        );
    }

    private void ProcessGetClientMovementConsignmentInfoFilteredMessage(GetClientMovementConsignmentInfoFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _consignmentRepositoriesFactory
                .NewConsignmentInfoRepository(connection)
                .GetClientMovementConsignmentInfoFiltered(
                    message.ClientNetId,
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset,
                    message.OrganizationIds,
                    message.Article
                )
        );
    }

    private void ProcessExportClientMovementInfoFilteredDocumentMessage(ExportClientMovementInfoFilteredDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IEnumerable<ClientMovementConsignmentInfo> clientMovementConsignmentInfos = _consignmentRepositoriesFactory
                .NewConsignmentInfoRepository(connection)
                .GetClientMovementConsignmentInfoFilteredFoxDocumentExport(
                    message.ClientNetId,
                    message.From,
                    message.To
                );

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewConsignmentXlsManager()
                    .ExportClientMovementInfoFilteredDocumentToXlsx(
                        message.PathToFolder,
                        clientMovementConsignmentInfos);

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private void ProcessExportIncomeMovementConsignmentInfoMessage(ExportIncomeMovementConsignmentInfoMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IEnumerable<IncomeConsignmentInfo> incomeConsignmentInfos = _consignmentRepositoriesFactory
                .NewConsignmentInfoRepository(connection)
                .GetIncomeConsignmentInfoFiltered(
                    message.ProductNetId,
                    message.From,
                    message.To
                );

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewConsignmentXlsManager()
                    .ExportIncomeMovementConsignmentDocumentToXlsx(
                        message.PathToFolder,
                        incomeConsignmentInfos);

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private void ProcessExportOutcomeMovementConsignmentInfoMessage(ExportOutcomeMovementConsignmentInfoMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IEnumerable<OutcomeConsignmentInfo> outcomeConsignmentInfos = _consignmentRepositoriesFactory
                .NewConsignmentInfoRepository(connection)
                .GetOutcomeConsignmentInfoFiltered(
                    message.ProductNetId,
                    message.From,
                    message.To
                );

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewConsignmentXlsManager()
                    .ExportOutcomeMovementConsignmentDocumentToXlsx(
                        message.PathToFolder,
                        outcomeConsignmentInfos);

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private void ProcessExportInfoIncomeMessage(ExportInfoIncomeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEnumerable<InfoIncome> Infos = _consignmentRepositoriesFactory
            .NewConsignmentInfoRepository(connection)
            .GetInfoIcomesFiltered(
                message.ProductNetId
            );
        Sender.Tell(Infos);
    }

    private void ProcessExportMovementInfoDocumentMessage(ExportMovementInfoDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IEnumerable<MovementConsignmentInfo> movementConsignmentInfos = _consignmentRepositoriesFactory
                .NewConsignmentInfoRepository(connection)
                .GetMovementConsignmentInfoFiltered(
                    message.MovementTypes,
                    message.ProductNetId,
                    message.From,
                    message.To,
                    message.MovementType
                );

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewConsignmentXlsManager()
                    .ExportMovementInfoDocumentToXlsx(
                        message.PathToFolder,
                        movementConsignmentInfos);

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }
}