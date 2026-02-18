using GBA.Domain.Entities.VatRates;

namespace GBA.Domain.Messages.VatRates;

public sealed class UpdateVatRateMessage {
    public UpdateVatRateMessage(
        VatRate vatRate) {
        VatRate = vatRate;
    }

    public VatRate VatRate { get; }
}