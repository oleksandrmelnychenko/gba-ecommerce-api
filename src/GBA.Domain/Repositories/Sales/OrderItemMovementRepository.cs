using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class OrderItemMovementRepository : IOrderItemMovementRepository {
    private readonly IDbConnection _connection;

    public OrderItemMovementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(OrderItemMovement movement) {
        _connection.Execute(
            "INSERT INTO [OrderItemMovement] (Qty, UserID, OrderItemID, MovementType, Updated) " +
            "VALUES (@Qty, @UserId, @OrderItemId, @MovementType, GETUTCDATE())",
            movement
        );
    }

    public void Update(OrderItemMovement movement) {
        _connection.Execute(
            "UPDATE [OrderItemMovement] " +
            "SET Qty = @Qty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            movement
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [OrderItemMovement] " +
            "SET Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void Remove(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [OrderItemMovement] " +
            "SET Deleted = 1 " +
            "WHERE ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByOrderItemId(long orderItemId) {
        _connection.Execute(
            "UPDATE [OrderItemMovement] " +
            "SET Deleted = 1 " +
            "WHERE OrderItemID = @OrderItemId",
            new { OrderItemId = orderItemId }
        );
    }

    public IEnumerable<OrderItemMovement> GetAllByOrderItemId(long orderItemId) {
        return _connection.Query<OrderItemMovement>(
            "SELECT * " +
            "FROM [OrderItemMovement] " +
            "WHERE OrderItemID = @OrderItemId " +
            "AND Deleted = 0 " +
            "ORDER BY ID DESC",
            new { OrderItemId = orderItemId }
        );
    }
}