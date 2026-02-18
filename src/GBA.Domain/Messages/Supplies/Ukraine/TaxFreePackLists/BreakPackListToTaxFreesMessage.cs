using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFreePackLists;

public sealed class BreakPackListToTaxFreesMessage {
    public BreakPackListToTaxFreesMessage(
        TaxFreePackList taxFreePackList,
        Guid userNetId) {
        TaxFreePackList = taxFreePackList;

        UserNetId = userNetId;
    }

    public TaxFreePackList TaxFreePackList { get; }

    public Guid UserNetId { get; }
}