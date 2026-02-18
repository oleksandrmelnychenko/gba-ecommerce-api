using System;

namespace GBA.Domain.Messages.Storages;

public sealed class GetProductSupplyInfoByStorageFilteredMessage {
    public GetProductSupplyInfoByStorageFilteredMessage(
        Guid storageNetId,
        Guid? supplierNetId,
        long limit,
        long offset,
        string value,
        DateTime from,
        DateTime to
    ) {
        StorageNetId = storageNetId;

        SupplierNetId = supplierNetId;

        From = from.Year.Equals(1) ? DateTime.Now.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;

        Value = string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    public Guid StorageNetId { get; }

    public Guid? SupplierNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public long Limit { get; }

    public long Offset { get; }

    public string Value { get; }
}