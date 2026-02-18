using GBA.Domain.Entities;

namespace GBA.Domain.EntityHelpers;

public sealed class PaymentCard : EntityBase {
    public string Number { get; set; }
}