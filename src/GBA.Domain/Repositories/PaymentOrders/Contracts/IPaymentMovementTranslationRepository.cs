using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentMovementTranslationRepository {
    void Add(PaymentMovementTranslation paymentMovementTranslation);

    void Update(PaymentMovementTranslation paymentMovementTranslation);

    PaymentMovementTranslation GetByPaymentMovementId(long id);
}