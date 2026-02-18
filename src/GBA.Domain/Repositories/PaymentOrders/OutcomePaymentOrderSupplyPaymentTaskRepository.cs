using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class OutcomePaymentOrderSupplyPaymentTaskRepository : IOutcomePaymentOrderSupplyPaymentTaskRepository {
    private readonly IDbConnection _connection;

    public OutcomePaymentOrderSupplyPaymentTaskRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<OutcomePaymentOrderSupplyPaymentTask> tasks) {
        _connection.Execute(
            "INSERT INTO [OutcomePaymentOrderSupplyPaymentTask] (Amount, OutcomePaymentOrderId, SupplyPaymentTaskId, Updated) " +
            "VALUES (@Amount, @OutcomePaymentOrderId, @SupplyPaymentTaskId, GETUTCDATE())",
            tasks
        );
    }
}