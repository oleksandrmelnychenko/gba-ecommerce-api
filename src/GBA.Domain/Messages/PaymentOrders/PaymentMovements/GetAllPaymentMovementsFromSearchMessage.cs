namespace GBA.Domain.Messages.PaymentOrders.PaymentMovements;

public sealed class GetAllPaymentMovementsFromSearchMessage {
    public GetAllPaymentMovementsFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}