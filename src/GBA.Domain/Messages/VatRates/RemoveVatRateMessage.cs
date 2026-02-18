using System;

namespace GBA.Domain.Messages.VatRates;

public sealed class RemoveVatRateMessage {
    public RemoveVatRateMessage(
        Guid id) {
        Id = id;
    }

    public Guid Id { get; }
}