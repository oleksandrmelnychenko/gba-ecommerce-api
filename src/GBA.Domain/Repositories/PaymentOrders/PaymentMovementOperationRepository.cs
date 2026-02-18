using System.Data;
using Dapper;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class PaymentMovementOperationRepository : IPaymentMovementOperationRepository {
    private readonly IDbConnection _connection;

    public PaymentMovementOperationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(PaymentMovementOperation paymentMovementOperation) {
        _connection.Execute(
            "INSERT INTO [PaymentMovementOperation] (PaymentMovementId, IncomePaymentOrderId, OutcomePaymentOrderId, PaymentRegisterTransferId, PaymentRegisterCurrencyExchangeId, Updated) " +
            "VALUES (@PaymentMovementId, @IncomePaymentOrderId, @OutcomePaymentOrderId, @PaymentRegisterTransferId, @PaymentRegisterCurrencyExchangeId, getutcdate())",
            paymentMovementOperation
        );
    }

    public void Update(PaymentMovementOperation paymentMovementOperation) {
        _connection.Execute(
            "UPDATE [PaymentMovementOperation] " +
            "SET PaymentMovementId = @PaymentMovementId, Updated = getutcdate() " +
            "WHERE [PaymentMovementOperation].ID = @Id",
            paymentMovementOperation
        );
    }
}