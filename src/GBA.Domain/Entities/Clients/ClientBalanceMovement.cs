namespace GBA.Domain.Entities.Clients;

public sealed class ClientBalanceMovement : EntityBase {
    public decimal Amount { get; set; }

    public decimal ExchangeRateAmount { get; set; }

    public ClientBalanceMovementType MovementType { get; set; }

    public long ClientAgreementId { get; set; }

    public ClientAgreement ClientAgreement { get; set; }
}