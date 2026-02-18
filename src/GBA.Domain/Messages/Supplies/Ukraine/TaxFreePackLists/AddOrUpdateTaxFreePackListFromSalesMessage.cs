using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFreePackLists;

public sealed class AddOrUpdateTaxFreePackListFromSalesMessage {
    public AddOrUpdateTaxFreePackListFromSalesMessage(TaxFreePackList taxFreePackList, Guid userNetId) {
        TaxFreePackList = taxFreePackList;

        UserNetId = userNetId;
    }

    public TaxFreePackList TaxFreePackList { get; set; }

    public Guid UserNetId { get; }
}