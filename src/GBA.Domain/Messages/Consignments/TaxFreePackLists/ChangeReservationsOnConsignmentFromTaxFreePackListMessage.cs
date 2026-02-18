using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Consignments.TaxFreePackLists;

public sealed class ChangeReservationsOnConsignmentFromTaxFreePackListMessage {
    public ChangeReservationsOnConsignmentFromTaxFreePackListMessage(
        long taxFreePackListId,
        IEnumerable<long> newlyAddedItemIds,
        IEnumerable<SupplyOrderUkraineCartItem> updatedItems,
        IEnumerable<SupplyOrderUkraineCartItem> deletedItems,
        IEnumerable<TaxFree> taxFrees,
        Guid userNetId,
        object originalSender) {
        TaxFreePackListId = taxFreePackListId;

        NewlyAddedItemIds = newlyAddedItemIds;

        UpdatedItems = updatedItems;

        DeletedItems = deletedItems;

        TaxFrees = taxFrees;

        UserNetId = userNetId;

        OriginalSender = originalSender;
    }

    public long TaxFreePackListId { get; }

    public IEnumerable<long> NewlyAddedItemIds { get; }

    public IEnumerable<SupplyOrderUkraineCartItem> UpdatedItems { get; }

    public IEnumerable<SupplyOrderUkraineCartItem> DeletedItems { get; }

    public IEnumerable<TaxFree> TaxFrees { get; }

    public Guid UserNetId { get; }

    public object OriginalSender { get; }
}