using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncConsignmentSpend {
    public decimal Amount { get; set; }

    public decimal TotalSpend { get; set; }

    public long AgreementCode { get; set; }

    public string CurrencyCode { get; set; }

    public byte[] DocumentId { get; set; }

    public string DocumentIdInString =>
        $"0x{BitConverter.ToString(DocumentId).Replace("-", "")}";
}