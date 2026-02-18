using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Domain.Messages.Sales.OrderItems;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Transporters.Contracts;
using GBA.Domain.Repositories.UpdateDataCarriers.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Sales;

public sealed class ProtocolActInvoiceActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ITransporterRepositoriesFactory _transporterRepositoriesFactory;
    private readonly IUpdateDataCarrierRepositoryFactory _updateDataCarrierRepositoryFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public ProtocolActInvoiceActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IUpdateDataCarrierRepositoryFactory updateDataCarrierRepositoryFactory,
        ITransporterRepositoriesFactory transporterRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactor
    ) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _updateDataCarrierRepositoryFactory = updateDataCarrierRepositoryFactory;
        _transporterRepositoriesFactory = transporterRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactor;

        Receive<GetHistorySaleMessage>(ProcessGetHistorySaleMessage);

        Receive<SetWarehousesShipmentMessage>(ProcessSetWarehousesShipmentMessage);

        Receive<SetActForEditMessage>(ProcesSetActForEditMessage);

        Receive<GetEditTransportersMessage>(ProcessGetEditTransportersMessage);

        Receive<GetEditActForEditingMessage>(ProcessGetEditActForEditingMessage);

        Receive<GetHistorySaleQtyMessage>(ProcessGetHistorySaleQtyMessage);

        Receive<GetEditTransporterQtyMessage>(ProcessGetEditTransporterQtyMessage);

        Receive<GetEditActForEditingQtyMessage>(ProcessGetEditActForEditingQtyMessage);
    }

    private void ProcesSetActForEditMessage(SetActForEditMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IHistoryInvoiceEditRepository historyInvoiceEditRepository = _saleRepositoriesFactory.NewHistoryInvoiceEditRepository(connection);
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
        IProductLocationRepository productLocationRepository = _productRepositoriesFactory.NewProductLocationRepository(connection);

        HistoryInvoiceEdit historyInvoiceEdit = historyInvoiceEditRepository.GetByNetId(message.NetId);

        Sale saleFromDb = saleRepository.GetByNetId(historyInvoiceEdit.Sale.NetUid);
        List<OrderItem> orderItems = orderItemRepository.GetAllWithProductMovementsBySaleId(saleFromDb.Id);

        foreach (OrderItemBaseShiftStatus OrderItemBaseShiftStatus in historyInvoiceEdit.OrderItemBaseShiftStatuses) {
            OrderItem orderItem = orderItems.FirstOrDefault(x => x.Id == OrderItemBaseShiftStatus.OrderItemId);

            orderItem.InvoiceDocumentQty = orderItem.InvoiceDocumentQty - OrderItemBaseShiftStatus.Qty;
            orderItemRepository.UpdateInvoiceDocumentQty(orderItem);
            foreach (ProductLocation productLocation in orderItem.ProductLocations) {
                productLocation.InvoiceDocumentQty = orderItem.InvoiceDocumentQty;
                productLocationRepository.UpdateIvoiceDocumentQty(productLocation);
            }
        }

        historyInvoiceEditRepository.UpdateIsDevelopment(message.NetId);

        historyInvoiceEditRepository.UpdateApproveUpdate(message.NetId);

        Sender.Tell(saleFromDb);
    }

    private void ProcessSetWarehousesShipmentMessage(SetWarehousesShipmentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IUpdateDataCarrierRepository updateDataCarrierRepository = _updateDataCarrierRepositoryFactory.NewUpdateDataCarrierRepository(connection);
        IWarehousesShipmentRepository warehousesShipmentRepository = _saleRepositoriesFactory.NewWarehousesShipmentRepository(connection);
        IShipmentListRepository shipmentListRepository = _saleRepositoriesFactory.NewShipmentListRepository(connection);
        ITransporterRepository transporterRepositoriesFactory = _transporterRepositoriesFactory.NewTransporterRepository(connection);

        UpdateDataCarrier updateDataCarrier = updateDataCarrierRepository.GetByNetId(message.NetId);
        List<UpdateDataCarrier> listUpdateDataCarrier = updateDataCarrierRepository.Get(updateDataCarrier.Sale.Id);
        List<UpdateDataCarrier> getTest = updateDataCarrierRepository.GetIsEditTransporter(updateDataCarrier.Sale.Id);
        int indexUpdateDataCarrier = getTest.FindIndex(item => item.Id.Equals(updateDataCarrier.Id));

        if (updateDataCarrier.Sale.WarehousesShipment != null)
            if (updateDataCarrier.TransporterId != updateDataCarrier.Sale?.WarehousesShipment.TransporterId) {
                ShipmentList shipmentList = shipmentListRepository.GetByTransporterNetId(updateDataCarrier.Transporter.NetUid);
                ShipmentList shipmentListOld = shipmentListRepository.GetByTransporterNetId(updateDataCarrier.Sale.WarehousesShipment.Transporter.NetUid);
                Transporter transporter = transporterRepositoriesFactory.GetById((long)updateDataCarrier.TransporterId);
                ShipmentListItem newShipmentListItem = new();
                if (shipmentListOld != null) {
                    ShipmentListItem shipmentListItemOld = shipmentListOld.ShipmentListItems.FirstOrDefault(x => x.SaleId == updateDataCarrier.SaleId);

                    if (shipmentListItemOld != null)
                        _saleRepositoriesFactory
                            .NewShipmentListItemRepository(connection).UpdateIsChangeTransporter(shipmentListItemOld.NetUid);
                    if (shipmentListItemOld != null) newShipmentListItem.QtyPlaces = shipmentListItemOld.QtyPlaces;
                }

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

                newShipmentListItem.SaleId = updateDataCarrier.SaleId;
                newShipmentListItem.ShipmentListId = shipmentList.Id;

                _saleRepositoriesFactory
                    .NewShipmentListItemRepository(connection)
                    .Add(
                        newShipmentListItem
                    );
            }

        warehousesShipmentRepository.Update(updateDataCarrier);
        updateDataCarrierRepository.UpdateIsDevelopment(listUpdateDataCarrier[indexUpdateDataCarrier].NetUid);
        updateDataCarrierRepository.UpdateIsDevelopment(message.NetId);
        Sender.Tell(updateDataCarrier);
    }

    private void ProcessGetEditTransportersMessage(GetEditTransportersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<UpdateDataCarrier> SalesHistoryModel =
            _saleRepositoriesFactory
                .NewProtocolActEditInvoiceRepository(connection)
                .GetEditTransporterModel(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset,
                    message.IsDevelopment
                );

        Sender.Tell(SalesHistoryModel);
    }

    private void ProcessGetEditActForEditingMessage(GetEditActForEditingMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<HistoryInvoiceEdit> SalesHistoryModel =
            _saleRepositoriesFactory
                .NewProtocolActEditInvoiceRepository(connection)
                .GetEditActForEditingModel(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset,
                    message.IsDevelopment
                );

        Sender.Tell(SalesHistoryModel);
    }

    private void ProcessGetHistorySaleMessage(GetHistorySaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<SalesHistoryModel> SalesHistoryModel =
            _saleRepositoriesFactory
                .NewProtocolActEditInvoiceRepository(connection)
                .GetSalesHistoryModel(
                    message.From,
                    message.To,
                    message.Limit,
                    message.Offset,
                    message.IsDevelopment
                );

        Sender.Tell(SalesHistoryModel);
    }

    private void ProcessGetHistorySaleQtyMessage(GetHistorySaleQtyMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory
            .NewProtocolActEditInvoiceRepository(connection)
            .GetSalesHistoryQtyModel());
    }

    private void ProcessGetEditTransporterQtyMessage(GetEditTransporterQtyMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory
            .NewProtocolActEditInvoiceRepository(connection)
            .GetEditTransporterQtyModel());
    }

    private void ProcessGetEditActForEditingQtyMessage(GetEditActForEditingQtyMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory
            .NewProtocolActEditInvoiceRepository(connection)
            .GetEditActForEditingQtyModel());
    }
}