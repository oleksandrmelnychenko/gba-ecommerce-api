using System;

namespace GBA.Domain.Messages.Consignments.TaxFreePackLists;

public sealed class RestoreReservationsOnConsignmentFromTaxFreePackListDeleteMessage {
    public RestoreReservationsOnConsignmentFromTaxFreePackListDeleteMessage(long taxFreePackListId, Guid userNetId) {
        TaxFreePackListId = taxFreePackListId;

        UserNetId = userNetId;
    }

    public long TaxFreePackListId { get; }

    public Guid UserNetId { get; }
}