using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.PaymentOrders.PaymentMovements;

public sealed class PaymentCostMovement : EntityBase {
    public PaymentCostMovement() {
        PaymentCostMovementTranslations = new HashSet<PaymentCostMovementTranslation>();

        PaymentCostMovementOperations = new HashSet<PaymentCostMovementOperation>();
    }

    public string OperationName { get; set; }

    public ICollection<PaymentCostMovementTranslation> PaymentCostMovementTranslations { get; set; }

    public ICollection<PaymentCostMovementOperation> PaymentCostMovementOperations { get; set; }
}