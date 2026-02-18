using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using Akka.Util.Internal;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Domain.Messages.Communications.Hubs;
using GBA.Domain.Messages.Sales;
using GBA.Domain.Messages.Sales.Charts;
using GBA.Domain.Messages.Sales.ShipmentLists;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.DocumentMonths.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Sales.SalesGetActors;

public sealed class BaseSalesGetActor : ReceiveActor {
    private static readonly Regex SpecialCharactersReplace = new("[$&+,:;=?@#|/\\\\'\"�<>. ^*()%!\\-]", RegexOptions.Compiled);
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IDocumentMonthRepositoryFactory _documentMonthRepositoryFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseSalesGetActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager,
        IDocumentMonthRepositoryFactory documentMonthRepositoryFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _documentMonthRepositoryFactory = documentMonthRepositoryFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        Receive<GetAllSalesByLifeCycleTypeMessage>(ProcessGetAllSalesByLifeCycleTypeMessage);

        Receive<GetAllTotalAmountMessage>(ProcessGetAllTotalAmountMessage);

        Receive<GetAllSubClientsSalesByClientNetIdMessage>(ProcessGetAllSubClientsSalesByClientNetIdMessage);

        Receive<GetAllSalesFilteredByTransporterMessage>(ProcessGetAllSalesFilteredByTransporterMessage);

        Receive<GetSaleInvoiceDocumentBySaleNetIdMessage>(ProcessGetSaleInvoiceDocumentBySaleNetIdMessage);

        Receive<GetSaleInvoiceDocumentBySaleHistoryNetIdMessage>(ProcessGetSaleInvoiceDocumentBySaleHistoryNetIdMessage);

        Receive<GetSaleInvoiceDocumentHistoryBySaleNetIdMessage>(ProcessGetSaleInvoiceDocumentHistoryBySaleNetIdMessage);

        Receive<GetSaleInvoicePzDocumentBySaleNetIdMessage>(ProcessGetSaleInvoicePzDocumentBySaleNetIdMessage);

        Receive<GetSaleStatisticWithResourceNameByNetIdMessage>(ProcessGetSaleStatisticWithResourceNameByNetIdMessage);

        Receive<GetOrderItemAndSaleStatisticMessage>(ProcessGetOrderItemAndSaleStatisticMessage);

        Receive<GetOrderItemAndSaleStatisticsMessage>(ProcessGetOrderItemAndSaleStatisticsMessage);

        Receive<GetOrderItemAndSaleStatisticAndIsNewSaleMessage>(ProcessGetOrderItemAndSaleStatisticAndIsNewSaleMessage);

        Receive<GetSaleByNetIdWithShiftedItemsMessage>(ProcessGetSaleByNetIdWithShiftedItemsMessage);

        Receive<GetSaleByNetIdWithShiftedItemsDocumentMessage>(ProcessGetSaleByNetIdWithShiftedItemsDocumentMessage);

        Receive<GetSaleByNetIdWithShiftedItemsHistoryDocumentMessage>(ProcessGetSaleByNetIdWithShiftedItemsHistoryDocumentMessage);

        Receive<GetTotalsBySalesManagersMessage>(ProcessGetTotalsBySalesManagersMessage);

        Receive<GetTotalForSalesByYearMessage>(ProcessGetTotalForSalesByYearMessage);

        Receive<GetCurrentSaleByClientAgreementOrSaleNetIdMessage>(ProcessGetCurrentSaleByClientAgreementOrSaleNetIdMessage);

        Receive<GetCurrentNotMergedSaleByClientAgreementMessage>(ProcessGetCurrentNotMergedSaleByClientAgreementMessage);

        Receive<GetSalesStatisticByDateRangeAndUserNetIdMessage>(ProcessGetSalesStatisticByDateRangeAndUserNetIdMessage);

        Receive<GetSaleMergeStatistic>(ProcessGetSaleMergeStatistic);

        Receive<GetSaleMergeStatisticWithOrderItemsMerged>(ProcessGetSaleMergeStatisticWithOrderItemsMerged);

        Receive<CalculateSaleWithOneTimeDiscountsMessage>(ProcessCalculateSaleWithOneTimeDiscountsMessage);

        Receive<GetAllSalesFromECommerceFromPlUkClientsMessage>(ProcessGetAllSalesFromECommerceFromPlUkClientsMessage);

        Receive<GetOrderItemsWithProductLocationBySaleNetIdMessage>(ProcessGetOrderItemsWithProductLocationBySaleNetIdMessage);

        Receive<GetAllSalesFromPlUkGroupedByClientFilteredMessage>(ProcessGetAllSalesFromPlUkGroupedByClientFilteredMessage);

        Receive<GetAllSalesForSaleReturnsFromSearchMessage>(ProcessGetAllSalesForSaleReturnsFromSearchMessage);

        Receive<UnlockVatSaleByNetIdMessage>(ProcessUnlockVatSaleByNetIdMessage);

        Receive<GetChartInfoSalesByClientMessage>(ProcessGetChartInfoSalesByClientMessage);

        Receive<GetInfoAboutSalesMessage>(ProcessGetInfoAboutSalesMessage);

        Receive<GetManagersSalesByProductTopMessage>(ProcessGetManagersSalesByProductTopMessage);

        Receive<ShipmentListForSalePrintDocumentsMessage>(ProcessShipmentListForSalePrintDocuments);

        Receive<ShipmentListForSalePrintDocumentsHistoryMessage>(ProcessShipmentListForSalePrintDocumentsHistory);

