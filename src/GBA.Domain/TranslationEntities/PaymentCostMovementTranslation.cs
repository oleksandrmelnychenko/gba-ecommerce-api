using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.TranslationEntities;

public class PaymentCostMovementTranslation : TranslationEntityBase {
    public string OperationName { get; set; }

    public long PaymentCostMovementId { get; set; }

    public virtual PaymentCostMovement PaymentCostMovement { get; set; }
}