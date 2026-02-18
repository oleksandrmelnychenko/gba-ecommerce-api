using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales;

public sealed class CalculateSaleWithOneTimeDiscountsMessage {
    public CalculateSaleWithOneTimeDiscountsMessage(Sale sale) {
        Sale = sale;
    }

    public Sale Sale { get; }
}