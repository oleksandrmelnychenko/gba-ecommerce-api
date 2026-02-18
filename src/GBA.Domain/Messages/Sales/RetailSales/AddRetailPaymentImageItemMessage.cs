using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Sales.RetailSales;

public sealed class AddRetailPaymentImageItemMessage {
    public AddRetailPaymentImageItemMessage(RetailClientPaymentImageItem paymentImageItem, string imageUrl) {
        PaymentImageItem = paymentImageItem;
        ImageUrl = imageUrl;
    }

    public RetailClientPaymentImageItem PaymentImageItem { get; }
    public string ImageUrl { get; }
}