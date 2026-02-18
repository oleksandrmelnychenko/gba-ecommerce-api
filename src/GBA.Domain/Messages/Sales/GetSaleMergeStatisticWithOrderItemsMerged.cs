using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSaleMergeStatisticWithOrderItemsMerged {
    public GetSaleMergeStatisticWithOrderItemsMerged(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}