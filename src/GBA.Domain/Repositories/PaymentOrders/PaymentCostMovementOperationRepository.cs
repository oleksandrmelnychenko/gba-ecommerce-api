using System.Data;
using Dapper;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class PaymentCostMovementOperationRepository : IPaymentCostMovementOperationRepository {
    private readonly IDbConnection _connection;

    public PaymentCostMovementOperationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(PaymentCostMovementOperation paymentCostMovementOperation) {
        _connection.Execute(
            "INSERT INTO [PaymentCostMovementOperation] " +
            "(PaymentCostMovementId, ConsumablesOrderItemId, DepreciatedConsumableOrderItemId, CompanyCarFuelingId, Updated) " +
            "VALUES (@PaymentCostMovementId, @ConsumablesOrderItemId, @DepreciatedConsumableOrderItemId, @CompanyCarFuelingId, getutcdate())",
            paymentCostMovementOperation
        );
    }

    public void Update(PaymentCostMovementOperation paymentCostMovementOperation) {
        _connection.Execute(
            "UPDATE [PaymentCostMovementOperation] " +
            "SET PaymentCostMovementId = @PaymentCostMovementId, ConsumablesOrderItemId = @ConsumablesOrderItemId, " +
            "DepreciatedConsumableOrderItemId = @DepreciatedConsumableOrderItemId, CompanyCarFuelingId = @CompanyCarFuelingId, Updated = getutcdate() " +
            "WHERE [PaymentCostMovementOperation].ID = @Id",
            paymentCostMovementOperation
        );
    }
}