using GBA.Domain.Entities.VatRates;

namespace GBA.Domain.Messages.VatRates;

public sealed class AddNewVatRateMessage {
    public AddNewVatRateMessage(
        VatRate vatRate) {
        VatRate = vatRate;
    }

    public VatRate VatRate { get; }
}