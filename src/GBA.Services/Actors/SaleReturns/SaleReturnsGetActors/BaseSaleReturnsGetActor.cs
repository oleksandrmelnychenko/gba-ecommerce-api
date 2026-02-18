using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ReportTypes;
using GBA.Domain.Messages.SaleReturns;
using GBA.Domain.Repositories.DocumentMonths.Contracts;
using GBA.Domain.Repositories.SaleReturns.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.SaleReturns.SaleReturnsGetActors;

public sealed class BaseSaleReturnsGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDocumentMonthRepositoryFactory _documentMonthRepositoryFactory;
    private readonly ISaleReturnRepositoriesFactory _saleReturnRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseSaleReturnsGetActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleReturnRepositoriesFactory saleReturnRepositoriesFactory,
        IDocumentMonthRepositoryFactory documentMonthRepositoryFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleReturnRepositoriesFactory = saleReturnRepositoriesFactory;
        _documentMonthRepositoryFactory = documentMonthRepositoryFactory;

        Receive<GetSaleReturnByNetIdMessage>(ProcessGetSaleReturnByNetIdMessage);

        Receive<GetSaleReturnPrintingDocumentsByNetIdMessage>(ProcessGetSaleReturnPrintingDocumentsByNetIdMessage);

        Receive<GetAllSaleReturnsMessage>(ProcessGetAllSaleReturnsMessage);

        Receive<GetAllSaleReturnsFilteredMessage>(ProcessGetAllSaleReturnsFilteredMessage);

        Receive<ExportSaleReturnDocumentMessage>(ProcessExportSaleReturnDocumentMessage);
    }

    private void ProcessGetSaleReturnByNetIdMessage(GetSaleReturnByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _saleReturnRepositoriesFactory
                .NewSaleReturnRepository(connection)
                .GetByNetId(message.NetId)
        );
    }

    private void ProcessGetSaleReturnPrintingDocumentsByNetIdMessage(GetSaleReturnPrintingDocumentsByNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            SaleReturn saleReturn =
                _saleReturnRepositoriesFactory
                    .NewSaleReturnRepository(connection)
                    .GetByNetIdForPrinting(
                        message.NetId
                    );

            List<ProductLocation> productLocations = new();
            List<SaleReturnItemProductPlacement> saleReturnItemProductPlacement = new();
            List<ProductLocationHistory> productLocationsHistory = new();
            foreach (SaleReturnItem saleReturnItem in saleReturn.SaleReturnItems) {
                OrderItem orderItem = saleReturnItem.OrderItem;
                foreach (SaleReturnItemProductPlacement productLocation in saleReturnItem.SaleReturnItemProductPlacements) {
                    if (productLocation.Qty.Equals(0))
                        continue;
                    List<SaleReturnItemProductPlacement> saleReturnItemproductPlacements = saleReturnItem.SaleReturnItemProductPlacements.Where(x =>
                        x.ProductPlacement.StorageNumber == productLocation.ProductPlacement.StorageNumber
                        && x.ProductPlacement.RowNumber == productLocation.ProductPlacement.RowNumber &&
                        x.ProductPlacement.CellNumber == productLocation.ProductPlacement.CellNumber).ToList();


                    if (saleReturnItemproductPlacements.Any()) {
                        double sumSaleReturnItemproductPlacements = saleReturnItemproductPlacements.Sum(x => x.Qty);
                        SaleReturnItemProductPlacement maxQtyLocation = saleReturnItemproductPlacements.First();
                        maxQtyLocation.Qty = sumSaleReturnItemproductPlacements;
                        List<SaleReturnItemProductPlacement> g = saleReturnItem.SaleReturnItemProductPlacements.Where(x =>
                            x.ProductPlacement.StorageNumber == productLocation.ProductPlacement.StorageNumber
                            && x.ProductPlacement.RowNumber == productLocation.ProductPlacement.RowNumber &&
                            x.ProductPlacement.CellNumber == productLocation.ProductPlacement.CellNumber && !x.Id.Equals(maxQtyLocation.Id)).ToList();
                        g.ForEach(x => x.Qty = 0);
                        saleReturnItemProductPlacement.Add(maxQtyLocation);
                    }
                }

                saleReturnItem.SaleReturnItemProductPlacements.Clear();
                saleReturnItem.SaleReturnItemProductPlacements = saleReturnItemProductPlacement;
                saleReturnItemProductPlacement = new List<SaleReturnItemProductPlacement>();
            }

            if (saleReturn == null) throw new Exception("SaleReturn with provided NetId does not exists");
            if (saleReturn.IsCanceled) throw new Exception("Selected SaleReturn is Canceled");

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl")) {
                (string xlsxFile, string pdfFile) =
                    _xlsFactoryManager
                        .NewSaleReturnXlsManager()
                        .ExportPlSaleReturnToXlsx(
                            message.Path,
                            saleReturn
                        );

                (string pzXlsxFile, string pzPdfFile) =
                    _xlsFactoryManager
                        .NewSaleReturnXlsManager()
                        .ExportPlInvoicePzToXlsx(
                            message.Path,
                            saleReturn
                        );

                Sender.Tell(
                    (xlsxFile, pdfFile, pzXlsxFile, pzPdfFile)
                );
            } else {
                if (saleReturn.Sale.IsVatSale) {
                    (string xlsFile, string pdfFile) =
                        _xlsFactoryManager
                            .NewSaleReturnXlsManager()
                            .ExportUkSaleReturnToXlsxFromVatSales(
                                message.Path,
                                saleReturn,
                                _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                            );

                    Sender.Tell(
                        (xlsFile, pdfFile, string.Empty, string.Empty)
                    );
                } else {
                    (string xlsFile, string pdfFile) =
                        _xlsFactoryManager
                            .NewSaleReturnXlsManager()
                            .ExportUkSaleReturnToXlsx(
                                message.Path,
                                saleReturn,
                                _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                            );

                    Sender.Tell(
                        (xlsFile, pdfFile, string.Empty, string.Empty)
                    );
                }
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllSaleReturnsMessage(GetAllSaleReturnsMessage _) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _saleReturnRepositoriesFactory
                .NewSaleReturnRepository(connection)
                .GetAll()
        );
    }

    private void ProcessGetAllSaleReturnsFilteredMessage(GetAllSaleReturnsFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _saleReturnRepositoriesFactory
                .NewSaleReturnRepository(connection)
                .GetAllFiltered(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset,
                    message.Value
                )
        );
    }

    private void ProcessExportSaleReturnDocumentMessage(ExportSaleReturnDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            string pdfFilePath = string.Empty;
            string excelFilePath = string.Empty;

            List<SaleReturnItemStatusName> reasons = _saleReturnRepositoriesFactory.NewSaleReturnItemStatusNameRepository(connection).GetAll();

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            switch (message.ReportType) {
                case SaleReturnReportType.GroupedByReasons: {
                    List<SaleReturn> saleReturns = _saleReturnRepositoriesFactory
                        .NewSaleReturnRepository(connection)
                        .GetFilteredGroupedByReasonReport(message.From, message.To, message.ForMyClients, user.Id, message.ClientNetId);

                    Dictionary<SaleReturnItemStatus, double> totalQuantitySaleReturnByReasons =
                        _saleReturnRepositoriesFactory
                            .NewSaleReturnItemStatusNameRepository(connection)
                            .GetSaleReturnQuantityGroupByReason(message.From, message.To, message.ForMyClients, user.Id, message.ClientNetId);

                    (excelFilePath, pdfFilePath) =
                        _xlsFactoryManager
                            .NewSaleReturnXlsManager()
                            .ExportSaleReturnGroupedByReasonReportToXlsx(
                                message.PathToFolder,
                                saleReturns,
                                reasons,
                                totalQuantitySaleReturnByReasons
                            );
                    break;
                }
                case SaleReturnReportType.Detail: {
                    List<Client> clients =
                        _saleReturnRepositoriesFactory
                            .NewSaleReturnRepository(connection)
                            .GetFilteredDetailReportByClient(message.From, message.To, message.ForMyClients, user.Id, message.ClientNetId, reasons);

                    (excelFilePath, pdfFilePath) =
                        _xlsFactoryManager
                            .NewSaleReturnXlsManager()
                            .ExportSaleReturnDetailReportToXlsx(
                                message.PathToFolder,
                                clients
                            );
                    break;
                }
                default:
                    Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
                    break;
            }

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }
}