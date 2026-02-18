namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class GetAllPaymentRegistersFromSearchMessage {
    public GetAllPaymentRegistersFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}