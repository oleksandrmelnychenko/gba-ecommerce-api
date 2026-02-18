namespace GBA.Domain.Entities.PaymentOrders.PaymentMovements;

public sealed class PaymentMovementOperation : EntityBase {
    public long PaymentMovementId { get; set; }

    public long? IncomePaymentOrderId { get; set; }

    public long? OutcomePaymentOrderId { get; set; }

    public long? PaymentRegisterTransferId { get; set; }

    public long? PaymentRegisterCurrencyExchangeId { get; set; }

    public PaymentMovement PaymentMovement { get; set; }

    public IncomePaymentOrder IncomePaymentOrder { get; set; }

    public OutcomePaymentOrder OutcomePaymentOrder { get; set; }

    public PaymentRegisterTransfer PaymentRegisterTransfer { get; set; }

    public PaymentRegisterCurrencyExchange PaymentRegisterCurrencyExchange { get; set; }
}