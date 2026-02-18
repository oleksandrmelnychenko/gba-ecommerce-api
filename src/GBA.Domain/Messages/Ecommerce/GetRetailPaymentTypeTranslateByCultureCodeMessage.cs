namespace GBA.Domain.Messages.Ecommerce;

public class GetRetailPaymentTypeTranslateByCultureCodeMessage {
    public GetRetailPaymentTypeTranslateByCultureCodeMessage(string code) {
        Code = code;
    }

    public string Code { get; }
}