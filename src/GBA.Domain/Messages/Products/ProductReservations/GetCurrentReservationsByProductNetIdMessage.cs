using System;

namespace GBA.Domain.Messages.Products.ProductReservations;

public sealed class GetCurrentReservationsByProductNetIdMessage {
    public GetCurrentReservationsByProductNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}