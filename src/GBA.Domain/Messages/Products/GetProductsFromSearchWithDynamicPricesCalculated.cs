using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetProductsFromSearchWithDynamicPricesCalculated {
    public GetProductsFromSearchWithDynamicPricesCalculated(string value, Guid netId, long limit, long offset) {
        Value = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();

        NetId = netId;

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public string Value { get; set; }

    public Guid NetId { get; }

    public long Limit { get; set; }

    public long Offset { get; set; }
}