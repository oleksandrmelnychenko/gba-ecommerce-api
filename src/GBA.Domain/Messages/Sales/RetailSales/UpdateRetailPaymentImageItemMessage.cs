using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Sales.RetailSales;

public sealed class UpdateRetailPaymentImageItemMessage {
    public UpdateRetailPaymentImageItemMessage(RetailClientPaymentImageItem paymentImageItem) {
        PaymentImageItem = paymentImageItem;
    }

    public RetailClientPaymentImageItem PaymentImageItem { get; }
}