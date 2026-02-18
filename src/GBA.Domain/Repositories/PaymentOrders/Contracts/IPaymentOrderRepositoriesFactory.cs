using System.Data;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentOrderRepositoriesFactory {
    IPaymentRegisterRepository NewPaymentRegisterRepository(IDbConnection connection);

    IIncomePaymentOrderRepository NewIncomePaymentOrderRepository(IDbConnection connection);

    IPaymentCurrencyRegisterRepository NewPaymentCurrencyRegisterRepository(IDbConnection connection);

    IIncomePaymentOrderSaleRepository NewIncomePaymentOrderSaleRepository(IDbConnection connection);

    IPaymentRegisterTransferRepository NewPaymentRegisterTransferRepository(IDbConnection connection);

    IPaymentRegisterCurrencyExchangeRepository NewPaymentRegisterCurrencyExchangeRepository(IDbConnection connection);

    IPaymentMovementRepository NewPaymentMovementRepository(IDbConnection connection);

    IPaymentMovementOperationRepository NewPaymentMovementOperationRepository(IDbConnection connection);

    IPaymentMovementTranslationRepository NewPaymentMovementTranslationRepository(IDbConnection connection);

    IOutcomePaymentOrderRepository NewOutcomePaymentOrderRepository(IDbConnection connection);

    IOutcomePaymentOrderConsumablesOrderRepository NewOutcomePaymentOrderConsumablesOrderRepository(IDbConnection connection);

    IAssignedPaymentOrderRepository NewAssignedPaymentOrderRepository(IDbConnection connection);

    IPaymentCostMovementRepository NewPaymentCostMovementRepository(IDbConnection connection);

    IPaymentCostMovementTranslationRepository NewPaymentCostMovementTranslationRepository(IDbConnection connection);

    IPaymentCostMovementOperationRepository NewPaymentCostMovementOperationRepository(IDbConnection connection);

    IOutcomePaymentOrderSupplyPaymentTaskRepository NewOutcomePaymentOrderSupplyPaymentTaskRepository(IDbConnection connection);

    IAdvancePaymentRepository NewAdvancePaymentRepository(IDbConnection connection);
}