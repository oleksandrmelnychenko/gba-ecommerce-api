using System;

namespace GBA.Domain.Messages.Consignments.Infos;

public sealed class ExportConsignmentAvailabilityFilteredMessage {
    public ExportConsignmentAvailabilityFilteredMessage(
        string path,
        Guid storageNetId,
        DateTime from,
        DateTime to,
        string vendorCode) {
        Path = path;
        StorageNetId = storageNetId;
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
        VendorCode = vendorCode;
    }

    public string Path { get; }

    public Guid StorageNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public string VendorCode { get; }
}