using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class WarehousesShipmentRepository : IWarehousesShipmentRepository {
    private readonly IDbConnection _connection;

    public WarehousesShipmentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Update(WarehousesShipment warehousesShipment) {
        _connection.Execute(
            "UPDATE WarehousesShipment " +
            "SET SaleId = @SaleId, ApproveUpdate = @ApproveUpdate, IsDevelopment = @IsDevelopment, TransporterId = @TransporterId, " +
            "IsCashOnDelivery = @IsCashOnDelivery, HasDocument = @HasDocument, CashOnDeliveryAmount = @CashOnDeliveryAmount, " +
            "Comment = @Comment, Number = @Number, MobilePhone = @MobilePhone, FullName = @FullName, " +
            "City = @City, Department = @Department, TtnPDFPath = @TtnPDFPath, ShipmentDate = @ShipmentDate, TTN = @TTN , Updated = getutcdate() " +
            "WHERE SaleId = @SaleId",
            warehousesShipment
        );
    }

    public void Update(UpdateDataCarrier warehousesShipment) {
        _connection.Execute(
            "UPDATE WarehousesShipment " +
            "SET SaleId = @SaleId, ApproveUpdate = @ApproveUpdate, IsDevelopment = @IsDevelopment, TransporterId = @TransporterId, " +
            "IsCashOnDelivery = @IsCashOnDelivery, HasDocument = @HasDocument, CashOnDeliveryAmount = @CashOnDeliveryAmount, " +
            "Comment = @Comment, Number = @Number, MobilePhone = @MobilePhone, FullName = @FullName, " +
            "City = @City, Department = @Department, TtnPDFPath = @TtnPDFPath, ShipmentDate = @ShipmentDate, TTN = @TTN , Updated = getutcdate() " +
            "WHERE SaleId = @SaleId",
            warehousesShipment
        );
    }

    public long Add(WarehousesShipment WarehousesShipment) {
        _connection.Execute(
            "INSERT INTO [WarehousesShipment] (SaleId, ApproveUpdate, IsDevelopment, TransporterId, UserId , IsCashOnDelivery, HasDocument, CashOnDeliveryAmount, Comment, " +
            "Number, MobilePhone, FullName, City, Department, TtnPDFPath, ShipmentDate, TTN, Updated) " +
            "VALUES (@SaleId, @ApproveUpdate , @IsDevelopment, @TransporterId, @UserId, @IsCashOnDelivery, @HasDocument, @CashOnDeliveryAmount, @Comment, @Number, " +
            "@MobilePhone, @FullName, @City, @Department, @TtnPDFPath, @ShipmentDate, @TTN, GETUTCDATE())",
            WarehousesShipment);
        return _connection.Query<long>(
            "SELECT ID FROM [WarehousesShipment] WHERE SaleId = @SaleId",
            new {
                WarehousesShipment.SaleId
            }
        ).FirstOrDefault();
    }

    public List<WarehousesShipment> GetAll(long saleId) {
        List<WarehousesShipment> shipment = new();
        Type[] types = {
            typeof(WarehousesShipment),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], WarehousesShipment> mapper = objects => {
            WarehousesShipment Shipment = (WarehousesShipment)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];

            if (user != null) Shipment.User = user;
            if (transporter != null) Shipment.Transporter = transporter;
            shipment.Add(Shipment);
            return Shipment;
        };

        _connection.Query(
            "SELECT " +
            "[WarehousesShipment].* " +
            ",[User].* " +
            ",[Transporter].* " +
            "From [WarehousesShipment] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [WarehousesShipment].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [WarehousesShipment].TransporterId " +
            "WHERE [WarehousesShipment].SaleId = @SaleID ",
            types,
            mapper,
            new {
                SaleID = saleId
            });
        return shipment;
    }

    public void UpdateApprove(Guid netId) {
        _connection.Execute(
            "UPDATE [WarehousesShipment] " +
            "SET ApproveUpdate = 1, Updated = getutcdate() " +
            "WHERE [WarehousesShipment].[NetUID] = @NetId",
            new { NetId = netId });
    }

    public WarehousesShipment Get(long saleId) {
        WarehousesShipment warehousesShipment = new();
        Type[] types = {
            typeof(WarehousesShipment),
            typeof(User),
            typeof(Transporter)
        };

        Func<object[], WarehousesShipment> mapper = objects => {
            WarehousesShipment Shipment = (WarehousesShipment)objects[0];
            User user = (User)objects[1];
            Transporter transporter = (Transporter)objects[2];

            if (user != null) Shipment.User = user;
            if (transporter != null) Shipment.Transporter = transporter;
            warehousesShipment = Shipment;
            return Shipment;
        };

        _connection.Query(
            "SELECT " +
            "[WarehousesShipment].* " +
            ",[User].* " +
            ",[Transporter].* " +
            "From [WarehousesShipment] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [WarehousesShipment].UserId " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [WarehousesShipment].TransporterId " +
            "WHERE [WarehousesShipment].SaleId = @SaleID ",
            types,
            mapper,
            new {
                SaleID = saleId
            });

        return warehousesShipment;
    }
}