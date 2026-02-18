using System;

namespace GBA.Domain.Messages.Products.ProductReservations;

public sealed class GetCurrentReservationsByProductNetIdAndVatMessage {
    public GetCurrentReservationsByProductNetIdAndVatMessage(
        Guid netId,
        bool withVat) {
        NetId = netId;
        WithVat = withVat;
    }

    public Guid NetId { get; }

    public bool WithVat { get; }
}