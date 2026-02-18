using System;

namespace GBA.Domain.Messages.Consignments.Remainings;

public sealed class GetGroupedConsignmentByStorageDocumentExportMessage {
    public GetGroupedConsignmentByStorageDocumentExportMessage(
        string pathToFolder,
        Guid? storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to) {
        PathToFolder = pathToFolder;

        StorageNetId = storageNetId;

        SupplierNetId = supplierNetId;

        From = from.Date;

        To = to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public string PathToFolder { get; }

    public Guid? StorageNetId { get; }

    public Guid? SupplierNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }
}