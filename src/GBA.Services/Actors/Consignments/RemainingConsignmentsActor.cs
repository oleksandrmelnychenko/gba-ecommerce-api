using System;
using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.EntityHelpers.Consignments;
using GBA.Domain.Messages.Consignments.Remainings;
using GBA.Domain.Repositories.Consignments.Contracts;

namespace GBA.Services.Actors.Consignments;

public sealed class RemainingConsignmentsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsignmentRepositoriesFactory _consignmentRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public RemainingConsignmentsActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IConsignmentRepositoriesFactory consignmentRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _consignmentRepositoriesFactory = consignmentRepositoriesFactory;

        Receive<GetRemainingConsignmentsByProductNetIdMessage>(ProcessGetRemainingConsignmentsByProductNetIdMessage);

        Receive<GetRemainingConsignmentsByStorageNetIdMessage>(ProcessGetRemainingConsignmentsByStorageNetIdMessage);

        Receive<GetRemainingConsignmentsByProductIncomeNetIdMessage>(ProcessGetRemainingConsignmentsByProductIncomeNetIdMessage);

        Receive<GetAllAvailableConsignmentsForSupplyReturnByProductAndStorageNetIdsMessage>(ProcessGetAllAvailableConsignmentsForSupplyReturnByProductAndStorageNetIdsMessage);

        Receive<GetRemainingConsignmentsByStorageNetIdFilteredMessage>(ProcessGetRemainingConsignmentsByStorageNetIdFilteredMessage);

        Receive<GetGroupedConsignmentsByStorageNetIdFilteredMessage>(ProcessGetGroupedConsignmentsByStorageNetIdFilteredMessage);

        Receive<GetRemainingProductsByStorageDocumentExportMessage>(ProcessGetRemainingProductsByStorageDocumentExportMessage);

        Receive<GetGroupedConsignmentByStorageDocumentExportMessage>(ProcessGetGroupedConsignmentByStorageDocumentExportMessage);
    }

    private void ProcessGetRemainingConsignmentsByProductNetIdMessage(GetRemainingConsignmentsByProductNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _consignmentRepositoriesFactory
                .NewRemainingConsignmentRepository(connection)
                .GetAllByProductNetId(
                    message.ProductNetId
                )
        );
    }

    private void ProcessGetRemainingConsignmentsByStorageNetIdMessage(GetRemainingConsignmentsByStorageNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _consignmentRepositoriesFactory
                .NewRemainingConsignmentRepository(connection)
                .GetAllByStorageNetId(
                    message.StorageNetId
                )
        );
    }

    private void ProcessGetRemainingConsignmentsByProductIncomeNetIdMessage(GetRemainingConsignmentsByProductIncomeNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _consignmentRepositoriesFactory
                .NewRemainingConsignmentRepository(connection)
                .GetAllByProductIncomeNetId(
                    message.ProductIncomeNetId
                )
        );
    }

    private void ProcessGetAllAvailableConsignmentsForSupplyReturnByProductAndStorageNetIdsMessage(
        GetAllAvailableConsignmentsForSupplyReturnByProductAndStorageNetIdsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _consignmentRepositoriesFactory
                .NewRemainingConsignmentRepository(connection)
                .GetAllAvailableConsignmentsForSupplyReturnByProductAndStorageNetIds(
                    message.ProductNetId,
                    message.StorageNetId
                )
        );
    }

    private void ProcessGetRemainingConsignmentsByStorageNetIdFilteredMessage(GetRemainingConsignmentsByStorageNetIdFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IRemainingConsignmentRepository remainingConsignmentRepository = _consignmentRepositoriesFactory.NewRemainingConsignmentRepository(connection);

        (decimal totalEuro, decimal accountingTotalEuro, decimal totalLocal, decimal accountingLocalEuro, double totalQty) =
            remainingConsignmentRepository.GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetId(message.StorageNetId);

        (decimal totalEuroFiltered, decimal accountingTotalEuroFiltered, decimal totalLocalFiltered, decimal accountingTotalLocalFiltered, double totalQtyFiltered) =
            remainingConsignmentRepository
                .GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetIdFiltered(
                    message.StorageNetId,
                    message.SupplierNetId,
                    message.From,
                    message.To,
                    message.SearchValue
                );

        Sender.Tell(
            new {
                Collection = _consignmentRepositoriesFactory
                    .NewRemainingConsignmentRepository(connection)
                    .GetAllByStorageNetIdFiltered(
                        message.StorageNetId,
                        message.SupplierNetId,
                        message.From,
                        message.To,
                        message.SearchValue,
                        message.Limit,
                        message.Offset
                    ),
                TotalQty = totalQty,
                TotalQtyFiltered = totalQtyFiltered,
                TotalAmount = totalEuro,
                AccountingTotalAmount = accountingTotalEuro,
                TotalAmountLocal = totalLocal,
                AccountingTotalAmountLocal = accountingLocalEuro,
                TotalAmountFiltered = totalEuroFiltered,
                AccountingTotalAmountFiltered = accountingTotalEuroFiltered,
                TotalAmountLocalFiltered = totalLocalFiltered,
                AccountingTotalAmountLocalFiltered = accountingTotalLocalFiltered
            }
        );
    }

    private void ProcessGetGroupedConsignmentsByStorageNetIdFilteredMessage(GetGroupedConsignmentsByStorageNetIdFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IRemainingConsignmentRepository remainingConsignmentRepository = _consignmentRepositoriesFactory.NewRemainingConsignmentRepository(connection);

        (decimal totalEuro, decimal accountingTotalEuro, decimal totalLocal, decimal accountingLocalEuro, double totalQty) =
            remainingConsignmentRepository.GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetId(message.StorageNetId);

        (decimal totalEuroFiltered, decimal accountingTotalEuroFiltered, decimal totalLocalFiltered, decimal accountingTotalLocalFiltered, double totalQtyFiltered) =
            remainingConsignmentRepository
                .GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetIdFiltered(
                    message.StorageNetId,
                    message.SupplierNetId,
                    message.From,
                    message.To
                );

        Sender.Tell(
            new {
                Collection = _consignmentRepositoriesFactory
                    .NewRemainingConsignmentRepository(connection)
                    .GetGroupedByStorageNetIdFiltered(
                        message.StorageNetId,
                        message.SupplierNetId,
                        message.From,
                        message.To,
                        message.Limit,
                        message.Offset
                    ),
                TotalQty = totalQty,
                TotalQtyFiltered = totalQtyFiltered,
                TotalAmount = totalEuro,
                AccountingTotalAmount = accountingTotalEuro,
                TotalAmountLocal = totalLocal,
                AccountingTotalAmountLocal = accountingLocalEuro,
                TotalAmountFiltered = totalEuroFiltered,
                AccountingTotalAmountFiltered = accountingTotalEuroFiltered,
                TotalAmountLocalFiltered = totalLocalFiltered,
                AccountingTotalAmountLocalFiltered = accountingTotalLocalFiltered
            }
        );
    }

    private void ProcessGetRemainingProductsByStorageDocumentExportMessage(GetRemainingProductsByStorageDocumentExportMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IRemainingConsignmentRepository remainingConsignmentRepository = _consignmentRepositoriesFactory.NewRemainingConsignmentRepository(connection);

            (decimal totalEuro, decimal accountingTotalEuro, decimal totalLocal, decimal accountingLocalEuro, double totalQty) =
                remainingConsignmentRepository.GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetId(message.StorageNetId);

            (decimal totalEuroFiltered, decimal accountingTotalEuroFiltered, decimal totalLocalFiltered, decimal accountingTotalLocalFiltered, double totalQtyFiltered) =
                remainingConsignmentRepository
                    .GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetIdFiltered(
                        message.StorageNetId,
                        message.SupplierNetId,
                        message.From,
                        message.To,
                        message.SearchValue
                    );

            List<RemainingConsignment> remainingConsignments = _consignmentRepositoriesFactory
                .NewRemainingConsignmentRepository(connection)
                .GetAllByStorageForDocumentExport(
                    message.StorageNetId,
                    message.SupplierNetId,
                    message.From,
                    message.To,
                    message.SearchValue
                );

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewConsignmentXlsManager()
                    .ExportGetRemainingProductsByStorageDocumentToXlsx(
                        message.PathToFolder,
                        remainingConsignments,
                        totalEuro,
                        accountingTotalEuro,
                        totalLocal,
                        accountingLocalEuro,
                        totalEuroFiltered,
                        accountingTotalEuroFiltered,
                        totalLocalFiltered,
                        accountingTotalLocalFiltered,
                        totalQty,
                        totalQtyFiltered
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private void ProcessGetGroupedConsignmentByStorageDocumentExportMessage(GetGroupedConsignmentByStorageDocumentExportMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IRemainingConsignmentRepository remainingConsignmentRepository = _consignmentRepositoriesFactory.NewRemainingConsignmentRepository(connection);

            (decimal totalEuro, decimal accountingTotalEuro, decimal totalLocal, decimal accountingLocalEuro, double totalQty) =
                remainingConsignmentRepository.GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetId(message.StorageNetId);

            (decimal totalEuroFiltered, decimal accountingTotalEuroFiltered, decimal totalLocalFiltered, decimal accountingTotalLocalFiltered, double totalQtyFiltered) =
                remainingConsignmentRepository
                    .GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetIdFiltered(
                        message.StorageNetId,
                        message.SupplierNetId,
                        message.From,
                        message.To
                    );

            List<GroupedConsignment> groupedConsignments = _consignmentRepositoriesFactory
                .NewRemainingConsignmentRepository(connection)
                .GetGroupedByStorageForDocumentExport(
                    message.StorageNetId,
                    message.SupplierNetId,
                    message.From,
                    message.To
                );

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewConsignmentXlsManager()
                    .ExportGetGroupedConsignmentByStorageDocumentToXlsx(
                        message.PathToFolder,
                        groupedConsignments,
                        totalEuro,
                        accountingTotalEuro,
                        totalLocal,
                        accountingLocalEuro,
                        totalEuroFiltered,
                        accountingTotalEuroFiltered,
                        totalLocalFiltered,
                        accountingTotalLocalFiltered,
                        totalQty,
                        totalQtyFiltered
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }
}