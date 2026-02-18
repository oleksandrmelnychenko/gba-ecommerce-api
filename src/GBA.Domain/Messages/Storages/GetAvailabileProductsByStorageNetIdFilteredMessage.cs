using System;

namespace GBA.Domain.Messages.Storages;

public sealed class GetAvailabileProductsByStorageNetIdFilteredMessage {
    public GetAvailabileProductsByStorageNetIdFilteredMessage(
        Guid storageNetId,
        long limit,
        long offset,
        string value
    ) {
        StorageNetId = storageNetId;

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;

        Value = string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    public Guid StorageNetId { get; }

    public long Limit { get; }

    public long Offset { get; }

    public string Value { get; }
}