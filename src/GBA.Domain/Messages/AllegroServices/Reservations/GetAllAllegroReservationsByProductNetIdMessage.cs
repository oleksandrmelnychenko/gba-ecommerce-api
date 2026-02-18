using System;

namespace GBA.Domain.Messages.AllegroServices.Reservations;

public sealed class GetAllAllegroReservationsByProductNetIdMessage {
    public GetAllAllegroReservationsByProductNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}