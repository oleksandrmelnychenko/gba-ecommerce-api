namespace GBA.Domain.Messages.PaymentOrders.PaymentCostMovements;

public sealed class GetAllPaymentCostMovementsFromSearchMessage {
    public GetAllPaymentCostMovementsFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}