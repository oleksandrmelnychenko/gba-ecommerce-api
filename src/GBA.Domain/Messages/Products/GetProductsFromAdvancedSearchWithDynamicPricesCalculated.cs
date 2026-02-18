using System;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Messages.Products;

public sealed class GetProductsFromAdvancedSearchWithDynamicPricesCalculated {
    public GetProductsFromAdvancedSearchWithDynamicPricesCalculated(
        string value,
        Guid netId,
        ProductAdvancedSearchMode mode,
        ProductAdvancedSortMode sortMode,
        long limit,
        long offset) {
        Value = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();

        NetId = netId;

        Mode = mode;

        SortMode = sortMode;

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public string Value { get; }

    public Guid NetId { get; }

    public ProductAdvancedSearchMode Mode { get; }

    public ProductAdvancedSortMode SortMode { get; }

    public long Limit { get; }

    public long Offset { get; }
}