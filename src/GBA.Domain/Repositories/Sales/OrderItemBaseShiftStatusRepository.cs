using System;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class OrderItemBaseShiftStatusRepository : IOrderItemBaseShiftStatusRepository {
    private readonly IDbConnection _connection;

    public OrderItemBaseShiftStatusRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(OrderItemBaseShiftStatus orderItemBaseShiftStatus) {
        return _connection.Query<long>(
                "INSERT INTO OrderItemBaseShiftStatus (ShiftStatus, Comment, Qty, CurrentQty, HistoryInvoiceEditId, OrderItemId, UserId, SaleId, Updated) " +
                "VALUES (@ShiftStatus, @Comment, @Qty, @CurrentQty, @HistoryInvoiceEditId, @OrderItemId, @UserId, @SaleId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                orderItemBaseShiftStatus
            )
            .Single();
    }

    public void Update(OrderItemBaseShiftStatus orderItemBaseShiftStatus) {
        _connection.Execute(
            "UPDATE OrderItemBaseShiftStatus SET " +
            "ShiftStatus = @ShiftStatus, Comment = @Comment, Qty = @Qty, OrderItemId = @OrderItemId, UserId = @UserId, SaleId = @SaleId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            orderItemBaseShiftStatus
        );
    }

    public OrderItemBaseShiftStatus GetByIdForConsignment(long id) {
        OrderItemBaseShiftStatus toReturn = null;

        Type[] types = {
            typeof(OrderItemBaseShiftStatus),
            typeof(OrderItem),
            typeof(ConsignmentItemMovement),
            typeof(ConsignmentItem)
        };

        Func<object[], OrderItemBaseShiftStatus> mapper = objects => {
            OrderItemBaseShiftStatus shiftStatus = (OrderItemBaseShiftStatus)objects[0];
            OrderItem orderItem = (OrderItem)objects[1];
            ConsignmentItemMovement movement = (ConsignmentItemMovement)objects[2];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[3];

            if (toReturn == null) {
                shiftStatus.OrderItem = orderItem;

                toReturn = shiftStatus;
            }

            if (movement == null) return shiftStatus;

            movement.ConsignmentItem = consignmentItem;

            toReturn.OrderItem.ConsignmentItemMovements.Add(movement);

            return shiftStatus;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [OrderItemBaseShiftStatus] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderItemBaseShiftStatus].OrderItemID " +
            "LEFT JOIN [ConsignmentItemMovement] " +
            "ON [ConsignmentItemMovement].OrderItemID = [OrderItem].ID " +
            "AND [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].[MovementType] = 0 " +
            "AND [ConsignmentItemMovement].[RemainingQty] <> 0 " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "WHERE [OrderItemBaseShiftStatus].ID = @Id",
            types,
            mapper,
            new { Id = id }
        );

        return toReturn;
    }
}