using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales;

public sealed class UpdateSaleFromEcommerceMessage {
    public UpdateSaleFromEcommerceMessage(Sale sale) {
        Sale = sale;
    }

    public Sale Sale { get; }
}