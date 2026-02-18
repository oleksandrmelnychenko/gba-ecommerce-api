using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.TranslationEntities;

public class PaymentMovementTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long PaymentMovementId { get; set; }

    public virtual PaymentMovement PaymentMovement { get; set; }
}