using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales.Shipments;

public sealed class ShipmentListItemRepository : IShipmentListItemRepository {
    private readonly IDbConnection _connection;

    public ShipmentListItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ShipmentListItem item) {
        _connection.Execute(
            "INSERT INTO [ShipmentListItem] (Comment, QtyPlaces, SaleId, ShipmentListId, Updated) " +
            "VALUES (@Comment, @QtyPlaces, @SaleId, @ShipmentListId, GETUTCDATE())",
            item
        );
    }

    public void Add(IEnumerable<ShipmentListItem> items) {
        _connection.Execute(
            "INSERT INTO [ShipmentListItem] (Comment, QtyPlaces, SaleId, ShipmentListId, Updated) " +
            "VALUES (@Comment, @QtyPlaces, @SaleId, @ShipmentListId, GETUTCDATE())",
            items
        );
    }

    public void Update(ShipmentListItem item) {
        _connection.Execute(
            "UPDATE [ShipmentListItem] " +
            "SET Comment = @Comment, QtyPlaces = @QtyPlaces, SaleId = @SaleId, ShipmentListId = @ShipmentListId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            item
        );
    }

    public void Update(IEnumerable<ShipmentListItem> items) {
        _connection.Execute(
            "UPDATE [ShipmentListItem] " +
            "SET Comment = @Comment, QtyPlaces = @QtyPlaces, SaleId = @SaleId, ShipmentListId = @ShipmentListId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            items
        );
    }

    public void RemoveAllByListIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ShipmentListItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ShipmentListID = @Id " +
            "AND ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void RemoveAllByIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "DELETE FROM [ShipmentListItem] " +
            "WHERE ShipmentListID = @Id " +
            "AND ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void UpdateIsChangeTransporter(Guid netId) {
        _connection.Execute(
            "UPDATE [ShipmentListItem] " +
            "SET IsChangeTransporter = 1, Updated = getutcdate() " +
            "WHERE [ShipmentListItem].[NetUID] = @NetId",
            new { NetId = netId });
    }
}