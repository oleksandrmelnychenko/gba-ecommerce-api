using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Messages.Ecommerce;

public sealed class UpdateRetailPaymentTypeTranslateMessage {
    public UpdateRetailPaymentTypeTranslateMessage(RetailPaymentTypeTranslate retailPaymentTypeTranslate) {
        RetailPaymentTypeTranslate = retailPaymentTypeTranslate;
    }

    public RetailPaymentTypeTranslate RetailPaymentTypeTranslate { get; }
}