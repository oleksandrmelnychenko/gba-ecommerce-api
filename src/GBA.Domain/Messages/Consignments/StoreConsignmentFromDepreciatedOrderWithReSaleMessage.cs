using System.Collections.Generic;

namespace GBA.Domain.Messages.Consignments;

public sealed class StoreConsignmentFromDepreciatedOrderWithReSaleMessage {
    public StoreConsignmentFromDepreciatedOrderWithReSaleMessage(
        long depreciatedOrderId,
        Dictionary<long, long> depreciatedOrderProductAvailabilityIds,
        bool withReSale) {
        DepreciatedOrderId = depreciatedOrderId;
        DepreciatedOrderProductAvailabilityIds = depreciatedOrderProductAvailabilityIds;
        WithReSale = withReSale;
    }

    public long DepreciatedOrderId { get; }
    public Dictionary<long, long> DepreciatedOrderProductAvailabilityIds { get; }
    public bool WithReSale { get; }
}