using System;

namespace GBA.Domain.Messages.Products.ProductPlacementMovements;

public sealed class GetAllProductPlacementStorageFilteredMessage {
    public GetAllProductPlacementStorageFilteredMessage(
        long[] storageIds,
        string value,
        DateTime to,
        long limit,
        long offset
    ) {
        StorageIds = storageIds;

        Value = string.IsNullOrEmpty(value) ? string.Empty : value;


        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public long[] StorageIds { get; }

    public string Value { get; }
    public DateTime To { get; }

    public long Limit { get; }

    public long Offset { get; }
}