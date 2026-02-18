using System;

namespace GBA.Domain.Messages.Consignments.Remainings;

public sealed class GetRemainingConsignmentsByStorageNetIdFilteredMessage {
    public GetRemainingConsignmentsByStorageNetIdFilteredMessage(
        Guid storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        string searchValue,
        int limit,
        int offset) {
        StorageNetId = storageNetId;

        SupplierNetId = supplierNetId;

        From = from.Date;

        To = to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        SearchValue = string.IsNullOrEmpty(searchValue) ? string.Empty : searchValue;

        Limit = limit <= 0 ? 40 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public Guid StorageNetId { get; }

    public Guid? SupplierNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public string SearchValue { get; }

    public int Limit { get; }

    public int Offset { get; }
}