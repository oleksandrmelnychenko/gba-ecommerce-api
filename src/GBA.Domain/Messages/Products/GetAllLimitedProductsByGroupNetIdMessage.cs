using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetAllLimitedProductsByGroupNetIdMessage {
    public GetAllLimitedProductsByGroupNetIdMessage(Guid netId, long limit, long offset) {
        NetId = netId;
        Limit = limit;
        Offset = offset;
    }

    public Guid NetId { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }
}