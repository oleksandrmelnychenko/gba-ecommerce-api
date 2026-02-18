using System;

namespace GBA.Domain.Messages.Consignments.Sads;

public sealed class RestoreReservationsOnConsignmentFromSadDeleteMessage {
    public RestoreReservationsOnConsignmentFromSadDeleteMessage(long sadId, Guid userNetId) {
        SadId = sadId;

        UserNetId = userNetId;
    }

    public long SadId { get; }

    public Guid UserNetId { get; }
}