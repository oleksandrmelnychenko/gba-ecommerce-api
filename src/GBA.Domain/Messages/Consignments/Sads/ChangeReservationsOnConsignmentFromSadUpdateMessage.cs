using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Consignments.Sads;

public class ChangeReservationsOnConsignmentFromSadUpdateMessage {
    public ChangeReservationsOnConsignmentFromSadUpdateMessage(
        long sadId,
        IEnumerable<long> createdItemIds,
        IEnumerable<SadItem> updatedItems,
        IEnumerable<SadItem> deletedItems,
        Guid userNetId,
        bool storeConsignmentMovement,
        object originalSender) {
        SadId = sadId;

        CreatedItemIds = createdItemIds;

        UpdatedItems = updatedItems;

        DeletedItems = deletedItems;

        UserNetId = userNetId;

        StoreConsignmentMovement = storeConsignmentMovement;

        OriginalSender = originalSender;
    }

    public long SadId { get; }

    public IEnumerable<long> CreatedItemIds { get; }

    public IEnumerable<SadItem> UpdatedItems { get; }

    public IEnumerable<SadItem> DeletedItems { get; }

    public Guid UserNetId { get; }

    public bool StoreConsignmentMovement { get; }

    public object OriginalSender { get; }
}