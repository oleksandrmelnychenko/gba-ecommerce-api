using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Sales.OrderPackages;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales.OrderPackages;

public sealed class OrderPackageRepository : IOrderPackageRepository {
    private readonly IDbConnection _connection;

    public OrderPackageRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(OrderPackage orderPackage) {
        return _connection.Query<long>(
                "INSERT INTO [OrderPackage] (CBM, Width, Height, Lenght, Weight, OrderId, Updated) " +
                "VALUES (@CBM, @Width, @Height, @Lenght, @Weight, @OrderId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                orderPackage
            )
            .Single();
    }

    public void Add(IEnumerable<OrderPackage> orderPackages) {
        _connection.Execute(
            "INSERT INTO [OrderPackage] (CBM, Width, Height, Lenght, Weight, OrderId, Updated) " +
            "VALUES (@CBM, @Width, @Height, @Lenght, @Weight, @OrderId, getutcdate())",
            orderPackages
        );
    }

    public void Update(OrderPackage orderPackage) {
        _connection.Execute(
            "UPDATE [OrderPackage] " +
            "SET CBM = @CBM, Width = @Width, Height = @Height, Lenght = @Lenght, Weight = @Weight, OrderId = @OrderId, Updated = getutcdate() " +
            "WHERE [OrderPackage].ID = @Id",
            orderPackage
        );
    }

    public void Update(IEnumerable<OrderPackage> orderPackages) {
        _connection.Execute(
            "UPDATE [OrderPackage] " +
            "SET CBM = @CBM, Width = @Width, Height = @Height, Lenght = @Lenght, Weight = @Weight, OrderId = @OrderId, Updated = getutcdate() " +
            "WHERE [OrderPackage].ID = @Id",
            orderPackages
        );
    }

    public OrderPackage GetById(long id) {
        return _connection.Query<OrderPackage>(
                "SELECT * " +
                "FROM [OrderPackage] " +
                "WHERE [OrderPackage].ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public void RemoveAllByOrderId(long orderId) {
        _connection.Execute(
            "UPDATE [OrderPackage] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [OrderPackage].OrderID = @OrderId",
            new { OrderId = orderId }
        );
    }

    public void RemoveAllByOrderIdExceptProvided(long orderId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [OrderPackage] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [OrderPackage].OrderID = @OrderId " +
            "AND [OrderPackage].ID NOT IN @Ids",
            new { OrderId = orderId, Ids = ids }
        );
    }
}