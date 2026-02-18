using System.Data;
using Dapper;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class AssignedPaymentOrderRepository : IAssignedPaymentOrderRepository {
    private readonly IDbConnection _connection;

    public AssignedPaymentOrderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(AssignedPaymentOrder assignedPaymentOrder) {
        _connection.Execute(
            "INSERT INTO [AssignedPaymentOrder] " +
            "(RootOutcomePaymentOrderId, RootIncomePaymentOrderId, AssignedOutcomePaymentOrderId, AssignedIncomePaymentOrderId, Updated) " +
            "VALUES (@RootOutcomePaymentOrderId, @RootIncomePaymentOrderId, @AssignedOutcomePaymentOrderId, @AssignedIncomePaymentOrderId, getutcdate())",
            assignedPaymentOrder
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [AssignedPaymentOrder] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [AssignedPaymentOrder].ID = @Id",
            new { Id = id }
        );
    }
}