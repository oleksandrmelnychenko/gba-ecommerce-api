using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncConsignmentDocumentInfo {
    public byte[] DocumentId { get; set; }

    public string DocumentIdInString =>
        $"0x{BitConverter.ToString(DocumentId).Replace("-", "")}";

    public long ClientCode { get; set; }

    public string StorageName { get; set; }

    public string DocumentArrivalNumber { get; set; }

    public DateTime? DocumentArrivalDate { get; set; }
}