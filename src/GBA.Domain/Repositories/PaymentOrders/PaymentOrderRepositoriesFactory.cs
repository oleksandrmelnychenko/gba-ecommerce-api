using System.Data;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class PaymentOrderRepositoriesFactory : IPaymentOrderRepositoriesFactory {
    public IPaymentRegisterRepository NewPaymentRegisterRepository(IDbConnection connection) {
        return new PaymentRegisterRepository(connection);
    }

    public IIncomePaymentOrderRepository NewIncomePaymentOrderRepository(IDbConnection connection) {
        return new IncomePaymentOrderRepository(connection);
    }

    public IPaymentCurrencyRegisterRepository NewPaymentCurrencyRegisterRepository(IDbConnection connection) {
        return new PaymentCurrencyRegisterRepository(connection);
    }

    public IIncomePaymentOrderSaleRepository NewIncomePaymentOrderSaleRepository(IDbConnection connection) {
        return new IncomePaymentOrderSaleRepository(connection);
    }

    public IPaymentRegisterTransferRepository NewPaymentRegisterTransferRepository(IDbConnection connection) {
        return new PaymentRegisterTransferRepository(connection);
    }

    public IPaymentRegisterCurrencyExchangeRepository NewPaymentRegisterCurrencyExchangeRepository(IDbConnection connection) {
        return new PaymentRegisterCurrencyExchangeRepository(connection);
    }

    public IPaymentMovementRepository NewPaymentMovementRepository(IDbConnection connection) {
        return new PaymentMovementRepository(connection);
    }

    public IPaymentMovementOperationRepository NewPaymentMovementOperationRepository(IDbConnection connection) {
        return new PaymentMovementOperationRepository(connection);
    }

    public IPaymentMovementTranslationRepository NewPaymentMovementTranslationRepository(IDbConnection connection) {
        return new PaymentMovementTranslationRepository(connection);
    }

    public IOutcomePaymentOrderRepository NewOutcomePaymentOrderRepository(IDbConnection connection) {
        return new OutcomePaymentOrderRepository(connection);
    }

    public IOutcomePaymentOrderConsumablesOrderRepository NewOutcomePaymentOrderConsumablesOrderRepository(IDbConnection connection) {
        return new OutcomePaymentOrderConsumablesOrderRepository(connection);
    }

    public IAssignedPaymentOrderRepository NewAssignedPaymentOrderRepository(IDbConnection connection) {
        return new AssignedPaymentOrderRepository(connection);
    }

    public IPaymentCostMovementRepository NewPaymentCostMovementRepository(IDbConnection connection) {
        return new PaymentCostMovementRepository(connection);
    }

    public IPaymentCostMovementTranslationRepository NewPaymentCostMovementTranslationRepository(IDbConnection connection) {
        return new PaymentCostMovementTranslationRepository(connection);
    }

    public IPaymentCostMovementOperationRepository NewPaymentCostMovementOperationRepository(IDbConnection connection) {
        return new PaymentCostMovementOperationRepository(connection);
    }

    public IOutcomePaymentOrderSupplyPaymentTaskRepository NewOutcomePaymentOrderSupplyPaymentTaskRepository(IDbConnection connection) {
        return new OutcomePaymentOrderSupplyPaymentTaskRepository(connection);
    }

    public IAdvancePaymentRepository NewAdvancePaymentRepository(IDbConnection connection) {
        return new AdvancePaymentRepository(connection);
    }
}