using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Messages.Sales.ShipmentLists;
using GBA.Domain.Repositories.DocumentMonths.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Transporters.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Sales;

public sealed class ShipmentListsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDocumentMonthRepositoryFactory _documentMonthRepositoryFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ITransporterRepositoriesFactory _transporterRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public ShipmentListsActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        ITransporterRepositoriesFactory transporterRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IDocumentMonthRepositoryFactory documentMonthRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _transporterRepositoriesFactory = transporterRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _documentMonthRepositoryFactory = documentMonthRepositoryFactory;

        Receive<AddNewShipmentListMessage>(ProcessAddNewShipmentListMessage);

        Receive<UpdateShipmentListMessage>(ProcessUpdateShipmentListMessage);

        Receive<GetShipmentListByNetIdMessage>(ProcessGetShipmentListByNetIdMessage);

        Receive<GetAllShipmentListsFilteredMessage>(ProcessGetAllShipmentListsFilteredMessage);

        Receive<GetCreateDocumentShipmentFilteredMessage>(ProcessGetCreateDocumentShipmentFilteredMessage);

        Receive<GetDocumentShipmentFilteredMessage>(ProcessGetDocumentShipmentFilteredMessage);

        Receive<AutoAddOrUpdateShipmentListFilteredMessage>(ProcessAutoAddOrUpdateShipmentListFilteredMessage);
    }

    private void ProcessAddNewShipmentListMessage(AddNewShipmentListMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.ShipmentList == null) throw new Exception(ShipmentListResourceNames.EMPTY_SHIPMENT_LIST);
            if (!message.ShipmentList.IsNew()) throw new Exception(ShipmentListResourceNames.NEW_SHIPMENT_LIST);
            if (!message.ShipmentList.ShipmentListItems.Any(i => i.Sale != null && !i.Sale.IsNew()))
                throw new Exception(ShipmentListResourceNames.NO_SHIPMENT_LIST_ITEMS);
            if (message.ShipmentList.Transporter == null || message.ShipmentList.Transporter.IsNew())
                throw new Exception(ShipmentListResourceNames.NO_SPECIFY_TRANSPORTER);

            IShipmentListRepository shipmentListRepository = _saleRepositoriesFactory.NewShipmentListRepository(connection);

            message.ShipmentList.TransporterId = message.ShipmentList.Transporter.Id;

            message.ShipmentList.ResponsibleId =
                message.ShipmentList.Responsible != null && !message.ShipmentList.Responsible.IsNew()
                    ? message.ShipmentList.Responsible.Id
                    : _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            message.ShipmentList.FromDate =
                message.ShipmentList.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.ShipmentList.FromDate);

            ShipmentList lastRecord = shipmentListRepository.GetLastRecord();

            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year))
                message.ShipmentList.Number = string.Format("{0:D9}", Convert.ToInt64(lastRecord.Number) + 1);
            else
                message.ShipmentList.Number = string.Format("{0:D9}", 1);

            message.ShipmentList.Id = shipmentListRepository.Add(message.ShipmentList);

            _saleRepositoriesFactory
                .NewShipmentListItemRepository(connection)
                .Add(
                    message
                        .ShipmentList
                        .ShipmentListItems
                        .Where(i => i.Sale != null && !i.Sale.IsNew())
                        .Select(item => {
                            item.ShipmentListId = message.ShipmentList.Id;
                            item.SaleId = item.Sale.Id;

                            return item;
                        })
                );

            Sender.Tell(
                shipmentListRepository
                    .GetById(
                        message.ShipmentList.Id
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateShipmentListMessage(UpdateShipmentListMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.ShipmentList == null) throw new Exception(ShipmentListResourceNames.EMPTY_SHIPMENT_LIST);
            if (message.ShipmentList.IsNew()) throw new Exception(ShipmentListResourceNames.NEW_SHIPMENT_LIST);
            if (!message.ShipmentList.ShipmentListItems.Any(i => i.Sale != null && !i.Sale.IsNew()))
                throw new Exception(ShipmentListResourceNames.NO_SHIPMENT_LIST_ITEMS);

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IShipmentListRepository shipmentListRepository = _saleRepositoriesFactory.NewShipmentListRepository(connection);
            IShipmentListItemRepository shipmentListItemRepository = _saleRepositoriesFactory.NewShipmentListItemRepository(connection);

            message.ShipmentList.ResponsibleId =
                message.ShipmentList.Responsible != null && !message.ShipmentList.Responsible.IsNew()
                    ? message.ShipmentList.Responsible.Id
                    : _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            message.ShipmentList.FromDate =
                message.ShipmentList.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow
                    : TimeZoneInfo.ConvertTimeToUtc(message.ShipmentList.FromDate);

            shipmentListRepository.Update(message.ShipmentList);

            shipmentListItemRepository
                .RemoveAllByIdExceptProvided(
                    message.ShipmentList.Id,
                    message.ShipmentList.ShipmentListItems.Where(i => !i.IsNew()).Select(i => i.Id)
                );

            shipmentListItemRepository
                .Add(
                    message
                        .ShipmentList
                        .ShipmentListItems
                        .Where(i => i.Sale != null && !i.Sale.IsNew() && i.IsNew())
                        .Select(item => {
                            item.ShipmentListId = message.ShipmentList.Id;
                            item.SaleId = item.Sale.Id;

                            saleRepository.UpdateShipmentInfo(item.Sale);

                            return item;
                        })
                );

            shipmentListItemRepository
                .Update(
                    message
                        .ShipmentList
                        .ShipmentListItems
                        .Where(i => i.Sale != null && !i.Sale.IsNew() && !i.IsNew())
                        .Select(item => {
                            saleRepository.UpdateShipmentInfo(item.Sale);

                            return item;
                        })
                );

            Sender.Tell(
                shipmentListRepository
                    .GetById(
                        message.ShipmentList.Id
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetShipmentListByNetIdMessage(GetShipmentListByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _saleRepositoriesFactory
                .NewShipmentListRepository(connection)
                .GetByNetId(
                    message.NetId
                )
        );
    }

    private void ProcessGetDocumentShipmentFilteredMessage(GetDocumentShipmentFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        string xlsxFile = string.Empty;
        string pdfFile = string.Empty;

        if (!message.NetId.Equals(Guid.Empty)) {
            ShipmentList shipmentList = _saleRepositoriesFactory
                .NewShipmentListRepository(connection)
                .GetByNetId(message.NetId);

            (xlsxFile, pdfFile) =
                _xlsFactoryManager
                    .NewSalesShipmentListManager()
                    .ExportSalesShipmentsToXlsx(
                        message.SaleInvoicesFolderPath,
                        shipmentList,
                        _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));
        } else {
            IEnumerable<ShipmentList> shipmentLists = _saleRepositoriesFactory
                .NewShipmentListRepository(connection)
                .GetDocumentFiltered();

            (xlsxFile, pdfFile) =
                _xlsFactoryManager
                    .NewSalesShipmentListManager()
                    .ExportAllSalesShipmentsToXlsx(
                        message.SaleInvoicesFolderPath,
                        shipmentLists,
                        _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));
        }

        Sender.Tell(new Tuple<string, string>(xlsxFile, pdfFile));
    }

    private void ProcessGetAllShipmentListsFilteredMessage(GetAllShipmentListsFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _saleRepositoriesFactory
                .NewShipmentListRepository(connection)
                .GetAllFiltered(
                    message.From,
                    message.To,
                    message.NetId,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessGetCreateDocumentShipmentFilteredMessage(GetCreateDocumentShipmentFilteredMessage message) {
        string xlsxFile = string.Empty;
        string pdfFile = string.Empty;
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        IShipmentListRepository shipmentListRepository = _saleRepositoriesFactory.NewShipmentListRepository(connection);

        Transporter transporter = _transporterRepositoriesFactory.NewTransporterRepository(connection).GetByNetId(message.NetId);

        if (transporter == null) {
            Sender.Tell(new ShipmentList());
        } else {
            List<Sale> sales =
                saleRepository
                    .GetAllFilteredByTransporterAndType(
                        message.From,
                        message.To,
                        message.NetId,
                        true
                    );

            ShipmentList shipmentList = shipmentListRepository.GetByTransporterNetId(message.NetId);

            if (shipmentList == null && !sales.Any()) {
                Sender.Tell(new ShipmentList());
            } else {
                if (shipmentList == null) {
                    shipmentList = new ShipmentList {
                        ResponsibleId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id,
                        TransporterId = transporter.Id,
                        FromDate = DateTime.UtcNow
                    };

                    ShipmentList lastRecord = shipmentListRepository.GetLastRecord();

                    if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year))
                        shipmentList.Number = string.Format("{0:D9}", Convert.ToInt64(lastRecord.Number) + 1);
                    else
                        shipmentList.Number = string.Format("{0:D9}", 1);

                    shipmentList.Id = shipmentListRepository.Add(shipmentList);

                    shipmentList = shipmentListRepository.GetById(shipmentList.Id);
                }

                if (sales.Any())
                    _saleRepositoriesFactory
                        .NewShipmentListItemRepository(connection)
                        .Add(
                            sales
                                .Select(sale => new ShipmentListItem {
                                    SaleId = sale.Id,
                                    ShipmentListId = shipmentList.Id
                                })
                        );

                shipmentList = shipmentListRepository.GetByNetId(shipmentList.NetUid);

                sales =
                    saleRepository
                        .GetAllByIds(
                            shipmentList
                                .ShipmentListItems
                                .Select(i => i.SaleId)
                                .ToList()
                        );

                CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

                foreach (Sale sale in sales)
                foreach (ShipmentListItem item in shipmentList.ShipmentListItems.Where(i => i.SaleId.Equals(sale.Id)))
                    item.Sale = sale;

                (xlsxFile, pdfFile) =
                    _xlsFactoryManager
                        .NewSalesShipmentListManager()
                        .ExportSalesShipmentsToXlsx(
                            message.SaleInvoicesFolderPath,
                            shipmentList,
                            _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));

                Sender.Tell(new Tuple<string, string>(xlsxFile, pdfFile));
            }
        }
    }

    private void ProcessAutoAddOrUpdateShipmentListFilteredMessage(AutoAddOrUpdateShipmentListFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        IShipmentListRepository shipmentListRepository = _saleRepositoriesFactory.NewShipmentListRepository(connection);

        Transporter transporter = _transporterRepositoriesFactory.NewTransporterRepository(connection).GetByNetId(message.NetId);

        if (transporter == null) {
            Sender.Tell(new ShipmentList());
        } else {
            List<Sale> sales =
                saleRepository
                    .GetAllFilteredByTransporterAndType(
                        message.From,
                        message.To,
                        message.NetId,
                        true
                    );

            ShipmentList shipmentList = shipmentListRepository.GetByTransporterFilteredNetId(message.From,
                message.To, message.NetId);
            //ShipmentList shipmentList = shipmentListRepository.GetByTransporterNetId(message.NetId);


            if (shipmentList == null && !sales.Any()) {
                Sender.Tell(new ShipmentList());
            } else {
                if (shipmentList == null) {
                    shipmentList = new ShipmentList {
                        ResponsibleId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id,
                        TransporterId = transporter.Id,
                        FromDate = DateTime.UtcNow
                    };

                    ShipmentList lastRecord = shipmentListRepository.GetLastRecord();

                    if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year))
                        shipmentList.Number = string.Format("{0:D9}", Convert.ToInt64(lastRecord.Number) + 1);
                    else
                        shipmentList.Number = string.Format("{0:D9}", 1);

                    shipmentList.Id = shipmentListRepository.Add(shipmentList);

                    shipmentList = shipmentListRepository.GetById(shipmentList.Id);
                }

                if (sales.Any())
                    _saleRepositoriesFactory
                        .NewShipmentListItemRepository(connection)
                        .Add(
                            sales
                                .Select(sale => new ShipmentListItem {
                                    SaleId = sale.Id,
                                    ShipmentListId = shipmentList.Id
                                })
                        );

                shipmentList = shipmentListRepository.GetByFillteredNetId(message.From,
                    message.To, shipmentList.NetUid);

                sales =
                    saleRepository
                        .GetAllByIds(
                            shipmentList
                                .ShipmentListItems
                                .Select(i => i.SaleId)
                                .ToList()
                        );

                CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

                foreach (Sale sale in sales)
                foreach (ShipmentListItem item in shipmentList.ShipmentListItems.Where(i => i.SaleId.Equals(sale.Id)))
                    item.Sale = sale;

                Sender.Tell(shipmentList);
            }
        }
    }

    private static void CalculatePricingsForSalesWithDynamicPrices(
        List<Sale> sales,
        IExchangeRateRepository exchangeRateRepository) {
        sales.ForEach(sale => {
            if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                ExchangeRate euroExchangeRate = exchangeRateRepository.GetEuroExchangeRateByCurrentCulture();

                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    orderItem.Product.CurrentPrice =
                        decimal.Round(orderItem.Product.CurrentPrice - orderItem.Product.CurrentPrice * orderItem.OneTimeDiscount / 100, 4, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.Product.CurrentPrice * euroExchangeRate.Amount, 4, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
                }
            } else {
                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.PricePerItem * orderItem.ExchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount =
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal =
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty) * orderItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.PricePerItem, 2, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                }
            }

            sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

            sale.TotalAmount = sale.Order.TotalAmount;
            sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
            sale.TotalCount = sale.Order.TotalCount;
        });
    }
}