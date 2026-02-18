using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class TotalConsignmentSpend {
    public byte[] Id { get; set; }

    public decimal TotalValue { get; set; }

    public string IdInString =>
        $"0x{BitConverter.ToString(Id).Replace("-", "")}";
}