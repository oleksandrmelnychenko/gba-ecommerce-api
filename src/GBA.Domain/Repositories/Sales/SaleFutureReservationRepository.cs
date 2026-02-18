using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class SaleFutureReservationRepository : ISaleFutureReservationRepository {
    private readonly IDbConnection _connection;

    public SaleFutureReservationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(AddSaleFutureReservationQuery message) {
        return _connection.Query<long>(
                "INSERT INTO [SaleFutureReservation] (ProductId, ClientId, SupplyOrderId, RemindDate, Count, Updated) " +
                "VALUES (" +
                "(SELECT [Product].ID FROM [Product] WHERE [Product].NetUID = @ProductNetId)" +
                ", " +
                "(SELECT [Client].ID FROM [Client] WHERE [Client].NetUID = @ClientNetId)" +
                ", " +
                "(SELECT [SupplyOrder].ID FROM [SupplyOrder] WHERE [SupplyOrder].NetUID = @SupplyOrderNetId)" +
                ", " +
                "@RemindDate, " +
                "@Count, " +
                "getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                message
            )
            .Single();
    }

    public SaleFutureReservation GetById(long id) {
        return _connection.Query<SaleFutureReservation, Client, Product, SupplyOrder, SupplyOrderNumber, SaleFutureReservation>(
                "SELECT * " +
                "FROM [SaleFutureReservation] " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SaleFutureReservation].ClientID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SaleFutureReservation].ProductID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SaleFutureReservation].SupplyOrderID " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "WHERE [SaleFutureReservation].ID = @Id",
                (reservation, client, product, order, orderNumber) => {
                    order.SupplyOrderNumber = orderNumber;

                    reservation.Client = client;
                    reservation.Product = product;
                    reservation.SupplyOrder = order;

                    return reservation;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public List<SaleFutureReservation> GetAll() {
        return _connection.Query<SaleFutureReservation, Client, Product, SupplyOrder, SupplyOrderNumber, SaleFutureReservation>(
                "SELECT * " +
                "FROM [SaleFutureReservation] " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SaleFutureReservation].ClientID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [SaleFutureReservation].ProductID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SaleFutureReservation].SupplyOrderID " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "ORDER BY [SaleFutureReservation].ID DESC",
                (reservation, client, product, order, orderNumber) => {
                    order.SupplyOrderNumber = orderNumber;

                    reservation.Client = client;
                    reservation.Product = product;
                    reservation.SupplyOrder = order;

                    return reservation;
                }
            )
            .ToList();
    }

    public void Delete(Guid netId) {
        _connection.Execute(
            "DELETE FROM [SaleFutureReservation] " +
            "WHERE [SaleFutureReservation].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}