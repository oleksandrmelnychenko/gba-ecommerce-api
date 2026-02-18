using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncProductTransferItem {
    public byte[] DocumentId { get; set; }

    public string DocumentIdInString =>
        $"0x{BitConverter.ToString(DocumentId).Replace("-", "")}";

    public string Number { get; set; }

    public DateTime DocumentDate { get; set; }

    public string Comment { get; set; }

    public bool ManagementAccounting { get; set; }

    public bool Accounting { get; set; }

    public string OrganizationName { get; set; }

    public string StorageFrom { get; set; }

    public string StorageTo { get; set; }

    public long Qty { get; set; }

    public long SourceProductCode { get; set; }
}