        Receive<UpdateSaleCommentByNetIdMessage>(ProcessUpdateSaleCommentByNetIdMessage);
    }


    private void ProcessUpdateSaleCommentByNetIdMessage(UpdateSaleCommentByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (message.Comment.Length > 450) {
                Sender.Tell(SaleResourceNames.CHARACTER_LIMIT_EXCEEDED);
                return;
            }

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Sale sale = saleRepository.GetByNetId(message.NetId);
            if (sale.WarehousesShipment != null) saleRepository.UpdateWarehousesShipmentCommentByNetId(sale.WarehousesShipment.NetUid, message.Comment);
            saleRepository.UpdateSaleCommentByNetId(message.NetId, message.Comment);


            Sender.Tell(true);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllSalesByLifeCycleTypeMessage(GetAllSalesByLifeCycleTypeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory.NewSaleRepository(connection).GetAllByLifeCycleType(message.SaleLifeCycleType));
    }

    private void ProcessGetAllTotalAmountMessage(GetAllTotalAmountMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.From == null)
            message.From = DateTime.ParseExact(DateTime.Now.ToString("yyyyMMdd"), "yyyyMMdd", CultureInfo.InvariantCulture);

        if (message.To == null)
            message.To = DateTime.ParseExact(DateTime.Now.ToString("yyyyMMdd"), "yyyyMMdd", CultureInfo.InvariantCulture).AddTicks(-1).AddDays(1);

        Sender.Tell(_saleRepositoriesFactory.NewSaleRepository(connection).GetAllTotalAmount(message.SaleLifeCycleType, message.From.Value, message.To.Value));
    }

    private void ProcessGetAllSubClientsSalesByClientNetIdMessage(GetAllSubClientsSalesByClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            List<Sale> sales = _saleRepositoriesFactory
                .NewSaleRepository(connection)
                .GetAllSubClientsSalesByClientNetId(message.ClientNetId);

            SaleActorsHelpers.CalculatePricingsForSales(sales, connection, _exchangeRateRepositoriesFactory, _saleRepositoriesFactory, false);

            Sender.Tell(sales);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllSalesFilteredByTransporterMessage(GetAllSalesFilteredByTransporterMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<Sale> sales =
            _saleRepositoriesFactory
                .NewSaleRepository(connection)
                .GetAllFilteredByTransporterAndType(
                    message.From,
                    message.To,
                    message.NetId
                );

        SaleActorsHelpers.CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
            _currencyRepositoriesFactory.NewCurrencyRepository(connection));

        Sender.Tell(sales);
    }

    private void ProcessGetSaleInvoiceDocumentBySaleHistoryNetIdMessage(GetSaleInvoiceDocumentBySaleHistoryNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        Sale saleFromDb = saleRepository.GetByNetIdWithProductLocations(message.NetId);
        saleFromDb.HistoryInvoiceEdit
            .SelectMany(item => item.OrderItemBaseShiftStatuses)
            .ToList()
            .ForEach(s => {
                OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(x => x.Id == s.OrderItemId);
                if (orderItem != null) {
                    orderItem.Qty += s.Qty;
                    orderItem.ProductLocationsHistory = orderItem.ProductLocationsHistory.Where(x => x.TypeOfMovement == TypeOfMovement.Return).ToList();
                }
            });
        foreach (HistoryInvoiceEdit history in saleFromDb.HistoryInvoiceEdit)
        foreach (ProductLocationHistory item in history.ProductLocationHistory) {
            OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);

            orderItem.ProductLocationsHistory.Add(item);
        }

        SelectProductLocationHistory(saleFromDb);

        saleFromDb.HistoryInvoiceEdit.Clear();

        if (saleFromDb == null) {
            Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale with provided NetId does not exists."));
        } else if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
            Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale should be in invoice stage."));
        } else if (saleFromDb.SaleInvoiceDocument == null) {
            SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleFromDb,
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

            if (!saleFromDb.SaleInvoiceNumberId.HasValue) {
                ISaleInvoiceNumberRepository saleInvoiceNumberRepository = _saleRepositoriesFactory.NewSaleInvoiceNumberRepository(connection);

                string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

                saleFromDb.SaleInvoiceNumber = new SaleInvoiceNumber {
                    Number =
                        string.Format(
                            "{0:D4}",
                            Convert.ToInt64(
                                saleFromDb.SaleNumber.Value.Substring(
                                    saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length,
                                    saleFromDb.SaleNumber.Value.Length - (saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length)
                                )
                            )
                        )
                };

                saleFromDb.SaleInvoiceNumberId = saleInvoiceNumberRepository.Add(saleFromDb.SaleInvoiceNumber);

                saleRepository.UpdateSaleInvoiceNumber(saleFromDb);
            }

            string xlsxFile;
            string pdfFile;

            if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaged))
                saleFromDb.Order.OrderItems.ForEach(orderItem => {
                    orderItem.Product.CurrentPrice = orderItem.PricePerItem;
                    orderItem.Product.CurrentLocalPrice = orderItem.PricePerItem * orderItem.ExchangeRateAmount;
                });

            if (saleFromDb.IsVatSale) {
                if (message.IsFromStorages)
                    (xlsxFile, pdfFile) =
                        _xlsFactoryManager
                            .NewSalesXlsManager()
                            .ExportUkDocumentPackageIsVatSaleToXlsx(
                                message.SaleInvoicesFolderPath,
                                saleFromDb,
                                _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                            );
                else
                    (xlsxFile, pdfFile) =
                        _xlsFactoryManager
                            .NewSalesXlsManager()
                            .ExportUkInvoiceIsVatSaleToXlsx(
                                message.SaleInvoicesFolderPath,
                                saleFromDb,
                                _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                            );
            } else {
                if (message.IsFromStorages)
                    (xlsxFile, pdfFile) =
                        _xlsFactoryManager
                            .NewSalesXlsManager()
                            .ExportUkInvoiceFromStorageToXlsx(
                                message.SaleInvoicesFolderPath,
                                saleFromDb,
                                _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                            );
                else
                    (xlsxFile, pdfFile) =
                        _xlsFactoryManager
                            .NewSalesXlsManager()
                            .ExportUkInvoiceToXlsx(
                                message.SaleInvoicesFolderPath,
                                saleFromDb,
                                _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                            );
            }

            Sender.Tell(
                new GenerateSaleInvoiceDocumentResponse(
                    xlsxFile,
                    string.Empty,
                    true,
                    pdfFile
                )
            );
        } else {
            SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleFromDb,
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

            if (!saleFromDb.SaleInvoiceNumberId.HasValue) {
                ISaleInvoiceNumberRepository saleInvoiceNumberRepository = _saleRepositoriesFactory.NewSaleInvoiceNumberRepository(connection);

                string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

                saleFromDb.SaleInvoiceNumber = new SaleInvoiceNumber {
                    Number =
                        string.Format(
                            "{0:D4}",
                            Convert.ToInt64(
                                saleFromDb.SaleNumber.Value.Substring(
                                    saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length,
                                    saleFromDb.SaleNumber.Value.Length - (saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length)
                                )
                            )
                        )
                };

                saleFromDb.SaleInvoiceNumberId = saleInvoiceNumberRepository.Add(saleFromDb.SaleInvoiceNumber);

                saleRepository.UpdateSaleInvoiceNumber(saleFromDb);
            }

            (string xlsxFile, string pdfFile) =
                _xlsFactoryManager
                    .NewSalesXlsManager()
                    .ExportPlInvoiceToXlsx(message.SaleInvoicesFolderPath, saleFromDb);

            Sender.Tell(new GenerateSaleInvoiceDocumentResponse(xlsxFile, string.Empty, true, pdfFile));
        }
    }

    private static void SelectProductLocationHistory(Sale saleFromDb) {
        List<ProductLocation> productLocations = new();
        List<ProductLocationHistory> productLocationsHistory = new();
        foreach (OrderItem orderItem in saleFromDb.Order.OrderItems) {
            foreach (ProductLocation productLocation in orderItem.ProductLocations) {
                if (productLocation.Qty.Equals(0))
                    continue;

                List<ProductLocation> location = orderItem.ProductLocations.Where(x => x.ProductPlacement.StorageNumber == productLocation.ProductPlacement.StorageNumber
                                                                                       && x.ProductPlacement.RowNumber == productLocation.ProductPlacement.RowNumber &&
                                                                                       x.ProductPlacement.CellNumber == productLocation.ProductPlacement.CellNumber).ToList();
                if (location.Any()) {
                    double sumLocation = location.Sum(x => x.Qty);
                    ProductLocation maxQtyLocation = location.First();
                    maxQtyLocation.Qty = sumLocation;
                    List<ProductLocation> g = orderItem.ProductLocations.Where(x => x.ProductPlacement.StorageNumber == productLocation.ProductPlacement.StorageNumber
                                                                                    && x.ProductPlacement.RowNumber == productLocation.ProductPlacement.RowNumber &&
                                                                                    x.ProductPlacement.CellNumber == productLocation.ProductPlacement.CellNumber &&
                                                                                    !x.Id.Equals(maxQtyLocation.Id)).ToList();
                    g.ForEach(x => x.Qty = 0);
                    productLocations.Add(maxQtyLocation);
                }
            }

            orderItem.ProductLocations.Clear();
            orderItem.ProductLocations = productLocations;
            productLocations = new List<ProductLocation>();
            foreach (ProductLocationHistory productLocation in orderItem.ProductLocationsHistory) {
                if (productLocation.Qty.Equals(0))
                    continue;

                List<ProductLocationHistory> location = orderItem.ProductLocationsHistory.Where(x =>
                        x.ProductPlacement.StorageNumber == productLocation.ProductPlacement.StorageNumber
                        && x.ProductPlacement.RowNumber == productLocation.ProductPlacement.RowNumber &&
                        x.ProductPlacement.CellNumber == productLocation.ProductPlacement.CellNumber)
                    .ToList();
                if (location.Any()) {
                    double sumLocation = location.Sum(x => x.Qty);
                    ProductLocationHistory maxQtyLocation = location.First();
                    maxQtyLocation.Qty = sumLocation;
                    List<ProductLocationHistory> g = orderItem.ProductLocationsHistory.Where(x => x.ProductPlacement.StorageNumber == productLocation.ProductPlacement.StorageNumber
                                                                                                  && x.ProductPlacement.RowNumber == productLocation.ProductPlacement.RowNumber &&
                                                                                                  x.ProductPlacement.CellNumber == productLocation.ProductPlacement.CellNumber &&
                                                                                                  !x.Id.Equals(maxQtyLocation.Id)).ToList();
                    g.ForEach(x => x.Qty = 0);
                    productLocationsHistory.Add(productLocation);
                }
            }

            orderItem.ProductLocationsHistory.Clear();
            orderItem.ProductLocationsHistory = productLocationsHistory;
            productLocationsHistory = new List<ProductLocationHistory>();
            foreach (ProductLocation productLocation in orderItem.ProductLocations) {
                List<ProductLocationHistory> locationHistory = orderItem.ProductLocationsHistory.Where(x =>
                        x.ProductPlacement.StorageNumber == productLocation.ProductPlacement.StorageNumber
                        && x.ProductPlacement.RowNumber == productLocation.ProductPlacement.RowNumber &&
                        x.ProductPlacement.CellNumber == productLocation.ProductPlacement.CellNumber)
                    .ToList();
                if (locationHistory.Any()) {
                    double sumLocation = locationHistory.Sum(x => x.Qty);
                    ProductLocationHistory maxQtyLocation = locationHistory.First();
                    maxQtyLocation.Qty = sumLocation;
                    productLocation.Qty += maxQtyLocation.Qty;
                    locationHistory.ForEach(x => x.Qty = 0);
                }
            }
        }
    }

    private void ProcessGetSaleInvoiceDocumentBySaleNetIdMessage(GetSaleInvoiceDocumentBySaleNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Sale saleFromDb = saleRepository.GetByNetIdWithProductLocations(message.NetId);
            if (saleFromDb.Order.OrderItems.Any())
                saleFromDb.Order.OrderItems.ForEach(x => x.ProductLocationsHistory = x.ProductLocationsHistory.Where(x => x.TypeOfMovement == TypeOfMovement.Return).ToList());
            SelectProductLocationHistory(saleFromDb);

            if (saleFromDb == null) {
                Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale with provided NetId does not exists."));
            } else if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale should be in invoice stage."));
            } else if (saleFromDb.SaleInvoiceDocument == null) {
                SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleFromDb,
                    _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

                if (!saleFromDb.SaleInvoiceNumberId.HasValue) {
                    ISaleInvoiceNumberRepository saleInvoiceNumberRepository = _saleRepositoriesFactory.NewSaleInvoiceNumberRepository(connection);

                    string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

                    saleFromDb.SaleInvoiceNumber = new SaleInvoiceNumber {
                        Number =
                            string.Format(
                                "{0:D4}",
                                Convert.ToInt64(
                                    saleFromDb.SaleNumber.Value.Substring(
                                        saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length,
                                        saleFromDb.SaleNumber.Value.Length - (saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length)
                                    )
                                )
                            )
                    };

                    saleFromDb.SaleInvoiceNumberId = saleInvoiceNumberRepository.Add(saleFromDb.SaleInvoiceNumber);

                    saleRepository.UpdateSaleInvoiceNumber(saleFromDb);
                }

                string xlsxFile;
                string pdfFile;

                if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaged))
                    saleFromDb.Order.OrderItems.ForEach(orderItem => {
                        orderItem.Product.CurrentPrice = orderItem.PricePerItem;
                        orderItem.Product.CurrentLocalPrice = orderItem.PricePerItem * orderItem.ExchangeRateAmount;
                    });

                if (saleFromDb.IsVatSale) {
                    if (message.IsFromStorages) {
                        foreach (HistoryInvoiceEdit history in saleFromDb.HistoryInvoiceEdit)
                            if (history.IsPrinted && !history.IsDevelopment)
                                foreach (OrderItemBaseShiftStatus shiftStatus in history.OrderItemBaseShiftStatuses) {
                                    OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(c => c.Id == shiftStatus.OrderItemId);
                                    orderItem.Qty += shiftStatus.Qty;
                                }

                        (xlsxFile, pdfFile) =
                            _xlsFactoryManager
                                .NewSalesXlsManager()
                                .ExportUkDocumentPackageIsVatSaleToXlsx(
                                    message.SaleInvoicesFolderPath,
                                    saleFromDb,
                                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                                );
                    } else {
                        // ��������� ��������
                        (xlsxFile, pdfFile) =
                            _xlsFactoryManager
                                .NewSalesXlsManager()
                                .ExportUkInvoiceIsVatSaleToXlsx(
                                    message.SaleInvoicesFolderPath,
                                    saleFromDb,
                                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                                );
                    }
                } else {
                    if (message.IsFromStorages) {
                        foreach (HistoryInvoiceEdit history in saleFromDb.HistoryInvoiceEdit)
                            if (history.IsPrinted && !history.IsDevelopment)
                                foreach (OrderItemBaseShiftStatus shiftStatus in history.OrderItemBaseShiftStatuses) {
                                    OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(c => c.Id == shiftStatus.OrderItemId);
                                    orderItem.Qty += shiftStatus.Qty;
                                    foreach (ProductLocation productLocation in orderItem.ProductLocations) productLocation.Qty += shiftStatus.Qty;
                                }

                        saleFromDb.Order.OrderItems = saleFromDb.Order.OrderItems.Where(x => x.Qty != 0).ToList();

                        (xlsxFile, pdfFile) =
                            _xlsFactoryManager
                                .NewSalesXlsManager()
                                .ExportUkInvoiceFromStorageToXlsx(
                                    message.SaleInvoicesFolderPath,
                                    saleFromDb,
                                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                                );
                    } else {
                        saleFromDb.HistoryInvoiceEdit
                            .SelectMany(item => item.OrderItemBaseShiftStatuses)
                            .ToList()
                            .ForEach(s => {
                                OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(x => x.Id == s.OrderItemId);
                                orderItem.ProductLocations.ForEach(y => {
                                    y.Qty += s.Qty;
                                });
                                if (orderItem != null) orderItem.Qty += s.Qty;
                            });
                        saleFromDb.HistoryInvoiceEdit.Clear();

                        (xlsxFile, pdfFile) =
                            _xlsFactoryManager
                                .NewSalesXlsManager()
                                .ExportUkInvoiceToXlsx(
                                    message.SaleInvoicesFolderPath,
                                    saleFromDb,
                                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                                );
                    }
                }

                Sender.Tell(
                    new GenerateSaleInvoiceDocumentResponse(
                        xlsxFile,
                        string.Empty,
                        true,
                        pdfFile
                    )
                );
            } else {
                SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleFromDb,
                    _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

                if (!saleFromDb.SaleInvoiceNumberId.HasValue) {
                    ISaleInvoiceNumberRepository saleInvoiceNumberRepository = _saleRepositoriesFactory.NewSaleInvoiceNumberRepository(connection);

                    string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

                    saleFromDb.SaleInvoiceNumber = new SaleInvoiceNumber {
                        Number =
                            string.Format(
                                "{0:D4}",
                                Convert.ToInt64(
                                    saleFromDb.SaleNumber.Value.Substring(
                                        saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length,
                                        saleFromDb.SaleNumber.Value.Length - (saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length)
                                    )
                                )
                            )
                    };

                    saleFromDb.SaleInvoiceNumberId = saleInvoiceNumberRepository.Add(saleFromDb.SaleInvoiceNumber);

                    saleRepository.UpdateSaleInvoiceNumber(saleFromDb);
                }

                (string xlsxFile, string pdfFile) =
                    _xlsFactoryManager
                        .NewSalesXlsManager()
                        .ExportPlInvoiceToXlsx(message.SaleInvoicesFolderPath, saleFromDb);

                Sender.Tell(new GenerateSaleInvoiceDocumentResponse(xlsxFile, string.Empty, true, pdfFile));
            }
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetSaleInvoiceDocumentHistoryBySaleNetIdMessage(GetSaleInvoiceDocumentHistoryBySaleNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IHistoryInvoiceEditRepository historyRepository = _saleRepositoriesFactory.NewHistoryInvoiceEditRepository(connection);
            HistoryInvoiceEdit historyInvoiceEdit = historyRepository.GetByNetId(message.HistoryNetId);
            Sale saleFromDb =
                _saleRepositoriesFactory.NewSaleRepository(connection)
                    .GetByNetIdWithProductLocations(message.NetId);
            List<HistoryInvoiceEdit> earlierHistoryEdits = saleFromDb.HistoryInvoiceEdit
                .Where(h => h.Created > historyInvoiceEdit.Created)
                .ToList();
            historyInvoiceEdit.OrderItemBaseShiftStatuses.ForEach(x => {
                OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(y => y.Id == x.OrderItemId);
                if (orderItem != null) {
                    orderItem.Qty = x.CurrentQty;
                    orderItem.ProductLocationsHistory = orderItem.ProductLocationsHistory.Where(x => x.TypeOfMovement == TypeOfMovement.Return).ToList();
                }

                //orderItem.ProductLocations.ForEach(y => {
                //    y.Qty = x.CurrentQty;
                //});
            });
            foreach (HistoryInvoiceEdit history in earlierHistoryEdits)
            foreach (ProductLocationHistory item in history.ProductLocationHistory) {
                OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
                orderItem.ProductLocationsHistory.Add(item);
            }


            SelectProductLocationHistory(saleFromDb);

            if (saleFromDb == null) {
                Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale with provided NetId does not exists."));
            } else if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale should be in invoice stage."));
            } else if (saleFromDb.SaleInvoiceDocument == null) {
                SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleFromDb,
                    _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

                if (!saleFromDb.SaleInvoiceNumberId.HasValue) {
                    ISaleInvoiceNumberRepository saleInvoiceNumberRepository = _saleRepositoriesFactory.NewSaleInvoiceNumberRepository(connection);

                    string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

                    saleFromDb.SaleInvoiceNumber = new SaleInvoiceNumber {
                        Number =
                            string.Format(
                                "{0:D4}",
                                Convert.ToInt64(
                                    saleFromDb.SaleNumber.Value.Substring(
                                        saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length,
                                        saleFromDb.SaleNumber.Value.Length - (saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length)
                                    )
                                )
                            )
                    };

                    saleFromDb.SaleInvoiceNumberId = saleInvoiceNumberRepository.Add(saleFromDb.SaleInvoiceNumber);

                    saleRepository.UpdateSaleInvoiceNumber(saleFromDb);
                }

                string xlsxFile;
                string pdfFile;

                if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaged))
                    saleFromDb.Order.OrderItems.ForEach(orderItem => {
                        orderItem.Product.CurrentPrice = orderItem.PricePerItem;
                        orderItem.Product.CurrentLocalPrice = orderItem.PricePerItem * orderItem.ExchangeRateAmount;
                    });

                if (saleFromDb.IsVatSale) {
                    if (message.IsFromStorages)
                        (xlsxFile, pdfFile) =
                            _xlsFactoryManager
                                .NewSalesXlsManager()
                                .ExportUkDocumentPackageIsVatSaleToXlsx(
                                    message.SaleInvoicesFolderPath,
                                    saleFromDb,
                                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                                );
                    else
                        (xlsxFile, pdfFile) =
                            _xlsFactoryManager
                                .NewSalesXlsManager()
                                .ExportUkInvoiceIsVatSaleToXlsx(
                                    message.SaleInvoicesFolderPath,
                                    saleFromDb,
                                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                                );
                } else {
                    if (message.IsFromStorages)
                        (xlsxFile, pdfFile) =
                            _xlsFactoryManager
                                .NewSalesXlsManager()
                                .ExportUkInvoiceFromStorageToXlsx(
                                    message.SaleInvoicesFolderPath,
                                    saleFromDb,
                                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                                );
                    else
                        (xlsxFile, pdfFile) =
                            _xlsFactoryManager
                                .NewSalesXlsManager()
                                .ExportUkInvoiceToXlsx(
                                    message.SaleInvoicesFolderPath,
                                    saleFromDb,
                                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                                );
                }

                Sender.Tell(
                    new GenerateSaleInvoiceDocumentResponse(
                        xlsxFile,
                        string.Empty,
                        true,
                        pdfFile
                    )
                );
            } else {
                SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleFromDb,
                    _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

                if (!saleFromDb.SaleInvoiceNumberId.HasValue) {
                    ISaleInvoiceNumberRepository saleInvoiceNumberRepository = _saleRepositoriesFactory.NewSaleInvoiceNumberRepository(connection);

                    string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

                    saleFromDb.SaleInvoiceNumber = new SaleInvoiceNumber {
                        Number =
                            string.Format(
                                "{0:D4}",
                                Convert.ToInt64(
                                    saleFromDb.SaleNumber.Value.Substring(
                                        saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length,
                                        saleFromDb.SaleNumber.Value.Length - (saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length)
                                    )
                                )
                            )
                    };

                    saleFromDb.SaleInvoiceNumberId = saleInvoiceNumberRepository.Add(saleFromDb.SaleInvoiceNumber);

                    saleRepository.UpdateSaleInvoiceNumber(saleFromDb);
                }

                (string xlsxFile, string pdfFile) =
                    _xlsFactoryManager
                        .NewSalesXlsManager()
                        .ExportPlInvoiceToXlsx(message.SaleInvoicesFolderPath, saleFromDb);

                Sender.Tell(new GenerateSaleInvoiceDocumentResponse(xlsxFile, string.Empty, true, pdfFile));
            }

            //1163
            //if (saleFromDb == null) {
            //    Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale with provided NetId does not exists."));
            //} else if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
            //    Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale should be in invoice stage."));
            //} else if (saleFromDb.SaleInvoiceDocument == null) {
            //    SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleFromDb,
            //        _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

            //    if (saleFromDb == null) {
            //        Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale with provided NetId does not exists."));
            //    } else if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
            //        Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale should be in invoice stage."));
            //    } else if (saleFromDb.SaleInvoiceDocument == null) {
            //        SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleFromDb,
            //            _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

            //        if (!saleFromDb.SaleInvoiceNumberId.HasValue) {
            //            ISaleInvoiceNumberRepository saleInvoiceNumberRepository = _saleRepositoriesFactory.NewSaleInvoiceNumberRepository(connection);

            //            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            //            saleFromDb.SaleInvoiceNumber = new SaleInvoiceNumber {
            //                Number =
            //                    string.Format(
            //                        "{0:D4}",
            //                        Convert.ToInt64(
            //                            saleFromDb.SaleNumber.Value.Substring(
            //                                saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length,
            //                                saleFromDb.SaleNumber.Value.Length - (saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length)
            //                            )
            //                        )
            //                    )
            //            };

            //            saleFromDb.SaleInvoiceNumberId = saleInvoiceNumberRepository.Add(saleFromDb.SaleInvoiceNumber);

            //            saleRepository.UpdateSaleInvoiceNumber(saleFromDb);
            //        }

            //        string xlsxFile;
            //        string pdfFile;

            //        if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaged)) {
            //            saleFromDb.Order.OrderItems.ForEach(orderItem => {
            //                orderItem.Product.CurrentPrice = orderItem.PricePerItem;
            //                orderItem.Product.CurrentLocalPrice = orderItem.PricePerItem * orderItem.ExchangeRateAmount;
            //            });
            //        }
            //        if (saleFromDb.IsVatSale) {
            //            (xlsxFile, pdfFile) =
            //                       _xlsFactoryManager
            //                           .NewSalesXlsManager()
            //                           .ExportUkInvoiceFromStorageToXlsxFromSale(
            //                               message.SaleInvoicesFolderPath,
            //                               saleFromDb,
            //                               _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
            //                           );
            //        } else {
            //            (xlsxFile, pdfFile) =
            //                   _xlsFactoryManager
            //                       .NewSalesXlsManager()
            //                       .ExportUkInvoiceFromStorageToXlsx(
            //                           message.SaleInvoicesFolderPath,
            //                           saleFromDb,
            //                           _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
            //                       );
            //        }
            //        Sender.Tell(
            //            new GenerateSaleInvoiceDocumentResponse(
            //                xlsxFile,
            //                string.Empty,
            //                true,
            //                pdfFile
            //            )
            //        );
            //    } else {
            //        SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleFromDb,
            //            _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

            //        if (!saleFromDb.SaleInvoiceNumberId.HasValue) {
            //            ISaleInvoiceNumberRepository saleInvoiceNumberRepository = _saleRepositoriesFactory.NewSaleInvoiceNumberRepository(connection);

            //            string currentMonth = MonthCodesResourceNames.GetCurrentMonthCode();

            //            saleFromDb.SaleInvoiceNumber = new SaleInvoiceNumber {
            //                Number =
            //                    string.Format(
            //                        "{0:D4}",
            //                        Convert.ToInt64(
            //                            saleFromDb.SaleNumber.Value.Substring(
            //                                saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length,
            //                                saleFromDb.SaleNumber.Value.Length - (saleFromDb.ClientAgreement.Agreement.Organization.Code.Length + currentMonth.Length)
            //                            )
            //                        )
            //                    )
            //            };

            //            saleFromDb.SaleInvoiceNumberId = saleInvoiceNumberRepository.Add(saleFromDb.SaleInvoiceNumber);

            //            saleRepository.UpdateSaleInvoiceNumber(saleFromDb);
            //        }

            //        (string xlsxFile, string pdfFile) =
            //            _xlsFactoryManager
            //                .NewSalesXlsManager()
            //                .ExportPlInvoiceToXlsx(message.SaleInvoicesFolderPath, saleFromDb);

            //        Sender.Tell(new GenerateSaleInvoiceDocumentResponse(xlsxFile, string.Empty, true, pdfFile));
            //    }
            //}
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetSaleInvoicePzDocumentBySaleNetIdMessage(GetSaleInvoicePzDocumentBySaleNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        Sale saleFromDb = saleRepository.GetByNetIdWithProductLocations(message.NetId);

        if (saleFromDb == null) {
            Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale with provided NetId does not exists."));
        } else if (saleFromDb.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
            Sender.Tell(new GenerateSaleInvoiceDocumentResponse(string.Empty, "Sale should be in invoice stage."));
        } else {
            SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(saleFromDb,
                _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

            saleRepository.UpdateIsPrintedPaymentInvoice(saleFromDb.Id);

            (string xlsxFile, string pdfFile) =
                _xlsFactoryManager
                    .NewSalesXlsManager()
                    .ExportPlInvoicePzToXlsx(message.SaleInvoicesFolderPath, saleFromDb);

            Sender.Tell(new GenerateSaleInvoiceDocumentResponse(xlsxFile, string.Empty, true, pdfFile));
        }
    }

    private void ProcessGetSaleStatisticWithResourceNameByNetIdMessage(GetSaleStatisticWithResourceNameByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        Sale saleFromDb = saleRepository.GetByNetId(message.SaleNetId);

        SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(saleFromDb, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
            _currencyRepositoriesFactory.NewCurrencyRepository(connection));

        dynamic[] toReturnData = new dynamic[4];

        SaleActorsHelpers.FormLifeCycleLine(saleRepository, saleFromDb.NetUid, toReturnData);

        List<SaleExchangeRate> saleExchangeRates = _saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).GetAllBySaleNetId(saleFromDb.NetUid);

        SaleStatistic saleStatistic = new() {
            Sale = saleFromDb,
            LifeCycleLine = toReturnData.ToList(),
            SaleExchangeRates = saleExchangeRates
        };

        Sender.Tell(new Tuple<SaleStatistic, string>(saleStatistic, message.SaleResourceName));

        if (message.PushCreatedNotification)
            ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR).Tell(new AddedSaleNotificationMessage(saleFromDb.NetUid));

        if (message.PushUpdatedNotification)
            ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR).Tell(new UpdatedSaleNotificationMessage(saleFromDb.NetUid));
    }

    private void ProcessGetOrderItemAndSaleStatisticMessage(GetOrderItemAndSaleStatisticMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IActorRef hubsActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        Currency uah = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetUAHCurrencyIfExists();

        decimal currentExchangeRateEurToUah = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetExchangeRateToEuroCurrency(uah);

        Sale saleFromDb = saleRepository.GetByNetId(message.SaleNetId);

        OrderItem orderItem = saleFromDb.Order.OrderItems.First(i => i.Id.Equals(message.OrderItemId));

        hubsActorRef.Tell(new UpdatedSaleNotificationMessage(message.SaleNetId));

        hubsActorRef.Tell(new GetProductNotificationMessage(orderItem.ProductId, saleFromDb.ClientAgreement.NetUid));

        orderItem =
            _saleRepositoriesFactory
                .NewOrderItemRepository(connection)
                .GetWithCalculatedProductPrices(
                    orderItem.NetUid,
                    saleFromDb.ClientAgreement.NetUid,
                    saleFromDb.ClientAgreement.Agreement.OrganizationId ?? 0,
                    saleFromDb.IsVatSale,
                    orderItem.IsFromReSale
                );

        foreach (OrderItem item in saleFromDb.Order.OrderItems)
            if (item.Id.Equals(orderItem.Id) && item.Discount != 0)
                orderItem.Discount = item.Discount;

        if (!orderItem.PricePerItem.Equals(decimal.Zero)) {
            orderItem.Product.CurrentPrice = orderItem.PricePerItem;
            orderItem.Product.CurrentLocalPrice = orderItem.Product.CurrentPrice * orderItem.ExchangeRateAmount;
        }

        orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;

        orderItem.TotalAmount =
            decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
        orderItem.TotalAmountLocal =
            decimal.Round(orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
        orderItem.TotalAmountEurToUah =
            decimal.Round(orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);

        orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);

        orderItem.Product.AvailableQtyPl =
            orderItem
                .Product
                .ProductAvailabilities
                .Where(a => a.Storage.Locale.ToLower().Equals("pl") && !a.Storage.ForDefective && !a.Storage.ForVatProducts)
                .Sum(a => a.Amount);
        orderItem.Product.AvailableQtyUk =
            orderItem
                .Product
                .ProductAvailabilities
                .Where(a => a.Storage.Locale.ToLower().Equals("uk") && !a.Storage.ForDefective &&
                            (!a.Storage.ForVatProducts || a.Storage.AvailableForReSale))
                .Sum(a => a.Amount);
        orderItem.Product.AvailableQtyPlVAT =
            orderItem
                .Product
                .ProductAvailabilities
                .Where(a => a.Storage.Locale.ToLower().Equals("pl") && !a.Storage.ForDefective && a.Storage.ForVatProducts)
                .Sum(a => a.Amount);
        orderItem.Product.AvailableQtyUkVAT =
            orderItem
                .Product
                .ProductAvailabilities
                .Where(a => a.Storage.Locale.ToLower().Equals("uk") && !a.Storage.ForDefective && a.Storage.ForVatProducts)
                .Sum(a => a.Amount);

        Sender.Tell(orderItem);
    }

    private void ProcessGetOrderItemAndSaleStatisticsMessage(GetOrderItemAndSaleStatisticsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IActorRef hubsActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        Sale saleFrom = saleRepository.GetByNetId(message.SaleFrom);
        Sale saleTo = saleRepository.GetByNetId(message.SaleTo);

        hubsActorRef.Tell(new UpdatedSaleNotificationMessage(saleFrom.NetUid));

        if (message.IsNewToSale)
            hubsActorRef.Tell(new AddedSaleNotificationMessage(saleTo.NetUid));
        else
            hubsActorRef.Tell(new UpdatedSaleNotificationMessage(saleTo.NetUid));

        if (saleTo.ClientAgreement.Agreement.OrganizationId != null) {
            OrderItem orderItem = _saleRepositoriesFactory.NewOrderItemRepository(connection)
                .GetBySaleNetIdAndOrderItemId(message.SaleTo, message.OrderItemId, (long)saleTo.ClientAgreement.Agreement.OrganizationId);

            SaleExchangeRate saleExchangeRate = _saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).GetEuroSaleExchangeRateBySaleNetId(saleFrom.NetUid);

            if (saleExchangeRate != null) {
                orderItem.Product.CurrentLocalPrice = Math.Round(orderItem.Product.CurrentPrice * saleExchangeRate.Value, 14);
                orderItem.TotalAmountLocal = Math.Round(orderItem.TotalAmount * saleExchangeRate.Value, 14);
            } else {
                ExchangeRate exchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

                orderItem.Product.CurrentLocalPrice = Math.Round(orderItem.Product.CurrentPrice * exchangeRate.Amount, 14);
                orderItem.TotalAmountLocal = Math.Round(orderItem.TotalAmount * exchangeRate.Amount, 14);
            }

            Sender.Tell(new Tuple<OrderItem, string>(orderItem, string.Empty));
        } else {
            Sender.Tell(new Tuple<OrderItem, string>(null, string.Empty));
        }
    }

    private void ProcessGetOrderItemAndSaleStatisticAndIsNewSaleMessage(GetOrderItemAndSaleStatisticAndIsNewSaleMessage message) {
        IActorRef hubsActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

        Sale sale;

        using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
            sale = _saleRepositoriesFactory.NewSaleRepository(connection).GetByNetIdWithAgreementOnly(message.SaleNetId);

            if (sale != null)
                if (message.IsNewSale)
                    hubsActorRef.Tell(new AddedSaleNotificationMessage(message.SaleNetId));
                else
                    hubsActorRef.Tell(new UpdatedSaleNotificationMessage(message.SaleNetId));

            hubsActorRef.Tell(new GetProductNotificationMessage(message.OrderItem.ProductId, sale?.ClientAgreement?.NetUid ?? Guid.Empty));
        }

        Sender.Tell(new Tuple<OrderItem, string>(message.OrderItem, message.ErrorMessage));

        if (sale == null) return;

        if (message.IsNewSale)
            hubsActorRef.Tell(new AddedSaleNotificationMessage(message.SaleNetId));
        else
            hubsActorRef.Tell(new UpdatedSaleNotificationMessage(message.SaleNetId));
    }

    private void ProcessGetSaleByNetIdWithShiftedItemsDocumentMessage(GetSaleByNetIdWithShiftedItemsDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);

        Sale saleFromDb = saleRepository.GetByNetIdWithProductLocations(message.NetId);


        (string xlsxFile, string pdfFile) =
            _xlsFactoryManager
                .NewSalesXlsManager()
                .ExportUkSaleByNetIdWithShiftedItemsToXlsx(
                    message.SaleInvoicesFolderPath,
                    saleFromDb,
                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                );

        Sender.Tell(new Tuple<string, string>(xlsxFile, pdfFile));
    }

    private void ProcessGetSaleByNetIdWithShiftedItemsHistoryDocumentMessage(GetSaleByNetIdWithShiftedItemsHistoryDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        IHistoryInvoiceEditRepository historyRepository = _saleRepositoriesFactory.NewHistoryInvoiceEditRepository(connection);

        Sale saleFromDb = saleRepository.GetByNetIdWithProductLocations(message.NetId);
        HistoryInvoiceEdit historyInvoiceEdit = historyRepository.GetByNetId(message.HistoryNetId);
        saleFromDb.HistoryInvoiceEdit = saleFromDb.HistoryInvoiceEdit.Where(x => x.Id == historyInvoiceEdit.Id).ToList();
        //saleFromDb.HistoryInvoiceEdit.Clear();
        //saleFromDb.HistoryInvoiceEdit.Add(historyInvoiceEdit);

        saleFromDb.Order.OrderItems.ForEach(x => {
            x.ProductLocationsHistory.Clear();
        });

        foreach (HistoryInvoiceEdit history in saleFromDb.HistoryInvoiceEdit)
        foreach (ProductLocationHistory item in history.ProductLocationHistory) {
            OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
            orderItem.ProductLocationsHistory.Add(item);
        }

        historyInvoiceEdit.OrderItemBaseShiftStatuses.ForEach(x => {
            OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(y => y.Id == x.OrderItemId);
            orderItem.ShiftStatuses.Add(x);
            orderItem.Qty = x.CurrentQty;
            orderItem.ProductLocations.ForEach(y => {
                y.Qty = x.CurrentQty;
            });
        });
        (string xlsxFile, string pdfFile) =
            _xlsFactoryManager
                .NewSalesXlsManager()
                .ExportUkSaleByNetIdWithShiftedItemsToXlsx(
                    message.SaleInvoicesFolderPath,
                    saleFromDb,
                    _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")
                );

        Sender.Tell(new Tuple<string, string>(xlsxFile, pdfFile));
    }

    private void ProcessGetSaleByNetIdWithShiftedItemsMessage(GetSaleByNetIdWithShiftedItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        Sale saleFromDb = saleRepository.GetByNetIdWithShiftedItems(message.NetId);

        SaleActorsHelpers.CalculatePricingsForSale(saleFromDb, connection, _exchangeRateRepositoriesFactory, _saleRepositoriesFactory);

        dynamic[] toReturnData = new dynamic[LifeCycleLineStatuses.STATUSES.Length];

        SaleActorsHelpers.FormLifeCycleLine(saleRepository, saleFromDb.NetUid, toReturnData);

        List<SaleExchangeRate> saleExchangeRates = _saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).GetAllBySaleNetId(saleFromDb.NetUid);

        SaleStatistic saleStatistic = new() {
            Sale = saleFromDb,
            LifeCycleLine = toReturnData.ToList(),
            SaleExchangeRates = saleExchangeRates
        };

        Sender.Tell(saleStatistic);
    }

    private void ProcessGetTotalsBySalesManagersMessage(GetTotalsBySalesManagersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<User> managers = _userRepositoriesFactory.NewUserRepository(connection).GetAllSalesManagers();

        List<Sale> sales = _saleRepositoriesFactory.NewSaleRepository(connection).GetAllByUserIds(managers.Select(m => m.Id));

        List<TotalBySalesManagers> totals = new();

        foreach (User manager in managers) {
            TotalBySalesManagers total = new() {
                LastName = manager.LastName,
                TotalSalesCount = sales.Count(s => s.UserId.Equals(manager.Id))
            };

            if (sales.Any(s => s.UserId.Equals(manager.Id)))
                foreach (Sale sale in sales.Where(s => s.UserId.Equals(manager.Id)).ToArray())
                foreach (OrderItem orderItem in sale.Order.OrderItems)
                    if (sale.ClientAgreement?.Agreement?.Pricing?.BasePricing != null) {
                        if (orderItem.Product.ProductPricings.Any(p => p.PricingId.Equals(sale.ClientAgreement.Agreement.Pricing.BasePricing.Id)))
                            total.TotalSalesAmount += orderItem.Product.ProductPricings.First().Price + orderItem.Product.ProductPricings.First().Price *
                                Convert.ToDecimal(sale.ClientAgreement.Agreement.Pricing.ExtraCharge) / 100;
                        else
                            total.TotalSalesAmount += orderItem.Product.ProductPricings.First().Price;
                    } else {
                        total.TotalSalesAmount += orderItem.Product.ProductPricings.First().Price;
                    }

            total.TotalSalesAmount = Math.Round(total.TotalSalesAmount, 14);

            totals.Add(total);
        }

        Sender.Tell(totals.OrderByDescending(t => t.TotalSalesAmount).ThenByDescending(t => t.TotalSalesCount));
    }

    private void ProcessGetTotalForSalesByYearMessage(GetTotalForSalesByYearMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory.NewSaleRepository(connection).GetTotalForSalesByYear(message.ClientNetId));
    }

    private void ProcessGetCurrentSaleByClientAgreementOrSaleNetIdMessage(GetCurrentSaleByClientAgreementOrSaleNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

        ClientAgreement clientAgreement = _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByNetIdWithoutIncludes(message.NetId);

        if (clientAgreement != null) {
            Sale currentSale = saleRepository.GetLastNewSaleByClientAgreementNetId(message.NetId);

            if (currentSale != null) {
                Sale saleToReturn = saleRepository.GetByNetId(currentSale.NetUid);

                SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(
                    saleToReturn,
                    _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                    currencyRepository
                );

                Sender.Tell(saleToReturn);
            } else {
                Sender.Tell(null);
            }
        } else {
            Sale currentSale = saleRepository.GetByNetId(message.NetId);

            if (currentSale != null) {
                SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(
                    currentSale,
                    _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                    currencyRepository
                );

                Sender.Tell(currentSale);
            } else {
                Sender.Tell(null);
            }
        }
    }

    private void ProcessGetCurrentNotMergedSaleByClientAgreementMessage(GetCurrentNotMergedSaleByClientAgreementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        ClientAgreement clientAgreement = _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByNetIdWithoutIncludes(message.NetId);

        if (clientAgreement != null) {
            Sale currentSale = saleRepository.GetLastNotMergedNewSaleByClientAgreementNetId(message.NetId);

            if (currentSale != null) {
                Sale saleToReturn = saleRepository.GetByNetId(currentSale.NetUid);

                SaleActorsHelpers.CalculatePricingsForSale(
                    saleToReturn,
                    connection,
                    _exchangeRateRepositoriesFactory,
                    _saleRepositoriesFactory);

                Sender.Tell(saleToReturn);
            } else {
                Sender.Tell(null);
            }
        } else {
            Sender.Tell(null);
        }
    }

    private void ProcessGetSalesStatisticByDateRangeAndUserNetIdMessage(GetSalesStatisticByDateRangeAndUserNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (!message.From.HasValue || message.From.Value.Year.Equals(1)) message.From = DateTime.Now.Date;

        if (!message.To.HasValue || message.To.Value.Year.Equals(1))
            message.To = DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
        else
            message.To = message.To.Value.AddHours(23).AddMinutes(59).AddSeconds(59);

        User manager = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

        Sender.Tell(
            _saleRepositoriesFactory
                .NewSaleRepository(connection)
                .GetSaleStatisticsByManagerRanged(manager.Id, message.From.Value, message.To.Value)
        );
    }

    private void ProcessGetSaleMergeStatistic(GetSaleMergeStatistic message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
            ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

            Sale saleFromDb = saleRepository.GetByNetId(message.SaleNetId);

            SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(saleFromDb, exchangeRateRepository, currencyRepository);

            List<SaleMerged> salesMerged = new();

            if (saleFromDb.InputSaleMerges.Any())
                AddCalculatedInputSales(saleRepository, exchangeRateRepository, saleFromDb, salesMerged, currencyRepository);

            saleFromDb.InputSaleMerges = salesMerged;

            List<SaleMerged> mergedSalesToRemove =
                saleFromDb
                    .InputSaleMerges
                    .Where(saleMerged => saleMerged.InputSale == null || !saleMerged.InputSale.Order.OrderItems.Any())
                    .ToList();

            mergedSalesToRemove.ForEach(merged => saleFromDb.InputSaleMerges.Remove(merged));

            Sender.Tell(saleFromDb);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetSaleMergeStatisticWithOrderItemsMerged(GetSaleMergeStatisticWithOrderItemsMerged message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Sale saleFromDb = saleRepository.GetByNetIdWithSaleMergedAndOrderItemsMerged(message.NetId);

            List<SaleMerged> salesMerged = new();

            if (saleFromDb.InputSaleMerges.Any())
                AddInputSales(saleRepository, saleFromDb, salesMerged);

            saleFromDb.InputSaleMerges = salesMerged;

            List<SaleMerged> mergedSalesToRemove =
                saleFromDb
                    .InputSaleMerges
                    .Where(saleMerged => saleMerged.InputSale == null || !saleMerged.InputSale.Order.OrderItems.Any())
                    .ToList();

            mergedSalesToRemove.ForEach(merged => saleFromDb.InputSaleMerges.Remove(merged));

            Sender.Tell(saleFromDb);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessCalculateSaleWithOneTimeDiscountsMessage(CalculateSaleWithOneTimeDiscountsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.Sale != null && !message.Sale.NetUid.Equals(Guid.Empty)) {
            Sale sale = _saleRepositoriesFactory.NewSaleRepository(connection).GetByNetId(message.Sale.NetUid);

            if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                ExchangeRate euroExchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    if (message.Sale.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id)))
                        orderItem.OneTimeDiscount = message.Sale.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).OneTimeDiscount;

                    orderItem.Product.CurrentPrice =
                        decimal.Round(
                            orderItem.Product.CurrentPrice - orderItem.Product.CurrentPrice * orderItem.OneTimeDiscount / 100,
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(
                            orderItem.Product.CurrentPrice * euroExchangeRate.Amount,
                            4,
                            MidpointRounding.AwayFromZero
                        );

                    orderItem.TotalAmount =
                        decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal =
                        orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                    orderItem.Product.CurrentPrice =
                        decimal.Round(orderItem.Product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);
                }

                sale.Order.TotalAmount =
                    decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 14, MidpointRounding.AwayFromZero);
                sale.Order.TotalAmountLocal =
                    decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 14, MidpointRounding.AwayFromZero);
                sale.Order.TotalCount =
                    sale.Order.OrderItems.Sum(o => o.Qty);

                sale.TotalAmount = sale.Order.TotalAmount;
                sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
                sale.TotalCount = sale.Order.TotalCount;
            } else {
                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    if (message.Sale.Order.OrderItems.Any(i => i.Id.Equals(orderItem.Id)))
                        orderItem.OneTimeDiscount = message.Sale.Order.OrderItems.First(i => i.Id.Equals(orderItem.Id)).OneTimeDiscount;

                    orderItem.Product.CurrentPrice =
                        decimal.Round(orderItem.Product.CurrentPrice - orderItem.Product.CurrentPrice * orderItem.OneTimeDiscount / 100, 14,
                            MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.Product.CurrentPrice * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount =
                        decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal =
                        orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                    orderItem.Product.CurrentPrice =
                        decimal.Round(orderItem.Product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);
                }

                sale.Order.TotalAmount =
                    decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 14, MidpointRounding.AwayFromZero);
                sale.Order.TotalAmountLocal =
                    decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 14, MidpointRounding.AwayFromZero);
                sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

                sale.TotalAmount = sale.Order.TotalAmount;
                sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
                sale.TotalCount = sale.Order.TotalCount;
            }

            Sender.Tell(sale);
        } else {
            Sender.Tell(message.Sale);
        }
    }

    private void ProcessGetAllSalesFromECommerceFromPlUkClientsMessage(GetAllSalesFromECommerceFromPlUkClientsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<Sale> sales =
            _saleRepositoriesFactory
                .NewSaleRepository(connection)
                .GetAllSalesFromECommerceFromPlUkClients();

        SaleActorsHelpers.CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
            _currencyRepositoriesFactory.NewCurrencyRepository(connection));

        Sender.Tell(sales);
    }

    private void ProcessGetOrderItemsWithProductLocationBySaleNetIdMessage(GetOrderItemsWithProductLocationBySaleNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _saleRepositoriesFactory
                .NewOrderItemRepository(connection)
                .GetAllBySaleNetIdWithProductLocation(
                    message.NetId
                )
        );
    }

    private void ProcessGetAllSalesFromPlUkGroupedByClientFilteredMessage(GetAllSalesFromPlUkGroupedByClientFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        List<Sale> sales =
            saleRepository
                .GetAllUkPlClientsSalesFiltered(
                    message.From,
                    message.To,
                    message.Value
                );

        SaleActorsHelpers.CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
            _currencyRepositoriesFactory.NewCurrencyRepository(connection));

        Sender.Tell(sales);
    }

    private void ProcessGetAllSalesForSaleReturnsFromSearchMessage(GetAllSalesForSaleReturnsFromSearchMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            List<Sale> sales =
                _saleRepositoriesFactory
                    .NewSaleRepository(connection)
                    .GetAllSalesForReturnsFromSearch(
                        message.From,
                        message.To,
                        SpecialCharactersReplace.Replace(message.Value, string.Empty),
                        message.ClientNetId,
                        message.OrganizationNetId
                    );

            SaleActorsHelpers.CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                _currencyRepositoriesFactory.NewCurrencyRepository(connection));

            Sender.Tell(sales);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUnlockVatSaleByNetIdMessage(UnlockVatSaleByNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            Sale saleFromDb = saleRepository.GetByNetId(message.SaleNetId);

            if (saleFromDb != null) {
                saleFromDb.IsLocked = false;

                saleRepository.UpdateLockInfo(saleFromDb);
                saleRepository.UpdateIsAcceptedToPacking(saleFromDb.Id, true);

                saleFromDb = saleRepository.GetByNetId(message.SaleNetId);

                Sender.Tell(saleFromDb);

                ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR).Tell(new UpdatedSaleNotificationMessage(saleFromDb.NetUid));
            } else {
                Sender.Tell(null);
            }
        } catch (UserForbidenException exc) {
            Sender.Tell(exc);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetChartInfoSalesByClientMessage(GetChartInfoSalesByClientMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            long clientId = _clientRepositoriesFactory.NewClientRepository(connection).GetIdByNetId(message.NetId);

            Dictionary<DateTime, decimal?> toReturn = _saleRepositoriesFactory
                .NewOrderItemRepository(connection)
                .GetChartInfoSalesByClient(
                    message.From,
                    message.To,
                    clientId,
                    message.TypePeriod);

            Sender.Tell(toReturn);
        } catch (Exception ex) {
            Sender.Tell(ex);
        }
    }

    private void ProcessGetInfoAboutSalesMessage(GetInfoAboutSalesMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            long? managerId = null;
            long? organizationId = null;

            if (message.NetIdOrganization.HasValue)
                organizationId = _organizationRepositoriesFactory.NewOrganizationRepository(connection).GetOrganizationIdByNetId(message.NetIdOrganization.Value);

            if (message.ForMySales)
                managerId = _userRepositoriesFactory.NewUserRepository(connection).GetUserIdByNetId(message.UserNetId);
            else if (message.NetIdManager.HasValue)
                managerId = _userRepositoriesFactory.NewUserRepository(connection).GetUserIdByNetId(message.NetIdManager.Value);

            InfoAboutSalesModel toReturn = _saleRepositoriesFactory
                .NewOrderItemRepository(connection)
                .GetInfoAboutSales(
                    message.From,
                    message.To,
                    managerId,
                    organizationId);

            if (toReturn == null) Sender.Tell(null);

            Sender.Tell(toReturn);
        } catch (Exception ex) {
            Sender.Tell(ex);
        }
    }

    private void ProcessGetManagersSalesByProductTopMessage(GetManagersSalesByProductTopMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            AllProductsSaleManagersModel toReturn = _saleRepositoriesFactory
                .NewOrderItemRepository(connection)
                .GetManagersProductSalesByTop(
                    message.From,
                    message.To,
                    message.TypeProductTop);

            Sender.Tell(toReturn);
        } catch (Exception ex) {
            Sender.Tell(ex);
        }
    }

    private void ProcessShipmentListForSalePrintDocuments(ShipmentListForSalePrintDocumentsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sale saleFromDb =
                _saleRepositoriesFactory.NewSaleRepository(connection)
                    .GetByNetIdWithProductLocations(message.NetId);
            saleFromDb.HistoryInvoiceEdit
                .SelectMany(item => item.OrderItemBaseShiftStatuses)
                .ToList()
                .ForEach(s => {
                    OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(x => x.Id == s.OrderItemId);
                    //orderItem.ProductLocations.ForEach(y => {
                    //    y.Qty += s.Qty;
                    //});
                    if (orderItem != null) {
                        orderItem.Qty += s.Qty;
                        orderItem.ProductLocationsHistory = orderItem.ProductLocationsHistory.Where(x => x.TypeOfMovement == TypeOfMovement.Return).ToList();
                    }
                });

            foreach (HistoryInvoiceEdit history in saleFromDb.HistoryInvoiceEdit)
            foreach (ProductLocationHistory item in history.ProductLocationHistory) {
                OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);

                orderItem.ProductLocationsHistory.Add(item);
            }

            SelectProductLocationHistory(saleFromDb);

            saleFromDb.HistoryInvoiceEdit.Clear();
            Sender.Tell(
                _xlsFactoryManager
                    .NewSalesXlsManager()
                    .ExportShipmentListForSale(message.PathToFolder, saleFromDb,
                        _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")));
        } catch {
            Sender.Tell((string.Empty, string.Empty));
        }
    }

    private void ProcessShipmentListForSalePrintDocumentsHistory(ShipmentListForSalePrintDocumentsHistoryMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IHistoryInvoiceEditRepository historyRepository = _saleRepositoriesFactory.NewHistoryInvoiceEditRepository(connection);
            HistoryInvoiceEdit historyInvoiceEdit = historyRepository.GetByNetId(message.HistoryNetId);
            Sale saleFromDb =
                _saleRepositoriesFactory.NewSaleRepository(connection)
                    .GetByNetIdWithProductLocations(message.NetId);
            historyInvoiceEdit.OrderItemBaseShiftStatuses.ForEach(x => {
                OrderItem orderItem = saleFromDb.Order.OrderItems.FirstOrDefault(y => y.Id == x.OrderItemId);
                if (orderItem != null) {
                    orderItem.Qty = x.CurrentQty;
                    orderItem.ProductLocationsHistory = orderItem.ProductLocationsHistory.Where(x => x.TypeOfMovement == TypeOfMovement.Return).ToList();
                }
            });
            SelectProductLocationHistory(saleFromDb);

            Sender.Tell(
                _xlsFactoryManager
                    .NewSalesXlsManager()
                    .ExportShipmentListForSale(message.PathToFolder, saleFromDb,
                        _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk")));
        } catch {
            Sender.Tell((string.Empty, string.Empty));
        }
    }

    private static void AddCalculatedInputSales(
        ISaleRepository saleRepository,
        IExchangeRateRepository exchangeRateRepository,
        Sale saleFromDb,
        ICollection<SaleMerged> salesMerged,
        ICurrencyRepository currencyRepository) {
        foreach (SaleMerged saleMerged in saleFromDb.InputSaleMerges) {
            saleMerged.InputSale = saleRepository.GetByIdWithAdditionalIncludes(saleMerged.InputSaleId);

            SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(saleMerged.InputSale, exchangeRateRepository, currencyRepository);

            salesMerged.Add(saleMerged);

            if (saleMerged.InputSale.InputSaleMerges.Any())
                AddCalculatedInputSales(
                    saleRepository,
                    exchangeRateRepository,
                    saleMerged.InputSale,
                    salesMerged,
                    currencyRepository);
        }
    }

    private static void AddInputSales(
        ISaleRepository saleRepository,
        Sale saleFromDb,
        ICollection<SaleMerged> salesMerged) {
        foreach (SaleMerged saleMerged in saleFromDb.InputSaleMerges) {
            saleMerged.InputSale = saleRepository.GetByIdWithOrderItemMerged(saleMerged.InputSaleId);

            salesMerged.Add(saleMerged);

            if (saleMerged.InputSale.InputSaleMerges.Any())
                AddInputSales(
                    saleRepository,
                    saleMerged.InputSale,
                    salesMerged
                );
        }
    }
}