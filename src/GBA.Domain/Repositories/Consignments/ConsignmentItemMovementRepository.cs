using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Repositories.Consignments.Contracts;

namespace GBA.Domain.Repositories.Consignments;

public sealed class ConsignmentItemMovementRepository : IConsignmentItemMovementRepository {
    private readonly IDbConnection _connection;

    public ConsignmentItemMovementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ConsignmentItemMovement movement) {
        _connection.Execute(
            "INSERT INTO [ConsignmentItemMovement] " +
            "(IsIncomeMovement, Qty, RemainingQty, MovementType, ConsignmentItemId, ProductIncomeItemId, DepreciatedOrderItemId, SupplyReturnItemId, OrderItemId, " +
            "ProductTransferItemId, OrderItemBaseShiftStatusId, TaxFreeItemId, SadItemId, Updated, ReSaleItemId) " +
            "VALUES " +
            "(@IsIncomeMovement, @Qty, @RemainingQty, @MovementType, @ConsignmentItemId, @ProductIncomeItemId, @DepreciatedOrderItemId, @SupplyReturnItemId, @OrderItemId, " +
            "@ProductTransferItemId, @OrderItemBaseShiftStatusId, @TaxFreeItemId, @SadItemId, GETUTCDATE(), @ReSaleItemId)",
            movement
        );
    }

    public void Add(IEnumerable<ConsignmentItemMovement> movements) {
        _connection.Execute(
            "INSERT INTO [ConsignmentItemMovement] " +
            "(IsIncomeMovement, Qty, RemainingQty, MovementType, ConsignmentItemId, ProductIncomeItemId, DepreciatedOrderItemId, SupplyReturnItemId, OrderItemId, " +
            "ProductTransferItemId, OrderItemBaseShiftStatusId, TaxFreeItemId, SadItemId, Updated) " +
            "VALUES " +
            "(@IsIncomeMovement, @Qty, @RemainingQty, @MovementType, @ConsignmentItemId, @ProductIncomeItemId, @DepreciatedOrderItemId, @SupplyReturnItemId, @OrderItemId, " +
            "@ProductTransferItemId, @OrderItemBaseShiftStatusId, @TaxFreeItemId, @SadItemId, GETUTCDATE())",
            movements
        );
    }

    public void UpdateRemainingQty(ConsignmentItemMovement movement) {
        _connection.Execute(
            "UPDATE [ConsignmentItemMovement] " +
            "SET RemainingQty = @RemainingQty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            movement
        );
    }
}