using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFreePackLists;

public sealed class FinishAddOrUpdateTaxFreePackListMessage {
    public FinishAddOrUpdateTaxFreePackListMessage(
        long taxFreePackListId,
        Guid userNetId,
        IEnumerable<TaxFree> taxFrees) {
        TaxFreePackListId = taxFreePackListId;

        UserNetId = userNetId;

        TaxFrees = taxFrees;
    }

    public long TaxFreePackListId { get; }

    public Guid UserNetId { get; }

    public IEnumerable<TaxFree> TaxFrees { get; }
}