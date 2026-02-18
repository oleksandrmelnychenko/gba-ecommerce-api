using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentCostMovementTranslationRepository {
    void Add(PaymentCostMovementTranslation paymentMovementTranslation);

    void Update(PaymentCostMovementTranslation paymentMovementTranslation);

    PaymentCostMovementTranslation GetByPaymentMovementId(long id);
}