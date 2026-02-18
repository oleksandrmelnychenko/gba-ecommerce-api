using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.PaymentOrders.PaymentMovements;

public sealed class PaymentMovement : EntityBase {
    public PaymentMovement() {
        PaymentMovementOperations = new HashSet<PaymentMovementOperation>();

        PaymentMovementTranslations = new HashSet<PaymentMovementTranslation>();
    }

    public string OperationName { get; set; }

    public ICollection<PaymentMovementOperation> PaymentMovementOperations { get; set; }

    public ICollection<PaymentMovementTranslation> PaymentMovementTranslations { get; set; }
}