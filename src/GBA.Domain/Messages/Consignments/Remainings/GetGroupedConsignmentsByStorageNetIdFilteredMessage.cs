using System;

namespace GBA.Domain.Messages.Consignments.Remainings;

public sealed class GetGroupedConsignmentsByStorageNetIdFilteredMessage {
    public GetGroupedConsignmentsByStorageNetIdFilteredMessage(
        Guid? storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        int limit,
        int offset) {
        StorageNetId = storageNetId;

        SupplierNetId = supplierNetId;

        From = from.Date;

        To = to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 40 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public Guid? StorageNetId { get; }

    public Guid? SupplierNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public int Limit { get; }

    public int Offset { get; }
}