using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentCostMovementOperationRepository {
    void Add(PaymentCostMovementOperation paymentCostMovementOperation);

    void Update(PaymentCostMovementOperation paymentCostMovementOperation);
}