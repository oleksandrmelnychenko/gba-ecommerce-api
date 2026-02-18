using System;

namespace GBA.Domain.Messages.Products.ProductReservations;

public sealed class GetReservationInfoByProductNetIdMessage {
    public GetReservationInfoByProductNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}