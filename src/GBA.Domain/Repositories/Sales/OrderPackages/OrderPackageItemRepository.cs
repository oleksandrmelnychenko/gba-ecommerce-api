using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Sales.OrderPackages;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales.OrderPackages;

public sealed class OrderPackageItemRepository : IOrderPackageItemRepository {
    private readonly IDbConnection _connection;

    public OrderPackageItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<OrderPackageItem> orderPackageItems) {
        _connection.Execute(
            "INSERT INTO [OrderPackageItem] (OrderItemId, OrderPackageId, Qty, Updated) " +
            "VALUES (@OrderItemId, @OrderPackageId, @Qty, getutcdate())",
            orderPackageItems
        );
    }

    public void Update(IEnumerable<OrderPackageItem> orderPackageItems) {
        _connection.Execute(
            "UPDATE [OrderPackageItem] " +
            "SET Qty = @Qty, Updated = getutcdate() " +
            "WHERE [OrderPackageItem].ID = @Id",
            orderPackageItems
        );
    }

    public void RemoveAllByOrderPackageId(long orderPackageId) {
        _connection.Execute(
            "UPDATE [OrderPackageItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [OrderPackageItem].OrderPackageID = @OrderPackageId",
            new { OrderPackageId = orderPackageId }
        );
    }

    public void RemoveAllByOrderPackageIdExceptProvided(long orderPackageId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [OrderPackageItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [OrderPackageItem].OrderPackageID = @OrderPackageId " +
            "AND [OrderPackageItem].ID NOT IN @Ids",
            new { OrderPackageId = orderPackageId, Ids = ids }
        );
    }
}