using System;

namespace GBA.Domain.Messages.Sales.Reservations;

public sealed class DeleteSaleFutureReservationByNetIdMessage {
    public DeleteSaleFutureReservationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}