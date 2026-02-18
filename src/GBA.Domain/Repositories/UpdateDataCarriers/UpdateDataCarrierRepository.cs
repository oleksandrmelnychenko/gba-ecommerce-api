using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Repositories.UpdateDataCarriers.Contracts;

namespace GBA.Domain.Repositories.UpdateDataCarriers;

public class UpdateDataCarrierRepository : IUpdateDataCarrierRepository {
    private readonly IDbConnection _connection;

    public UpdateDataCarrierRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(UpdateDataCarrier updateDataCarrier) {
        return _connection.Query<long>(
            "INSERT INTO [UpdateDataCarrier] (SaleId, ApproveUpdate, IsDevelopment, TransporterId, UserId, IsCashOnDelivery, HasDocument, CashOnDeliveryAmount, Comment, " +
            "Number, MobilePhone, FullName, City, Department, TtnPDFPath, ShipmentDate, IsEditTransporter, TTN, Updated) " +
            "VALUES (@SaleId, @ApproveUpdate, @IsDevelopment, @TransporterId, @UserId, @IsCashOnDelivery, @HasDocument, @CashOnDeliveryAmount, @Comment, @Number, " +
            "@MobilePhone, @FullName, @City, @Department, @TtnPDFPath, @ShipmentDate,@IsEditTransporter, @TTN, GETUTCDATE());" +
            "SELECT SCOPE_IDENTITY() ",
            updateDataCarrier).FirstOrDefault();
    }

    public UpdateDataCarrier GetId(long saleId) {
        UpdateDataCarrier updateDataCarriers = new();
        Type[] types = {
            typeof(UpdateDataCarrier),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], UpdateDataCarrier> mapper = objects => {
            UpdateDataCarrier updateDataCarrier = (UpdateDataCarrier)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];

            if (user != null) updateDataCarrier.User = user;
            if (transporter != null) updateDataCarrier.Transporter = transporter;
            updateDataCarriers = updateDataCarrier;
            return updateDataCarrier;
        };

        _connection.Query(
            "SELECT " +
            "[UpdateDataCarrier].* " +
            ",[User].* " +
            ",[Transporter].* " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [UpdateDataCarrier].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [UpdateDataCarrier].TransporterId " +
            "WHERE [UpdateDataCarrier].SaleId = @SaleID ",
            types,
            mapper,
            new {
                SaleID = saleId
            });
        return updateDataCarriers;
    }

    public List<UpdateDataCarrier> Get(long saleId) {
        List<UpdateDataCarrier> updateDataCarriers = new();
        Type[] types = {
            typeof(UpdateDataCarrier),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], UpdateDataCarrier> mapper = objects => {
            UpdateDataCarrier updateDataCarrier = (UpdateDataCarrier)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];

            if (user != null) updateDataCarrier.User = user;
            if (transporter != null) updateDataCarrier.Transporter = transporter;
            updateDataCarriers.Add(updateDataCarrier);
            return updateDataCarrier;
        };

        _connection.Query(
            "SELECT " +
            "[UpdateDataCarrier].* " +
            ",[User].* " +
            ",[Transporter].* " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [UpdateDataCarrier].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [UpdateDataCarrier].TransporterId " +
            "WHERE [UpdateDataCarrier].SaleId = @SaleID " +
            "AND [UpdateDataCarrier].IsEditTransporter = 0 ",
            types,
            mapper,
            new {
                SaleID = saleId
            });
        return updateDataCarriers;
    }


    public List<UpdateDataCarrier> GetIsEditTransporter(long saleId) {
        List<UpdateDataCarrier> updateDataCarriers = new();
        Type[] types = {
            typeof(UpdateDataCarrier),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], UpdateDataCarrier> mapper = objects => {
            UpdateDataCarrier updateDataCarrier = (UpdateDataCarrier)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];

            if (user != null) updateDataCarrier.User = user;
            if (transporter != null) updateDataCarrier.Transporter = transporter;
            updateDataCarriers.Add(updateDataCarrier);
            return updateDataCarrier;
        };

        _connection.Query(
            "SELECT " +
            "[UpdateDataCarrier].* " +
            ",[User].* " +
            ",[Transporter].* " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [UpdateDataCarrier].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [UpdateDataCarrier].TransporterId " +
            "WHERE [UpdateDataCarrier].SaleId = @SaleID " +
            "AND [UpdateDataCarrier].IsEditTransporter = 1 ",
            types,
            mapper,
            new {
                SaleID = saleId
            });
        return updateDataCarriers;
    }

    public UpdateDataCarrier GetByNetId(Guid netId) {
        UpdateDataCarrier updateDataCarrier = new();
        Type[] types = {
            typeof(UpdateDataCarrier),
            typeof(User),
            typeof(Transporter),
            typeof(Sale),
            typeof(WarehousesShipment),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], UpdateDataCarrier> mapper = objects => {
            UpdateDataCarrier updateDataCarriers = (UpdateDataCarrier)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];
            Sale sale = (Sale)objects[3];
            WarehousesShipment warehousesShipment = (WarehousesShipment)objects[4];
            User userWarehousesShipment = (User)objects[5];
            Transporter transporterWarehousesShipment = (Transporter)objects[6];

            if (warehousesShipment != null) {
                warehousesShipment.Transporter = transporterWarehousesShipment;
                warehousesShipment.User = userWarehousesShipment;

                sale.WarehousesShipment = warehousesShipment;
            }

            updateDataCarriers.Sale = sale;
            if (user != null) updateDataCarriers.User = user;
            if (transporter != null) updateDataCarriers.Transporter = transporter;
            updateDataCarrier = updateDataCarriers;
            return updateDataCarrier;
        };

        return _connection.Query(
            "SELECT " +
            "[UpdateDataCarrier].* " +
            ",[User].* " +
            ",[Transporter].* " +
            ",[Sale].* " +
            ",[WarehousesShipment].* " +
            ",[UserWarehousesShipment].* " +
            ",[TransporterWarehousesShipment].* " +
            "From [UpdateDataCarrier] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [UpdateDataCarrier].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [UpdateDataCarrier].TransporterId " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [UpdateDataCarrier].SaleID " +
            "LEFT JOIN [WarehousesShipment] " +
            "ON [WarehousesShipment].SaleID = [Sale].ID " +
            "LEFT JOIN [User] AS [UserWarehousesShipment]" +
            "ON [UserWarehousesShipment].ID = [WarehousesShipment].UserId " +
            "LEFT JOIN [Transporter] as [TransporterWarehousesShipment]  " +
            "ON [TransporterWarehousesShipment].ID = [WarehousesShipment].TransporterId " +
            "WHERE [UpdateDataCarrier].NetUid = @NetId ",
            types,
            mapper,
            new {
                NetId = netId
            }).SingleOrDefault();
    }

    public void UpdateIsDevelopment(Guid netId) {
        _connection.Execute(
            "UPDATE [UpdateDataCarrier] " +
            "SET IsDevelopment = 1, Updated = getutcdate() " +
            "WHERE [UpdateDataCarrier].[NetUID] = @NetId",
            new { NetId = netId });
    }
}