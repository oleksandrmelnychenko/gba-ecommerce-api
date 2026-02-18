using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentMovementOperationRepository {
    void Add(PaymentMovementOperation paymentMovementOperation);

    void Update(PaymentMovementOperation paymentMovementOperation);
}