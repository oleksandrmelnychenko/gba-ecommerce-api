using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class OutcomePaymentOrderConsumablesOrderRepository : IOutcomePaymentOrderConsumablesOrderRepository {
    private readonly IDbConnection _connection;

    public OutcomePaymentOrderConsumablesOrderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(OutcomePaymentOrderConsumablesOrder order) {
        _connection.Execute(
            "INSERT INTO [OutcomePaymentOrderConsumablesOrder] (OutcomePaymentOrderId, ConsumablesOrderId, Updated) " +
            "VALUES (@OutcomePaymentOrderId, @ConsumablesOrderId, GETUTCDATE())",
            order
        );
    }

    public void Add(IEnumerable<OutcomePaymentOrderConsumablesOrder> orders) {
        _connection.Execute(
            "INSERT INTO [OutcomePaymentOrderConsumablesOrder] (OutcomePaymentOrderId, ConsumablesOrderId, Updated) " +
            "VALUES (@OutcomePaymentOrderId, @ConsumablesOrderId, GETUTCDATE())",
            orders
        );
    }

    public void Update(IEnumerable<OutcomePaymentOrderConsumablesOrder> orders) {
        _connection.Execute(
            "UPDATE [OutcomePaymentOrderConsumablesOrder] " +
            "SET OutcomePaymentOrderId = @OutcomePaymentOrderId, ConsumablesOrderId = @ConsumablesOrderId, Updated = GETUTCDATE() " +
            "WHERE [OutcomePaymentOrderConsumablesOrder].ID = @Id",
            orders
        );
    }
}