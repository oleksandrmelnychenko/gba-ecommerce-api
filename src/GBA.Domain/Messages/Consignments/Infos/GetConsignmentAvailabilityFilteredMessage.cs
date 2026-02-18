using System;

namespace GBA.Domain.Messages.Consignments.Infos;

public sealed class GetConsignmentAvailabilityFilteredMessage {
    public GetConsignmentAvailabilityFilteredMessage(
        Guid storageNetId,
        DateTime from,
        DateTime to,
        string vendorCode,
        int limit,
        int offset) {
        StorageNetId = storageNetId;
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
        VendorCode = vendorCode;
        Limit = limit == 0 ? 20 : limit;
        Offset = offset;
    }

    public Guid StorageNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public string VendorCode { get; }

    public int Limit { get; }

    public int Offset { get; }
}