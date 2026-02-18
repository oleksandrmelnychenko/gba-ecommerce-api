using System;

namespace GBA.Domain.Messages.Consignments.Remainings;

public sealed class GetRemainingProductsByStorageDocumentExportMessage {
    public GetRemainingProductsByStorageDocumentExportMessage(
        string pathToFolder,
        Guid storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        string searchValue) {
        StorageNetId = storageNetId;
        SupplierNetId = supplierNetId;
        From = from.Date;
        To = to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
        SearchValue = string.IsNullOrEmpty(searchValue) ? string.Empty : searchValue;
        PathToFolder = pathToFolder;
    }

    public string PathToFolder { get; }

    public Guid StorageNetId { get; }

    public Guid? SupplierNetId { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public string SearchValue { get; }
}